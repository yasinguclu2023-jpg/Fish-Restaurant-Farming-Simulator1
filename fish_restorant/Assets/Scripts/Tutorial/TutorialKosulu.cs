using UnityEngine;

// ===== TUTORIAL (kaldirilabilir) =====
// Bu dosyanin TAMAMI tutorial'a aittir. Kaldirmak icin "Tutorial" klasorunu sil.

/// <summary>Bir tutorial panelini kapatan kosulun tipi.</summary>
public enum TutorialKosulTipi
{
    [Tooltip("Magazadan alinan bir prefab yere konuldu")]
    NesneYerlestirildi,
    BolgeyeGirildi,
    UIAcildi,
    KategoriAcildi,
    BitkiSulandi,
    BitkiHasatEdildi,
    OltaAlindi,
    KasaDoldu,
    KesimYapildi,
    MakineyeKasaKonuldu,
    EkmekTezgahta,
    MalzemeEklendi,
    SandvicSarildi,
    DukkanAcildi,
    IzgaradaBalikVar,
    KuvetteUrunVar,
    ParaKazanildi,

    [Tooltip("Sahnede belirli tag'e sahip obje olustu mu? (orn. balik tutuldu)")]
    SahnedeUrunVar,

    [Tooltip("Belirtilen GUNE gelindi mi? (Sayi = hedef gun. Gun sonu objesine basilinca saglanir.)")]
    GunGeldi,

    [Tooltip("Izgaradaki balik E tusu ile CEVRILDI mi? (Sayi = kac balik)")]
    BalikCevrildi,

    [Tooltip("Yigindan SARMA KAGIDI alindi mi? (Adim basladigi ANA gore yeni kagit sayilir.)")]
    SarmaKagidiAlindi,

    [Tooltip("Oyuncu belirtilen TUSA basti mi? ('Tus' alanindan sec. Paneli kapatmak icin.)")]
    TusaBasildi,

    Manuel
}

/// <summary>
/// Bir tutorial adiminin TEK bir sarti.
///
/// Bir panelde birden fazla sart olabilir; HEPSI saglaninca panel kapanir.
/// Sart bir kez saglandiginda KILITLENIR (oyuncu sonradan bozamaz).
///
/// HANGI TIPTE NE ATANACAK (hedef alani):
///   NesneYerlestirildi  -> "Hedef Prefab" = magazadaki prefab (Hedef bos)
///   BolgeyeGirildi      -> Hedef = TutorialBolgesi
///   BitkiSulandi        -> Hedef = BitkiBuyumeSistemi (sahnede duran belirli bir saksi)
///                          HEDEF BOS BIRAKILIRSA: sahnede kac bitki sulandigini SAYAR,
///                          "Sayi" kadar olunca sart saglanir. Magazadan alinip oyun
///                          sirasinda dikilen bitkiler icin BUNU KULLAN (referans verilemez).
///   BitkiHasatEdildi    -> Hedef = BitkiBuyumeSistemi
///   KategoriAcildi      -> Hedef = UIYerlestirmeSistemi, Sayi = kategori index (0,1..)
///   OltaAlindi          -> Hedef = NesneYerlestirmeSistemi (bos birakirsan otomatik bulunur)
///   KasaDoldu           -> Hedef = KasaSistemi / BalikKasaSistemi, Sayi = urun adedi
///   MakineyeKasaKonuldu -> Hedef = UretimMakinesiSistemi (ekmek/bira/sarap makinesi)
///   EkmekTezgahta       -> Hedef = EkmekYerlestirmeSistemi.
///                          HEDEF BOS BIRAKILIRSA sahnedeki TUM sarma kagitlarina bakar
///                          (sarma kagidi oyun sirasinda uretildigi icin referans verilemez).
///   MalzemeEklendi      -> Hedef = EkmekMalzemeAlici, Sayi = dolu slot adedi.
///                          HEDEF BOS BIRAKILIRSA tum sarma kagitlarina bakar.
///   SandvicSarildi      -> Hedef = EkmekYerlestirmeSistemi. Hedef bos = tum kagitlara bakar.
///   BalikCevrildi       -> Hedef gerekmez. Sayi = kac balik E ile cevrildi.
///   DukkanAcildi        -> Hedef = ZamanSistemi (bos birakirsan otomatik bulunur)
///   IzgaradaBalikVar    -> Hedef = PisirmeSistemi, Sayi = balik adedi
///   KuvetteUrunVar      -> Hedef = KuvetSistemi, Sayi = dilim adedi
///   GunGeldi            -> Sayi = HEDEF GUN (orn. 2 -> 2. gune gecilince saglanir).
///                          Hedef bos birakilabilir, GunYoneticisi otomatik bulunur.
///   KesimYapildi        -> Sayi = kac kesim
///   ParaKazanildi       -> Sayi = kac kez para kazanildi
///   UIAcildi            -> hicbir sey (herhangi bir UI paneli acilinca)
///   Manuel              -> "Manuel Id" yaz, disaridan TutorialYoneticisi.ManuelTetikle("id")
/// </summary>
[System.Serializable]
public class TutorialKosulu
{
    [Tooltip("Bu sart nasil saglanacak?")]
    public TutorialKosulTipi tip = TutorialKosulTipi.NesneYerlestirildi;

