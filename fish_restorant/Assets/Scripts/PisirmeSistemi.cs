using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pişirme yüzeyine eklenir (ızgara, tava vb.).
/// Birden fazla balık yerleştirilebilir - her birinin shader'ı bağımsız pişer.
/// 
/// Kurulum:
/// 1. Izgara/tava objesine bu scripti ekle
/// 2. Collider olmalı ve RaycastSistemi'nin etkilesimLayer'ında olmalı
/// 3. Balık prefab'ında PisirilebilirNesne componenti olmalı
/// </summary>
public class PisirmeSistemi : MonoBehaviour
{
    [Header("Pişirme Ayarları")]
    [Tooltip("Pişirme süresi (saniye) - slider 0'dan 1'e bu sürede çıkar")]
    [SerializeField] private float pismeSuresi = 5f;

    [Tooltip("Sadece bu tag'lere sahip nesneler pişirilebilir. Boş bırakırsan hepsini kabul eder.")]
    [SerializeField] private string[] kabulEdilenTagler;

    [Header("Pişmişlik Eşikleri (%) - UI'daki doneness yüzdesiyle aynı")]
    [Tooltip("Balığın pişmiş GÖRÜNMEYE başladığı yüzde. Altı = ÇİĞ.")]
    [SerializeField] private float pismeGorunumYuzde = 80f;
    [Tooltip("Pişmiş sayılmanın bittiği yüzde. Görünüm ile bu değer arası = PİŞMİŞ. Üstü = YANMIŞ.")]
    [SerializeField] private float pismeBitmeYuzde = 120f;
    [Tooltip("Tam yanmış görünümün oluştuğu yüzde (yanma slider'ı burada dolar).")]
    [SerializeField] private float yanmaBitisYuzde = 200f;

    [Header("Shader Ayarları")]
    [Tooltip("Shader'daki pişme slider property adı")]
    [SerializeField] private string shaderSliderAdi = "_PismeSlider";

    [Tooltip("Shader'daki yanma (2. pişme) slider property adı")]
    [SerializeField] private string shaderSlider2Adi = "_pisirme2";

    [Header("Ses Ayarları")]
    [Tooltip("Izgara üzerinde nesne varken çalan döngüsel ses (cızırtı vb.)")]
    [SerializeField] private AudioClip pismeSesi;

    [Tooltip("Ses seviyesi")]
    [Range(0f, 1f)]
    [SerializeField] private float sesSeviyesi = 0.5f;

    [Tooltip("Nesne konulduğunda çalan tek seferlik ses (cız efekti)")]
    [SerializeField] private SesVerisi konulmaSesi;

    // Ses
    private AudioSource pismeAudioSource;

    // Aktif pişen nesneler
    private class PisenNesneVerisi
    {
        public GameObject nesne;
        public PisirilebilirNesne pisirilebilirData;
        public Renderer aktifRenderer;
        public float pismeIlerleme;
        public bool pismeAktif;
    }

    private List<PisenNesneVerisi> pisenNesneler = new List<PisenNesneVerisi>();

    private int shaderSliderID;
    private int shaderSlider2ID;
    private MaterialPropertyBlock propBlock;

