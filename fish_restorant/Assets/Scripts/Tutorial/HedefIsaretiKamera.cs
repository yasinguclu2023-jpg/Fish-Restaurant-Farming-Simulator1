using UnityEngine;
using UnityEngine.Rendering.Universal;

// ===== TUTORIAL (kaldirilabilir) =====

/// <summary>
/// HEDEF IKONUNU DUVARLARIN ARKASINDAN GORUNUR YAPAN KAMERA KURULUMU.
///
/// SHADER YOK. Yontem, projede zaten kullanilan yontemin aynisi (EldeNesneKamera):
///   1. Ana kamera "HedefIsareti" layer'ini GORMEZ (culling mask'ten cikarilir).
///   2. Ana kameraya bagli bir OVERLAY kamera olusturulur, SADECE o layer'i gorur.
///   3. URP overlay kamerasi derinligi temizleyip dunyadan SONRA cizer
///      -> ikon her zaman en ustte kalir, duvar onu kapatamaz.
///
/// Overlay kameranin FOV / near / far degerleri ana kameradan KOPYALANIR;
/// yoksa ikon yanlis boyutta ya da yanlis yerde gorunur.
///
/// KURULUM:
/// 1. Bu scripti ANA KAMERAYA ekle (MainCamera tag'li olan).
/// 2. "Layer Adi" alani "HedefIsareti" olsun (o layer'i once olusturmus olmalisin).
/// 3. Baska bir sey yapmana gerek yok, gerisini kendisi kurar.
/// </summary>
public class HedefIsaretiKamera : MonoBehaviour
{
    [Header("Kamera")]
    [Tooltip("Bos birakirsan once bu objedeki Camera, o da yoksa Camera.main kullanilir.")]
    [SerializeField] private Camera anaKamera;

    [Header("Layer")]
    [Tooltip("Hedef ikonunun bulundugu layer'in ADI. Bu layer'i Unity'de olusturmus olmalisin.")]
    [SerializeField] private string layerAdi = "HedefIsareti";

    [Header("Senkron")]
    [Tooltip("ACIK: Ana kameranin FOV'u oyun sirasinda degisirse overlay kamera da uyar. " +
             "(Nisan alma / kosma zoom'u varsa acik birak.)")]
    [SerializeField] private bool fovuSurekliEsitle = true;

    [Header("Teshis")]
    [SerializeField] private bool teshisLogu = true;

    private Camera isaretKamerasi;
    private int isaretLayer = -1;
    private float sonFov = -1f;

    /// <summary>Ikon layer'inin numarasi (-1 = layer bulunamadi).</summary>
    public int IsaretLayer => isaretLayer;

    /// <summary>
    /// Oyuncu kamerasini bulur.
    ///   1) Camera.main ("MainCamera" tag'li kamera)
    ///   2) Tag yoksa: sahnedeki AKTIF, ekrana cizen, URP'de "Base" tipli ilk kamera.
    /// Bu projede kameralarda MainCamera tag'i olmayabildigi icin 2. yol sart
    /// (IkonYoneticisi de ayni yedegi kullaniyor).
    /// </summary>
    public static Camera AnaKamerayiBul()
    {
        if (Camera.main != null) return Camera.main;

        Camera[] hepsi = FindObjectsOfType<Camera>();

        for (int i = 0; i < hepsi.Length; i++)
        {
            Camera c = hepsi[i];
            if (c == null || !c.isActiveAndEnabled) continue;

            // Render texture'a cizenler (el kamerasi, ayna vb.) oyuncu kamerasi degildir
            if (c.targetTexture != null) continue;

            // Overlay kameralar da degil; bize STACK'IN SAHIBI olan Base kamera lazim
            var veri = c.GetUniversalAdditionalCameraData();
            if (veri != null && veri.renderType != CameraRenderType.Base) continue;

            return c;
        }

        return null;
    }

