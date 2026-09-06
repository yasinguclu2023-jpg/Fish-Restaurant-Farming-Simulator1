using System.Collections;
using UnityEngine;

/// <summary>
/// Kamera altındaki sulama kovası (watering can) modeline eklenir.
///
/// SADECE GÖRSELDEN sorumludur: eğilir, su particle'ını akıtır, akış sesini çalar.
/// Hangi bitkinin sulandığını bilmez; BitkiSulamaSistemi tarafından Baslat()/Bitir() ile sürülür.
///
/// BASILI TUTMA MANTIĞI:
/// Baslat() -> kova eğilir, gecikme sonrası su AKMAYA BAŞLAR ve akmaya devam eder.
/// Bitir()  -> su kesilir, kova yumuşakça eski haline döner.
/// Yani sulama süresini oyuncunun tuşu ne kadar tuttuğu belirler.
///
/// KURULUM:
/// 1. Kova modeline (veya prefab'ına) ekle.
/// 2. "Eğilen Nesne" boş kalırsa bu objenin kendisi eğilir.
/// 3. Su particle'ını ata: Play On Awake KAPALI, Looping AÇIK olsun.
/// </summary>
public class SulamaKovasi : MonoBehaviour
{
    [Header("Eğilme (Rotasyon) Ayarları")]
    [Tooltip("Eğilecek nesne. Boş bırakılırsa bu scriptin olduğu nesne eğilir.")]
    [SerializeField] private Transform egilenNesne;

    [Tooltip("Hangi eksende eğilsin. X için (1,0,0), Y için (0,1,0), Z için (0,0,1).")]
    [SerializeField] private Vector3 egilmeEkseni = new Vector3(1f, 0f, 0f);

    [Tooltip("Kaç DERECE eğilsin. Eksi değer ters yöne eğer.")]
    [SerializeField] private float egilmeAcisi = 45f;

    [Tooltip("Eğilme süresi (saniye). Eğilme ve geri dönüş için ayrı ayrı geçerlidir.")]
    [SerializeField] private float egilmeSuresi = 0.4f;

    [Header("Su Particle Ayarları")]
    [Tooltip("Açılıp kapanacak su particle'ı.")]
    [SerializeField] private ParticleSystem suParticle;

    [Tooltip("Sulama başladıktan KAÇ saniye SONRA su akmaya başlasın.")]
    [SerializeField] private float particleAcilmaGecikmesi = 0.4f;

    [Header("Akış Sesi (Loop)")]
    [Tooltip("Su akarken çalacak ses. Kova kafanın önünde olduğu için 2D çalınır.")]
    [SerializeField] private SesVerisi akisSesi;

    [Tooltip("Ses kapanırken yumuşak kısılma süresi (saniye). 0 = anında kes.")]
    [SerializeField] private float sesKapanmaSuresi = 0.25f;

    private AudioSource _akisKaynagi;
    private Quaternion _baslangicRot;
    private Coroutine _donusCo;
    private Coroutine _particleCo;
    private Coroutine _sesCo;

    /// <summary>Kova şu an eğik / eğilmekte mi?</summary>
    public bool Calisiyor { get; private set; }

    /// <summary>Bitir() sonrası kovanın eski haline dönmesi kaç saniye sürer.</summary>
    public float GeriDonusSuresi => Mathf.Max(egilmeSuresi, 0.01f);

    void Awake()
    {
        if (egilenNesne == null)
            egilenNesne = transform;

        _baslangicRot = egilenNesne.localRotation;

        AkisKaynagiHazirla();
    }

    void AkisKaynagiHazirla()
    {
        if (akisSesi == null || !akisSesi.GecerliMi) return;

        _akisKaynagi = gameObject.AddComponent<AudioSource>();
        _akisKaynagi.playOnAwake = false;
        _akisKaynagi.loop = true;
        _akisKaynagi.spatialBlend = 0f; // Kova kameranın önünde -> 2D
        _akisKaynagi.clip = akisSesi.RastgeleKlipAl();
    }

