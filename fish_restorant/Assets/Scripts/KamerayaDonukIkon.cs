using UnityEngine;

/// <summary>
/// Dünyada duran ve sürekli kameraya dönen ikon/UI (susuz bitki göstergesi vb.).
///
/// Bu scriptin UPDATE'İ YOKTUR. Tüm ikonlar tek bir IkonYoneticisi tarafından döndürülür;
/// 20 ikon için 20 ayrı Update yerine 1 Update çalışır.
///
/// KURULUM:
/// 1. World Space Canvas'a (veya SpriteRenderer'lı objeye) ekle.
/// 2. Gizlenme Mesafesi'ni gir (0 = hep görünür).
/// 3. Bitki prefabında BitkiBuyumeSistemi > "Susuz İkonu" alanına bu objeyi sürükle.
///    Böylece susuz olunca açılır, sulanınca kapanır.
/// </summary>
public class KamerayaDonukIkon : MonoBehaviour
{
    [Header("Görünürlük")]
    [Tooltip("AÇIK: İkon sadece oyuncu bu nesneye BAKARKEN görünür.\n" +
             "KAPALI: Mesafe içindeyken her zaman görünür.")]
    [SerializeField] private bool sadeceBakincaGoster = true;

    [Tooltip("İkonun ait olduğu nesne (bitki kökü). Boş bırakılırsa parent kullanılır. " +
             "Oyuncu bu nesneye veya child'larına bakınca ikon görünür.")]
    [SerializeField] private Transform sahip;

    [Header("Mesafe")]
    [Tooltip("Bu mesafeden UZAKTAYSA ikon gizlenir. 0 = mesafe kontrolü yok, hep görünür.")]
    [SerializeField] private float gizlenmeMesafesi = 15f;

    [Tooltip("Histerezis payı. Sınırda öne arkaya sallanınca ikon açılıp kapanmasın diye " +
             "kapanma mesafesi = Gizlenme Mesafesi + bu pay olur.")]
    [SerializeField] private float gizlenmePayi = 1f;

    [Header("Dönüş")]
    [Tooltip("İkon arkasını dönüyorsa işaretle.")]
    [SerializeField] private bool tersCevir = false;

    [Tooltip("AÇIK: Prefabta verdiğin eğim (X ve Z açıları) korunur, ikon sadece sağa-sola döner. " +
             "Yani canvas'ı yatırdıysan yatık kalır.\n" +
             "KAPALI: İkon her zaman dimdik durur, prefabtaki eğim silinir.")]
    [SerializeField] private bool egimiKoru = true;

    // Cache
    private Transform _t;
    private Canvas _canvas;
    private Renderer[] _rendererlar;
    private bool _gorunur = true;

    // Prefabta verilen eğim (X/Z). Y bileşeni atılır, onu billboard belirler.
    private Quaternion _egim = Quaternion.identity;

    /// <summary>Yöneticinin her frame Transform aramaması için cache'lenmiş transform.</summary>
    public Transform Govde => _t;

    /// <summary>Açılma mesafesinin karesi (karekök hesabı yapılmasın diye).</summary>
    public float AcilmaMesafesiKare { get; private set; }

    /// <summary>Kapanma mesafesinin karesi (histerezis dahil).</summary>
    public float KapanmaMesafesiKare { get; private set; }

    /// <summary>Mesafe kontrolü kapalı mı? (Gizlenme Mesafesi = 0)</summary>
    public bool MesafeKontrolsuz => gizlenmeMesafesi <= 0f;

    public bool Gorunur => _gorunur;

    /// <summary>Sadece bakılınca mı görünsün?</summary>
    public bool SadeceBakincaGoster => sadeceBakincaGoster;

    void Awake()
    {
        _t = transform;

        if (sahip == null)
            sahip = _t.parent != null ? _t.parent : _t;

        _canvas = GetComponent<Canvas>();
        if (_canvas == null)
            _rendererlar = GetComponentsInChildren<Renderer>(true);

        MesafeleriHesapla();
        EgimiOku();
    }