    [Tooltip("SADECE 'NesneYerlestirildi' icin: magazadaki prefab'i surukle. " +
             "Oyuncu bu prefab'i yere koyunca sart saglanir.")]
    public GameObject hedefPrefab;

    [Tooltip("Sartin bakacagi sahne bileseni. Hangi tipte ne atanacagi icin " +
             "scriptin ustundeki listeye bak. Bazi tiplerde bos birakilabilir.")]
    public MonoBehaviour hedef;

    [Tooltip("Gerekli miktar (urun adedi, kesim sayisi...) ya da 'KategoriAcildi' icin kategori index'i.")]
    public int sayi = 1;

    [Tooltip("SADECE 'SahnedeUrunVar' icin: aranacak TAG. Sahnede bu tag'li obje " +
             "'Sayi' kadar olunca sart saglanir. Ornek: balik tutulunca sahneye balik dusuyorsa " +
             "baligin tag'ini yaz.")]
    public string hedefTag;

    [Tooltip("SADECE 'Manuel' icin: disaridan tetiklenecek kimlik. " +
             "Kod: TutorialYoneticisi.ManuelTetikle(\"kimlik\")")]
    public string manuelId;

    [Tooltip("SADECE 'TusaBasildi' icin: hangi tusa basilinca sart saglansin.")]
    public KeyCode tus = KeyCode.T;

    [Tooltip("Bu sart HENUZ SAGLANMAMISKEN duvar arkasi ikonunun gosterecegi yer. " +
             "Sart saglanir saglanmaz ikon buradan kalkar, sonraki sartin yerine gecer " +
             "(hicbiri kalmazsa kaybolur). Bos birakilabilir.")]
    public Transform hedefNoktasi;

    // --- Runtime (kaydedilmez) ---
    [System.NonSerialized] private bool saglandi;
    [System.NonSerialized] private int sayac;
    [System.NonSerialized] private bool hasatHazirGorundu;
    [System.NonSerialized] private int baslangicSayisi = -1; // adim basindaki temel sayim

    /// <summary>Sart saglandi mi? (bir kez saglaninca kilitli kalir)</summary>
    public bool Saglandi => saglandi;

    /// <summary>Akis basinda / adim acilirken cagrilir.</summary>
    public void Sifirla()
    {
        saglandi = false;
        sayac = 0;
        hasatHazirGorundu = false;
        baslangicSayisi = -1; // ilk kontrolde yeniden olculecek
    }

    /// <summary>Sarti kilitle (event'ten ya da poll'den).</summary>
    private void Kilitle() => saglandi = true;

