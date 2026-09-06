using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Sulama akışının beynidir. Oyuncu (kamera parent'ı) üzerine eklenir.
///
/// AKIŞ (BASILI TUTMA):
/// Susuz bitkiye bakıp sol tıka BAS -> kova çıkar, eğilir, su akar, bar dolmaya başlar.
/// Tuşu BASILI TUT -> bar dolmaya devam eder.
/// Bar dolunca -> para düşer, bitki sulanır, kova geri gider.
/// Tuşu erken BIRAKIRSAN -> sulama iptal, bitki sulanmaz ve PARA HARCANMAZ.
///
/// KURULUM:
/// 1. Oyuncu objesine (FirstPersonController'ın olduğu yere) ekle.
/// 2. Kameranın altına BOŞ bir GameObject aç (ör. "KovaNoktasi"), kovanın elde duracağı
///    yere/açıya getir -> "Kova Pozisyonu" alanına hierarchy'den sürükle.
/// 3. Kova modelini/prefab'ını "Kova Objesi" alanına sürükle.
/// </summary>
public class BitkiSulamaSistemi : MonoBehaviour
{
    public static BitkiSulamaSistemi Instance { get; private set; }

    [Header("Kova")]
    [Tooltip("Sulama kovası modeli. SAHNEDEKİ obje de olur, PROJECT'teki prefab da olur:\n" +
             "- Sahne objesi ise doğrudan kullanılır, Kova Pozisyonu noktasına ışınlanır.\n" +
             "- Prefab ise oyun başlarken bir kopyası üretilip o noktaya yerleştirilir.")]
    [SerializeField] private GameObject kovaObjesi;

    [Tooltip("KOVANIN IŞINLANACAĞI NOKTA. Kameranın altına boş bir GameObject aç, " +
             "kovanın elde duracağı yere koy ve buraya sürükle.")]
    [SerializeField] private Transform kovaPozisyonu;

    [Tooltip("Işınlanan kovaya ek konum kayması (ince ayar).")]
    [SerializeField] private Vector3 pozisyonOfseti = Vector3.zero;

    [Tooltip("Işınlanan kovaya ek açı (ince ayar, derece).")]
    [SerializeField] private Vector3 rotasyonOfseti = Vector3.zero;

    [Tooltip("Kovanın ölçeği. Sıfır bırakılırsa modelin kendi ölçeği korunur.")]
    [SerializeField] private Vector3 kovaOlcegi = Vector3.zero;

    [Tooltip("Açık: kova otomatik 'EldeNesne' layer'ına alınır -> el kamerası görür, " +
             "duvara/yakın nesneye girmez. Su particle'ı da bu layer'a geçer.")]
    [SerializeField] private bool eldeNesneLayeriKullan = true;

    [Tooltip("Boş bırakılırsa kova objesinin üzerinden otomatik bulunur.")]
    [SerializeField] private SulamaKovasi kova;

    [Tooltip("Kova geri döndükten kaç saniye SONRA gizlensin.")]
    [SerializeField] private float kovaGizlemeGecikmesi = 0.1f;

    [Header("Basılı Tutma")]
    [Tooltip("Sulamanın tamamlanması için tuşun kaç saniye BASILI TUTULACAĞI.")]
    [SerializeField] private float sulamaSuresi = 3f;

    [Tooltip("Sulama için basılı tutulacak tuş. Sol tık = Mouse0.")]
    [SerializeField] private KeyCode sulamaTusu = KeyCode.Mouse0;

    [Tooltip("Açık: sulama sırasında bitkiden başka yere bakarsan sulama iptal olur.")]
    [SerializeField] private bool bakisiKaybedinceIptal = true;

    [Tooltip("Açık: Tab ile UI paneli açıkken sulama başlamaz, açık sulama varsa iptal olur.")]
    [SerializeField] private bool uiAcikkenSulamaYok = true;

    [Header("Sesler")]
    [Tooltip("Para yetmediğinde çalacak uyarı sesi (2D).")]
    [SerializeField] private SesVerisi yetersizParaSesi;

    // UI bu eventleri dinler (gevşek bağlı)
    public event Action OnSulamaBasladi;
    public event Action<float> OnSulamaIlerleme; // 0-1
    public event Action OnSulamaBitti;
    public event Action OnSulamaIptal;

    public bool SulamaAktif { get; private set; }
    public float SulamaSuresi => sulamaSuresi;

    /// <summary>Sahnede gerçekten kullanılan kova. Prefab atanmışsa bunun KOPYASIDIR.</summary>
    private GameObject kovaOrnegi;

    private RaycastSistemi raycastSistemi;
    private BitkiBuyumeSistemi aktifBitki;
    private float gecenSure;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Debug.LogWarning("[Sulama] Sahnede birden fazla BitkiSulamaSistemi var.", this);

        KovayiHazirla();
    }

    void KovayiHazirla()
    {
        if (kovaObjesi == null)
        {
            Debug.LogError("[Sulama] 'Kova Objesi' atanmamış! Sulama kovası görünmez.", this);
            return;
        }

        if (kovaPozisyonu == null)
        {
            Debug.LogError("[Sulama] 'Kova Pozisyonu' atanmamış! Kameranın altına boş bir " +
                           "GameObject aç ve bu alana sürükle.", this);
            return;
        }

        // scene.IsValid() == false -> obje sahnede değil, PROJECT'teki bir prefab asset'i.
        // Prefab asset'inin transform'u değiştirilemez (Unity engeller), o yüzden kopyasını üretiyoruz.
        if (kovaObjesi.scene.IsValid())
        {
            kovaOrnegi = kovaObjesi;
        }
        else
        {
            kovaOrnegi = Instantiate(kovaObjesi);
            kovaOrnegi.name = kovaObjesi.name;
        }

        // Inspector'a prefab'ın İÇİNDEKİ SulamaKovasi sürüklenmiş olabilir.
        // O prefab asset'ini döndürmeye çalışmak aynı hataya yol açar -> kopyadakini kullan.
        if (kova != null && !kova.gameObject.scene.IsValid())
            kova = null;

        if (kova == null)
            kova = kovaOrnegi.GetComponentInChildren<SulamaKovasi>(true);

        if (kova == null)
            Debug.LogWarning("[Sulama] Kova objesinde SulamaKovasi scripti yok -> kova eğilmez, su akmaz.", this);

        KovayiYerlestir();
        kovaOrnegi.SetActive(false);
    }

    void Start()
    {
        raycastSistemi = FindObjectOfType<RaycastSistemi>();

        // Layer ayarı Start'ta: EldeNesneKamera kendi layer'ını Awake'te hesaplıyor
        if (!eldeNesneLayeriKullan || kovaOrnegi == null) return;

        EldeNesneKamera elKamera = FindObjectOfType<EldeNesneKamera>();
        if (elKamera != null)
            elKamera.NesneLayerAyarla(kovaOrnegi);
        else
            Debug.LogWarning("[Sulama] EldeNesneKamera bulunamadı, kova layer'ı değiştirilmedi.", this);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ================== BASILI TUTMA DÖNGÜSÜ ==================

    void Update()
    {
        if (!SulamaAktif) return;

        // 0) Sulama sırasında UI açıldıysa (Tab) hemen kes
        if (uiAcikkenSulamaYok && UIYoneticisi.HerhangiBirUIAcikMi)
        {
            Iptal("UI açıldı");
            return;
        }

        // 1) Tuş bırakıldı mı?
        if (!Input.GetKey(sulamaTusu))
        {
            Iptal("tuş bırakıldı");
            return;
        }

        // 2) Bitki hâlâ geçerli mi? (yok edilmiş / başka yolla sulanmış olabilir)
        if (aktifBitki == null || !aktifBitki.SulanabilirMi)
        {
            Iptal("bitki geçersiz");
            return;
        }

        // 3) Hâlâ o bitkiye bakıyor mu?
        if (bakisiKaybedinceIptal && raycastSistemi != null &&
            raycastSistemi.BakilanBitki != aktifBitki)
        {
            Iptal("bitkiden başka yere bakıldı");
            return;
        }

        gecenSure += Time.deltaTime;
        OnSulamaIlerleme?.Invoke(Mathf.Clamp01(gecenSure / sulamaSuresi));

        if (gecenSure >= sulamaSuresi)
            Tamamla();
    }

    /// <summary>
    /// Sulamayı başlatır (tuşa BASILDIĞI an). Para burada DÜŞMEZ, sadece yeterli mi diye bakılır;
    /// ödeme sulama tamamlanınca yapılır. NesneAlmaSistemi sol tıkta bunu çağırır.
    /// </summary>
    public bool SulamayaBasla(BitkiBuyumeSistemi bitki)
    {
        if (SulamaAktif) return false;
        if (bitki == null || !bitki.SulanabilirMi) return false;

        // UI (Tab paneli) açıkken sulama yapılmaz
        if (uiAcikkenSulamaYok && UIYoneticisi.HerhangiBirUIAcikMi) return false;

        if (!ParaYeterliMi(bitki.SulamaBedeli)) return false;

        aktifBitki = bitki;
        gecenSure = 0f;
        SulamaAktif = true;

        KovayiGoster();
        OnSulamaBasladi?.Invoke();
        OnSulamaIlerleme?.Invoke(0f);

        return true;
    }

    void Tamamla()
    {
        BitkiBuyumeSistemi bitki = aktifBitki;

        // Ödeme SON ANDA yapılır: yarıda bırakılan sulama para yakmaz
        if (bitki != null && ParaOde(bitki.SulamaBedeli))
            bitki.Sulandi();

        Bitir();
        OnSulamaBitti?.Invoke();
    }

    void Iptal(string sebep)
    {
        Bitir();
        OnSulamaIptal?.Invoke();
    }

    void Bitir()
    {
        SulamaAktif = false;
        aktifBitki = null;
        gecenSure = 0f;

        if (kova != null)
            kova.Bitir();

        StartCoroutine(KovayiGecikmeliGizle());
    }

    private IEnumerator KovayiGecikmeliGizle()
    {
        // Kova yumuşakça geri dönsün, sonra gizlensin
        float bekle = kovaGizlemeGecikmesi + (kova != null ? kova.GeriDonusSuresi : 0f);

        if (bekle > 0f)
            yield return new WaitForSeconds(bekle);

        // Bu sırada yeni bir sulama başladıysa kovayı gizleme
        if (!SulamaAktif)
            KovayiGizle();
    }

    // ================== PARA ==================

    bool ParaYeterliMi(int bedel)
    {
        if (bedel <= 0) return true;

        if (EkonomiYoneticisi.Instance == null)
        {
            Debug.LogWarning("[Sulama] EkonomiYoneticisi bulunamadı, sulama ücretsiz yapılacak.");
            return true;
        }

        if (!EkonomiYoneticisi.Instance.YeterliMi(bedel))
        {
            Debug.Log($"[Sulama] Yetersiz para! Gereken: {bedel}, mevcut: {EkonomiYoneticisi.Instance.Para}");

            if (yetersizParaSesi != null && SesYoneticisi.Instance != null)
                SesYoneticisi.Instance.SesCal2D(yetersizParaSesi);

            return false;
        }

        return true;
    }

    bool ParaOde(int bedel)
    {
        if (bedel <= 0) return true;
        if (EkonomiYoneticisi.Instance == null) return true;

        if (!EkonomiYoneticisi.Instance.Harca(bedel))
        {
            // Sulama sırasında para başka yere harcanmış olabilir
            Debug.Log($"[Sulama] Sulama bitti ama para yetmedi! Gereken: {bedel}");

            if (yetersizParaSesi != null && SesYoneticisi.Instance != null)
                SesYoneticisi.Instance.SesCal2D(yetersizParaSesi);

            return false;
        }

        return true;
    }

    // ================== KOVA ==================

    /// <summary>
    /// Kovayı hedef noktaya ışınlar (parent + local konum/açı/ölçek).
    /// Hem oyun başında hem her sulamada çağrılır, böylece kova asla kaymaz.
    /// </summary>
    void KovayiYerlestir()
    {
        if (kovaOrnegi == null || kovaPozisyonu == null) return;

        Transform t = kovaOrnegi.transform;
        t.SetParent(kovaPozisyonu, false);
        t.localPosition = pozisyonOfseti;
        t.localRotation = Quaternion.Euler(rotasyonOfseti);

        if (kovaOlcegi != Vector3.zero)
            t.localScale = kovaOlcegi;
    }

    void KovayiGoster()
    {
        if (kovaOrnegi == null) return;

        KovayiYerlestir();
        kovaOrnegi.SetActive(true);

        if (kova != null)
            kova.Baslat();
    }

    void KovayiGizle()
    {
        if (kovaOrnegi != null)
            kovaOrnegi.SetActive(false);
    }

    // ===== EDITOR TEST =====
    // Play modunda bu scriptin sağ üst köşesindeki üç noktadan çağırabilirsin.

    [ContextMenu("Test - Kovayı Göster")]
    void TestKovayiGoster() => KovayiGoster();

    [ContextMenu("Test - Kovayı Gizle")]
    void TestKovayiGizle() => KovayiGizle();
}
