using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyuncu belirlenen bir nesneye BAKINCA, o nesneye atanmış UI objesini (Image/panel) açar.
///
/// Sprite ataması YOKTUR: Canvas altında hazırladığın Image'ı olduğu gibi gösterir.
/// Yani görselin boyutu, rengi, çerçevesi, içindeki yazısı - hepsi editörde senin ayarladığın gibi kalır.
///
/// NEDEN MERKEZÎ?
/// Her nesneye ayrı Update koymak yerine tek bir yönetici çalışır. Bakılan nesne
/// RaycastSistemi'nden okunur; nesne değişmediği sürece hiçbir arama yapılmaz.
/// Aynı anda sadece bir görsel açık olur.
///
/// ÜÇ KAYIT YÖNTEMİ (hepsi birlikte kullanılabilir):
/// 1) "Eşleşmeler" listesine SAHNE nesnesi sürükle -> o nesne için birebir eşleşme.
/// 2) "Eşleşmeler" listesine PREFAB sürükle -> o prefabın SAHNEDEKİ TÜM kopyaları için geçerli.
///    (İsim üzerinden eşleşir: "Fırın (Clone)", "Fırın (1)" hepsi "Fırın" sayılır.)
/// 3) Nesnenin üstüne <see cref="BakisGorseli"/> bileşeni ekle -> isme hiç bakmaz.
///
/// GÖSTERİLECEK UI iki şekilde verilebilir:
/// • Canvas altındaki HAZIR obje -> doğrudan açılıp kapanır.
/// • Project'teki UI PREFABI -> Canvas altına bir kez kopyalanır, sonra o kopya kullanılır.
///   (Prefab üstündeki BakisGorseli sahnedeki bir objeyi gösteremez; Unity prefablara
///    sahne referansı vermeye izin vermez. Orada UI prefabı kullan.)
/// </summary>
public class BakisGorseliYoneticisi : MonoBehaviour
{
    public enum KonumModu
    {
        /// <summary>UI sahnede koyduğun yerde sabit durur (ekranın ortası/köşesi vb.).</summary>
        Sabit,

        /// <summary>UI, bakılan nesnenin ekrandaki konumunu takip eder.</summary>
        NesneyiTakipEt
    }

    [System.Serializable]
    public class Eslesme
    {
        [Tooltip("Bakılınca görsel çıkacak nesne.\n" +
                 "• SAHNE nesnesi sürüklersen sadece o nesne için geçerli olur.\n" +
                 "• PREFAB sürüklersen o prefabın sahnedeki tüm kopyaları için geçerli olur.\n" +
                 "Her iki durumda da nesnenin child'larına bakmak da sayılır.")]
        public GameObject nesne;

        [Tooltip("Bu nesneye bakınca açılacak UI objesi.\n" +
                 "• Canvas altındaki hazır Image/panel'i sürükle, VEYA\n" +
                 "• Project'teki bir UI prefabını sürükle (Canvas altına kopyalanır).")]
        public GameObject ui;
    }

    /// <summary>Bir nesne için tutulan gösterim verisi.</summary>
    public class Kayit
    {
        /// <summary>Inspector'da verilen UI (sahne objesi veya prefab).</summary>
        public GameObject kaynak;

        /// <summary>Ekranda gerçekten kullanılan obje (kaynak prefabsa onun kopyası).</summary>
        public GameObject ornek;

        public CanvasGroup grup;
        public RectTransform rect;
        public Canvas canvas;
        public bool hazir;
    }

    [Header("Referanslar")]
    [Tooltip("UI PREFABI kullanırsan kopyaların doğacağı yer (genelde Canvas). " +
             "Boş bırakılırsa sahnedeki ilk Canvas kullanılır.")]
    [SerializeField] private Transform uiKapsayici;

    [Tooltip("Takip modu için kamera. Boş bırakılırsa Camera.main kullanılır.")]
    [SerializeField] private Camera kamera;

    [Header("Eşleşmeler")]
    [Tooltip("Nesne/prefab + gösterilecek UI çiftleri.")]
    [SerializeField] private Eslesme[] eslesmeler;

