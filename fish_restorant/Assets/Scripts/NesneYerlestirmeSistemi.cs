using UnityEngine;
using OutlineFx;

public class NesneYerlestirmeSistemi : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Camera oyuncuKamerasi;

    [Header("Yerleştirme Ayarları")]
    [SerializeField] private float maxMesafe = 5f;
    [SerializeField] private KeyCode dondurTusu = KeyCode.R;
    [SerializeField] private float donmeAcisi = 45f;
    [SerializeField] private float tikGecikme = 0.3f;

    [Tooltip("Kasada 'Tus Ile Yerlestirme' acikken preview'i acip kapatan tus.")]
    [SerializeField] private KeyCode yerlesimAcKapaTusu = KeyCode.X;

    [Header("Preview Ayarları")]
    [SerializeField] private Color gecerliRenk = new Color(0f, 1f, 0f, 0.4f);
    [SerializeField] private Color gecersizRenk = new Color(1f, 0f, 0f, 0.4f);

    [Header("Çarpışma Ayarları")]
    [SerializeField] private float carpismaToleransi = 0.05f;

    [Header("Yerleştirilen Nesne Layer")]
    [SerializeField] private string yerlestirilenNesneLayerAdi = "YerlestirilenNesne";

    [Header("Satın Alma")]
    [Tooltip("Yerleştirme anında para yetmezse çalacak uyarı sesi (2D, opsiyonel).")]
    [SerializeField] private SesVerisi yetersizParaSesi;

    private GameObject eldeNesne;
    private GameObject previewNesne;
    private Transform elPozisyonu;
    private YerlestirilebilirNesne yerlestirilebilirData;
    private Material previewMateryal;
    private Renderer[] previewRendererlar;
    private Vector3 orijinalScale;
    private float mevcutRotasyon;
    private float sonTikZamani;
    private bool yerlesimModu;
    private bool yerlesimGecerli;
    private bool oncekiGecerliDurum;
    private int orijinalLayer;
    private int yerlestirilenNesneLayer;

    // Tus ile yerlesim: kasada bayrak aciksa preview surekli gozukmez, X ile acilip kapanir
    private bool tusIleYerlesimGerekli;
    private bool previewGoster = true;

    private bool prefabModuAktif;
    private GameObject bekleyenPrefab;
    private int bekleyenFiyat; // Prefab modunda satın alma bedeli. Ödeme YERE KONUNCA yapılır.
    private NesneSesVerisi eldeNesneSesVerisi;
    private bool oltaModuAktif;

    // Ekmek sistemi spawn preview
    private GameObject spawnPreviewNesne;
    private bool spawnPreviewAktif;

    private struct BekleyenEldeNesne
    {
        public GameObject nesne;
        public Vector3 scale;
        public Transform elPoz;
        public int layer;
        public bool aktif;
        public void Temizle() { nesne = null; elPoz = null; scale = Vector3.zero; layer = 0; aktif = false; }
    }
    private BekleyenEldeNesne bekleyenEldeNesne;

    private RaycastSistemi raycastSistemi;
    private EldeNesneKamera eldeNesneKamera;
    private LayerMask engelLayerlari;
    private LayerMask carpismakontrolLayer;

    private Ray ray;
    private RaycastHit hit;
    private static readonly Vector3 ViewportCenter = new Vector3(0.5f, 0.5f, 0f);
    private Collider[] overlapSonuclari = new Collider[10];
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    void Start()
    {
        if (oyuncuKamerasi == null) oyuncuKamerasi = Camera.main;
        raycastSistemi = FindObjectOfType<RaycastSistemi>();
        eldeNesneKamera = FindObjectOfType<EldeNesneKamera>();
        if (raycastSistemi != null) engelLayerlari = raycastSistemi.EngelLayer;
        yerlestirilenNesneLayer = LayerMask.NameToLayer(yerlestirilenNesneLayerAdi);
        if (yerlestirilenNesneLayer == -1) { Debug.LogWarning($"'{yerlestirilenNesneLayerAdi}' layerı bulunamadı!"); yerlestirilenNesneLayer = 0; }
        carpismakontrolLayer = engelLayerlari | (1 << yerlestirilenNesneLayer);
        PreviewMateryalOlustur();
    }

    void PreviewMateryalOlustur()
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) { Debug.LogError("URP Lit shader bulunamadı!"); return; }
        previewMateryal = new Material(urpShader);
        previewMateryal.SetFloat("_Surface", 1);
        previewMateryal.SetFloat("_Blend", 0);
        previewMateryal.SetFloat("_AlphaClip", 0);
        previewMateryal.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        previewMateryal.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        previewMateryal.SetFloat("_ZWrite", 0);
        previewMateryal.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        previewMateryal.renderQueue = 3000;
        previewMateryal.SetColor(BaseColorID, gecerliRenk);
    }

    void Update()
    {
        if (!yerlesimModu) return;

        // Tus ile yerlesim modu: preview'i ac/kapa. Yerlesim modu kapanmaz; sol tik ile
        // kasaya doldurma / makineye koyma / cope atma akislari calismaya devam eder.
        if (tusIleYerlesimGerekli && Input.GetKeyDown(yerlesimAcKapaTusu))
        {
            previewGoster = !previewGoster;
            if (!previewGoster)
            {
                SpawnPreviewTemizle();
                if (previewNesne != null) PreviewGizle();
            }
        }

        PreviewGuncelle();
        if (Input.GetKeyDown(dondurTusu)) { mevcutRotasyon += donmeAcisi; if (mevcutRotasyon >= 360f) mevcutRotasyon = 0f; }
        if (Time.time - sonTikZamani < tikGecikme) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (EldekiKasayaUrunDoldurDene()) return; // <-- YENİ: Elde kasa varken bakılan ürünü kasaya doldur
            if (CopKovasinaBirakmaDene()) return;
            if (OltaStandiBirakmaDene()) return;
            if (MakineyeBirakmaDene()) return;      // <-- YENİ: Makine entegrasyonu
            if (KizartmaMakinesineBirakmaDene()) return; // <-- YENİ: Kalamar kızartma
            if (KasayaBirakmaDene()) return;
            if (KuveteBirakmaDene()) return;
            if (KesmeTahtasinaBirakmaDene()) return;
            if (EkmekSistemineBirakmaDene()) return;
            if (TepsiYigininaGeriKoyDene()) return;
            if (TepsiyeBirakmaDene()) return;
            if (PisirmeyeBirakmaDene()) return;
            if (yerlesimGecerli) NesneyiYerlestir();
        }
        else if (Input.GetMouseButtonDown(1) && !oltaModuAktif)
        {
            // Sadece prefab yerleştirme modu sağ tık ile iptal edilir (bekleyen elde nesneye döner).
            // Elde gerçek bir nesne tutuluyorsa sağ tık onu BIRAKMAZ; nesne elde kalır,
            // yerleştirme modu devam eder ve yalnızca sol tıkla geçerli bir yüzeye bırakılabilir.
            if (prefabModuAktif)
                YerlesimIptal();
        }
    }

    void PreviewGuncelle()
    {
        if (previewNesne == null) return;

        // Tus ile yerlesim acik ve oyuncu henuz X'e basmadiysa hicbir preview gosterme.
        if (!previewGoster)
        {
            SpawnPreviewTemizle();
            PreviewGizle();
            return;
        }

        // Elde doldurulabilir kasa ile kabul edilen bir ürüne (ör. balık) bakılıyorsa,
        // kasayı yerleştirme değil doldurma yapılacağı için preview gösterme.
        if (EldeKasayaDoldurmaHedefiVarMi(out _, out _, out _))
        {
            SpawnPreviewTemizle();
            PreviewGizle();
            return;
        }

        YerlestirilebilirNesne aktifData = prefabModuAktif
            ? previewNesne.GetComponent<YerlestirilebilirNesne>()
            : yerlestirilebilirData;
        if (aktifData == null) return;

        ray = oyuncuKamerasi.ViewportPointToRay(ViewportCenter);

        if (!prefabModuAktif && eldeNesne != null)
        {
            RaycastHit onKontrolHit;
            if (Physics.Raycast(ray, out onKontrolHit, maxMesafe))
            {
                GameObject vurulan = onKontrolHit.collider.gameObject;

                // ===== ÜRETİM MAKİNESİ PREVIEW ===== (YENİ)
                UretimMakinesiSistemi bulunanMakine = MakineBul(vurulan);
                if (bulunanMakine != null)
                {
                    SpawnPreviewTemizle();
                    if (bulunanMakine.KasaKoyulabilirMi && bulunanMakine.KasaKabulEdilirMi(eldeNesne))
                    {
                        PreviewGosterSlot(bulunanMakine.KasaKoymaNoktasi);
                    }
                    else { PreviewGizle(); }
                    return;
                }
                // =====================================

                // Kesme tahtası
                KesmeTahtasiSistemi bulunanTahta = TahtaBul(vurulan);
                if (bulunanTahta != null)
                {
                    SpawnPreviewTemizle();
                    Transform bosSlot = bulunanTahta.IlkBosSlot;
                    if (bosSlot != null && bulunanTahta.NesneKabulEdilirMi(eldeNesne))
                    {
                        PreviewGosterSlot(bosSlot);
                    }
                    else { PreviewGizle(); }
                    return;
                }

                // Ekmek sistemi
                EkmekYerlestirmeSistemi bulunanEkmek = EkmekSistemiBul(vurulan);
                if (bulunanEkmek != null)
                {
                    Transform previewNokta = bulunanEkmek.PreviewNoktasiBul(eldeNesne);
                    if (previewNokta != null && bulunanEkmek.NesneKabulEdilirMi(eldeNesne))
                    {
                        GameObject spawnPrefab = bulunanEkmek.SpawnPrefabBul(eldeNesne);
                        if (spawnPrefab != null)
                        {
                            if (previewNesne.activeSelf) previewNesne.SetActive(false);
                            if (spawnPreviewNesne == null || !spawnPreviewAktif) SpawnPreviewOlustur(spawnPrefab);
                            if (spawnPreviewNesne != null)
                            {
                                spawnPreviewNesne.SetActive(true);
                                SetLayerRecursive(spawnPreviewNesne, 0);
                                spawnPreviewNesne.transform.SetParent(null);
                                spawnPreviewNesne.transform.position = previewNokta.position;
                                Quaternion spawnRot;
                                if (bulunanEkmek.SpawnRotasyonBilgisiAl(eldeNesne, out spawnRot))
                                    spawnPreviewNesne.transform.rotation = previewNokta.rotation * spawnRot;
                                else
                                    spawnPreviewNesne.transform.rotation = previewNokta.rotation;
                            }
                        }
                        else
                        {
                            SpawnPreviewTemizle();
                            PreviewGosterSlot(previewNokta);
                        }
                        yerlesimGecerli = true;
                        if (!oncekiGecerliDurum) { previewMateryal.SetColor(BaseColorID, gecerliRenk); oncekiGecerliDurum = true; }
                    }
                    else { SpawnPreviewTemizle(); PreviewGizle(); }
                    return;
                }

                SpawnPreviewTemizle();

                // Tepsi (özgür pozisyon)
                TepsiSistemi bulunanTepsi = TepsiBul(vurulan);
                if (bulunanTepsi != null)
                {
                    if (bulunanTepsi.NesneKabulEdilirMi(eldeNesne))
                    { PreviewGosterOzgur(onKontrolHit.point); }
                    else { PreviewGizle(); }
                    return;
                }

                // Pişirme yüzeyi (özgür pozisyon)
                PisirmeSistemi bulunanPisirme = PisirmeBul(vurulan);
                if (bulunanPisirme != null)
                {
                    if (bulunanPisirme.NesneKabulEdilirMi(eldeNesne)
                        && (eldeNesne.GetComponent<PisirilebilirNesne>() != null
                            || eldeNesne.GetComponentInParent<PisirilebilirNesne>() != null
                            || eldeNesne.GetComponentInChildren<PisirilebilirNesne>() != null))
                    { PreviewGosterOzgur(onKontrolHit.point); }
                    else { PreviewGizle(); }
                    return;
                }

                // Kasa/Küvet - gizle
                if (KasaBul(vurulan) != null || KuvetBul(vurulan) != null)
                { PreviewGizle(); return; }
            }
        }

        // Normal preview
        LayerMask raycastLayer = aktifData.yerlesebilecegiLayerlar | engelLayerlari;
        if (Physics.Raycast(ray, out hit, maxMesafe, raycastLayer))
        {
            int vurulanLayer = hit.collider.gameObject.layer;
            bool engelMi = ((1 << vurulanLayer) & engelLayerlari) != 0;
            bool yerlesimYuzeyiMi = ((1 << vurulanLayer) & aktifData.yerlesebilecegiLayerlar) != 0;

            if (engelMi) { PreviewGizle(); return; }

            if (yerlesimYuzeyiMi)
            {
                if (!previewNesne.activeSelf) previewNesne.SetActive(true);
                Vector3 pozisyon = hit.point + aktifData.yereOfset;
                previewNesne.transform.SetPositionAndRotation(pozisyon, Quaternion.Euler(0f, mevcutRotasyon, 0f));
                yerlesimGecerli = !ColliderCarpisiyorMu();
                if (yerlesimGecerli != oncekiGecerliDurum)
                { previewMateryal.SetColor(BaseColorID, yerlesimGecerli ? gecerliRenk : gecersizRenk); oncekiGecerliDurum = yerlesimGecerli; }
            }
        }
        else { PreviewGizle(); }
    }

    // === PREVIEW YARDIMCILARI ===

    void PreviewGosterSlot(Transform slot)
    {
        if (!previewNesne.activeSelf) previewNesne.SetActive(true);
        SetLayerRecursive(previewNesne, 0);
        previewNesne.transform.SetParent(null);
        previewNesne.transform.position = slot.position;
        previewNesne.transform.rotation = slot.rotation;
        previewNesne.transform.localScale = orijinalScale;
        yerlesimGecerli = true;
        if (!oncekiGecerliDurum) { previewMateryal.SetColor(BaseColorID, gecerliRenk); oncekiGecerliDurum = true; }
    }

    void PreviewGosterOzgur(Vector3 nokta)
    {
        if (!previewNesne.activeSelf) previewNesne.SetActive(true);
        SetLayerRecursive(previewNesne, 0);
        previewNesne.transform.SetParent(null);
        previewNesne.transform.position = nokta;
        previewNesne.transform.rotation = Quaternion.Euler(0f, mevcutRotasyon, 0f);
        previewNesne.transform.localScale = orijinalScale;
        yerlesimGecerli = true;
        if (!oncekiGecerliDurum) { previewMateryal.SetColor(BaseColorID, gecerliRenk); oncekiGecerliDurum = true; }
    }

    void PreviewGizle()
    {
        if (previewNesne.activeSelf) previewNesne.SetActive(false);
        yerlesimGecerli = false;
    }

    // === SİSTEM BULMA ===

    KesmeTahtasiSistemi TahtaBul(GameObject o) { var t = o.GetComponent<KesmeTahtasiSistemi>(); if (t) return t; t = o.GetComponentInParent<KesmeTahtasiSistemi>(); if (t) return t; return o.GetComponentInChildren<KesmeTahtasiSistemi>(); }
    KuvetSistemi KuvetBul(GameObject o) { var t = o.GetComponent<KuvetSistemi>(); if (t) return t; t = o.GetComponentInParent<KuvetSistemi>(); if (t) return t; return o.GetComponentInChildren<KuvetSistemi>(); }
    KasaSistemi KasaBul(GameObject o) { var t = o.GetComponent<KasaSistemi>(); if (t) return t; t = o.GetComponentInParent<KasaSistemi>(); if (t) return t; return o.GetComponentInChildren<KasaSistemi>(); }
    PisirmeSistemi PisirmeBul(GameObject o) { var t = o.GetComponent<PisirmeSistemi>(); if (t) return t; t = o.GetComponentInParent<PisirmeSistemi>(); if (t) return t; return o.GetComponentInChildren<PisirmeSistemi>(); }
    EkmekYerlestirmeSistemi EkmekSistemiBul(GameObject o) { var t = o.GetComponent<EkmekYerlestirmeSistemi>(); if (t) return t; t = o.GetComponentInParent<EkmekYerlestirmeSistemi>(); if (t) return t; return o.GetComponentInChildren<EkmekYerlestirmeSistemi>(); }
    TepsiSistemi TepsiBul(GameObject o) { var t = o.GetComponent<TepsiSistemi>(); if (t) return t; t = o.GetComponentInParent<TepsiSistemi>(); if (t) return t; return o.GetComponentInChildren<TepsiSistemi>(); }
    TepsiYiginiSistemi TepsiYiginiBul(GameObject o) { var t = o.GetComponent<TepsiYiginiSistemi>(); if (t) return t; t = o.GetComponentInParent<TepsiYiginiSistemi>(); if (t) return t; return o.GetComponentInChildren<TepsiYiginiSistemi>(); }

    // YENİ: Makine bulma
    UretimMakinesiSistemi MakineBul(GameObject o) { var t = o.GetComponent<UretimMakinesiSistemi>(); if (t) return t; t = o.GetComponentInParent<UretimMakinesiSistemi>(); if (t) return t; return o.GetComponentInChildren<UretimMakinesiSistemi>(); }

    // YENİ: Elde tutulan nesne, preview'i X tusuna baglayan bir kasa mi?
    bool TusIleYerlesimMi(GameObject nesne)
    {
        if (nesne == null) return false;

        KasaSistemi kasa = nesne.GetComponent<KasaSistemi>();
        if (kasa == null) kasa = nesne.GetComponentInChildren<KasaSistemi>();
        if (kasa != null) return kasa.TusIleYerlestirme;

        BalikKasaSistemi balikKasa = nesne.GetComponent<BalikKasaSistemi>();
        if (balikKasa == null) balikKasa = nesne.GetComponentInChildren<BalikKasaSistemi>();
        return balikKasa != null && balikKasa.TusIleYerlestirme;
    }

    // YENİ: Kalamar kızartma bulma
    KalamarKizartma KizartmaBul(GameObject o) { var t = o.GetComponent<KalamarKizartma>(); if (t) return t; t = o.GetComponentInParent<KalamarKizartma>(); if (t) return t; return o.GetComponentInChildren<KalamarKizartma>(); }

    // === ÇARPIŞMA ===

    bool ColliderCarpisiyorMu()
    {
        if (previewRendererlar == null || previewRendererlar.Length == 0) return false;
        Bounds bounds = previewRendererlar[0].bounds;
        for (int i = 1; i < previewRendererlar.Length; i++) bounds.Encapsulate(previewRendererlar[i].bounds);
        Vector3 halfExtents = bounds.extents * (1f - carpismaToleransi);
        return Physics.OverlapBoxNonAlloc(bounds.center, halfExtents, overlapSonuclari, previewNesne.transform.rotation, carpismakontrolLayer) > 0;
    }

    // === BIRAKMA ===

    // Elde doldurulabilir bir kasa var VE bakılan obje bu kasanın kabul ettiği bir ürün mü?
    // Öyleyse hedef ürünü ve kasayı döndürür (true). Hem preview gizleme hem doldurma bunu paylaşır.
    bool EldeKasayaDoldurmaHedefiVarMi(out GameObject urun, out KasaSistemi kasa, out BalikKasaSistemi balikKasa)
    {
        urun = null; kasa = null; balikKasa = null;
        if (prefabModuAktif || eldeNesne == null) return false;

        // Elde tutulan bir kasa mı? (sebze kasası veya balık kasası)
        kasa = eldeNesne.GetComponent<KasaSistemi>();
        if (kasa == null) kasa = eldeNesne.GetComponentInChildren<KasaSistemi>();

        if (kasa == null)
        {
            balikKasa = eldeNesne.GetComponent<BalikKasaSistemi>();
            if (balikKasa == null) balikKasa = eldeNesne.GetComponentInChildren<BalikKasaSistemi>();
        }

        bool doldurmaAktif = (kasa != null && kasa.EldeDoldurmaAktif)
                          || (balikKasa != null && balikKasa.EldeDoldurmaAktif);
        if (!doldurmaAktif) return false;

        // Bakılan objeyi RaycastSistemi'nden al: eldeki kasa "el" layer'ında olduğu için
        // kendini seçmez, ayrıca outline/menzil ayarları burada tutarlı olur.
        if (raycastSistemi == null || !raycastSistemi.ObjeyeBakiyorMu) return false;
        GameObject bakilan = raycastSistemi.BakilanObje;
        if (bakilan == null) return false;

        // Kasanın kabul ettiği tag'e sahip ürünü çöz (mesh child'a tıklansa bile root'u bul).
        urun = UrunKabulEdilenBul(bakilan, kasa, balikKasa);
        if (urun == null) return false;
        if (urun == eldeNesne || urun.transform.IsChildOf(eldeNesne.transform)) { urun = null; return false; }

        return true;
    }

    // YENİ: Elde bir kasa tutulurken bakılan ürünü (ör. balık) kasanın içine doldur.
    // Sadece kasada "eldeykenDoldurulabilir" açıksa çalışır; kapalıysa (varsayılan) hiçbir
    // mevcut kasa davranışı değişmez. Ürün alınmazsa false döner, normal sol tık akışı devam eder.
    bool EldekiKasayaUrunDoldurDene()
    {
        if (!EldeKasayaDoldurmaHedefiVarMi(out GameObject urun, out KasaSistemi kasa, out BalikKasaSistemi balikKasa))
            return false;

        // Ürünü fiziksel olarak "toplanmış" hale getir.
        Vector3 dunyaScale = urun.transform.lossyScale;
        if (urun.TryGetComponent(out Rigidbody rb)) { rb.isKinematic = true; rb.useGravity = false; }
        Collider[] cols = urun.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;

        // Kasaya ekle (mevcut UrunEkle mantığı yeniden kullanılıyor).
        if (kasa != null) kasa.UrunEkle(urun);
        else balikKasa.UrunEkle(urun);

        // Dünya ölçeğini koru (kasa scale'i 1 değilse ürün bozulmasın).
        Transform yeniParent = urun.transform.parent;
        if (yeniParent != null)
        {
            Vector3 ps = yeniParent.lossyScale;
            if (ps.x != 0f && ps.y != 0f && ps.z != 0f)
                urun.transform.localScale = new Vector3(dunyaScale.x / ps.x, dunyaScale.y / ps.y, dunyaScale.z / ps.z);
        }

        // Ürün artık elde tutulan kasanın çocuğu; onu da "elde" layer'ına al.
        // Aksi halde ürün ana kamerayla, kasa el kamerasıyla (farklı FOV) çizilir ve kaymış görünür.
        if (eldeNesneKamera != null) eldeNesneKamera.NesneLayerAyarla(urun);

        // Ses efekti.
        if (urun.TryGetComponent(out NesneSesVerisi sv)) sv.BirakmaSesiCal(urun.transform.position);
        else if (eldeNesneSesVerisi != null) eldeNesneSesVerisi.BirakmaSesiCal();

        // Kasa preview'ini yenile ki eklenen ürün eldeki kasada görünsün.
        if (previewNesne != null) { Destroy(previewNesne); previewNesne = null; }
        PreviewOlusturElden();

        return true;
    }

    // Bakılan objeden başlayıp parent zincirinde, kasanın kabul ettiği ilk objeyi bulur.
    GameObject UrunKabulEdilenBul(GameObject bakilan, KasaSistemi kasa, BalikKasaSistemi balikKasa)
    {
        Transform t = bakilan.transform;
        while (t != null)
        {
            GameObject g = t.gameObject;
            if ((kasa != null && kasa.UrunKabulEdilirMi(g)) ||
                (balikKasa != null && balikKasa.UrunKabulEdilirMi(g)))
                return g;
            t = t.parent;
        }
        return null;
    }

    // YENİ: Makineye kasa bırakma
    bool MakineyeBirakmaDene()
    {
        if (prefabModuAktif || eldeNesne == null) return false;
        Ray r = oyuncuKamerasi.ViewportPointToRay(ViewportCenter); RaycastHit h;
        if (!Physics.Raycast(r, out h, maxMesafe)) return false;

        UretimMakinesiSistemi makine = MakineBul(h.collider.gameObject);
        if (makine == null || !makine.KasaKoyulabilirMi) return false;
        if (!makine.KasaKabulEdilirMi(eldeNesne)) return false;

        if (previewNesne != null) Destroy(previewNesne);
        if (eldeNesneKamera != null) eldeNesneKamera.NesneLayerSifirla(eldeNesne, orijinalLayer);

        eldeNesne.transform.localScale = orijinalScale;
        makine.KasaKoy(eldeNesne);

        if (eldeNesneSesVerisi != null) eldeNesneSesVerisi.BirakmaSesiCal(makine.transform.position);
        YerlesimModuKapat();
        return true;
    }

    // YENİ: Kızartma makinesine bırakma (sepet elde kalır, sadece kalamar slot'u aktif olur)
    bool KizartmaMakinesineBirakmaDene()
    {
        if (prefabModuAktif || eldeNesne == null) return false;
        Ray r = oyuncuKamerasi.ViewportPointToRay(ViewportCenter); RaycastHit h;
        if (!Physics.Raycast(r, out h, maxMesafe)) return false;

        KalamarKizartma kizartma = KizartmaBul(h.collider.gameObject);
        if (kizartma == null) return false;
        if (!kizartma.NesneKabulEdilirMi(eldeNesne)) return false;
        if (!kizartma.BosSlotVar) return false;

        if (kizartma.KalamarYerlestir())
        {
            // Sepeti yok et
            if (previewNesne != null) Destroy(previewNesne);
            Destroy(eldeNesne);
            YerlesimModuKapat();
            return true;
        }
        return false;
    }

    bool KasayaBirakmaDene()
    {
        if (prefabModuAktif || eldeNesne == null) return false;
        Ray r = oyuncuKamerasi.ViewportPointToRay(ViewportCenter); RaycastHit h;
        if (!Physics.Raycast(r, out h, maxMesafe)) return false;
        KasaSistemi kasa = KasaBul(h.collider.gameObject);
        if (kasa == null || !kasa.UrunKabulEdilirMi(eldeNesne)) return false;
        if (previewNesne != null) Destroy(previewNesne);
        if (eldeNesneKamera != null) eldeNesneKamera.NesneLayerSifirla(eldeNesne, orijinalLayer);
        kasa.UrunEkle(eldeNesne); eldeNesne.transform.localScale = orijinalScale;
        if (eldeNesne.TryGetComponent(out Collider col)) col.enabled = true;
        if (eldeNesneSesVerisi != null) eldeNesneSesVerisi.BirakmaSesiCal(kasa.transform.position);
        YerlesimModuKapat(); return true;
    }

    bool KuveteBirakmaDene()
    {
        if (prefabModuAktif || eldeNesne == null) return false;
        Ray r = oyuncuKamerasi.ViewportPointToRay(ViewportCenter); RaycastHit h;
        if (!Physics.Raycast(r, out h, maxMesafe)) return false;
        KuvetSistemi kuvet = KuvetBul(h.collider.gameObject);
        if (kuvet == null || !kuvet.NesneKabulEdilirMi(eldeNesne)) return false;
        if (previewNesne != null) Destroy(previewNesne);
        if (eldeNesneSesVerisi != null) eldeNesneSesVerisi.BirakmaSesiCal(kuvet.transform.position);
        kuvet.SebzeKoy(eldeNesne);
        YerlesimModuKapat(); return true;
    }

    bool KesmeTahtasinaBirakmaDene()
    {
        if (prefabModuAktif || eldeNesne == null) return false;
        Ray r = oyuncuKamerasi.ViewportPointToRay(ViewportCenter); RaycastHit h;
        if (!Physics.Raycast(r, out h, maxMesafe)) return false;
        KesmeTahtasiSistemi tahta = TahtaBul(h.collider.gameObject);
        if (tahta == null || !tahta.NesneKabulEdilirMi(eldeNesne)) return false;
        if (previewNesne != null) Destroy(previewNesne);
        if (eldeNesneKamera != null) eldeNesneKamera.NesneLayerSifirla(eldeNesne, orijinalLayer);
        tahta.NesneYerlestir(eldeNesne, orijinalScale, orijinalLayer);
        YerlesimModuKapat(); return true;
    }

    bool EkmekSistemineBirakmaDene()
    {
        if (prefabModuAktif || eldeNesne == null) return false;
        Ray r = oyuncuKamerasi.ViewportPointToRay(ViewportCenter); RaycastHit h;
        if (!Physics.Raycast(r, out h, maxMesafe)) return false;
        EkmekYerlestirmeSistemi ekmekSistemi = EkmekSistemiBul(h.collider.gameObject);
        if (ekmekSistemi == null || !ekmekSistemi.NesneKabulEdilirMi(eldeNesne)) return false;
        SpawnPreviewTemizle();
        if (previewNesne != null) Destroy(previewNesne);
        if (eldeNesneKamera != null) eldeNesneKamera.NesneLayerSifirla(eldeNesne, orijinalLayer);
        bool basarili = ekmekSistemi.NesneYerlestir(eldeNesne, orijinalScale, orijinalLayer);
        if (basarili) { YerlesimModuKapat(); return true; }
        return false;
    }

    bool TepsiYigininaGeriKoyDene()
    {
        if (prefabModuAktif || eldeNesne == null) return false;
        Ray r = oyuncuKamerasi.ViewportPointToRay(ViewportCenter); RaycastHit h;
        if (!Physics.Raycast(r, out h, maxMesafe)) return false;

        TepsiYiginiSistemi yigini = TepsiYiginiBul(h.collider.gameObject);
        if (yigini == null) return false;
        if (!yigini.TepsiGeriKonabilirMi(eldeNesne)) return false;

        if (previewNesne != null) Destroy(previewNesne);

        // Tepsi geri konulur (elde tutulan destroy edilir, child açılır)
        bool basarili = yigini.TepsiGeriKoy(eldeNesne);
        if (basarili)
        {
            // eldeNesne destroy edildi, YerlesimModuKapat'ta null olacak - sorun yok
            YerlesimModuKapat();
            return true;
        }
        return false;
    }

    bool TepsiyeBirakmaDene()
    {
        if (prefabModuAktif || eldeNesne == null) return false;
        Ray r = oyuncuKamerasi.ViewportPointToRay(ViewportCenter); RaycastHit h;
        if (!Physics.Raycast(r, out h, maxMesafe)) return false;
        TepsiSistemi tepsi = TepsiBul(h.collider.gameObject);
        if (tepsi == null || !tepsi.NesneKabulEdilirMi(eldeNesne)) return false;
        Vector3 yerlesimPoz = previewNesne != null ? previewNesne.transform.position : h.point;
        Quaternion yerlesimRot = Quaternion.Euler(0f, mevcutRotasyon, 0f);
        if (previewNesne != null) Destroy(previewNesne);
        if (eldeNesneKamera != null) eldeNesneKamera.NesneLayerSifirla(eldeNesne, orijinalLayer);
        bool basarili = tepsi.NesneYerlestir(eldeNesne, orijinalScale, orijinalLayer, yerlesimPoz, yerlesimRot);
        if (basarili) { YerlesimModuKapat(); return true; }
        return false;
    }

    bool PisirmeyeBirakmaDene()
    {
        if (prefabModuAktif || eldeNesne == null) return false;
        Ray r = oyuncuKamerasi.ViewportPointToRay(ViewportCenter); RaycastHit h;
        if (!Physics.Raycast(r, out h, maxMesafe)) return false;
        PisirmeSistemi pisirme = PisirmeBul(h.collider.gameObject);
        if (pisirme == null || !pisirme.NesneKabulEdilirMi(eldeNesne)) return false;
        if (eldeNesne.GetComponent<PisirilebilirNesne>() == null && eldeNesne.GetComponentInParent<PisirilebilirNesne>() == null && eldeNesne.GetComponentInChildren<PisirilebilirNesne>() == null) return false;
        Vector3 yerlesimPoz = previewNesne != null ? previewNesne.transform.position : h.point;
        Quaternion yerlesimRot = Quaternion.Euler(0f, mevcutRotasyon, 0f);
        if (previewNesne != null) Destroy(previewNesne);
        if (eldeNesneKamera != null) eldeNesneKamera.NesneLayerSifirla(eldeNesne, orijinalLayer);
        bool basarili = pisirme.NesneYerlestir(eldeNesne, orijinalScale, orijinalLayer, yerlesimPoz, yerlesimRot);
        if (basarili) { YerlesimModuKapat(); return true; }
        return false;
    }

    // Cop kovasi: elde tutulan objeyi sil; konteynerse (ICopEtkilesimi) sadece icini bosalt.
    bool CopKovasinaBirakmaDene()
    {
        if (prefabModuAktif || eldeNesne == null) return false;
        Ray r = oyuncuKamerasi.ViewportPointToRay(ViewportCenter); RaycastHit h;
        if (!Physics.Raycast(r, out h, maxMesafe)) return false;

        CopKovasi cop = h.collider.GetComponent<CopKovasi>();
        if (cop == null) cop = h.collider.GetComponentInParent<CopKovasi>();
        if (cop == null) return false;

        // Elde tutulan bir konteyner mi? (tepsi / kuvet / balik kasasi)
        ICopEtkilesimi etkilesim = eldeNesne.GetComponent<ICopEtkilesimi>();
        if (etkilesim == null) etkilesim = eldeNesne.GetComponentInChildren<ICopEtkilesimi>();
        if (etkilesim == null) etkilesim = eldeNesne.GetComponentInParent<ICopEtkilesimi>();

        // true: bosaltildi (kap kalsin) - false/yok: komple silinsin
        bool bosaltildi = etkilesim != null && etkilesim.CopeAtildi();

        cop.SesCal(h.point);

        if (bosaltildi)
        {
            // Kap elde kalir. Icerik Destroy edildi ama Unity'de Destroy FRAME SONUNA ertelenir;
            // preview'i hemen yenilersek eski (dolu) icerigi kopyalar. Bir frame bekleyip yenile.
            if (previewNesne != null) { Destroy(previewNesne); previewNesne = null; }
            StartCoroutine(PreviewiGecikmeliYenile());
        }
        else
        {
            // Normal obje (veya silinecek kasa): komple yok et, elden cik.
            if (previewNesne != null) Destroy(previewNesne);
            Destroy(eldeNesne);
            YerlesimModuKapat();
        }
        return true;
    }

    // Cop sonrasi: deferred Destroy'lar tamamlansin diye bir frame bekleyip preview'i yeniden olustur.
    System.Collections.IEnumerator PreviewiGecikmeliYenile()
    {
        yield return null; // icerik bu karede yok edilecek; bir sonraki karede kap gercekten bos olur
        if (yerlesimModu && !prefabModuAktif && eldeNesne != null)
        {
            if (previewNesne != null) { Destroy(previewNesne); previewNesne = null; }
            PreviewOlusturElden();
        }
    }

    // === YERLEŞTİRME ===

    // ===== TUTORIAL (kaldirilabilir) =====
    /// <summary>
    /// Bir nesne YERE KONULDUĞUNDA tetiklenir: (yerlesenNesne, kaynakPrefab).
    /// kaynakPrefab sadece MAĞAZADAN alınan nesnelerde doludur; elde taşınan bir
    /// nesne bırakıldıysa null gelir. Tutorial buna abone olup "domates dikildi mi?"
    /// gibi adımları takip eder. Abone yoksa hiçbir maliyeti yoktur.
    /// </summary>
    public event System.Action<GameObject, GameObject> NesneYerlestirildi;
    // ===== TUTORIAL SONU =====

    void NesneyiYerlestir()
    {
        // SATIN ALMA: para nesne yere konarken düşer. Yetmiyorsa yerleştirme yapılmaz,
        // oyuncu yerleştirme modunda kalır ve sağ tıkla iptal edebilir.
        if (prefabModuAktif && !SatinAlmayiOde()) return;

        Vector3 yerlesimPoz = previewNesne.transform.position;
        Quaternion yerlesimRot = previewNesne.transform.rotation;
        Destroy(previewNesne);

        if (prefabModuAktif)
        {
            GameObject yeniNesne = Instantiate(bekleyenPrefab, yerlesimPoz, yerlesimRot);
            YerlestirilenNesneyiAyarla(yeniNesne);
            if (yeniNesne.TryGetComponent(out NesneSesVerisi sesVerisi)) sesVerisi.BirakmaSesiCal(yerlesimPoz);
            NesneYerlestirildi?.Invoke(yeniNesne, bekleyenPrefab); // TUTORIAL (kaldirilabilir)
            YerlesimModuKapat(); BekleyenEldeNesneyiDevamEttir();
        }
        else
        {
            if (eldeNesneKamera != null) eldeNesneKamera.NesneLayerSifirla(eldeNesne, orijinalLayer);
            eldeNesne.transform.SetParent(null);
            eldeNesne.transform.SetPositionAndRotation(yerlesimPoz, yerlesimRot);
            eldeNesne.transform.localScale = orijinalScale;
            YerlestirilenNesneyiAyarla(eldeNesne);
            if (eldeNesneSesVerisi != null) eldeNesneSesVerisi.BirakmaSesiCal();
            if (previewNesne != null) Destroy(previewNesne);
            NesneYerlestirildi?.Invoke(eldeNesne, null); // TUTORIAL (kaldirilabilir)
            YerlesimModuKapat();
        }
    }

    /// <summary>
    /// Bekleyen fiyatı öder. Bedava (0) ise veya ekonomi yoksa true döner.
    /// Seçim anında para yeterliydi ama araya başka harcama girdiyse burada yakalanır.
    /// </summary>
    bool SatinAlmayiOde()
    {
        if (bekleyenFiyat <= 0) return true;

        if (EkonomiYoneticisi.Instance == null)
        {
            Debug.LogWarning("[Yerlestirme] EkonomiYoneticisi yok, nesne bedava yerleştirildi.");
            return true;
        }

        if (EkonomiYoneticisi.Instance.Harca(bekleyenFiyat))
            return true;

        Debug.Log($"[Yerlestirme] Yetersiz para! Gereken: {bekleyenFiyat}, " +
                  $"mevcut: {EkonomiYoneticisi.Instance.Para}");

        if (yetersizParaSesi != null && SesYoneticisi.Instance != null)
            SesYoneticisi.Instance.SesCal2D(yetersizParaSesi);

        return false;
    }

    void BekleyenEldeNesneyiDevamEttir()
    {
        if (!bekleyenEldeNesne.aktif || bekleyenEldeNesne.nesne == null) { bekleyenEldeNesne.Temizle(); return; }
        bekleyenEldeNesne.nesne.SetActive(true);
        YerlestirmeyeBaslaInternal(bekleyenEldeNesne.nesne, bekleyenEldeNesne.scale, bekleyenEldeNesne.elPoz, bekleyenEldeNesne.layer, false);
        bekleyenEldeNesne.Temizle();
    }

    void YerlesimModuKapat()
    {
        SpawnPreviewTemizle();
        yerlesimModu = false; yerlesimGecerli = false; oncekiGecerliDurum = false;
        eldeNesne = null; previewNesne = null; yerlestirilebilirData = null;
        elPozisyonu = null; previewRendererlar = null; mevcutRotasyon = 0f;
        orijinalLayer = 0; prefabModuAktif = false; bekleyenPrefab = null; bekleyenFiyat = 0; eldeNesneSesVerisi = null;
        oltaModuAktif = false;
        tusIleYerlesimGerekli = false; previewGoster = true;
    }

    // === BAŞLATMA ===

    void YerlestirmeyeBaslaInternal(GameObject nesne, Vector3 scale, Transform elPoz, int eskiLayer, bool prefabModu)
    {
        if (!nesne.TryGetComponent(out yerlestirilebilirData)) { Debug.LogWarning("Bu nesne yerleştirilebilir değil!"); return; }
        eldeNesne = nesne; orijinalScale = scale; elPozisyonu = elPoz; orijinalLayer = eskiLayer;
        // Kasada "tus ile yerlestirme" acikken preview kapali baslar, X ile acilir.
        tusIleYerlesimGerekli = !prefabModu && TusIleYerlesimMi(nesne);
        previewGoster = !tusIleYerlesimGerekli;
        yerlesimModu = true; mevcutRotasyon = 0f; sonTikZamani = Time.time;
        yerlesimGecerli = false; oncekiGecerliDurum = true; prefabModuAktif = prefabModu;
        nesne.TryGetComponent(out eldeNesneSesVerisi);
        if (nesne.TryGetComponent(out OutlineFx.OutlineFx outline)) outline.enabled = false;
        PreviewOlusturElden();
    }

    /// <param name="fiyat">Satın alma bedeli. 0 = bedava. Para nesne YERE KONUNCA düşer.</param>
    public void PrefabIleYerlestirmeBaslat(GameObject prefab, int fiyat = 0)
    {
        if (prefab == null) { Debug.LogError("Prefab null!"); return; }
        if (prefab.GetComponent<YerlestirilebilirNesne>() == null) { Debug.LogWarning("Bu prefab'da YerlestirilebilirNesne yok!"); return; }
        if (yerlesimModu && !prefabModuAktif && eldeNesne != null)
        {
            bekleyenEldeNesne.nesne = eldeNesne; bekleyenEldeNesne.scale = orijinalScale;
            bekleyenEldeNesne.elPoz = elPozisyonu; bekleyenEldeNesne.layer = orijinalLayer; bekleyenEldeNesne.aktif = true;
            eldeNesne.SetActive(false);
            if (previewNesne != null) { Destroy(previewNesne); previewNesne = null; }
            yerlesimModu = false;
        }
        // Prefab modunda preview her zaman gorunur (X kurali sadece elde tutulan kasalar icin).
        tusIleYerlesimGerekli = false; previewGoster = true;
        bekleyenPrefab = prefab; bekleyenFiyat = fiyat; prefabModuAktif = true; yerlesimModu = true;
        mevcutRotasyon = 0f; sonTikZamani = Time.time; yerlesimGecerli = false; oncekiGecerliDurum = true;
        PreviewOlusturPrefabdan();
    }

    // === İPTAL ===

    void YerlesimIptal()
    {
        SpawnPreviewTemizle();
        if (prefabModuAktif)
        {
            if (previewNesne != null) Destroy(previewNesne);
            YerlesimModuKapat(); BekleyenEldeNesneyiDevamEttir();
        }
        else
        {
            if (eldeNesne != null)
            {
                if (eldeNesneKamera != null) eldeNesneKamera.NesneLayerSifirla(eldeNesne, orijinalLayer);
                eldeNesne.transform.SetParent(null);
                eldeNesne.transform.position = transform.position + transform.forward * 1.5f;
                eldeNesne.transform.localScale = orijinalScale;
                YerlestirilenNesneyiAyarla(eldeNesne);
                if (eldeNesneSesVerisi != null) eldeNesneSesVerisi.BirakmaSesiCal();
            }
            if (previewNesne != null) Destroy(previewNesne);
            YerlesimModuKapat();
        }
    }

    // === PREVIEW ===

    void PreviewOlusturElden()
    {
        previewNesne = Instantiate(eldeNesne); previewNesne.name = "Preview";
        previewNesne.transform.SetParent(null); previewNesne.transform.localScale = orijinalScale;
        SetLayerRecursive(previewNesne, orijinalLayer);
        PreviewTemizle(previewNesne); PreviewMateryalUygula(); previewNesne.SetActive(false);
    }

    void PreviewOlusturPrefabdan()
    {
        previewNesne = Instantiate(bekleyenPrefab); previewNesne.name = "Preview";
        previewNesne.transform.SetParent(null);
        orijinalScale = previewNesne.transform.localScale; orijinalLayer = bekleyenPrefab.layer;
        PreviewTemizle(previewNesne); PreviewMateryalUygula(); previewNesne.SetActive(false);
    }

    void SpawnPreviewOlustur(GameObject spawnPrefab)
    {
        SpawnPreviewTemizle();
        spawnPreviewNesne = Instantiate(spawnPrefab); spawnPreviewNesne.name = "SpawnPreview";
        PreviewTemizle(spawnPreviewNesne);
        Renderer[] rr = spawnPreviewNesne.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < rr.Length; i++)
        { if (rr[i] is ParticleSystemRenderer) continue; Material[] m = new Material[rr[i].sharedMaterials.Length]; for (int j = 0; j < m.Length; j++) m[j] = previewMateryal; rr[i].sharedMaterials = m; }
        spawnPreviewAktif = true;
    }

    void SpawnPreviewTemizle() { if (spawnPreviewNesne != null) { Destroy(spawnPreviewNesne); spawnPreviewNesne = null; } spawnPreviewAktif = false; }

    void PreviewTemizle(GameObject preview)
    {
        if (preview.TryGetComponent(out Rigidbody rb)) Destroy(rb);
        Collider[] cols = preview.GetComponentsInChildren<Collider>(true); for (int i = 0; i < cols.Length; i++) Destroy(cols[i]);
        if (preview.TryGetComponent(out OutlineFx.OutlineFx outline)) Destroy(outline);
        if (preview.TryGetComponent(out NesneSesVerisi sv)) Destroy(sv);
        ParticleSystem[] ps = preview.GetComponentsInChildren<ParticleSystem>(true); for (int i = 0; i < ps.Length; i++) Destroy(ps[i].gameObject);

        // Dünya UI'ları (bitkinin susuz ikonu vb.) hayalet önizlemede görünmesin
        KamerayaDonukIkon[] ikonlar = preview.GetComponentsInChildren<KamerayaDonukIkon>(true);
        for (int i = 0; i < ikonlar.Length; i++) Destroy(ikonlar[i].gameObject);

        // Önizleme prefabın CANLI kopyası: büyüme/susuzluk mantığı çalışıp ses çalmasın,
        // ikonu geri açmasın. Component Start'tan önce kapatılırsa Start hiç çalışmaz.
        BitkiBuyumeSistemi[] bitkiler = preview.GetComponentsInChildren<BitkiBuyumeSistemi>(true);
        for (int i = 0; i < bitkiler.Length; i++) bitkiler[i].enabled = false;
    }

    void PreviewMateryalUygula()
    {
        Renderer[] tr = previewNesne.GetComponentsInChildren<Renderer>();
        var fl = new System.Collections.Generic.List<Renderer>();
        for (int i = 0; i < tr.Length; i++) { if (tr[i] != null && !(tr[i] is ParticleSystemRenderer)) fl.Add(tr[i]); }
        previewRendererlar = fl.ToArray();
        for (int i = 0; i < previewRendererlar.Length; i++)
        { Material[] m = new Material[previewRendererlar[i].sharedMaterials.Length]; for (int j = 0; j < m.Length; j++) m[j] = previewMateryal; previewRendererlar[i].sharedMaterials = m; }
    }

    // === YARDIMCI ===

    void YerlestirilenNesneyiAyarla(GameObject nesne)
    {
        SetLayerRecursive(nesne, yerlestirilenNesneLayer);
        Collider[] cols = nesne.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++) { cols[i].enabled = true; cols[i].isTrigger = false; }
        if (nesne.TryGetComponent(out Rigidbody rb)) { rb.isKinematic = true; rb.useGravity = false; }
    }

    void SetLayerRecursive(GameObject obj, int layer)
    { obj.layer = layer; foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, layer); }

    // === OLTA STANDI ===

    bool OltaStandiBirakmaDene()
    {
        if (!oltaModuAktif || eldeNesne == null) return false;

        // Olta mı kontrol et
        OltaEntegrasyon oltaKontrol = eldeNesne.GetComponent<OltaEntegrasyon>();
        if (oltaKontrol == null) return false;

        ray = oyuncuKamerasi.ViewportPointToRay(ViewportCenter);
        if (!Physics.Raycast(ray, out hit, maxMesafe))
            return false;

        OltaStandi stant = hit.collider.GetComponent<OltaStandi>();
        if (stant == null)
            stant = hit.collider.GetComponentInParent<OltaStandi>();

        if (stant == null || stant.OltaVarMi) return false;

        // Oltayı stanta koy
        if (previewNesne != null) Destroy(previewNesne);

        eldeNesne.transform.SetParent(null);
        eldeNesne.transform.localScale = orijinalScale;

        // Collider'ları aç
        Collider[] cols = eldeNesne.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].enabled = true;
            cols[i].isTrigger = false;
        }

        if (eldeNesne.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        stant.OltaKoy(eldeNesne);
        YerlesimModuKapat();
        return true;
    }

    // === PUBLIC ===

    public void YerlestirmeyeBasla(GameObject nesne, Vector3 scale, Transform elPoz, int eskiLayer) { YerlestirmeyeBaslaInternal(nesne, scale, elPoz, eskiLayer, false); }

    public void OltaYerlestirmeyeBasla(GameObject nesne, Vector3 scale, Transform elPoz, int eskiLayer)
    {
        oltaModuAktif = true;
        YerlestirmeyeBaslaInternal(nesne, scale, elPoz, eskiLayer, false);
    }

    public bool YerlesimModuAktif => yerlesimModu;
    public bool EldeNesneVarMi => (yerlesimModu && !prefabModuAktif && eldeNesne != null) || bekleyenEldeNesne.aktif;
    public bool PrefabModuAktifMi => prefabModuAktif;
    public bool OltaModuAktifMi => oltaModuAktif;
}