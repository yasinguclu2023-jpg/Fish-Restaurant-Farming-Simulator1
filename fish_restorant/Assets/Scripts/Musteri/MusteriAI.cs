using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Musteri yapay zekasi - FSM (durum makinesi) + NavMeshAgent.
///
/// Akis:
///   Spawn -> sisteme katil -> en ondeki bos sira noktasina yuru (dinamik) -> SiradaBekle
///   -> EN ONDEYSE ve board bossa SIPARIS VER -> oyuncu board'a tiklayinca
///   -> MASAYA YURU (atanan koltuk) -> OTUR (oturma animasyonu).
/// Sira ilerledikce (onden biri ayrilinca) arkadakiler bir slot one yurur (otomatik animasyon).
///
/// KURULUM (prefab):
/// - NavMeshAgent (zorunlu).
/// - Opsiyonel: Animator (yurume + oturma parametreleri).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class MusteriAI : MonoBehaviour
{
    public enum Durum
    {
        Bosta,                 // Henuz akis baslamadi
        SirayaGirmeyiBekliyor, // Sira (kapasite) dolu, yer acilmasini bekliyor
        SiraNoktasinaYuruyor,  // Atanan / en ondeki bos slota yuruyor
        SiradaBekliyor,        // Slotta bekliyor
        SiparisVeriyor,        // En onde, board'da siparisi gosterildi, oyuncu tikini bekliyor
        MasaBekliyor,          // Siparis alindi ama bos masa yok, bekliyor
        MasayaYuruyor,         // Atanan koltuga yuruyor
        Oturuyor,              // Koltuga oturdu
        Yiyor,                 // Dogru siparis geldi, yiyor
        CikisaYuruyor          // Kalkti, spawn noktasina donuyor (sonra yok olur)
    }

    [Header("Referanslar")]
    [Tooltip("Bos birakirsan ayni objedeki NavMeshAgent otomatik bulunur.")]
    [SerializeField] private NavMeshAgent agent;
    [Tooltip("Opsiyonel. Bos birakirsan child'larda Animator aranir.")]
    [SerializeField] private Animator animator;

    [Header("Animator Parametreleri (sonradan ayarlanabilir)")]
    [Tooltip("Yurume bool parametresi. Bos birakirsan set edilmez.")]
    [SerializeField] private string yurumeBoolParametresi = "isWalking";
    [Tooltip("Oturma bool parametresi. Bos birakirsan set edilmez.")]
    [SerializeField] private string oturmaBoolParametresi = "isSitting";
    [Tooltip("Yeme bool parametresi. Bos birakirsan set edilmez.")]
    [SerializeField] private string yemeBoolParametresi = "isEating";

    [Header("Hareket")]
    [Tooltip("Karakterin yurume hizi (NavMeshAgent.speed). 0 = NavMeshAgent'taki mevcut deger kullanilir.")]
    [SerializeField] private float yurumeHizi = 3.5f;
    [Tooltip("Hedef noktaya bu mesafe kadar yaklasinca varilmis sayilir / slot kapilir.")]
    [SerializeField] private float varisMesafesi = 0.3f;
    [Tooltip("Sira noktasina varinca tam konuma snap'le ve noktanin yonune don.")]
    [SerializeField] private bool siraNoktasinaSnaple = true;

    [Header("Yeme / Ayrilma")]
    [Tooltip("Dogru yemek gelince kac saniye yesin.")]
    [SerializeField] private float yemeSuresi = 25f;
    [Tooltip("Yanlis yemek gelince kac saniye bekleyip (yemeden) kalksin.")]
    [SerializeField] private float yanlisBeklemeSuresi = 3f;

    [Header("Sabir / Bekleme (saniye, 0 = sonsuz)")]
    [Tooltip("EN ONE gelip siparis verirken baslar. Bu sure icinde siparisi ALINMAZSA olumsuz ses cikarip gider. Arkadakiler siralarini bekler (sayaclari islemez).")]
    [SerializeField] private float siraSabri = 30f;
    [Tooltip("Masaya oturunca baslar. Bu sure icinde yemek GELMEZSE siparis iptal olur, olumsuz ses cikarip kalkip gider.")]
    [SerializeField] private float masaSabri = 60f;

    [Header("El Yemekleri (dogru servis olunca ana urune gore acilir)")]
    [Tooltip("Her ana urun (Balik/Ahtapot) icin, elde acilacak obje. Objeler baslangicta KAPALI olsun.")]
    [SerializeField] private ElYemegi[] elYemekleri;

    [Header("Ses (opsiyonel)")]
    [SerializeField] private SesVerisi gelisSesi;

    [Header("Tepki Ses Bankasi")]
    [Tooltip("Olumlu/olumsuz tepki seslerinin bankasi (ScriptableObject).")]
    [SerializeField] private MusteriSesBankasi sesBankasi;

    [System.Serializable]
    public class ElYemegi
    {
        [Tooltip("Bu el objesi hangi ana urun icin (siparisteki Balik/Ahtapot Malzeme'si).")]
        public Malzeme anaUrun;
        [Tooltip("Elde acilacak obje (karakterin eline sabitli, baslangicta kapali).")]
        public GameObject elObjesi;
    }

    // runtime
    private Durum durum = Durum.Bosta;
    private int suankiHedefSlot = -1;
    private Transform oturmaNoktam;
    private int masaNom = -1;
    private Transform donusNoktam;        // spawn/donus noktasi
    private bool yemekSureciBasladi;      // Ye veya YanlisServis bir kez calissin
    private List<GameObject> masadanSilinecekler; // kalkarken silinecek icecek/kalamar alt objeleri
    private int kazanilacakPara;          // dogru servis: yiyip kalkinca eklenecek para
    private float siraSayaci;             // sira sabri geri sayimi
    private bool siraSayaciAktif;
    private float masaSayaci;             // masa sabri geri sayimi
    private bool masaSayaciAktif;

    public Durum MevcutDurum => durum;

    /// <summary>
    /// GUN SIFIRLANIRKEN cagrilir: musteriyi AKIS'i beklemeden aninda yok eder.
    /// Oturdugu masayi bosaltir ve masasindaki tuketilen objeleri (icecek/kalamar) siler.
    /// (Sira/siparis kayitlari GunYoneticisi tarafindan ayrica temizlenir.)
    /// </summary>
    public void AninadaYokEt()
    {
        StopAllCoroutines();
        MasadakileriSil();

        if (oturmaNoktam != null && MasaYonetimi.Instance != null)
            MasaYonetimi.Instance.YeriBosalt(oturmaNoktam);

        Destroy(gameObject);
    }

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Yurume hizini uygula (0 ise NavMeshAgent'in mevcut hizi kullanilir)
        if (agent != null && yurumeHizi > 0f) agent.speed = yurumeHizi;
    }

    /// <summary>Yurume hizini calisma aninda degistir (istege bagli, koddan).</summary>
    public void YurumeHiziniAyarla(float hiz)
    {
        yurumeHizi = hiz;
        if (agent != null && hiz > 0f) agent.speed = hiz;
    }

    /// <summary>Uretici tarafindan spawn'dan hemen sonra cagrilir. donusNoktasi = geri donecegi spawn.</summary>
    public void AkisiBaslat(Transform donusNoktasi)
    {
        suankiHedefSlot = -1;
        oturmaNoktam = null;
        masaNom = -1;
        donusNoktam = donusNoktasi;
        yemekSureciBasladi = false;
        OturmaAnimasyonu(false);
        YemeAnimasyonu(false);
        ElYemekleriKapat();

        if (gelisSesi != null && SesYoneticisi.Instance != null)
            SesYoneticisi.Instance.SesCal(gelisSesi, transform.position);

        SistemeKatilmayiDene();
    }

    void Update()
    {
        SiraSabriGuncelle();
        MasaSabriGuncelle();

        switch (durum)
        {
            case Durum.SirayaGirmeyiBekliyor:
                SistemeKatilmayiDene();
                break;

            case Durum.SiraNoktasinaYuruyor:
                SiradaIlerle();
                break;

            case Durum.SiradaBekliyor:
                SiparisKontrol();
                break;

            case Durum.MasayaYuruyor:
                if (HedefeVardiMi()) MasayaVardi();
                break;

            case Durum.CikisaYuruyor:
                if (HedefeVardiMi()) Destroy(gameObject);
                break;
        }
    }

    // ---------------- SIRA ----------------

    private void SistemeKatilmayiDene()
    {
        SiraYonetimi sira = SiraYonetimi.Instance;
        if (sira != null && sira.SistemeKatil(this))
        {
            suankiHedefSlot = -1;
            DurumDegistir(Durum.SiraNoktasinaYuruyor);
        }
        else
        {
            DurumDegistir(Durum.SirayaGirmeyiBekliyor);
            if (agent != null) agent.isStopped = true;
            YurumeAnimasyonu(false);
        }
    }

    private void SiradaIlerle()
    {
        SiraYonetimi sira = SiraYonetimi.Instance;
        if (sira == null) return;

        int hedefSlot = sira.HedefSlotIndex(this);
        if (hedefSlot < 0) return;

        Transform nokta = sira.HedefNokta(this);
        if (nokta == null) return;

        if (hedefSlot != suankiHedefSlot)
        {
            suankiHedefSlot = hedefSlot;
            HedefeGit(nokta.position);
        }

        float mesafe = Vector3.Distance(transform.position, nokta.position);
        float esik = Mathf.Max(varisMesafesi, agent != null ? agent.stoppingDistance : 0f);
        if (mesafe <= esik)
        {
            if (sira.SlotClaimDene(this, hedefSlot))
                SlotaYerles(nokta);
        }
    }

    private void SlotaYerles(Transform nokta)
    {
        if (siraNoktasinaSnaple && nokta != null && agent != null)
        {
            agent.Warp(nokta.position);
            transform.rotation = nokta.rotation;
        }
        if (agent != null) agent.isStopped = true;
        DurumDegistir(Durum.SiradaBekliyor);
        YurumeAnimasyonu(false);
    }

    /// <summary>SiraYonetimi sira ilerleyince (one kayinca) cagirir: yeni slota dogru yuru.</summary>
    public void SiradaIlerlemeyeBasla()
    {
        suankiHedefSlot = -1;
        if (agent != null) agent.isStopped = false;
        DurumDegistir(Durum.SiraNoktasinaYuruyor);
    }

    // ---------------- SIPARIS ----------------

    private void SiparisKontrol()
    {
        SiraYonetimi sira = SiraYonetimi.Instance;
        if (sira == null || !sira.BastaMi(this)) return;

        SiparisYonetimi sy = SiparisYonetimi.Instance;
        if (sy == null || !sy.SiparisAlinabilirMi()) return;

        sy.SiparisBaslat(this);
        DurumDegistir(Durum.SiparisVeriyor);
        SiraSabriniBaslat(); // EN ONE gelip siparis vermeye basladi: sabir sayaci SIMDI baslar
        // Siparis verilirken yerinde idle bekler; oyuncu tiklayinca SiparisYonetimi
        // MasayaGit() veya MasaBekle() cagiracak.
    }

    // ---------------- MASA ----------------

    /// <summary>SiparisYonetimi cagirir: atanan koltuga yuru.</summary>
    public void MasayaGit(Transform koltuk, int masaNo = -1)
    {
        siraSayaciAktif = false; // siparis alindi, sira sabri biter
        oturmaNoktam = koltuk;
        masaNom = masaNo;
        if (koltuk == null) { MasaBekle(); return; }

        DurumDegistir(Durum.MasayaYuruyor);
        HedefeGit(koltuk.position);
    }

    /// <summary>
    /// Dogru siparis geldi: el objesini ac + yeme animasyonu + sure sonunda kalk-git.
    /// masadaSilinecekObjeler: musteri kalkarken (yemeSuresi sonunda) silinecek icecek/kalamar
    /// alt objeleri. Kaplar (bardak/kasa) masada kalir. null olabilir.
    /// kazanilacakParaMiktari: yiyip kalkinca eklenecek para (siparisin toplam fiyati).
    /// </summary>
    public void Ye(Malzeme anaUrun, List<GameObject> masadaSilinecekObjeler = null, int kazanilacakParaMiktari = 0)
    {
        if (yemekSureciBasladi) return;
        yemekSureciBasladi = true;
        masaSayaciAktif = false; // yemek geldi, masa sabri biter

        masadanSilinecekler = masadaSilinecekObjeler;
        kazanilacakPara = kazanilacakParaMiktari;

        // Olumlu (mutlu) ses
        TepkiSesiCal(sesBankasi != null ? sesBankasi.OlumluSes() : null);

        DurumDegistir(Durum.Yiyor);
        ElYemegiSec(anaUrun);
        YemeAnimasyonu(true);
        StartCoroutine(YedikSonraAyril());
    }

    /// <summary>Yanlis siparis geldi: bir sure bekleyip yemeden kalk-git.</summary>
    public void YanlisServis()
    {
        if (yemekSureciBasladi) return;
        yemekSureciBasladi = true;
        masaSayaciAktif = false; // yemek (yanlis da olsa) geldi, masa sabri biter

        // Olumsuz (mutsuz) ses
        TepkiSesiCal(sesBankasi != null ? sesBankasi.OlumsuzSes() : null);

        StartCoroutine(YanlisSonraAyril());
    }

    // Verilen sesi musterinin konumunda calar (null/gecersizse hicbir sey yapmaz).
    private void TepkiSesiCal(SesVerisi ses)
    {
        if (ses != null && SesYoneticisi.Instance != null)
            SesYoneticisi.Instance.SesCal(ses, transform.position);
    }

    private IEnumerator YedikSonraAyril()
    {
        yield return new WaitForSeconds(yemeSuresi);

        // Yeme bitti, kalkmadan hemen once masaya PARA birak (oyuncu tiklayinca hesaba eklenir).
        // Sadece dogru servis bu yola girer.
        if (kazanilacakPara > 0 && ParaYoneticisi.Instance != null)
            ParaYoneticisi.Instance.ParaBirak(masaNom, kazanilacakPara);

        Ayril();
    }

    private IEnumerator YanlisSonraAyril()
    {
        yield return new WaitForSeconds(yanlisBeklemeSuresi);
        Ayril();
    }

    // Kalk, masayi bosalt, spawn'a dogru yuru (varinca yok olur)
    private void Ayril()
    {
        // Tuketilen alt objeleri (icecek/kalamar) sil - kaplar (bardak/kasa) masada kalir.
        MasadakileriSil();

        if (MasaYonetimi.Instance != null) MasaYonetimi.Instance.YeriBosalt(oturmaNoktam);
        if (SiparisYonetimi.Instance != null) SiparisYonetimi.Instance.MasaSiparisiTamamla(masaNom);

        ElYemekleriKapat();
        CikisaYonel();
    }

    // Ayaga kalk (animasyonlari kapat), agent'i navmesh'e al ve spawn'a yuru (varinca yok olur).
    // Hem normal ayrilma hem sira/masa zaman asimi bunu kullanir.
    private void CikisaYonel()
    {
        YemeAnimasyonu(false);
        OturmaAnimasyonu(false);

        // Donus noktasi yoksa direkt yok ol
        if (donusNoktam == null)
        {
            Destroy(gameObject);
            return;
        }

        // Otururken agent kapatilmis olabilir; tekrar ac ve navmesh'e yerlestir
        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit h, 3f, NavMesh.AllAreas))
                agent.Warp(h.position);
        }

        DurumDegistir(Durum.CikisaYuruyor);
        HedefeGit(donusNoktam.position);
    }

    // Ana urune gore dogru el objesini ac, digerlerini kapat
    private void ElYemegiSec(Malzeme anaUrun)
    {
        Debug.Log($"[MusteriAI] ElYemegiSec -> anaUrun: {(anaUrun != null ? anaUrun.name : "NULL")}, " +
                  $"elYemekleri sayisi: {(elYemekleri != null ? elYemekleri.Length : 0)}", this); // TEST

        if (elYemekleri == null) return;
        for (int i = 0; i < elYemekleri.Length; i++)
        {
            if (elYemekleri[i] == null || elYemekleri[i].elObjesi == null)
            {
                Debug.LogWarning($"[MusteriAI] elYemekleri[{i}] eksik (anaUrun ya da elObjesi atanmamis).", this); // TEST
                continue;
            }
            bool eslesti = elYemekleri[i].anaUrun == anaUrun;
            Debug.Log($"[MusteriAI] elYemekleri[{i}] anaUrun: {(elYemekleri[i].anaUrun != null ? elYemekleri[i].anaUrun.name : "NULL")} -> {(eslesti ? "ACILDI" : "kapali")}", this); // TEST
            elYemekleri[i].elObjesi.SetActive(eslesti);
        }
    }

    private void ElYemekleriKapat()
    {
        if (elYemekleri == null) return;
        for (int i = 0; i < elYemekleri.Length; i++)
            if (elYemekleri[i] != null && elYemekleri[i].elObjesi != null)
                elYemekleri[i].elObjesi.SetActive(false);
    }

    // Musteri kalkarken masadaki tuketilen alt objeleri (icecek/kalamar) yok eder.
    // Kaplar (bardak/kasa) masada kalir. Liste yoksa (yanlis servis vb.) hicbir sey yapmaz.
    private void MasadakileriSil()
    {
        if (masadanSilinecekler == null) return;
        for (int i = 0; i < masadanSilinecekler.Count; i++)
            if (masadanSilinecekler[i] != null) Destroy(masadanSilinecekler[i]);
        masadanSilinecekler = null;
    }

    /// <summary>SiparisYonetimi cagirir: bos masa yok, beklemede kal.</summary>
    public void MasaBekle()
    {
        siraSayaciAktif = false; // siparis alindi, sira sabri biter
        DurumDegistir(Durum.MasaBekliyor);
        if (agent != null) agent.isStopped = true;
        YurumeAnimasyonu(false);
    }

    private void MasayaVardi()
    {
        // Agent'i kapat, sandalyedeki empty'ye tam snap (navmesh disinda bile dogru otursun)
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        if (oturmaNoktam != null)
        {
            transform.position = oturmaNoktam.position;
            transform.rotation = oturmaNoktam.rotation;
        }

        DurumDegistir(Durum.Oturuyor);
        YurumeAnimasyonu(false);
        OturmaAnimasyonu(true);
        MasaSabriniBaslat(); // oturdu: masa sabri baslar
    }

    // ---------------- SABIR / ZAMAN ASIMI ----------------

    private void SiraSabriniBaslat()
    {
        if (siraSayaciAktif) return;   // zaten calisiyor (sirada ilerlerken sifirlanmasin)
        if (siraSabri <= 0f) return;   // 0 = sonsuz
        siraSayaci = siraSabri;
        siraSayaciAktif = true;
    }

    private void SiraSabriGuncelle()
    {
        if (!siraSayaciAktif) return;
        // Sadece EN ONDE (siparis verirken) sayar; arkadakiler siralarini bekler, sayaclari islemez.
        if (durum != Durum.SiparisVeriyor) return;

        siraSayaci -= Time.deltaTime;
        if (siraSayaci <= 0f) SiradanVazgec();
    }

    // Sira sabri doldu: (varsa) board siparisini iptal et, siradan cik, olumsuz ses, spawn'a git.
    private void SiradanVazgec()
    {
        siraSayaciAktif = false;

        // Board'da siparisi gosteriliyorsa iptal et (board temizle + ayrilan masayi birak)
        if (SiparisYonetimi.Instance != null)
            SiparisYonetimi.Instance.SiparisiIptalEt(this);

        // Siradan cik (slot birak, arkadakiler ilerlesin)
        if (SiraYonetimi.Instance != null)
            SiraYonetimi.Instance.SistemdenAyril(this);

        TepkiSesiCal(sesBankasi != null ? sesBankasi.OlumsuzSes() : null);
        CikisaYonel();
    }

    private void MasaSabriniBaslat()
    {
        if (masaSabri <= 0f) { masaSayaciAktif = false; return; } // 0 = sonsuz
        masaSayaci = masaSabri;
        masaSayaciAktif = true;
    }

    private void MasaSabriGuncelle()
    {
        if (!masaSayaciAktif) return;
        if (durum != Durum.Oturuyor) return;

        masaSayaci -= Time.deltaTime;
        if (masaSayaci <= 0f) MasadanKalkZamanAsimi();
    }

    // Masa sabri doldu: siparisi iptal et (mutfaktan kaldir), olumsuz ses, kalk-git.
    private void MasadanKalkZamanAsimi()
    {
        if (yemekSureciBasladi) return; // arada yemek geldiyse dokunma
        yemekSureciBasladi = true;
        masaSayaciAktif = false;

        // Fisi mutfak monitorlerinden kaldir + kayittan dus (ekrandan gitsin)
        if (SiparisYonetimi.Instance != null)
            SiparisYonetimi.Instance.MasaSiparisiniIptalEt(masaNom);

        TepkiSesiCal(sesBankasi != null ? sesBankasi.OlumsuzSes() : null);
        Ayril(); // masayi bosaltir + spawn'a yurur
    }

    // ---------------- YARDIMCI ----------------

    private void HedefeGit(Vector3 hedef)
    {
        // Agent NavMesh'e oturmadiysa komut gonderme (hata spam'ini onler)
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        agent.isStopped = false;
        agent.SetDestination(hedef);
        YurumeAnimasyonu(true);
    }

    private bool HedefeVardiMi()
    {
        if (agent == null) return true;
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return false;
        if (agent.pathPending) return false;

        float esik = Mathf.Max(varisMesafesi, agent.stoppingDistance);
        if (agent.remainingDistance > esik) return false;

        return !agent.hasPath || agent.velocity.sqrMagnitude < 0.05f;
    }

    private void YurumeAnimasyonu(bool yuruyorMu)
    {
        if (animator != null && !string.IsNullOrEmpty(yurumeBoolParametresi))
            animator.SetBool(yurumeBoolParametresi, yuruyorMu);
    }

    private void OturmaAnimasyonu(bool oturuyorMu)
    {
        if (animator != null && !string.IsNullOrEmpty(oturmaBoolParametresi))
            animator.SetBool(oturmaBoolParametresi, oturuyorMu);
    }

    private void YemeAnimasyonu(bool yiyorMu)
    {
        if (animator != null && !string.IsNullOrEmpty(yemeBoolParametresi))
            animator.SetBool(yemeBoolParametresi, yiyorMu);
    }

    private void DurumDegistir(Durum yeni)
    {
        durum = yeni;
    }
}