    [Header("Davranış")]
    [Tooltip("Nesneye kaç saniye baktıktan sonra görsel çıksın? 0 = anında.")]
    [SerializeField] private float gosterimGecikmesi = 0f;

    [Tooltip("Açılma/kapanma solma süresi (saniye). 0 = anında.")]
    [SerializeField] private float solmaSuresi = 0.12f;

    [Tooltip("Tab paneli gibi bir UI açıkken görsel gizlensin mi?")]
    [SerializeField] private bool uiAcikkenGizle = true;

    [Header("Konum")]
    [SerializeField] private KonumModu konumModu = KonumModu.Sabit;

    [Tooltip("Takip modunda nesnenin dünya konumuna eklenecek kayma (genelde Y ile yukarı alınır).")]
    [SerializeField] private Vector3 dunyaKaymasi = new Vector3(0f, 0.5f, 0f);

    [Tooltip("Takip modunda ekran üzerinde uygulanacak piksel kayması.")]
    [SerializeField] private Vector2 ekranKaymasi = Vector2.zero;

    [Header("Ses (Opsiyonel)")]
    [Tooltip("Görsel her açıldığında çalınacak 2D ses. Boş bırakılabilir.")]
    [SerializeField] private SesVerisi gosterimSesi;

    /// <summary>Sahnedeki aktif yönetici. BakisGorseli bileşenleri buraya kayıt olur.</summary>
    public static BakisGorseliYoneticisi Aktif { get; private set; }

    // ================== KAYIT DEFTERLERİ ==================

    // Birebir eşleşme: sahne nesneleri ve BakisGorseli bileşenleri.
    // Statik: BakisGorseli bileşenleri yönetici Awake'inden ÖNCE de kayıt olabilsin diye.
    private static readonly Dictionary<Transform, Kayit> _kayitlar = new Dictionary<Transform, Kayit>(32);

    // Prefab eşleşmesi: normalleştirilmiş isim -> kayıt. Prefabın tüm kopyaları buraya düşer.
    private static readonly Dictionary<string, Kayit> _prefabKayitlari = new Dictionary<string, Kayit>(16);

    /// <summary>
    /// Domain Reload kapalıyken (Enter Play Mode Options) statikler önceki oturumdan taşınır;
    /// yok olmuş nesnelerin kayıtları sözlükte asılı kalmasın diye temizlenir.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StatikleriSifirla()
    {
        _kayitlar.Clear();
        _prefabKayitlari.Clear();
        Aktif = null;
    }

    /// <summary>
    /// Tek bir nesneyi UI ile eşleştirir (birebir). Aynı nesne tekrar kaydedilirse üzerine yazılır.
    /// <see cref="BakisGorseli"/> bileşeni bunu kullanır.
    /// </summary>
    public static Kayit Kaydet(Transform sahip, GameObject ui)
    {
        if (sahip == null || ui == null) return null;

        if (!_kayitlar.TryGetValue(sahip, out Kayit kayit))
        {
            kayit = new Kayit();
            _kayitlar[sahip] = kayit;
        }

        KaynakAta(kayit, ui);
        return kayit;
    }

    public static void Sil(Transform sahip)
    {
        if (sahip == null) return;
        _kayitlar.Remove(sahip);
    }

    /// <summary>
    /// Bir prefabın TÜM kopyaları için UI tanımlar. Eşleşme isim üzerinden yapılır:
    /// "Fırın (Clone)" ve "Fırın (1)" -> "Fırın".
    /// </summary>
    public static Kayit PrefabKaydet(string prefabAdi, GameObject ui)
    {
        if (string.IsNullOrEmpty(prefabAdi) || ui == null) return null;

        string ad = AdiNormallestir(prefabAdi);

        if (!_prefabKayitlari.TryGetValue(ad, out Kayit kayit))
        {
            kayit = new Kayit();
            _prefabKayitlari[ad] = kayit;
        }

        KaynakAta(kayit, ui);
        return kayit;
    }

    public static void PrefabSil(string prefabAdi)
    {
        if (string.IsNullOrEmpty(prefabAdi)) return;
        _prefabKayitlari.Remove(AdiNormallestir(prefabAdi));
    }

