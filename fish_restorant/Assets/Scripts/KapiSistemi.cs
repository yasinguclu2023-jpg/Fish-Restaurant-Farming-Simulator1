using UnityEngine;
using System.Collections;

/// <summary>
/// Kapı açma sistemi. Kapıya bakıp SOL TIK yapınca kapı kendi Y ekseninde
/// "acilmaAcisi" kadar (varsayılan -90) yumuşak şekilde döner. Tekrar tıklanınca kapanır.
///
/// Optimize: Update içinde sadece sol tık basıldığı karede iş yapılır,
/// dönüş coroutine ile yürür (animasyon bitince coroutine kapanır, boşa Update dönmez).
///
/// KURULUM:
/// 1. Kapının PİVOTU menteşede olmalı. Değilse: boş bir GameObject oluştur (Kapi_Pivot),
///    menteşe hizasına koy, kapı meshini onun child'ı yap, bu scripti PİVOTA ekle.
/// 2. Kapı meshinde Collider olmalı ve layer'ı RaycastSistemi'nin etkilesimLayer'ında olmalı.
/// 3. Kapının tag'i, NesneAlmaSistemi'ndeki tagPozisyonlari listesindeki hiçbir tag ile
///    AYNI OLMAMALI (yoksa sağ tıkta kapı eline alınmaya çalışılır).
/// 4. Sesler: Project > sağ tık > Ses Sistemi/Ses Verisi ile KapiAcilma / KapiKapanma
///    asset'lerini oluştur ve alanlara sürükle. (Boş bırakılırsa sessiz çalışır.)
/// 5. raycastSistemi boş bırakılırsa sahnede otomatik bulunur.
/// </summary>
public class KapiSistemi : MonoBehaviour
{
    [Header("Dönüş Ayarları")]
    [Tooltip("Kapının Y ekseninde döneceği açı. -90 = sola açılır, 90 = sağa açılır.")]
    [SerializeField] private float acilmaAcisi = -90f;

    [Tooltip("Açılma/kapanma animasyonunun süresi (saniye).")]
    [SerializeField] private float acilmaSuresi = 0.5f;

    [Tooltip("Dönüşün hız eğrisi. Varsayılan yumuşak giriş-çıkış.")]
    [SerializeField] private AnimationCurve acilmaEgrisi = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Açık: Tekrar tıklayınca kapanır. Kapalı: Kapı sadece bir kez açılır.")]
    [SerializeField] private bool acKapaModu = true;

    [Header("Otomatik Kapanma")]
    [Tooltip("Açık: Kapı açıldıktan bir süre sonra kendiliğinden kapanır.")]
    [SerializeField] private bool otomatikKapanma = false;

    [Tooltip("Kapı açıldıktan kaç saniye sonra otomatik kapanacak.")]
    [SerializeField] private float kapanmaGecikmesi = 3f;

    [Header("Kilit")]
    [Tooltip("Açık: Kapı tıklamayla açılmaz. KilitAc() ile açılabilir.")]
    [SerializeField] private bool kilitli = false;

    [Header("Sesler")]
    [SerializeField] private SesVerisi acilmaSesi;
    [SerializeField] private SesVerisi kapanmaSesi;
    [Tooltip("Kilitli kapıya tıklanınca çalar (opsiyonel).")]
    [SerializeField] private SesVerisi kilitliSesi;

    [Header("Sistem")]
    [Tooltip("Boş bırakırsan sahnede otomatik bulunur.")]
    [SerializeField] private RaycastSistemi raycastSistemi;

    [Tooltip("Açık: Tab paneli (UI) açıkken kapı tıklaması çalışmaz.")]
    [SerializeField] private bool uiAcikkenCalismasin = true;

    // Rotasyon cache
    private Quaternion kapaliRotasyon;
    private Quaternion acikRotasyon;

    // Durum
    private bool acik;
    private Coroutine donusRutini;
    private Coroutine otomatikKapanmaRutini;

    // Yerleştirme sistemi tüm kapılar için ortak, tek sefer bulunur
    private static NesneYerlestirmeSistemi yerlestirmeSistemi;