    void Awake()
    {
        shaderSliderID = Shader.PropertyToID(shaderSliderAdi);
        shaderSlider2ID = Shader.PropertyToID(shaderSlider2Adi);
        propBlock = new MaterialPropertyBlock();

        // Döngüsel pişme sesi için AudioSource oluştur
        pismeAudioSource = gameObject.AddComponent<AudioSource>();
        pismeAudioSource.clip = pismeSesi;
        pismeAudioSource.loop = true;
        pismeAudioSource.playOnAwake = false;
        pismeAudioSource.volume = sesSeviyesi;
        pismeAudioSource.spatialBlend = 1f; // 3D ses
        pismeAudioSource.minDistance = 1f;
        pismeAudioSource.maxDistance = 15f;
        pismeAudioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    // Maksimum pişme ilerlemesi = yanma bitiş. Bu noktada pişme durur (tam yanık).
    private float MaxIlerleme => yanmaBitisYuzde / 100f;

    void OnValidate()
    {
        // Eşikler mantıklı sırada kalsın: görünüm <= bitme <= yanma bitiş
        pismeGorunumYuzde = Mathf.Max(0f, pismeGorunumYuzde);
        if (pismeBitmeYuzde < pismeGorunumYuzde) pismeBitmeYuzde = pismeGorunumYuzde;
        if (yanmaBitisYuzde < pismeBitmeYuzde) yanmaBitisYuzde = pismeBitmeYuzde;
        // Yapısal tavan %300 (PisirilebilirNesne ilerlemeyi 0-3 arası clamp'ler).
        if (yanmaBitisYuzde > 300f) yanmaBitisYuzde = 300f;
    }

    void Update()
    {
        for (int i = pisenNesneler.Count - 1; i >= 0; i--)
        {
            var veri = pisenNesneler[i];

            // Destroy edilmiş nesne kontrolü
            if (veri.nesne == null)
            {
                pisenNesneler.RemoveAt(i);
                continue;
            }

            if (!veri.pismeAktif || veri.pismeIlerleme >= MaxIlerleme) continue;

            // Kademeli artış
            veri.pismeIlerleme += Time.deltaTime / pismeSuresi;
            veri.pismeIlerleme = Mathf.Clamp(veri.pismeIlerleme, 0f, MaxIlerleme);

            // Shader güncelle (eşikler Inspector'dan: görünüm / bitme / yanma bitiş)
            ShaderIlerlemeUygula(veri.aktifRenderer, veri.pismeIlerleme);

            // İlerlemeyi kaydet
            if (veri.pisirilebilirData != null)
            {
                if (!veri.pisirilebilirData.Cevirildi)
                    veri.pisirilebilirData.AltPismeIlerlemeyiAyarla(veri.pismeIlerleme);
                else
                    veri.pisirilebilirData.UstPismeIlerlemeyiAyarla(veri.pismeIlerleme);
            }

            // Pişme tamamlandı (yanma dahil)
            if (veri.pismeIlerleme >= MaxIlerleme)
            {
                veri.pismeAktif = false;
            }

            // "Pişmiş" durumuna ilk girildiğinde (görünüm eşiğini geçince) bir kez işaretle + log
            float gorunumIlerleme = pismeGorunumYuzde / 100f;
            if (veri.pismeIlerleme >= gorunumIlerleme &&
                veri.pismeIlerleme - (Time.deltaTime / pismeSuresi) < gorunumIlerleme)
            {
                if (veri.pisirilebilirData != null)
                {
                    if (!veri.pisirilebilirData.Cevirildi)
                        veri.pisirilebilirData.AltPismeDurumunuAyarla(true);
                    else
                        veri.pisirilebilirData.UstPismeDurumunuAyarla(true);
                }

                string yuz = (veri.pisirilebilirData != null && veri.pisirilebilirData.Cevirildi) ? "Üst" : "Alt";
                Debug.Log($"[Pişirme] {yuz} taraf pişti! Nesne: {veri.nesne.name}");
            }
        }
    }

    /// <summary>
    /// Nesneyi pişirme yüzeyine yerleştir. Birden fazla nesne kabul eder.
    /// </summary>
    public bool NesneYerlestir(GameObject nesne, Vector3 orijinalScale, int orijinalLayer, Vector3 vurusNoktasi, Quaternion rotasyon)
    {
        if (nesne == null) return false;
        if (!NesneKabulEdilirMi(nesne)) return false;

        PisirilebilirNesne pisirilebilir = nesne.GetComponent<PisirilebilirNesne>();
        if (pisirilebilir == null)
            pisirilebilir = nesne.GetComponentInParent<PisirilebilirNesne>();
        if (pisirilebilir == null)
            pisirilebilir = nesne.GetComponentInChildren<PisirilebilirNesne>();

        if (pisirilebilir == null)
        {
            Debug.Log("[Pişirme] Bu nesne pişirilebilir değil (PisirilebilirNesne yok).");
            return false;
        }

        if (!pisirilebilir.ModellerGecerli)
        {
            Debug.LogError("[Pişirme] Alt veya üst model referansı atanmamış!");
            return false;
        }

        // Pozisyon ayarla
        nesne.transform.SetParent(null);
        nesne.transform.rotation = rotasyon;
        nesne.transform.localScale = orijinalScale;

        // Mesh'in alt noktası ile pivot arasındaki farkı hesapla
        // Böylece balık ızgaranın içine gömülmez
        nesne.transform.position = vurusNoktasi;
        Renderer[] rendererlar = nesne.GetComponentsInChildren<Renderer>();
        if (rendererlar.Length > 0)
        {
            Bounds toplamBounds = rendererlar[0].bounds;
            for (int r = 1; r < rendererlar.Length; r++)
            {
                if (rendererlar[r] is ParticleSystemRenderer) continue;
                toplamBounds.Encapsulate(rendererlar[r].bounds);
            }
            float meshAltNokta = toplamBounds.min.y;
            float pivotY = nesne.transform.position.y;
            float offset = pivotY - meshAltNokta;
            nesne.transform.position = vurusNoktasi + Vector3.up * offset;
        }

        // Fizik
        if (nesne.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (nesne.TryGetComponent(out Collider col))
        {
            col.enabled = true;
            col.isTrigger = false;
        }

        SetLayerRecursive(nesne, orijinalLayer);

        // Konulma sesi (tek seferlik cız efekti)
        if (konulmaSesi != null && SesYoneticisi.Instance != null)
            SesYoneticisi.Instance.SesCal(konulmaSesi, nesne.transform.position);

        // Fiziksel rotasyona göre hangi yüzün altta olduğunu tespit et
        pisirilebilir.FizikselYuzTespitEt();

        // Efektleri aç (yüzey tag'i ile kontrol)
        pisirilebilir.EfektleriAc(gameObject.tag);

        // Listeye ekle ve pişirmeyi başlat
        var veri = new PisenNesneVerisi
        {
            nesne = nesne,
            pisirilebilirData = pisirilebilir,
            aktifRenderer = pisirilebilir.AktifYuzRenderer,
            pismeIlerleme = pisirilebilir.AktifYuzIlerleme,
            pismeAktif = !pisirilebilir.AktifYuzPisti
        };

        pisenNesneler.Add(veri);

        // Döngüsel pişme sesini başlat (ilk nesne konulduğunda)
        PismeSesiGuncelle();

        // Shader başlangıç
        ShaderIlerlemeUygula(veri.aktifRenderer, veri.pismeIlerleme);

        string yuz = pisirilebilir.Cevirildi ? "Üst" : "Alt";
        Debug.Log($"[Pişirme] {yuz} yüz pişirme başladı! Süre: {pismeSuresi}s, Nesne: {nesne.name}");

        return true;
    }

    /// <summary>
    /// Belirli bir nesneyi (veya parent/child'ını) yüzeyden al
    /// </summary>
    public GameObject NesneAl(GameObject nesne)
    {
        for (int i = 0; i < pisenNesneler.Count; i++)
        {
            GameObject kayitli = pisenNesneler[i].nesne;
            if (kayitli == null) continue;

            if (kayitli == nesne || nesne.transform.IsChildOf(kayitli.transform) || kayitli.transform.IsChildOf(nesne.transform))
            {
                // Efektleri kapat
                if (pisenNesneler[i].pisirilebilirData != null)
                    pisenNesneler[i].pisirilebilirData.EfektleriKapat();

                pisenNesneler.RemoveAt(i);
                PismeSesiGuncelle();
                return kayitli;
            }
        }
        return null;
    }

    /// <summary>
    /// Herhangi bir nesneyi al (geriye uyumluluk)
    /// </summary>
    public GameObject NesneAl()
    {
        if (pisenNesneler.Count == 0) return null;

        var veri = pisenNesneler[pisenNesneler.Count - 1];
        GameObject alinan = veri.nesne;

        // Efektleri kapat
        if (veri.pisirilebilirData != null)
            veri.pisirilebilirData.EfektleriKapat();

        pisenNesneler.RemoveAt(pisenNesneler.Count - 1);
        PismeSesiGuncelle();
        return alinan;
    }

    /// <summary>
    /// Belirli nesnenin pişmesini duraklat
    /// </summary>
    public void PismeDuraklat(GameObject nesne)
    {
        var veri = VeriBul(nesne);
        if (veri != null) veri.pismeAktif = false;
    }

    /// <summary>
    /// Çevirme sonrası yeni yüzün pişirmesini başlat
    /// </summary>
    public void CevirmeSonrasiPismeDevam(GameObject nesne)
    {
        var veri = VeriBul(nesne);
        if (veri == null || veri.pisirilebilirData == null) return;

        veri.aktifRenderer = veri.pisirilebilirData.AktifYuzRenderer;

        if (veri.aktifRenderer == null)
        {
            Debug.LogError("[Pişirme] Çevirme sonrası aktif yüz renderer'ı null!");
            return;
        }

        if (veri.pisirilebilirData.AktifYuzIlerleme >= MaxIlerleme)
        {
            Debug.Log("[Pişirme] Bu yüz tamamen yanmış.");
            veri.pismeIlerleme = MaxIlerleme;
            veri.pismeAktif = false;
            return;
        }

        veri.pismeIlerleme = veri.pisirilebilirData.AktifYuzIlerleme;
        veri.pismeAktif = true;

        ShaderIlerlemeUygula(veri.aktifRenderer, veri.pismeIlerleme);

        string yuz = veri.pisirilebilirData.Cevirildi ? "Üst" : "Alt";
        Debug.Log($"[Pişirme] Çevirme sonrası {yuz} yüz pişirme devam! İlerleme: {veri.pismeIlerleme:F2}");
    }

    /// <summary>
    /// Bu nesne (veya parent/child'ı) bu yüzeyde mi?
    /// </summary>
    public bool NesneBuYuzeydeMi(GameObject nesne)
    {
        for (int i = 0; i < pisenNesneler.Count; i++)
        {
            GameObject kayitli = pisenNesneler[i].nesne;
            if (kayitli == null) continue;

            if (kayitli == nesne) return true;
            if (nesne.transform.IsChildOf(kayitli.transform)) return true;
            if (kayitli.transform.IsChildOf(nesne.transform)) return true;
        }
        return false;
    }

    /// <summary>
    /// Nesneyi veya parent/child'ını bul ve veriyi döndür
    /// </summary>
    PisenNesneVerisi VeriBul(GameObject nesne)
    {
        for (int i = 0; i < pisenNesneler.Count; i++)
        {
            GameObject kayitli = pisenNesneler[i].nesne;
            if (kayitli == null) continue;

            if (kayitli == nesne) return pisenNesneler[i];
            if (nesne.transform.IsChildOf(kayitli.transform)) return pisenNesneler[i];
            if (kayitli.transform.IsChildOf(nesne.transform)) return pisenNesneler[i];
        }
        return null;
    }

    public bool NesneKabulEdilirMi(GameObject nesne)
    {
        if (nesne == null) return false;

        if (kabulEdilenTagler == null || kabulEdilenTagler.Length == 0)
            return true;

        string nesneTag = nesne.tag;
        for (int i = 0; i < kabulEdilenTagler.Length; i++)
        {
            if (kabulEdilenTagler[i] == nesneTag)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Izgara üzerinde nesne varsa döngüsel sesi çal, yoksa durdur
    /// </summary>
    void PismeSesiGuncelle()
    {
        if (pismeAudioSource == null || pismeSesi == null) return;

        if (pisenNesneler.Count > 0)
        {
            if (!pismeAudioSource.isPlaying)
                pismeAudioSource.Play();
        }
        else
        {
            if (pismeAudioSource.isPlaying)
                pismeAudioSource.Stop();
        }
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    /// <summary>
    /// Verilen ilerlemeye (0-3) göre shader slider'larını (pişme + yanma) hesaplayıp uygular.
    /// Eşikler Inspector'dan: pismeGorunumYuzde / pismeBitmeYuzde / yanmaBitisYuzde.
    /// - slider1 (pişme rengi): 0 -> görünüm arası 0→1, sonra sabit 1.
    /// - slider2 (yanma rengi): bitme -> yanma bitiş arası 0→1.
    /// </summary>
    private void ShaderIlerlemeUygula(Renderer renderer, float ilerleme)
    {
        if (renderer == null) return;

        float gorunum = pismeGorunumYuzde / 100f;
        float bitme = pismeBitmeYuzde / 100f;
        float yanmaBitis = yanmaBitisYuzde / 100f;

        float slider1 = gorunum > 0.0001f ? Mathf.Clamp01(ilerleme / gorunum) : 1f;

        float slider2 = 0f;
        float yanmaAralik = yanmaBitis - bitme;
        if (ilerleme > bitme && yanmaAralik > 0.0001f)
            slider2 = Mathf.Clamp01((ilerleme - bitme) / yanmaAralik);

        renderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(shaderSliderID, slider1);
        propBlock.SetFloat(shaderSlider2ID, slider2);
        renderer.SetPropertyBlock(propBlock);
    }

    /// <summary>Balığın (bir yüzünün) pişme durumu.</summary>
    public enum PismeDurumu { Cig, Pismis, Yanmis }

    /// <summary>
    /// Verilen ilerleme değerinin (0-3) hangi pişme durumuna denk geldiğini döndürür.
    /// ilerleme &lt; görünüm -> Cig ; görünüm..bitme -> Pismis ; &gt; bitme -> Yanmis.
    /// İleride servis/kalite için: DurumHesapla(balik.AltPismeIlerleme) gibi kullanılır.
    /// </summary>
    public PismeDurumu DurumHesapla(float ilerleme)
    {
        float gorunum = pismeGorunumYuzde / 100f;
        float bitme = pismeBitmeYuzde / 100f;
        if (ilerleme < gorunum) return PismeDurumu.Cig;
        if (ilerleme <= bitme) return PismeDurumu.Pismis;
        return PismeDurumu.Yanmis;
    }

    // === Public Properties ===
    public bool NesneVarMi => pisenNesneler.Count > 0;
    public int NesneSayisi => pisenNesneler.Count;
    public float PismeSuresi => pismeSuresi;
    public string[] KabulEdilenTagler => kabulEdilenTagler;
}