    /// <summary>UI kaynağını atar; değiştiyse hazırlığı sıfırlar ve yönetici varsa hemen hazırlar.</summary>
    static void KaynakAta(Kayit kayit, GameObject ui)
    {
        if (kayit.kaynak != ui)
        {
            kayit.kaynak = ui;
            kayit.ornek = null;
            kayit.grup = null;
            kayit.rect = null;
            kayit.canvas = null;
            kayit.hazir = false;
        }

        if (Aktif != null)
            Aktif.Hazirla(kayit);
    }

    /// <summary>
    /// Kopya eklerini temizler: "Fırın (Clone)" -> "Fırın", "Fırın (1)" -> "Fırın".
    /// Böylece Instantiate edilen veya sahnede çoğaltılan nesneler prefabıyla eşleşir.
    /// </summary>
    static string AdiNormallestir(string ad)
    {
        int cloneIndeksi = ad.IndexOf("(Clone)", System.StringComparison.Ordinal);
        if (cloneIndeksi >= 0)
            ad = ad.Substring(0, cloneIndeksi);

        ad = ad.TrimEnd();

        // Sondaki " (1)", " (12)" gibi sayısal kopya eklerini at
        while (ad.Length > 2 && ad[ad.Length - 1] == ')')
        {
            int acilis = ad.LastIndexOf('(');
            if (acilis <= 0) break;

            bool tamamiSayi = ad.Length - acilis > 2;
            for (int i = acilis + 1; i < ad.Length - 1 && tamamiSayi; i++)
            {
                if (!char.IsDigit(ad[i])) tamamiSayi = false;
            }

            if (!tamamiSayi) break;

            ad = ad.Substring(0, acilis).TrimEnd();
        }

        return ad;
    }

    // ================== ÇALIŞMA DURUMU ==================

    private GameObject _sonBakilan;      // en son raycast sonucu (değişmediyse arama yapılmaz)
    private Kayit _aktifKayit;           // şu an bakılan nesnenin kaydı
    private Transform _aktifSahip;       // eşleşmenin yakalandığı transform (takip modu bunu kullanır)
    private Kayit _gosterilenKayit;      // ekranda açık olan kayıt
    private float _bekleme;              // gösterim gecikmesi sayacı
    private float _alfa;
    private bool _raycastUyarisiVerildi;

    void Awake()
    {
        Aktif = this;
    }

    void OnDestroy()
    {
        if (Aktif == this) Aktif = null;
    }

    void OnEnable()
    {
        EslesmeleriKaydet();

        // Yönetici geç uyandıysa, daha önce kayıt olmuş BakisGorseli'leri de hazırla
        foreach (var kayit in _kayitlar.Values) Hazirla(kayit);
        foreach (var kayit in _prefabKayitlari.Values) Hazirla(kayit);
    }

    void OnDisable()
    {
        Gizle();
        EslesmeleriSil();
    }

    /// <summary>
    /// Inspector listesini kayıt defterlerine yazar. Sahne nesnesi mi prefab mı olduğuna
    /// gameObject.scene.IsValid() ile karar verir: prefab asset'leri bir sahneye ait değildir.
    /// </summary>
    void EslesmeleriKaydet()
    {
        if (eslesmeler == null) return;

        for (int i = 0; i < eslesmeler.Length; i++)
        {
            Eslesme e = eslesmeler[i];
            if (e == null || e.nesne == null) continue;

            if (e.ui == null)
            {
                Debug.LogWarning($"[BakisGorseliYoneticisi] '{e.nesne.name}' için UI atanmamış.", this);
                continue;
            }

            if (PrefabMi(e.nesne))
                PrefabKaydet(e.nesne.name, e.ui);
            else
                Kaydet(e.nesne.transform, e.ui);
        }
    }

    void EslesmeleriSil()
    {
        if (eslesmeler == null) return;

        for (int i = 0; i < eslesmeler.Length; i++)
        {
            Eslesme e = eslesmeler[i];
            if (e == null || e.nesne == null) continue;

            if (PrefabMi(e.nesne))
                PrefabSil(e.nesne.name);
            else
                Sil(e.nesne.transform);
        }
    }