    void Awake()
    {
        isaretLayer = LayerMask.NameToLayer(layerAdi);

        if (isaretLayer == -1)
        {
            Debug.LogError($"[HedefIsaretiKamera] '{layerAdi}' layer'i BULUNAMADI!\n" +
                           "Once Unity'de bu layer'i olustur: herhangi bir objeyi sec -> " +
                           "Inspector sag ust 'Layer' menusu -> Add Layer... -> bos bir satira " +
                           $"'{layerAdi}' yaz.", this);
            enabled = false;
            return;
        }

        if (anaKamera == null) anaKamera = GetComponent<Camera>();
        if (anaKamera == null) anaKamera = AnaKamerayiBul();

        if (anaKamera == null)
        {
            Debug.LogError("[HedefIsaretiKamera] Ana kamera bulunamadi! Bu scripti kameraya ekle " +
                           "ya da 'Ana Kamera' alanini doldur.", this);
            enabled = false;
            return;
        }

        IsaretKamerasiniOlustur();
        KameralariAyarla();

        if (teshisLogu) DurumuYaz();
    }

    /// <summary>
    /// Ne kuruldu, ne kurulamadi hepsini Console'a yazar.
    /// Ikon duvarin arkasindan gorunmuyorsa ONCE BUNA BAK.
    /// (Play modunda component'e sag tik -> "Teshis: Durumu Yaz")
    /// </summary>
    [ContextMenu("Teshis: Durumu Yaz")]
    public void DurumuYaz()
    {
        var sb = new System.Text.StringBuilder(512);
        sb.AppendLine("=== [HedefIsaretiKamera] TESHIS ===");
        sb.AppendLine($"Layer adi   : '{layerAdi}'  -> numara: {isaretLayer}" +
                      (isaretLayer == -1 ? "   <<< LAYER YOK! Once Unity'de olustur." : ""));

        if (anaKamera == null)
        {
            sb.AppendLine("Ana kamera  : YOK  <<< scripti kameraya ekledin mi?");
            Debug.LogError(sb.ToString(), this);
            return;
        }

        bool anaGoruyor = isaretLayer >= 0 && (anaKamera.cullingMask & (1 << isaretLayer)) != 0;

        sb.AppendLine($"Ana kamera  : '{anaKamera.name}'  (tag: {anaKamera.tag})");
        sb.AppendLine($"  bu layer'i goruyor mu : {anaGoruyor}" +
                      (anaGoruyor ? "   <<< SORUN! Gormemeli." : "   (dogru)"));

        var anaVeri = anaKamera.GetUniversalAdditionalCameraData();
        sb.AppendLine($"  renderType            : {anaVeri.renderType}" +
                      (anaVeri.renderType != CameraRenderType.Base
                          ? "   <<< SORUN! Base olmali, yoksa stack calismaz." : "   (dogru)"));
        sb.AppendLine($"  stack'teki kamera     : {anaVeri.cameraStack.Count} adet");

        if (isaretKamerasi == null)
        {
            sb.AppendLine("Isaret kamerasi: YOK  <<< olusturulamadi");
            Debug.LogError(sb.ToString(), this);
            return;
        }

        var veri = isaretKamerasi.GetUniversalAdditionalCameraData();
        bool stackte = anaVeri.cameraStack.Contains(isaretKamerasi);

        sb.AppendLine($"Isaret kamerasi: '{isaretKamerasi.name}'");
        sb.AppendLine($"  renderType   : {veri.renderType}" +
                      (veri.renderType != CameraRenderType.Overlay ? "   <<< SORUN!" : "   (dogru)"));
        sb.AppendLine($"  clearDepth   : {veri.clearDepth}" +
                      (!veri.clearDepth ? "   <<< SORUN! Duvar arkasi bunu ister." : "   (dogru)"));
        sb.AppendLine($"  stack'te mi  : {stackte}" +
                      (!stackte ? "   <<< SORUN! Ana kameranin stack'inde degil." : "   (dogru)"));
        sb.AppendLine($"  FOV / far    : {isaretKamerasi.fieldOfView} / {isaretKamerasi.farClipPlane}");

        // Sahnedeki TUM kameralari listele: script yanlis kameraya eklenmis olabilir.
        Camera[] hepsi = FindObjectsOfType<Camera>();
        sb.AppendLine($"--- Sahnedeki kameralar ({hepsi.Length}) ---");
        for (int i = 0; i < hepsi.Length; i++)
        {
            Camera c = hepsi[i];
            if (c == null) continue;

            var cv = c.GetUniversalAdditionalCameraData();
            bool goruyor = isaretLayer >= 0 && (c.cullingMask & (1 << isaretLayer)) != 0;

            sb.AppendLine($"  '{c.name}'  tag:{c.tag}  tip:{cv.renderType}  " +
                          $"aktif:{c.isActiveAndEnabled}  bu layer'i goruyor:{goruyor}" +
                          (goruyor && c != isaretKamerasi ? "   <<< BU KAMERA IKONU NORMAL CIZIYOR!" : ""));
        }

        Debug.Log(sb.ToString(), this);
    }

