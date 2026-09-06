using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sahnedeki TÜM KamerayaDonukIkon'ları tek Update'te döndürür ve mesafeye göre gizler.
///
/// NEDEN MERKEZÎ?
/// Her ikona ayrı Update koymak, ikon sayısı kadar managed->native geçiş demektir.
/// 20 ikon = 20 Update yerine burada 1 Update çalışır.
///
/// EK OPTİMİZASYON:
/// Y ekseni billboard sadece kameranın POZİSYONUNA bağlıdır. Kamera yerinde dönerken
/// (mouse ile bakınırken) ikonların açısı değişmez -> kamera hareket etmediyse
/// döngüye hiç girilmez.
///
/// KURULUM: Gerekmez. Sahnede yoksa ilk ikon kaydolduğunda kendini otomatik oluşturur.
/// İstersen boş bir GameObject'e elle de ekleyebilirsin.
/// </summary>
public class IkonYoneticisi : MonoBehaviour
{
    [Header("Kamera")]
    [Tooltip("Boş bırakılırsa Camera.main kullanılır.")]
    [SerializeField] private Camera hedefKamera;

    [Tooltip("Kameranın 'hareket etti' sayılması için gereken en küçük yer değiştirme. " +
             "Altındaki hareketlerde ikonlar güncellenmez.")]
    [SerializeField] private float hareketEsigi = 0.001f;

    private static IkonYoneticisi _instance;
    private static bool _uygulamaKapaniyor;

    /// <summary>
    /// Statik alanları sıfırlar. "Enter Play Mode Options" ile Domain Reload KAPALIYSA
    /// statikler önceki oturumdan taşınır; _uygulamaKapaniyor true kalırsa yönetici bir daha
    /// hiç oluşmaz ve ikonlar yönetilmeden ekranda asılı kalırdı.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StatikleriSifirla()
    {
        _instance = null;
        _uygulamaKapaniyor = false;
    }

    private readonly List<KamerayaDonukIkon> _ikonlar = new List<KamerayaDonukIkon>(32);
    private Vector3 _oncekiKameraPoz;
    private GameObject _oncekiBakilan;
    private bool _zorlaGuncelle = true;
    private float _hareketEsigiKare;

