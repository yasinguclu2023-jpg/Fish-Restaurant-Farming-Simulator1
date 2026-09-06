using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Siparis akisinin orkestrasi (Singleton).
/// Musteri <-> Siparis <-> Board <-> Masa <-> Sira arasini baglar.
///
/// Akis:
/// 1. Ondeki musteri SiparisBaslat() cagirir -> siparis uretilir, bos masa ayrilir,
///    ana board'da gosterilir (masa no = siparis no).
/// 2. Oyuncu ana board'a bakip sol tiklar -> AktifSiparisiGonder():
///    siparis 2 mutfak ekranina yazilir, ana board temizlenir, musteri siradan cikar
///    (sira ilerler) ve masasina yurur. Masa yoksa yer acilana kadar bekler.
///
/// KURULUM: Sahnede bos GameObject + bu script. Referanslari ata:
/// uretici, anaBoard (tiklanabilir), mutfakEkranlari (2 adet).
/// </summary>
public class SiparisYonetimi : MonoBehaviour
{
    public static SiparisYonetimi Instance { get; private set; }

    [Header("Referanslar")]
    [SerializeField] private SiparisUretici uretici;
    [Tooltip("Gelen siparisin gosterildigi ana board (tiklanabilir olmali).")]
    [SerializeField] private SiparisGosterici anaBoard;
    [Tooltip("Siparisin gonderilecegi mutfak monitorleri (2 adet).")]
    [SerializeField] private MutfakEkrani[] mutfakEkranlari;

    [Header("Mutfak Kapasitesi")]
    [Tooltip("Monitorlerde ayni anda gosterilebilecek maksimum siparis fisi.")]
    [SerializeField] private int maxMutfakSiparisi = 6;

    [Header("Ses (opsiyonel)")]
    [Tooltip("Siparis alininca (register tik) calan ses.")]
    [SerializeField] private SesVerisi siparisAlmaSesi;

    private int mutfakSiparisSayisi;

    // Aktif (board'da bekleyen, henuz gonderilmemis) siparis
    private MusteriAI aktifMusteri;
    private Siparis aktifSiparis;
    private Transform aktifKoltuk;

    // Siparisi alindi ama bos masa yoktu; yer acilinca yollanacak (nadir durum)
    private MusteriAI masaBekleyenMusteri;
    private Siparis masaBekleyenSiparis;

    // masaNo -> o masadaki aktif siparis (servis eslestirmesi icin)
    private readonly Dictionary<int, Siparis> masaSiparisleri = new Dictionary<int, Siparis>();
    // masaNo -> o masada oturan musteri (dogru servis olunca yemesi icin)
    private readonly Dictionary<int, MusteriAI> masaMusterileri = new Dictionary<int, MusteriAI>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// GUN YENIDEN BASLARKEN cagrilir: ana board'u ve mutfak monitorlerini temizler,
    /// bekleyen/aktif siparisleri ve masa kayitlarini sifirlar.
    /// </summary>
    public void Sifirla()
    {
        if (anaBoard != null) anaBoard.Temizle();

        if (mutfakEkranlari != null)
            for (int i = 0; i < mutfakEkranlari.Length; i++)
                if (mutfakEkranlari[i] != null) mutfakEkranlari[i].TumFisleriKaldir();

        mutfakSiparisSayisi = 0;

        aktifMusteri = null;
        aktifSiparis = null;
        aktifKoltuk = null;

        masaBekleyenMusteri = null;
        masaBekleyenSiparis = null;

        masaSiparisleri.Clear();
        masaMusterileri.Clear();
    }

    /// <summary>Su an yeni siparis alinabilir mi? (Board bos mu)</summary>
    public bool SiparisAlinabilirMi() => aktifMusteri == null;

    /// <summary>Ana board'da gosterilen aktif siparis var mi?</summary>
    public bool EkrandaSiparisVar => aktifMusteri != null;

    /// <summary>Mutfak monitorleri dolu mu? (6 fis) Doluysa yeni siparis gonderilemez.</summary>
    public bool MutfakDoluMu => mutfakSiparisSayisi >= maxMutfakSiparisi;