    /// <summary>
    /// Başlangıçtaki (prefabta verilen) eğimi kaydeder. Y bileşeni atılır çünkü
    /// sağa-sola dönüşü billboard belirler; X/Z eğimi olduğu gibi korunur.
    /// Bir kez, ilk dönüşten ÖNCE çalışmalıdır.
    /// </summary>
    void EgimiOku()
    {
        Vector3 aci = _t.rotation.eulerAngles;
        _egim = Quaternion.Euler(aci.x, 0f, aci.z);
    }

    /// <summary>
    /// Play modunda ikonu elle eğdiysen bunu çağırarak yeni eğimi kalıcı hale getirebilirsin.
    /// (Component'in sağ üstündeki ⋮ menüsünden.)
    /// </summary>
    [ContextMenu("Eğimi Yeniden Oku")]
    public void EgimiYenidenOku()
    {
        EgimiOku();
    }

    void OnValidate()
    {
        MesafeleriHesapla();
    }

    void MesafeleriHesapla()
    {
        float acilma = Mathf.Max(0f, gizlenmeMesafesi);
        float kapanma = acilma + Mathf.Max(0f, gizlenmePayi);

        AcilmaMesafesiKare = acilma * acilma;
        KapanmaMesafesiKare = kapanma * kapanma;
    }

    void OnEnable()
    {
        // "Sadece bakınca göster" modunda GİZLİ başla: yönetici bakış kontrolünü yapıp
        // gerekirse aynı karede açar. Böylece ikon bir kare bile boşuna görünmez.
        bool baslangicGorunur = !sadeceBakincaGoster;

        _gorunur = baslangicGorunur;
        Uygula(baslangicGorunur);

        IkonYoneticisi.Kaydet(this);
    }

    void OnDisable()
    {
        IkonYoneticisi.Sil(this);
    }

    /// <summary>
    /// Oyuncunun baktığı nesne bu ikonun sahibi mi? (Sahibin child'ına bakmak da sayılır.)
    /// </summary>
    public bool BakiliyorMu(GameObject bakilanObje)
    {
        if (bakilanObje == null || sahip == null) return false;

        // IsChildOf kendisini de kapsar: sahibe veya alt mesh'ine bakmak yeterli
        return bakilanObje.transform.IsChildOf(sahip);
    }

    /// <summary>
    /// Y ekseninde kameraya döner (ikon dik durur, yan yatmaz).
    /// Sadece kamera POZİSYONUNA bağlıdır; kamera yerinde dönerse hesap değişmez.
    /// </summary>
    public void KamerayaDon(Vector3 kameraPozisyonu)
    {
        Vector3 yon = _t.position - kameraPozisyonu;
        yon.y = 0f;

        if (yon.sqrMagnitude < 0.0001f) return;

        if (tersCevir) yon = -yon;

        // yon.y = 0 olduğu için bu SAF bir Y (yaw) dönüşüdür
        Quaternion yatayDonus = Quaternion.LookRotation(yon);

        // Önce kameraya dön, sonra prefabtaki eğimi üzerine uygula
        _t.rotation = egimiKoru ? yatayDonus * _egim : yatayDonus;
    }

    /// <summary>
    /// Çizimi açar/kapatır. SetActive KULLANILMAZ: obje aktif kalır, yöneticideki kaydı bozulmaz
    /// ve canvas yeniden kurulmaz (rebuild maliyeti oluşmaz).
    /// </summary>
    public void GorunurlukAyarla(bool deger)
    {
        if (_gorunur == deger) return;

        _gorunur = deger;
        Uygula(deger);
    }

    void Uygula(bool deger)
    {
        if (_canvas != null)
        {
            _canvas.enabled = deger;
            return;
        }

        if (_rendererlar == null) return;

        for (int i = 0; i < _rendererlar.Length; i++)
        {
            if (_rendererlar[i] != null)
                _rendererlar[i].enabled = deger;
        }
    }
}