    /// <summary>
    /// "Hedef" alanina SURUKLENEN bilesen yanlis olsa bile dogrusunu bulur.
    ///
    /// Inspector'da bir objeyi surukledigin de Unity o objedeki HERHANGI bir bileseni
    /// secebiliyor (orn. KuvetSistemi yerine OutlineFx). Bu yuzden once bilesenin
    /// kendisine, olmazsa ayni objeye, parent'ina ve child'larina bakiyoruz.
    /// Boylece "objeyi surukledim ama olmadi" durumu yasanmaz.
    /// </summary>
    private T HedefBul<T>() where T : Component
    {
        // Aranacak obje: once "Hedef" alani, o bossa "Hedef Prefab" alani.
        // (Sahnedeki obje yanlislikla Hedef Prefab'a surukIenmis olabilir - ikisini de kabul et.)
        GameObject kaynak = null;

        if (hedef != null)
        {
            if (hedef is T dogruTip) return dogruTip;
            kaynak = hedef.gameObject;
        }
        else if (hedefPrefab != null)
        {
            kaynak = hedefPrefab;
        }

        if (kaynak == null) return null;

        T bilesen = kaynak.GetComponent<T>();
        if (bilesen != null) return bilesen;

        bilesen = kaynak.GetComponentInParent<T>();
        if (bilesen != null) return bilesen;

        bilesen = kaynak.GetComponentInChildren<T>();
        if (bilesen != null) return bilesen;

        if (!hedefUyarisiVerildi)
        {
            hedefUyarisiVerildi = true;
            Debug.LogWarning($"[TutorialKosulu] '{tip}' sarti icin '{kaynak.name}' " +
                             $"objesinde {typeof(T).Name} bileseni BULUNAMADI. " +
                             "Hedef alanina dogru objeyi surukledigine emin ol.");
        }

        return null;
    }

    [System.NonSerialized] private bool hedefUyarisiVerildi;

    // ================== POLL (her kontrol araliginda) ==================

