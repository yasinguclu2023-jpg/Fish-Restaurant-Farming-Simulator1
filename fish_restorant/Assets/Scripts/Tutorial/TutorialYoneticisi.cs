using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ===== TUTORIAL (kaldirilabilir) =====

/// <summary>
/// TUTORIAL BEYNI (Singleton).
///
/// Gunun akisini bulur, panelleri SIRAYLA acar/kapatir. Panel icerigini bilmez;
/// sadece "sartlar saglandi mi?" diye bakar, saglaninca paneli kapatip sonrakini acar.
///
/// OYUNA HIC KARISMAZ: hicbir etkilesimi kilitlemez, imleci/oyunu durdurmaz.
/// Bu obje sahnede yoksa ya da "Aktif" tiki kapaliysa oyun normal calisir.
///
/// KURULUM:
/// 1. Sahnede bos bir GameObject olustur -> adini "TutorialYoneticisi" yap -> bu scripti ekle.
/// 2. "Aktif" tikini ac.
/// 3. "Gun Akislari" listesine satir ekle: Gun = 1, Akis = TutorialAkisi_Gun1 objesi.
/// 4. Diger referanslari bos birakabilirsin, sahnede otomatik bulunur.
///
/// KALDIRMA: Bu objeyi sil (ya da Aktif tikini kapat). Tamamen silmek icin
/// "Tutorial" klasorunu ve NesneYerlestirmeSistemi'ndeki isaretli blogu sil.
/// </summary>
public class TutorialYoneticisi : MonoBehaviour
{
    public static TutorialYoneticisi Instance { get; private set; }

    /// <summary>Bir gun ile o gunun akisini eslestirir.</summary>
    [System.Serializable]
    public class GunAkisi
    {
        [Tooltip("Kacinci gun? (1'den baslar)")]
        [Min(1)] public int gun = 1;

        [Tooltip("O gunun panel sirasini tutan TutorialAkisi objesi.")]
        public TutorialAkisi akis;
    }

    [Header("Ana Ayar")]
    [Tooltip("KAPALI ise tutorial hic calismaz, oyun normal akar.")]
    [SerializeField] private bool aktif = true;

    [Header("Gun Akislari")]
    [Tooltip("Hangi gunde hangi akis oynatilacak. Bir gun icin akis yoksa o gun tutorial cikmaz.")]
    [SerializeField] private List<GunAkisi> gunAkislari = new List<GunAkisi>();

    [Header("Bagli Sistemler (bos birakirsan otomatik bulunur)")]
    [SerializeField] private GunYoneticisi gunYoneticisi;
    [SerializeField] private NesneYerlestirmeSistemi yerlestirmeSistemi;

    [Tooltip("Duvarlarin arkasindan da gorunen ORTAK hedef ikonu (sahnedeki HedefIsareti objesi). " +
             "Her adim kendi 'Hedef Noktasi'na tasir. Bos birakirsan ikon hic cikmaz.")]
    [SerializeField] private HedefIsareti ortakIsaret;

    [Tooltip("ACIK: Ortak ikon atanmissa ve sahnede HedefIsaretiKamera yoksa, ana kameraya " +
             "OTOMATIK eklenir. Boylece kamerayi elle bulup script eklemen gerekmez. " +
             "(Sadece oyun sirasinda eklenir, sahneni kalici olarak degistirmez.)")]
    [SerializeField] private bool kamerayiOtomatikKur = true;

    [Header("Zamanlama")]
    [Tooltip("Sartlar kac saniyede bir kontrol edilsin? (her frame DEGIL - optimizasyon)")]
    [Range(0.05f, 1f)]
    [SerializeField] private float kontrolAraligi = 0.2f;

    [Tooltip("Gun basladiktan kac saniye sonra ilk panel acilsin?")]
    [Min(0f)]
    [SerializeField] private float baslangicGecikmesi = 1f;

    [Header("Gorunurluk")]
    [Tooltip("ACIK: Tab ile magaza/UI paneli acilinca tutorial paneli GIZLENIR, " +
             "UI kapaninca tekrar gorunur. Adim takibi gizliyken de devam eder.")]
    [SerializeField] private bool uiAcikkenGizle = true;

