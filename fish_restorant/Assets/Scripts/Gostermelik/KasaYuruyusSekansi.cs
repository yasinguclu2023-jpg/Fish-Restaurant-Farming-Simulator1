using UnityEngine;

/// <summary>
/// Karakterin üstüne eklenir. Dýþarýdan SekansiBaslat() çaðrýlýnca:
/// Idle'dan çýkar -> belirlenen açýda (varsayýlan saða) döner -> düz yürür -> durur.
/// Steam sayfasý çekimi için tasarlandý; mesafe veya süre ile durdurulabilir.
/// </summary>
public class KasaYuruyusSekansi : MonoBehaviour
{
    public enum DurmaModu { Sure, Mesafe }

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [Tooltip("Animator'daki yürüme bool parametresinin adý (Idle <-> Walk geçiþi)")]
    [SerializeField] private string yurumeBoolParametresi = "isWalking";

    [Header("Dönüþ Ayarlarý")]
    [Tooltip("Mevcut yöne göre dönülecek açý. Sað = +90, Sol = -90")]
    [SerializeField] private float donusAcisi = 90f;
    [Tooltip("Dönüþ hýzý (derece / saniye)")]
    [SerializeField] private float donusHizi = 180f;

    [Header("Yürüyüþ Ayarlarý")]
    [Tooltip("Yürüme hýzý (birim / saniye)")]
    [SerializeField] private float yurumeHizi = 2f;
    [SerializeField] private DurmaModu durmaModu = DurmaModu.Sure;
    [Tooltip("DurmaModu = Sure ise: kaç saniye yürüsün")]
    [SerializeField] private float yurumeSuresi = 3f;
    [Tooltip("DurmaModu = Mesafe ise: kaç birim yürüsün")]
    [SerializeField] private float yurumeMesafesi = 5f;
    [Tooltip("Açýksa: yürüme bitince karakter sahneden silinir.")]
    [SerializeField] private bool bitinceYokEt = false;

    [Header("Ses (Opsiyonel)")]
    [SerializeField] private AudioSource sesKaynagi;
    [SerializeField] private AudioClip yurumeSesi;

    [Header("Sekans Sýrasýnda Kapatýlacaklar (Opsiyonel)")]
    [Tooltip("Örn. karakter oyuncuysa FPS controller script'ini buraya ata; sekans bitince geri açýlýr.")]
    [SerializeField] private MonoBehaviour[] sekansSirasindaKapat;

    private enum Durum { Bosta, Donuyor, Yuruyor }
    private Durum durum = Durum.Bosta;

    private Quaternion hedefRotasyon;
    private float yurunenMesafe;
    private float gecenSure;

    /// <summary>Sekans þu an çalýþýyor mu?</summary>
    public bool SekansAktif => durum != Durum.Bosta;

    void Reset()
    {
        // Component eklenince otomatik dene
        animator = GetComponent<Animator>();
    }

    /// <summary>Hareket sekansýný baþlatýr. Zaten çalýþýyorsa tekrar tetiklemez.</summary>
    public void SekansiBaslat()
    {
        if (durum != Durum.Bosta) return;

        // Mevcut yöne göre dönüþ hedefini hesapla
        hedefRotasyon = transform.rotation * Quaternion.Euler(0f, donusAcisi, 0f);

        // Varsa diðer kontrolcüleri kapat (oyuncu hareketi/mouse look çakýþmasýn)
        KontrolculeriAyarla(false);

        // Animator'ý yürümeye geçir
        if (animator != null && !string.IsNullOrEmpty(yurumeBoolParametresi))
            animator.SetBool(yurumeBoolParametresi, true);

        // Yürüme sesi (loop)
        if (sesKaynagi != null && yurumeSesi != null)
        {
            sesKaynagi.clip = yurumeSesi;
            sesKaynagi.loop = true;
            sesKaynagi.Play();
        }

        yurunenMesafe = 0f;
        gecenSure = 0f;
        durum = Durum.Donuyor;
    }

    /// <summary>Sekansý zorla durdurur (gerekirse).</summary>
    public void SekansiDurdur() => SekansiBitir();

    void Update()
    {
        switch (durum)
        {
            case Durum.Donuyor:
                DonusGuncelle();
                break;
            case Durum.Yuruyor:
                YuruyusGuncelle();
                break;
        }
    }

    void DonusGuncelle()
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, hedefRotasyon, donusHizi * Time.deltaTime);

        // Hedefe ulaþýnca yürüyüþe geç
        if (Quaternion.Angle(transform.rotation, hedefRotasyon) < 0.1f)
        {
            transform.rotation = hedefRotasyon;
            durum = Durum.Yuruyor;
        }
    }

    void YuruyusGuncelle()
    {
        float adim = yurumeHizi * Time.deltaTime;
        transform.position += transform.forward * adim;

        yurunenMesafe += adim;
        gecenSure += Time.deltaTime;

        bool bitti = durmaModu == DurmaModu.Sure
            ? gecenSure >= yurumeSuresi
            : yurunenMesafe >= yurumeMesafesi;

        if (bitti) SekansiBitir();
    }

    void SekansiBitir()
    {
        if (durum == Durum.Bosta) return;

        durum = Durum.Bosta;

        // Animator'ý tekrar Idle'a
        if (animator != null && !string.IsNullOrEmpty(yurumeBoolParametresi))
            animator.SetBool(yurumeBoolParametresi, false);

        // Sesi durdur
        if (sesKaynagi != null && sesKaynagi.isPlaying)
            sesKaynagi.Stop();

        // Kontrolcüleri geri aç
        KontrolculeriAyarla(true);

        // Opsiyonel: karakteri sahneden sil
        if (bitinceYokEt)
            Destroy(gameObject);
    }

    void KontrolculeriAyarla(bool aktif)
    {
        if (sekansSirasindaKapat == null) return;
        for (int i = 0; i < sekansSirasindaKapat.Length; i++)
            if (sekansSirasindaKapat[i] != null)
                sekansSirasindaKapat[i].enabled = aktif;
    }

    void OnValidate()
    {
        if (yurumeHizi < 0) yurumeHizi = 0;
        if (donusHizi < 1) donusHizi = 1;
        if (yurumeSuresi < 0) yurumeSuresi = 0;
        if (yurumeMesafesi < 0) yurumeMesafesi = 0;
    }
}