    /// <summary>
    /// Durum tabanli sartlari kontrol eder. Olay tabanli sartlar (NesneYerlestirildi,
    /// KesimYapildi, ParaKazanildi, Manuel) burada kontrol edilmez; onlar bildirimle gelir.
    /// </summary>
    public bool Guncelle()
    {
        if (saglandi) return true;

        switch (tip)
        {
            case TutorialKosulTipi.BolgeyeGirildi:
                {
                    TutorialBolgesi bolge = HedefBul<TutorialBolgesi>();
                    if (bolge != null && bolge.Girildi) Kilitle();
                }
                break;

            case TutorialKosulTipi.UIAcildi:
                if (UIYoneticisi.HerhangiBirUIAcikMi) Kilitle();
                break;

            case TutorialKosulTipi.KategoriAcildi:
                {
                    UIYerlestirmeSistemi magaza = HedefBul<UIYerlestirmeSistemi>();
                    if (magaza != null && magaza.AktifKategoriIndex == sayi) Kilitle();
                }
                break;

            case TutorialKosulTipi.BitkiSulandi:
                if (hedef != null)
                {
                    BitkiBuyumeSistemi bitki = HedefBul<BitkiBuyumeSistemi>();
                    if (bitki != null && !bitki.SusuzMu) Kilitle();
                }
                else
                {
                    // HEDEF BOS = "sahnede kac bitki sulandi?" diye sayar.
                    // Bitkiler magazadan alinip oyun sirasinda dikildigi icin
                    // Inspector'dan referans verilemez; bu yuzden sayma yontemi sart.
                    if (SulanmisBitkiSayisi() >= Mathf.Max(1, sayi)) Kilitle();
                }
                break;

            case TutorialKosulTipi.BitkiHasatEdildi:
                {
                    BitkiBuyumeSistemi b2 = HedefBul<BitkiBuyumeSistemi>();
                    if (b2 != null)
                    {
                        // Once "hasat hazir" gorulur, sonra kaybolur -> hasat edilmistir.
                        if (b2.HasatHazirMi) hasatHazirGorundu = true;
                        else if (hasatHazirGorundu) Kilitle();
                    }
                }
                break;

            case TutorialKosulTipi.OltaAlindi:
                {
                    NesneYerlestirmeSistemi y = HedefBul<NesneYerlestirmeSistemi>();
                    if (y == null) y = TutorialYoneticisi.YerlestirmeSisteminiBul();
                    if (y != null && y.OltaModuAktifMi) Kilitle();
                }
                break;

            case TutorialKosulTipi.KasaDoldu:
                {
                    BalikKasaSistemi bk = HedefBul<BalikKasaSistemi>();
                    if (bk != null) { if (bk.MevcutUrunSayisi >= sayi) Kilitle(); }
                    else
                    {
                        KasaSistemi ks = HedefBul<KasaSistemi>();
                        if (ks != null && ks.MevcutUrunSayisi >= sayi) Kilitle();
                    }
                }
                break;

            case TutorialKosulTipi.MakineyeKasaKonuldu:
                {
                    UretimMakinesiSistemi mak = HedefBul<UretimMakinesiSistemi>();
                    if (mak != null && mak.KasaMevcut) Kilitle();
                }
                break;

            case TutorialKosulTipi.EkmekTezgahta:
                if (hedef != null)
                {
                    EkmekYerlestirmeSistemi e1 = HedefBul<EkmekYerlestirmeSistemi>();
                    if (e1 != null && e1.EkmekVar) Kilitle();
                }
                else
                {
                    // HEDEF BOS = sahnedeki TUM sarma kagitlarina bak.
                    // (Sarma kagidi oyun sirasinda uretildigi icin Inspector'dan referans verilemez.)
                    var sistemler = EkmekSistemleriniAl();
                    for (int i = 0; i < sistemler.Length; i++)
                        if (sistemler[i] != null && sistemler[i].EkmekVar) { Kilitle(); break; }
                }
                break;

            case TutorialKosulTipi.MalzemeEklendi:
                if (hedef != null)
                {
                    EkmekMalzemeAlici alici = HedefBul<EkmekMalzemeAlici>();
                    if (alici != null && (alici.ToplamSlotSayisi - alici.BosSlotSayisi) >= sayi) Kilitle();
                }
                else
                {
                    // HEDEF BOS = sahnedeki herhangi bir sarma kagidinda "Sayi" kadar malzeme var mi?
                    var sistemler = EkmekSistemleriniAl();
                    for (int i = 0; i < sistemler.Length; i++)
                    {
                        EkmekMalzemeAlici a = sistemler[i] != null ? sistemler[i].MalzemeAlici : null;
                        if (a == null) continue;

                        if ((a.ToplamSlotSayisi - a.BosSlotSayisi) >= Mathf.Max(1, sayi)) { Kilitle(); break; }
                    }
                }
                break;

            case TutorialKosulTipi.SandvicSarildi:
                if (hedef != null)
                {
                    EkmekYerlestirmeSistemi e2 = HedefBul<EkmekYerlestirmeSistemi>();
                    if (e2 != null && e2.SarmaYapildiMi) Kilitle();
                }
                else
                {
                    var sistemler = EkmekSistemleriniAl();
                    for (int i = 0; i < sistemler.Length; i++)
                        if (sistemler[i] != null && sistemler[i].SarmaYapildiMi) { Kilitle(); break; }
                }
                break;

            case TutorialKosulTipi.BalikCevrildi:
                if (CevrilmisBalikSayisi() >= Mathf.Max(1, sayi)) Kilitle();
                break;

            case TutorialKosulTipi.SarmaKagidiAlindi:
                {
                    // Sarma kagidi prefabinda EkmekYerlestirmeSistemi var. Kagit yigindan
                    // ALINDIGI anda sahnede yeni bir tane olusur. Sahnede zaten duran
                    // tezgahlari saymamak icin ADIM BASLARKENKI sayiyi temel aliriz.
                    if (baslangicSayisi < 0)
                    {
                        baslangicSayisi = Object.FindObjectsOfType<EkmekYerlestirmeSistemi>().Length;
                        break;
                    }

                    if (EkmekSistemleriniAl().Length >= baslangicSayisi + Mathf.Max(1, sayi))
                        Kilitle();
                }
                break;

            case TutorialKosulTipi.DukkanAcildi:
                {
                    ZamanSistemi z = HedefBul<ZamanSistemi>();
                    if (z == null) z = TutorialYoneticisi.ZamanSisteminiBul();
                    if (z != null && z.AcikMi()) Kilitle();
                }
                break;

            case TutorialKosulTipi.IzgaradaBalikVar:
                {
                    PisirmeSistemi pis = HedefBul<PisirmeSistemi>();
                    if (pis != null && pis.NesneSayisi >= sayi) Kilitle();
                }
                break;

            case TutorialKosulTipi.KuvetteUrunVar:
                {
                    KuvetSistemi kuvet = HedefBul<KuvetSistemi>();
                    if (kuvet != null && kuvet.AktifModelSayisi >= sayi) Kilitle();
                }
                break;

            case TutorialKosulTipi.SahnedeUrunVar:
                if (TagliObjeSayisi(hedefTag) >= Mathf.Max(1, sayi)) Kilitle();
                break;

            case TutorialKosulTipi.GunGeldi:
                {
                    GunYoneticisi gy = HedefBul<GunYoneticisi>();
                    if (gy == null) gy = GunYoneticisi.Instance;

                    // Sayi = hedef gun. Ornek: Sayi 2 -> 2. gune gecilince saglanir.
                    if (gy != null && gy.Gun >= Mathf.Max(1, sayi)) Kilitle();
                }
                break;
        }

        return saglandi;
    }