    void OnDestroy()
    {
        // Kamera yok olurken stack'ten duzgunce cikar (sahne degisiminde hata birakmasin).
        if (anaKamera == null || isaretKamerasi == null) return;

        var anaVeri = anaKamera.GetUniversalAdditionalCameraData();
        if (anaVeri != null && anaVeri.cameraStack.Contains(isaretKamerasi))
            anaVeri.cameraStack.Remove(isaretKamerasi);
    }

    void IsaretKamerasiniOlustur()
    {
        GameObject kamObj = new GameObject("HedefIsaretiKamerasi");
        kamObj.transform.SetParent(anaKamera.transform, false);
        kamObj.transform.localPosition = Vector3.zero;
        kamObj.transform.localRotation = Quaternion.identity;

        isaretKamerasi = kamObj.AddComponent<Camera>();

        // URP: overlay kamera olarak isaretle
        var veri = isaretKamerasi.GetUniversalAdditionalCameraData();
        veri.renderType = CameraRenderType.Overlay;

        // NOT: Duvar arkasindan gorunmeyi saglayan sey overlay kameranin DERINLIGI
        // TEMIZLEMESIDIR. URP 14'te "clearDepth" salt-okunurdur (koddan set edilemez)
        // ama overlay kameralarda VARSAYILANI ZATEN TRUE'dur, yani istedigimiz davranis hazir gelir.
        // Yanlislikla kapanmis olursa asagidaki teshis raporu bunu "SORUN!" diye bildirir.

        // Overlay kamera post-process ile ugrasmasin (ikon net kalsin, bosuna maliyet olmasin)
        veri.renderPostProcessing = false;

        // Ana kameranin stack'ine ekle
        var anaVeri = anaKamera.GetUniversalAdditionalCameraData();
        anaVeri.cameraStack.Add(isaretKamerasi);
    }

    void KameralariAyarla()
    {
        // Ana kamera bu layer'i GORMESIN (bit temizlenir; diger layer'lara dokunulmaz,
        // bu yuzden EldeNesneKamera ile cakismaz).
        anaKamera.cullingMask &= ~(1 << isaretLayer);

        // Isaret kamerasi SADECE bu layer'i gorsun
        isaretKamerasi.cullingMask = 1 << isaretLayer;

        // ONEMLI: goruntu birebir ortussun diye ana kameranin optigi kopyalanir.
        isaretKamerasi.fieldOfView = anaKamera.fieldOfView;
        isaretKamerasi.nearClipPlane = anaKamera.nearClipPlane;
        isaretKamerasi.farClipPlane = anaKamera.farClipPlane;
        isaretKamerasi.orthographic = anaKamera.orthographic;
        isaretKamerasi.orthographicSize = anaKamera.orthographicSize;

        sonFov = anaKamera.fieldOfView;
    }

    void LateUpdate()
    {
        if (!fovuSurekliEsitle || isaretKamerasi == null || anaKamera == null) return;

        // Sadece DEGISINCE yaz (her frame bosuna atama yapma)
        if (Mathf.Approximately(sonFov, anaKamera.fieldOfView)) return;

        sonFov = anaKamera.fieldOfView;
        isaretKamerasi.fieldOfView = sonFov;
    }
}
// ===== TUTORIAL SONU =====