    void Awake()
    {
        // Sahnedeki mevcut rotasyon "kapalı" kabul edilir
        Vector3 kapaliEuler = transform.localEulerAngles;
        kapaliRotasyon = Quaternion.Euler(kapaliEuler);
        acikRotasyon = Quaternion.Euler(kapaliEuler + new Vector3(0f, acilmaAcisi, 0f));

        if (raycastSistemi == null)
            raycastSistemi = FindObjectOfType<RaycastSistemi>();

        if (yerlestirmeSistemi == null)
            yerlestirmeSistemi = FindObjectOfType<NesneYerlestirmeSistemi>();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (raycastSistemi == null) return;

        // UI açıkken arkadaki kapı açılmasın
        if (uiAcikkenCalismasin && UIYoneticisi.HerhangiBirUIAcikMi) return;

        // Yerleştirme modundayken tıklama yerleştirmeye ait
        if (yerlestirmeSistemi != null && yerlestirmeSistemi.YerlesimModuAktif) return;

        if (!BuKapiyaBakiliyorMu()) return;

        if (kilitli)
        {
            SesCal(kilitliSesi);
            return;
        }

        Degistir();
    }

    bool BuKapiyaBakiliyorMu()
    {
        GameObject bakilan = raycastSistemi.BakilanObje;
        if (bakilan == null) return false;
        return bakilan == gameObject || bakilan.transform.IsChildOf(transform);
    }

    // ===================== PUBLIC API =====================

    /// <summary>
    /// Kapı açıksa kapatır, kapalıysa açar. (acKapaModu kapalıysa sadece açar.)
    /// </summary>
    public void Degistir()
    {
        if (acik)
        {
            if (acKapaModu) Kapat();
        }
        else
        {
            Ac();
        }
    }

    /// <summary>
    /// Kapıyı açar. Zaten açıksa hiçbir şey yapmaz.
    /// </summary>
    public void Ac()
    {
        if (acik) return;

        acik = true;
        SesCal(acilmaSesi);
        DonusBaslat(acikRotasyon, true);
    }

    /// <summary>
    /// Kapıyı kapatır. Zaten kapalıysa hiçbir şey yapmaz.
    /// </summary>
    public void Kapat()
    {
        if (!acik) return;

        acik = false;
        SesCal(kapanmaSesi);
        DonusBaslat(kapaliRotasyon, false);
    }

    /// <summary>Kilidi açar (kapı tekrar tıklanabilir olur).</summary>
    public void KilitAc() => kilitli = false;

    /// <summary>Kapıyı kilitler (tıklamayla açılmaz).</summary>
    public void KilitKapat() => kilitli = true;

    public bool Acik => acik;
    public bool Kilitli => kilitli;
    public bool AnimasyonSuruyor => donusRutini != null;

    // ===================== İÇ İŞLEYİŞ =====================

    void DonusBaslat(Quaternion hedef, bool acilis)
    {
        if (donusRutini != null) StopCoroutine(donusRutini);
        if (otomatikKapanmaRutini != null)
        {
            StopCoroutine(otomatikKapanmaRutini);
            otomatikKapanmaRutini = null;
        }

        donusRutini = StartCoroutine(DonusRutini(hedef, acilis));
    }

    IEnumerator DonusRutini(Quaternion hedef, bool acilis)
    {
        Quaternion baslangic = transform.localRotation;

        // Yarıda kesilen dönüşte kalan açı kadar süre kullan (hız sabit kalsın)
        float toplamAci = Mathf.Abs(acilmaAcisi);
        float kalanAci = Quaternion.Angle(baslangic, hedef);
        float sure = toplamAci > 0.01f ? acilmaSuresi * (kalanAci / toplamAci) : 0f;

        if (sure > 0.01f)
        {
            float gecen = 0f;
            while (gecen < sure)
            {
                gecen += Time.deltaTime;
                float t = acilmaEgrisi.Evaluate(Mathf.Clamp01(gecen / sure));
                transform.localRotation = Quaternion.Slerp(baslangic, hedef, t);
                yield return null;
            }
        }

        transform.localRotation = hedef;
        donusRutini = null;

        if (acilis && otomatikKapanma && acKapaModu)
            otomatikKapanmaRutini = StartCoroutine(OtomatikKapanmaRutini());
    }

    IEnumerator OtomatikKapanmaRutini()
    {
        yield return new WaitForSeconds(kapanmaGecikmesi);
        otomatikKapanmaRutini = null;
        Kapat();
    }

    void SesCal(SesVerisi ses)
    {
        if (ses == null || SesYoneticisi.Instance == null) return;
        SesYoneticisi.Instance.SesCal(ses, transform.position);
    }
}