    /// <summary>
    /// Sulamayı başlatır: kova eğilir, gecikme sonrası su akmaya başlar ve AKMAYA DEVAM EDER.
    /// Durdurmak için Bitir() çağrılmalıdır.
    /// </summary>
    public void Baslat()
    {
        if (!gameObject.activeInHierarchy) return;

        RutinleriDurdur();

        // Referans rotasyonu BAŞLARKEN oku: obje her açıldığında localRotation
        // yeniden ayarlandığı için Awake'teki değer bayatlıyor ve açı zıplaması oluyordu.
        _baslangicRot = egilenNesne.localRotation;

        Calisiyor = true;
        _donusCo = StartCoroutine(EgilRutini());
        _particleCo = StartCoroutine(SuyuAcRutini());
    }

    /// <summary>
    /// Sulamayı bitirir: su kesilir, kova yumuşakça eski haline döner.
    /// (Hem tamamlanınca hem tuş bırakılınca çağrılır.)
    /// </summary>
    public void Bitir()
    {
        if (!Calisiyor) return;

        RutinleriDurdur();
        SuyuKapat();
        AkisSesiBitir();

        if (gameObject.activeInHierarchy)
            _donusCo = StartCoroutine(GeriDonRutini());
        else
            AnindaSifirla();
    }

    /// <summary>Animasyonsuz, anında başlangıç haline döndürür.</summary>
    public void AnindaSifirla()
    {
        RutinleriDurdur();
        SuyuKapat();

        if (_akisKaynagi != null)
            _akisKaynagi.Stop();

        if (egilenNesne != null)
            egilenNesne.localRotation = _baslangicRot;

        Calisiyor = false;
    }

    void OnDisable()
    {
        // Kova gizlendiğinde rutinler yarım kalmasın, kova eğik unutulmasın
        AnindaSifirla();
    }

    void RutinleriDurdur()
    {
        if (_donusCo != null) { StopCoroutine(_donusCo); _donusCo = null; }
        if (_particleCo != null) { StopCoroutine(_particleCo); _particleCo = null; }
        if (_sesCo != null) { StopCoroutine(_sesCo); _sesCo = null; }
    }

    void SuyuKapat()
    {
        // StopEmitting: yeni damla üretilmez ama havadakiler yere düşer
        if (suParticle != null)
            suParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private IEnumerator EgilRutini()
    {
        Quaternion hedefRot = _baslangicRot * Quaternion.AngleAxis(egilmeAcisi, egilmeEkseni.normalized);
        yield return YumusakDonus(egilenNesne.localRotation, hedefRot, egilmeSuresi);
        _donusCo = null;
    }

    private IEnumerator GeriDonRutini()
    {
        yield return YumusakDonus(egilenNesne.localRotation, _baslangicRot, egilmeSuresi);
        Calisiyor = false;
        _donusCo = null;
    }

    private IEnumerator YumusakDonus(Quaternion baslangic, Quaternion bitis, float sure)
    {
        float guvenliSure = Mathf.Max(sure, 0.01f);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / guvenliSure;
            float yumusak = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            egilenNesne.localRotation = Quaternion.Slerp(baslangic, bitis, yumusak);
            yield return null;
        }

        egilenNesne.localRotation = bitis;
    }

    private IEnumerator SuyuAcRutini()
    {
        if (particleAcilmaGecikmesi > 0f)
            yield return new WaitForSeconds(particleAcilmaGecikmesi);

        if (suParticle != null)
            suParticle.Play();

        AkisSesiBaslat();

        _particleCo = null;
    }

    void AkisSesiBaslat()
    {
        if (_akisKaynagi == null || akisSesi == null) return;

        if (_sesCo != null) { StopCoroutine(_sesCo); _sesCo = null; }

        _akisKaynagi.volume = akisSesi.SesSeviyesi;
        _akisKaynagi.pitch = akisSesi.RastgelePitch;
        _akisKaynagi.Play();
    }

    void AkisSesiBitir()
    {
        if (_akisKaynagi == null || !_akisKaynagi.isPlaying) return;

        if (sesKapanmaSuresi <= 0f || !gameObject.activeInHierarchy)
        {
            _akisKaynagi.Stop();
            return;
        }

        _sesCo = StartCoroutine(SesKis());
    }

    private IEnumerator SesKis()
    {
        float baslangicSes = _akisKaynagi.volume;
        float t = 0f;

        while (t < sesKapanmaSuresi)
        {
            t += Time.deltaTime;
            _akisKaynagi.volume = Mathf.Lerp(baslangicSes, 0f, t / sesKapanmaSuresi);
            yield return null;
        }

        _akisKaynagi.Stop();
        _akisKaynagi.volume = baslangicSes;
        _sesCo = null;
    }
}