    /// <summary>Ondeki musteri tarafindan cagrilir: siparis uret + masa ayir + board'a yaz.</summary>
    public void SiparisBaslat(MusteriAI musteri)
    {
        if (musteri == null || uretici == null) return;
        if (aktifMusteri != null) return; // board dolu

        aktifMusteri = musteri;

        // FRAGMAN: bu musterinin senaryosu varsa sabit siparis + sabit masa kullan
        FragmanSistemi.FragmanMusteri senaryo = FragmanSistemi.Instance != null
            ? FragmanSistemi.Instance.SenaryoAl(musteri)
            : null;

        aktifSiparis = senaryo != null
            ? uretici.SiparisOlusturAdaGore(senaryo.balikAdi, senaryo.digerAdlar)
            : uretici.SiparisUret();

        Transform koltuk = null;
        int no = -1;
        bool masaAyrildi = false;

        // Senaryo varsa once BELIRTILEN masa/koltuk denenir
        if (senaryo != null && MasaYonetimi.Instance != null)
            masaAyrildi = MasaYonetimi.Instance.YerAyirBelirli(senaryo.masaIndex, senaryo.koltukIndex, out koltuk, out no);

        // Senaryo yoksa ya da belirtilen masa uygun degilse: normal rastgele masa
        if (!masaAyrildi && MasaYonetimi.Instance != null)
            masaAyrildi = MasaYonetimi.Instance.YerAyir(out koltuk, out no);

        if (masaAyrildi)
        {
            aktifKoltuk = koltuk;
            aktifSiparis.masaNo = no;
        }
        else
        {
            aktifKoltuk = null;
            aktifSiparis.masaNo = -1;
        }

        // Senaryo kullanildi, kayittan dus
        if (senaryo != null && FragmanSistemi.Instance != null)
            FragmanSistemi.Instance.MusteriyiUnut(musteri);

        if (anaBoard != null) anaBoard.Goster(aktifSiparis);
    }

    /// <summary>Ana board'a tiklayinca cagrilir: siparisi mutfaga gonder, musteriyi yolla.</summary>
    public void AktifSiparisiGonder()
    {
        if (aktifMusteri == null || aktifSiparis == null) return;

        // Mutfak doluysa (6 fis) gonderme; musteri board'da bekler, sira ilerlemez.
        if (MutfakDoluMu) return;

        // Her monitore birer fis ekle
        if (mutfakEkranlari != null)
        {
            for (int i = 0; i < mutfakEkranlari.Length; i++)
                if (mutfakEkranlari[i] != null) mutfakEkranlari[i].FisEkle(aktifSiparis);
        }
        mutfakSiparisSayisi++;

        // Siparis alindi sesi
        if (siparisAlmaSesi != null && SesYoneticisi.Instance != null)
            SesYoneticisi.Instance.SesCal2D(siparisAlmaSesi);

        if (anaBoard != null) anaBoard.Temizle();

        // Siradan cikar -> sira bir ileri kayar
        if (SiraYonetimi.Instance != null)
            SiraYonetimi.Instance.SistemdenAyril(aktifMusteri);

        if (aktifKoltuk != null)
        {
            if (aktifSiparis.masaNo > 0)
            {
                masaSiparisleri[aktifSiparis.masaNo] = aktifSiparis;
                masaMusterileri[aktifSiparis.masaNo] = aktifMusteri;
            }
            aktifMusteri.MasayaGit(aktifKoltuk, aktifSiparis.masaNo);
        }
        else
        {
            // Bos masa yoktu: yer acilana kadar bekle
            masaBekleyenMusteri = aktifMusteri;
            masaBekleyenSiparis = aktifSiparis;
            aktifMusteri.MasaBekle();
        }

        aktifMusteri = null;
        aktifSiparis = null;
        aktifKoltuk = null;
    }