    /// <summary>Sürüklenen şey prefab asset'i mi, sahnedeki nesne mi?</summary>
    static bool PrefabMi(GameObject nesne)
    {
        // Prefab asset'leri hiçbir sahneye ait değildir -> scene.IsValid() false döner.
        return !nesne.scene.IsValid();
    }

    /// <summary>
    /// UI'ı kullanıma hazırlar: prefabsa Canvas altına bir kez kopyalar, CanvasGroup'unu
    /// bulur/ekler ve kapatır. Tekrar çağrılması zararsızdır.
    /// </summary>
    void Hazirla(Kayit kayit)
    {
        if (kayit == null || kayit.hazir || kayit.kaynak == null) return;

        kayit.hazir = true;

        GameObject hedef;

        if (PrefabMi(kayit.kaynak))
        {
            Transform ust = KapsayiciyiAl();
            if (ust == null)
            {
                Debug.LogError($"[BakisGorseliYoneticisi] '{kayit.kaynak.name}' UI prefabı için Canvas " +
                               "bulunamadı! 'UI Kapsayıcı' alanına Canvas'ını sürükle.", this);
                kayit.hazir = false;
                return;
            }

            hedef = Instantiate(kayit.kaynak, ust, false);
            hedef.name = kayit.kaynak.name;
        }
        else
        {
            hedef = kayit.kaynak;
        }

        kayit.ornek = hedef;
        kayit.rect = hedef.transform as RectTransform;
        // includeInactive: UI objesi editörde kapalı bırakılmış olabilir
        kayit.canvas = hedef.GetComponentInParent<Canvas>(true);

        kayit.grup = hedef.GetComponent<CanvasGroup>();
        if (kayit.grup == null)
            kayit.grup = hedef.AddComponent<CanvasGroup>();

        // Görsel tıklamayı engellemesin
        kayit.grup.blocksRaycasts = false;
        kayit.grup.interactable = false;
        kayit.grup.alpha = 0f;

        hedef.SetActive(false);
    }

    Transform KapsayiciyiAl()
    {
        if (uiKapsayici != null) return uiKapsayici;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null) uiKapsayici = canvas.transform;