    [Header("Teshis")]
    [Tooltip("Hangi adim acildi / hangi sart saglandi, Console'a yazar.")]
    [SerializeField] private bool teshisLogu = true;

    // --- Runtime ---
    private TutorialAkisi aktifAkis;
    private int aktifIndex = -1;
    private TutorialAdimi aktifAdim;
    private float sonrakiKontrol;
    private int sonPara;
    private bool olaylaraAbone;
    private Coroutine acilisRutini;
    private bool panelGizli; // UI acik oldugu icin gecici gizlendi mi?
    private Transform sonIsaretHedefi; // ikonun su an gosterdigi nokta (bosuna guncellememek icin)

    /// <summary>Su an bir panel acik mi?</summary>
    public bool PanelAcik => aktifAdim != null;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[Tutorial] Sahnede birden fazla TutorialYoneticisi var. Fazlasi yok sayildi.", this);
            Destroy(this);
        }
    }

    void Start()
    {
        if (Instance != this) return;

        ReferanslariBul();

        if (!aktif)
        {
            TumAkislariKapat();
            return;
        }

        OlaylaraAboneOl();

        int gun = gunYoneticisi != null ? gunYoneticisi.Gun : 1;
        AkisBaslat(gun);
    }

    void OnDestroy()
    {
        OlaylardanCik();

        if (gunYoneticisi != null)
            gunYoneticisi.GunYenidenBaslatildi -= YeniGunBasladi;

        if (Instance == this) Instance = null;
    }

    void ReferanslariBul()
    {
        if (gunYoneticisi == null)
            gunYoneticisi = GunYoneticisi.Instance != null ? GunYoneticisi.Instance : FindObjectOfType<GunYoneticisi>();

        if (yerlestirmeSistemi == null)
            yerlestirmeSistemi = FindObjectOfType<NesneYerlestirmeSistemi>();

        if (gunYoneticisi != null)
            gunYoneticisi.GunYenidenBaslatildi += YeniGunBasladi;

        KamerayiKur();
    }

    /// <summary>
    /// Ortak ikon atanmissa, duvar arkasindan gorunmeyi saglayan kamera kurulumunun
    /// sahnede oldugundan emin olur; yoksa ana kameraya kendisi ekler.
    /// Boylece kullanicinin dogru kamerayi bulup script eklemesi gerekmez.
    /// </summary>
    void KamerayiKur()
    {
        if (!kamerayiOtomatikKur || ortakIsaret == null) return;
        if (FindObjectOfType<HedefIsaretiKamera>() != null) return; // zaten kurulu

        Camera ana = HedefIsaretiKamera.AnaKamerayiBul();

        if (ana == null)
        {
            Debug.LogWarning("[Tutorial] Oyuncu kamerasi bulunamadi! Hedef ikonu duvarlarin arkasindan " +
                             "GORUNMEYECEK. Cozum: HedefIsaretiKamera scriptini elle kameraya ekle.", this);
            return;
        }

        ana.gameObject.AddComponent<HedefIsaretiKamera>();

        if (teshisLogu)
            Debug.Log($"[Tutorial] HedefIsaretiKamera '{ana.name}' kamerasina otomatik eklendi.", this);
    }

    // ================== OLAY ABONELIKLERI ==================

    void OlaylaraAboneOl()
    {
        if (olaylaraAbone) return;

        if (yerlestirmeSistemi != null)
            yerlestirmeSistemi.NesneYerlestirildi += NesneYerlestirildiGeldi;

        if (BasiliTutmaYoneticisi.Instance != null)
            BasiliTutmaYoneticisi.Instance.OnTamamlandi += TutmaTamamlandi;

        if (EkonomiYoneticisi.Instance != null)
        {
            sonPara = EkonomiYoneticisi.Instance.Para;
            EkonomiYoneticisi.Instance.ParaDegisti += ParaDegisti;
        }

        olaylaraAbone = true;
    }

    void OlaylardanCik()
    {
        if (!olaylaraAbone) return;

        if (yerlestirmeSistemi != null)
            yerlestirmeSistemi.NesneYerlestirildi -= NesneYerlestirildiGeldi;

        if (BasiliTutmaYoneticisi.Instance != null)
            BasiliTutmaYoneticisi.Instance.OnTamamlandi -= TutmaTamamlandi;

        if (EkonomiYoneticisi.Instance != null)
            EkonomiYoneticisi.Instance.ParaDegisti -= ParaDegisti;

        olaylaraAbone = false;
    }

    void NesneYerlestirildiGeldi(GameObject yerlesen, GameObject kaynakPrefab)
    {
        if (aktifAdim == null || aktifAdim.kosullar == null) return;

        for (int i = 0; i < aktifAdim.kosullar.Count; i++)
        {
            TutorialKosulu k = aktifAdim.kosullar[i];
            if (k == null) continue;

            bool oncekiDurum = k.Saglandi;
            k.NesneYerlestirildiBildir(yerlesen, kaynakPrefab);

            if (!oncekiDurum && k.Saglandi) SartSaglandiLogu(k);
        }

        TamamlanmaKontrol();
    }

    void TutmaTamamlandi(BasiliTutmaYoneticisi.TutmaKaynagi kaynak)
    {
        if (kaynak != BasiliTutmaYoneticisi.TutmaKaynagi.Kesme) return;
        if (aktifAdim == null || aktifAdim.kosullar == null) return;

        for (int i = 0; i < aktifAdim.kosullar.Count; i++)
        {
            TutorialKosulu k = aktifAdim.kosullar[i];
            if (k == null) continue;

            bool oncekiDurum = k.Saglandi;
            k.KesimBildir();

            if (!oncekiDurum && k.Saglandi) SartSaglandiLogu(k);
        }

        TamamlanmaKontrol();
    }

    void ParaDegisti(int yeniPara)
    {
        bool artti = yeniPara > sonPara;
        sonPara = yeniPara;

        if (!artti || aktifAdim == null || aktifAdim.kosullar == null) return;

        for (int i = 0; i < aktifAdim.kosullar.Count; i++)
        {
            TutorialKosulu k = aktifAdim.kosullar[i];
            if (k == null) continue;

            bool oncekiDurum = k.Saglandi;
            k.ParaKazanildiBildir();

            if (!oncekiDurum && k.Saglandi) SartSaglandiLogu(k);
        }

        TamamlanmaKontrol();
    }

    /// <summary>
    /// DISARIDAN TETIKLEME. Kodun ulasamadigi olaylar icin (orn. balik yakalama).
    /// Kullanim: FishingSystem'in UnityEvent'ine bu objeyi surukle -> ManuelTetikle -> "balik" yaz.
    /// </summary>
    public void ManuelTetikle(string id)
    {
        if (!aktif || aktifAdim == null || aktifAdim.kosullar == null) return;

        for (int i = 0; i < aktifAdim.kosullar.Count; i++)
        {
            TutorialKosulu k = aktifAdim.kosullar[i];
            if (k == null) continue;

            bool oncekiDurum = k.Saglandi;
            k.ManuelBildir(id);

            if (!oncekiDurum && k.Saglandi) SartSaglandiLogu(k);
        }

        TamamlanmaKontrol();
    }

    // ================== AKIS ==================

    void YeniGunBasladi()
    {
        if (!aktif) return;

        int gun = gunYoneticisi != null ? gunYoneticisi.Gun : 1;
        AkisBaslat(gun);
    }

    /// <summary>Verilen gunun akisini bastan baslatir. O gun icin akis yoksa tutorial cikmaz.</summary>
    public void AkisBaslat(int gun)
    {
        AkisiDurdur();

        aktifAkis = AkisBul(gun);

        if (aktifAkis == null)
        {
            if (teshisLogu) Debug.Log($"[Tutorial] Gun {gun} icin akis tanimli degil, tutorial calismayacak.", this);
            return;
        }

        aktifAkis.Sifirla();
        aktifIndex = -1;

        if (teshisLogu)
            Debug.Log($"[Tutorial] Gun {gun} akisi basladi: {aktifAkis.AdimSayisi} adim.", this);

        SonrakiAdimaGec(baslangicGecikmesi);
    }

    /// <summary>Akisi durdurur, acik paneli kapatir.</summary>
    public void AkisiDurdur()
    {
        if (acilisRutini != null) { StopCoroutine(acilisRutini); acilisRutini = null; }

        if (aktifAdim != null) aktifAdim.Goster(false);
        IsaretiAyarla(null, false);

        aktifAdim = null;
        aktifIndex = -1;
        aktifAkis = null;
    }

    void SonrakiAdimaGec(float gecikme)
    {
        if (aktifAdim != null)
        {
            aktifAdim.Goster(false);
            IsaretiAyarla(null, false);
            SesCal(aktifAdim.kapanisSesi);

            if (teshisLogu)
                Debug.Log($"[Tutorial] Adim tamamlandi: '{aktifAdim.adimAdi}'", this);
        }

        aktifAdim = null;
        aktifIndex++;

        if (aktifAkis == null || aktifIndex >= aktifAkis.AdimSayisi)
        {
            if (teshisLogu && aktifAkis != null)
                Debug.Log("[Tutorial] Akis bitti, tum paneller kapali.", this);

            aktifAkis = null;
            return;
        }

        if (acilisRutini != null) StopCoroutine(acilisRutini);
        acilisRutini = StartCoroutine(AdimiAcRutini(aktifIndex, gecikme));
    }

    IEnumerator AdimiAcRutini(int index, float gecikme)
    {
        TutorialAdimi adim = aktifAkis != null ? aktifAkis.AdimAl(index) : null;

        float bekle = gecikme > 0f ? gecikme : (adim != null ? adim.acilisGecikmesi : 0f);
        if (bekle > 0f) yield return new WaitForSeconds(bekle);

        acilisRutini = null;

        if (adim == null) { SonrakiAdimaGec(0f); yield break; }

        adim.Sifirla();

        // Adim acilirken UI (Tab) zaten acikse panel GIZLI baslar; UI kapaninca gorunur.
        panelGizli = uiAcikkenGizle && UIYoneticisi.HerhangiBirUIAcikMi;
        adim.Goster(!panelGizli);
        IsaretiAyarla(adim, !panelGizli);

        SesCal(adim.acilisSesi);

        aktifAdim = adim;

        if (teshisLogu)
            Debug.Log($"[Tutorial] Adim acildi ({index + 1}/{aktifAkis.AdimSayisi}): '{adim.adimAdi}'", this);

        // Oyuncu bu isi ZATEN yapmis olabilir -> hemen kontrol et, gerekiyorsa atla.
        adim.Guncelle();
        TamamlanmaKontrol();
    }

    void Update()
    {
        if (!aktif || aktifAdim == null) return;

        // Gorunurluk: UI (Tab) acikken paneli gizle, kapaninca geri goster.
        // Her frame bakilir ki tepki anlik olsun; sadece DEGISINCE SetActive cagrilir.
        if (uiAcikkenGizle)
        {
            bool uiAcik = UIYoneticisi.HerhangiBirUIAcikMi;
            if (uiAcik != panelGizli)
            {
                panelGizli = uiAcik;
                aktifAdim.Goster(!panelGizli);
                IsaretiAyarla(aktifAdim, !panelGizli);
            }
        }

        // Tus sartlari HER FRAME kontrol edilir; 0.2 sn'lik dongu hizli basisi kacirirdi.
        if (aktifAdim.TuslariKontrolEt())
        {
            TamamlanmaKontrol();
            if (aktifAdim == null) return; // adim bu karede bittiyse devam etme
        }

        if (Time.time < sonrakiKontrol) return;

        sonrakiKontrol = Time.time + kontrolAraligi;

        aktifAdim.Guncelle();
        TamamlanmaKontrol();
    }

    void TamamlanmaKontrol()
    {
        if (aktifAdim == null) return;

        // Sartlardan biri saglanmis olabilir -> ikon siradaki hedefe gecsin
        // (panel hala acik olsa bile ikon o isten kalkar).
        IsaretiTazele();

        if (!aktifAdim.TamamlandiMi) return;

        SonrakiAdimaGec(0f);
    }

    // ================== YARDIMCI ==================

    TutorialAkisi AkisBul(int gun)
    {
        if (gunAkislari == null) return null;

        for (int i = 0; i < gunAkislari.Count; i++)
            if (gunAkislari[i] != null && gunAkislari[i].gun == gun)
                return gunAkislari[i].akis;

        return null;
    }

    void TumAkislariKapat()
    {
        if (gunAkislari == null) return;

        for (int i = 0; i < gunAkislari.Count; i++)
            if (gunAkislari[i] != null && gunAkislari[i].akis != null)
                gunAkislari[i].akis.Sifirla();
    }

    /// <summary>
    /// Ortak ikonu bu adimin hedefine tasir. Adim yoksa, hedef noktasi bos ise
    /// ya da panel gizliyse ikon kapatilir.
    /// </summary>
    void IsaretiAyarla(TutorialAdimi adim, bool goster)
    {
        if (ortakIsaret == null) return;

        // Sartlar saglandikca ikon kendiliginden sonraki hedefe gecer,
        // hicbiri kalmayinca kalkar.
        Transform hedef = (goster && adim != null) ? adim.AktifHedefNoktasi() : null;

        // Hedef degismediyse dokunma (her kontrolde bosuna SetActive/konum islemi yapilmasin)
        if (hedef == sonIsaretHedefi) return;
        sonIsaretHedefi = hedef;

        if (hedef == null) ortakIsaret.Gizle();
        else ortakIsaret.Hedefe(hedef);
    }

    /// <summary>Aktif adimin ikon hedefini yeniden hesaplar (sart saglandiginda cagrilir).</summary>
    void IsaretiTazele()
    {
        IsaretiAyarla(aktifAdim, !panelGizli);
    }

    void SesCal(SesVerisi ses)
    {
        if (ses == null || SesYoneticisi.Instance == null) return;
        SesYoneticisi.Instance.SesCal2D(ses);
    }

    void SartSaglandiLogu(TutorialKosulu k)
    {
        if (teshisLogu) Debug.Log($"[Tutorial] Sart saglandi: {k.Ad()}", this);
    }

    /// <summary>TutorialKosulu icin: sahnedeki yerlestirme sistemini bulur.</summary>
    public static NesneYerlestirmeSistemi YerlestirmeSisteminiBul()
    {
        if (Instance != null && Instance.yerlestirmeSistemi != null) return Instance.yerlestirmeSistemi;
        return FindObjectOfType<NesneYerlestirmeSistemi>();
    }

    /// <summary>TutorialKosulu icin: sahnedeki zaman sistemini bulur.</summary>
    public static ZamanSistemi ZamanSisteminiBul()
    {
        return FindObjectOfType<ZamanSistemi>();
    }

    // ================== TEST ==================

    /// <summary>
    /// Acik adimin sartlarini ve hangisinin BEKLEDIGINI Console'a yazar.
    /// Panel kapanmiyorsa once buna bak. (Play modunda component'e sag tik)
    /// </summary>
    [ContextMenu("Teshis: Bekleyen Sartlar")]
    void TestBekleyenSartlar()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[Tutorial] Sadece Play modunda.", this); return; }

        if (aktifAdim == null)
        {
            Debug.Log("[Tutorial] Su an acik bir adim yok (akis bitmis ya da hic baslamamis olabilir).", this);
            return;
        }

        var sb = new System.Text.StringBuilder(256);
        sb.AppendLine($"=== [Tutorial] '{aktifAdim.adimAdi}' sartlari ===");

        if (aktifAdim.kosullar == null || aktifAdim.kosullar.Count == 0)
        {
            sb.AppendLine("  (hic sart yok -> adim aninda tamamlanir)");
        }
        else
        {
            for (int i = 0; i < aktifAdim.kosullar.Count; i++)
            {
                TutorialKosulu k = aktifAdim.kosullar[i];
                if (k == null) { sb.AppendLine($"  [{i}] (bos sart)"); continue; }

                sb.AppendLine($"  [{i}] {(k.Saglandi ? "TAMAM   " : "BEKLIYOR")}  {k.Ad()}");
            }
        }

        Debug.Log(sb.ToString(), this);
    }

    [ContextMenu("Test: Siradaki Adima Gec")]
    void TestSonrakiAdim()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[Tutorial] Sadece Play modunda.", this); return; }
        SonrakiAdimaGec(0f);
    }

    [ContextMenu("Test: Akisi Bastan Baslat")]
    void TestBastanBaslat()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[Tutorial] Sadece Play modunda.", this); return; }
        AkisBaslat(gunYoneticisi != null ? gunYoneticisi.Gun : 1);
    }
}
// ===== TUTORIAL SONU =====