    /// <summary>
    /// En eski mutfak siparisini (her iki monitorden) kaldirir, kapasiteden bir yer acar.
    /// Simdilik test tusu cagiriyor; ileride "yemek servis edildi" buna baglanacak.
    /// </summary>
    public void EnEskiyiKaldir()
    {
        if (mutfakSiparisSayisi <= 0) return;

        if (mutfakEkranlari != null)
        {
            for (int i = 0; i < mutfakEkranlari.Length; i++)
                if (mutfakEkranlari[i] != null) mutfakEkranlari[i].EnEskiFisiKaldir();
        }
        mutfakSiparisSayisi = Mathf.Max(0, mutfakSiparisSayisi - 1);
    }

    /// <summary>Belirtilen masadaki aktif siparisi dondurur (yoksa null). Servis eslestirmesi icin.</summary>
    public Siparis MasaSiparisiniAl(int masaNo)
    {
        masaSiparisleri.TryGetValue(masaNo, out Siparis s);
        return s;
    }

    /// <summary>Belirli bir siparisi (servis edilince) tum monitorlerden kaldirir, kapasite acilir.</summary>
    public void SiparisiKaldir(Siparis siparis)
    {
        if (siparis == null) return;

        bool kaldirildi = false;
        if (mutfakEkranlari != null)
        {
            for (int i = 0; i < mutfakEkranlari.Length; i++)
                if (mutfakEkranlari[i] != null && mutfakEkranlari[i].FisKaldir(siparis))
                    kaldirildi = true;
        }

        if (kaldirildi) mutfakSiparisSayisi = Mathf.Max(0, mutfakSiparisSayisi - 1);
    }

    /// <summary>Belirtilen masada oturan musteriyi dondurur (yoksa null).</summary>
    public MusteriAI MasaMusterisiniAl(int masaNo)
    {
        masaMusterileri.TryGetValue(masaNo, out MusteriAI m);
        return m;
    }

    /// <summary>Masadaki siparisi tamamlanmis say (kayittan cikar). Yeme/servis sonrasi icin.</summary>
    public void MasaSiparisiTamamla(int masaNo)
    {
        masaSiparisleri.Remove(masaNo);
        masaMusterileri.Remove(masaNo);
    }

    /// <summary>
    /// SIRA SABRI doldu: bu musteri su an board'da siparis gosteriyorsa iptal et
    /// (board temizle + ayrilan masayi birak). Arkadaki musteriyse hicbir sey yapmaz.
    /// </summary>
    public void SiparisiIptalEt(MusteriAI musteri)
    {
        if (musteri == null || aktifMusteri != musteri) return;

        if (aktifKoltuk != null && MasaYonetimi.Instance != null)
            MasaYonetimi.Instance.YeriBosalt(aktifKoltuk);

        if (anaBoard != null) anaBoard.Temizle();

        aktifMusteri = null;
        aktifSiparis = null;
        aktifKoltuk = null;
    }

    /// <summary>
    /// MASA SABRI doldu: o masadaki siparisi mutfak monitorlerinden kaldir + kayittan dus.
    /// </summary>
    public void MasaSiparisiniIptalEt(int masaNo)
    {
        if (masaSiparisleri.TryGetValue(masaNo, out Siparis s) && s != null)
            SiparisiKaldir(s);

        masaSiparisleri.Remove(masaNo);
        masaMusterileri.Remove(masaNo);
    }

    void Update()
    {
        // Masa bekleyen varsa ve yer acildiysa yolla (nadir)
        if (masaBekleyenMusteri != null && MasaYonetimi.Instance != null &&
            MasaYonetimi.Instance.YerAyir(out Transform koltuk, out int no))
        {
            int gosterilecekNo = no;
            if (masaBekleyenSiparis != null)
            {
                masaBekleyenSiparis.masaNo = no;
                if (no > 0)
                {
                    masaSiparisleri[no] = masaBekleyenSiparis;
                    masaMusterileri[no] = masaBekleyenMusteri;
                }
            }

            masaBekleyenMusteri.MasayaGit(koltuk, gosterilecekNo);
            masaBekleyenMusteri = null;
            masaBekleyenSiparis = null;
        }
    }
}