        return uiKapsayici;
    }

    // LateUpdate: RaycastSistemi Update'te ışını attıktan SONRA çalışır, bir kare gecikme olmaz
    void LateUpdate()
    {
        GameObject bakilan = BakilaniAl();

        if (bakilan != _sonBakilan)
        {
            _sonBakilan = bakilan;
            _aktifKayit = KayitBul(bakilan, out _aktifSahip);
            _bekleme = gosterimGecikmesi;
        }

        // Eşleşen nesne yok olduysa (Destroy) kaydı bırak
        if (_aktifKayit != null && _aktifSahip == null)
            _aktifKayit = null;

        bool gosterilsin = _aktifKayit != null;

        if (gosterilsin && _bekleme > 0f)
        {
            _bekleme -= Time.deltaTime;
            gosterilsin = false;
        }

        if (gosterilsin)
            Goster(_aktifKayit);

        SolmayiIsle(gosterilsin);

        if (gosterilsin && konumModu == KonumModu.NesneyiTakipEt)
            KonumuGuncelle();
    }

    /// <summary>Raycast'in baktığı nesneyi verir. UI açıkken veya sistem yokken null döner.</summary>
    GameObject BakilaniAl()
    {
        if (uiAcikkenGizle && UIYoneticisi.HerhangiBirUIAcikMi)
            return null;

        if (RaycastSistemi.Aktif == null)
        {
            if (!_raycastUyarisiVerildi)
            {
                _raycastUyarisiVerildi = true;
                Debug.LogWarning("[BakisGorseliYoneticisi] RaycastSistemi bulunamadı! " +
                                 "Hiçbir görsel gösterilemez.", this);
            }
            return null;
        }

        return RaycastSistemi.Aktif.BakilanObje;
    }

    /// <summary>
    /// Bakılan nesneden yukarı doğru yürüyerek kayıtlı bir eşleşme arar.
    /// Her adımda önce birebir kayıt, sonra prefab ismi denenir. Böylece nesnenin
    /// alt mesh'ine/collider'ına bakmak da yeterli olur.
    /// Sadece bakılan nesne DEĞİŞTİĞİNDE çağrılır.
    /// </summary>
    Kayit KayitBul(GameObject bakilan, out Transform eslesenSahip)
    {
        eslesenSahip = null;

        if (bakilan == null) return null;
        if (_kayitlar.Count == 0 && _prefabKayitlari.Count == 0) return null;

        Transform t = bakilan.transform;

        while (t != null)
        {
            if (_kayitlar.TryGetValue(t, out Kayit kayit))
            {
                eslesenSahip = t;
                return kayit;
            }

            if (_prefabKayitlari.Count > 0 &&
                _prefabKayitlari.TryGetValue(AdiNormallestir(t.name), out Kayit prefabKaydi))
            {
                eslesenSahip = t;
                return prefabKaydi;
            }

            t = t.parent;
        }

        return null;
    }

    void Goster(Kayit kayit)
    {
        if (_gosterilenKayit == kayit) return;

        Hazirla(kayit);
        if (kayit.ornek == null) return;

        // Önceki görseli kapat (aynı UI objesini paylaşıyorlarsa kapatma)
        if (_gosterilenKayit != null && _gosterilenKayit.ornek != kayit.ornek)
            Kapat(_gosterilenKayit);

        _gosterilenKayit = kayit;
        _alfa = 0f;

        kayit.grup.alpha = 0f;
        kayit.ornek.SetActive(true);

        if (gosterimSesi != null && SesYoneticisi.Instance != null)
            SesYoneticisi.Instance.SesCal2D(gosterimSesi);
    }

    void SolmayiIsle(bool gosterilsin)
    {
        float hedef = gosterilsin ? 1f : 0f;

        if (Mathf.Approximately(_alfa, hedef))
        {
            // Tamamen kapandıysa objeyi kapat (aynı nesneye tekrar bakınca ses yeniden çalsın)
            if (hedef == 0f && _gosterilenKayit != null)
                Gizle();
            return;
        }

        _alfa = solmaSuresi > 0f
            ? Mathf.MoveTowards(_alfa, hedef, Time.deltaTime / solmaSuresi)
            : hedef;

        if (_gosterilenKayit != null && _gosterilenKayit.grup != null)
            _gosterilenKayit.grup.alpha = _alfa;

        if (_alfa <= 0f)
            Gizle();
    }

    void Gizle()
    {
        if (_gosterilenKayit != null)
            Kapat(_gosterilenKayit);

        _gosterilenKayit = null;
        _alfa = 0f;
    }

    static void Kapat(Kayit kayit)
    {
        if (kayit.grup != null) kayit.grup.alpha = 0f;
        if (kayit.ornek != null) kayit.ornek.SetActive(false);
    }

    /// <summary>Takip modunda UI'ı eşleşen nesnenin ekran konumuna taşır.</summary>
    void KonumuGuncelle()
    {
        if (_aktifSahip == null || _gosterilenKayit == null) return;

        Kayit kayit = _gosterilenKayit;
        if (kayit.rect == null) return;

        RectTransform ustRect = kayit.rect.parent as RectTransform;
        if (ustRect == null) return;

        Camera kam = KamerayiAl();
        if (kam == null) return;

        Vector3 ekranNoktasi = kam.WorldToScreenPoint(_aktifSahip.position + dunyaKaymasi);

        // Nesne kameranın arkasındaysa gizle; öne geçince solma değeri geri yüklenir
        if (ekranNoktasi.z < 0f)
        {
            kayit.grup.alpha = 0f;
            return;
        }

        kayit.grup.alpha = _alfa;

        ekranNoktasi.x += ekranKaymasi.x;
        ekranNoktasi.y += ekranKaymasi.y;

        // Overlay canvas'ta kamera null verilmeli, diğerlerinde canvas kamerası
        Camera uiKamerasi = (kayit.canvas != null && kayit.canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? kayit.canvas.worldCamera
            : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                ustRect, ekranNoktasi, uiKamerasi, out Vector2 yerelNokta))
        {
            kayit.rect.anchoredPosition = yerelNokta;
        }
    }

    Camera KamerayiAl()
    {
        if (kamera != null) return kamera;

        kamera = Camera.main;
        return kamera;
    }
}