    // ================== OLAY BILDIRIMLERI ==================

    /// <summary>Bir nesne yere konuldu (NesneYerlestirmeSistemi event'i).</summary>
    public void NesneYerlestirildiBildir(GameObject yerlesen, GameObject kaynakPrefab)
    {
        if (saglandi || tip != TutorialKosulTipi.NesneYerlestirildi) return;
        if (hedefPrefab == null) return;

        // Magazadan alindiysa kaynak prefab birebir eslesir.
        // Elde tasinan bir nesne birakildiysa kaynak yoktur; ad uzerinden esleriz
        // ("Domates(Clone)" gibi adlar hedefPrefab adiyla baslar).
        bool eslesti = kaynakPrefab == hedefPrefab ||
                       (kaynakPrefab == null && yerlesen != null &&
                        yerlesen.name.StartsWith(hedefPrefab.name, System.StringComparison.OrdinalIgnoreCase));

        if (!eslesti) return;

        sayac++;
        if (sayac >= Mathf.Max(1, sayi)) Kilitle();
    }

    /// <summary>Bir kesim tamamlandi (BasiliTutmaYoneticisi event'i).</summary>
    public void KesimBildir()
    {
        if (saglandi || tip != TutorialKosulTipi.KesimYapildi) return;

        sayac++;
        if (sayac >= Mathf.Max(1, sayi)) Kilitle();
    }

    /// <summary>Para kazanildi (EkonomiYoneticisi ParaDegisti, artis yonunde).</summary>
    public void ParaKazanildiBildir()
    {
        if (saglandi || tip != TutorialKosulTipi.ParaKazanildi) return;

        sayac++;
        if (sayac >= Mathf.Max(1, sayi)) Kilitle();
    }

    /// <summary>
    /// Tus kontrolu. HER FRAME cagrilir (0.2 sn'lik dongu hizli basisi kacirir).
    /// true donerse sart bu karede saglandi.
    /// </summary>
    public bool TusKontrolEt()
    {
        if (saglandi || tip != TutorialKosulTipi.TusaBasildi) return false;
        if (!Input.GetKeyDown(tus)) return false;

        Kilitle();
        return true;
    }

    /// <summary>Disaridan manuel tetikleme (UnityEvent vb.).</summary>
    public void ManuelBildir(string id)
    {
        if (saglandi || tip != TutorialKosulTipi.Manuel) return;
        if (string.IsNullOrEmpty(manuelId) || manuelId != id) return;

        sayac++;
        if (sayac >= Mathf.Max(1, sayi)) Kilitle();
    }

    // Sahnedeki bitki taramasi icin ortak onbellek: ayni karede birden fazla kosul
    // sorarsa sahne bir kez taranir.
    private static float sonTaramaZamani = -1f;
    private static int sonSulanmisSayi;