    /// <summary>Sahnedeki yönetici. Yoksa otomatik oluşturulur.</summary>
    public static IkonYoneticisi Instance
    {
        get
        {
            if (_instance != null || _uygulamaKapaniyor) return _instance;

            _instance = FindObjectOfType<IkonYoneticisi>();

            if (_instance == null)
            {
                GameObject obj = new GameObject("IkonYoneticisi (otomatik)");
                _instance = obj.AddComponent<IkonYoneticisi>();
            }

            return _instance;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        _hareketEsigiKare = hareketEsigi * hareketEsigi;
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    void OnApplicationQuit()
    {
        // Oyun kapanırken yeni yönetici objesi oluşturulmasın
        _uygulamaKapaniyor = true;
    }

    // ================== KAYIT ==================

    public static void Kaydet(KamerayaDonukIkon ikon)
    {
        if (ikon == null || _uygulamaKapaniyor) return;

        IkonYoneticisi yonetici = Instance;
        if (yonetici == null) return;

        if (!yonetici._ikonlar.Contains(ikon))
            yonetici._ikonlar.Add(ikon);

        // Yeni ikon kamera dursa bile bir kez hizalanmalı
        yonetici._zorlaGuncelle = true;
    }

    public static void Sil(KamerayaDonukIkon ikon)
    {
        if (ikon == null || _instance == null) return;

        _instance._ikonlar.Remove(ikon);
    }

    // ================== DÖNGÜ ==================

    // LateUpdate: oyuncu/kamera hareketi bittikten SONRA çalışır, ikon titremez
    void LateUpdate()
    {
        if (_ikonlar.Count == 0) return;

        Camera kam = KamerayiAl();
        if (kam == null) return;

        Vector3 kamPoz = kam.transform.position;

        // Oyuncunun baktığı nesne: "sadece bakınca göster" modundaki ikonlar buna bakar
        GameObject bakilan = null;

        if (RaycastSistemi.Aktif != null)
        {
            bakilan = RaycastSistemi.Aktif.BakilanObje;
        }
        else if (!_raycastUyarisiVerildi)
        {
            _raycastUyarisiVerildi = true;
            Debug.LogWarning("[IkonYoneticisi] RaycastSistemi bulunamadı! " +
                             "'Sadece Bakınca Göster' modundaki ikonlar hiç görünmez.", this);
        }
        bool bakilanDegisti = bakilan != _oncekiBakilan;

        // Kamera hareket etmedi, bakılan nesne değişmedi ve yeni ikon yoksa hiçbir şey yapma
        if (!_zorlaGuncelle && !bakilanDegisti &&
            (kamPoz - _oncekiKameraPoz).sqrMagnitude < _hareketEsigiKare)
            return;

        _oncekiKameraPoz = kamPoz;
        _oncekiBakilan = bakilan;
        _zorlaGuncelle = false;

        for (int i = _ikonlar.Count - 1; i >= 0; i--)
        {
            KamerayaDonukIkon ikon = _ikonlar[i];

            if (ikon == null)
            {
                _ikonlar.RemoveAt(i);
                continue;
            }

            // 1) Bakış kontrolü: bakılmıyorsa mesafe hesabına bile girme
            if (ikon.SadeceBakincaGoster && !ikon.BakiliyorMu(bakilan))
            {
                ikon.GorunurlukAyarla(false);
                continue;
            }

            // 2) Mesafe kontrolü yoksa doğrudan döndür
            if (ikon.MesafeKontrolsuz)
            {
                ikon.GorunurlukAyarla(true);
                ikon.KamerayaDon(kamPoz);
                continue;
            }

            float mesafeKare = (ikon.Govde.position - kamPoz).sqrMagnitude;

            // Histerezis: görünürken kapanma mesafesine, gizliyken açılma mesafesine bakılır.
            // Sınırda gidip gelirken canvas'ın sürekli açılıp kapanmasını (rebuild) önler.
            bool gorunsun = ikon.Gorunur
                ? mesafeKare <= ikon.KapanmaMesafesiKare
                : mesafeKare <= ikon.AcilmaMesafesiKare;

            ikon.GorunurlukAyarla(gorunsun);

            // Gizliyse döndürmeye gerek yok
            if (gorunsun)
                ikon.KamerayaDon(kamPoz);
        }
    }

    private bool _kameraUyarisiVerildi;
    private bool _raycastUyarisiVerildi;

    /// <summary>
    /// Kamerayı bulur: elle atanan -> Camera.main (MainCamera tag'i) -> sahnedeki ilk kamera.
    /// Kamera bulunamazsa ikonlar dönmez, bu yüzden bir kez net hata basar.
    /// </summary>
    Camera KamerayiAl()
    {
        if (hedefKamera != null) return hedefKamera;

        hedefKamera = Camera.main;
        if (hedefKamera != null) return hedefKamera;

        // Sahnede "MainCamera" tag'li kamera yoksa ilk aktif kamerayı kullan
        Camera[] kameralar = FindObjectsOfType<Camera>();
        for (int i = 0; i < kameralar.Length; i++)
        {
            // El kamerası gibi overlay kameraları atla (oyuncu kamerası değiller)
            if (kameralar[i].targetTexture != null) continue;

            hedefKamera = kameralar[i];
            break;
        }

        if (hedefKamera == null && !_kameraUyarisiVerildi)
        {
            _kameraUyarisiVerildi = true;
            Debug.LogError("[IkonYoneticisi] Kamera bulunamadı! İkonlar kameraya dönemez. " +
                           "Oyuncu kamerana 'MainCamera' tag'i ver veya IkonYoneticisi'ndeki " +
                           "'Hedef Kamera' alanına sürükle.", this);
        }

        return hedefKamera;
    }

    /// <summary>Kayıtlı ikon sayısı (debug/profil için).</summary>
    public int IkonSayisi => _ikonlar.Count;
}