    /// <summary>
    /// Sahnedeki SULANMIS bitki sayisi. Yeni dikilen bitki susuz baslar,
    /// oyuncu suladiginda bu sayi artar.
    /// </summary>
    private static int SulanmisBitkiSayisi()
    {
        // 0.15 sn'den yeni bir tarama varsa onu kullan (bosuna sahne taramayalim)
        if (Time.time - sonTaramaZamani < 0.15f) return sonSulanmisSayi;
        sonTaramaZamani = Time.time;

        BitkiBuyumeSistemi[] hepsi = Object.FindObjectsOfType<BitkiBuyumeSistemi>();

        int sayac = 0;
        for (int i = 0; i < hepsi.Length; i++)
            if (hepsi[i] != null && !hepsi[i].SusuzMu) sayac++;

        sonSulanmisSayi = sayac;
        return sayac;
    }

    // Tag taramasi icin onbellek (ayni karede birden fazla kosul sorabilir)
    private static float sonTagTaramasi = -1f;
    private static string sonTarananTag;
    private static int sonTagSayisi;
    private static bool tagUyarisiVerildi;

    /// <summary>Sahnede verilen tag'e sahip kac obje var? Tag tanimli degilse 0 doner.</summary>
    private static int TagliObjeSayisi(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return 0;

        if (tag == sonTarananTag && Time.time - sonTagTaramasi < 0.15f) return sonTagSayisi;

        sonTagTaramasi = Time.time;
        sonTarananTag = tag;

        try
        {
            sonTagSayisi = GameObject.FindGameObjectsWithTag(tag).Length;
        }
        catch (UnityException)
        {
            if (!tagUyarisiVerildi)
            {
                tagUyarisiVerildi = true;
                Debug.LogWarning($"[TutorialKosulu] '{tag}' tag'i Unity'de TANIMLI DEGIL. " +
                                 "Tag'i olustur ya da dogru yazdigindan emin ol.");
            }
            sonTagSayisi = 0;
        }

        return sonTagSayisi;
    }

    // --- Sarma kagitlari icin ortak onbellek ---
    private static float sonEkmekTaramasi = -1f;
    private static EkmekYerlestirmeSistemi[] sonEkmekSistemleri;

    /// <summary>
    /// Sahnedeki tum sarma kagitlari/ekmek tezgahlari. Sarma kagitlari oyun sirasinda
    /// uretildigi icin her seferinde yeniden taranmali; 0.15 sn onbellekle ucuzlatilir.
    /// </summary>
    private static EkmekYerlestirmeSistemi[] EkmekSistemleriniAl()
    {
        if (sonEkmekSistemleri != null && Time.time - sonEkmekTaramasi < 0.15f)
            return sonEkmekSistemleri;

        sonEkmekTaramasi = Time.time;
        sonEkmekSistemleri = Object.FindObjectsOfType<EkmekYerlestirmeSistemi>();
        return sonEkmekSistemleri;
    }

    // --- Cevrilmis balik sayimi icin ortak onbellek ---
    private static float sonBalikTaramasi = -1f;
    private static int sonCevrilmisSayi;

    /// <summary>
    /// Izgarada E tusuyla CEVRILMIS balik sayisi.
    /// PisirilebilirNesne.Cevirildi bayragini okur (oyun koduna dokunulmaz).
    /// </summary>
    private static int CevrilmisBalikSayisi()
    {
        if (Time.time - sonBalikTaramasi < 0.15f) return sonCevrilmisSayi;
        sonBalikTaramasi = Time.time;

        PisirilebilirNesne[] hepsi = Object.FindObjectsOfType<PisirilebilirNesne>();

        int sayac = 0;
        for (int i = 0; i < hepsi.Length; i++)
            if (hepsi[i] != null && hepsi[i].Cevirildi) sayac++;

        sonCevrilmisSayi = sayac;
        return sayac;
    }

    /// <summary>Teshis logu icin okunakli ad.</summary>
    public string Ad()
    {
        if (tip == TutorialKosulTipi.NesneYerlestirildi && hedefPrefab != null)
            return tip + "(" + hedefPrefab.name + ")";
        if (hedef != null)
            return tip + "(" + hedef.gameObject.name + ")";
        return tip.ToString();
    }
}
// ===== TUTORIAL SONU =====
