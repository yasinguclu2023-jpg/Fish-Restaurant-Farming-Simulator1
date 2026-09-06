using System.Collections.Generic;
using UnityEngine;

// ===== TUTORIAL (kaldirilabilir) =====

/// <summary>
/// TEK BIR TUTORIAL ADIMI = senin Canvas'ta tasarladigin BIR PANEL + onu kapatan sartlar.
///
/// Panel acilir, sartlarin HEPSI saglaninca kapanir ve sonraki panele gecilir.
/// Panelin icinde ne yazdigini kod BILMEZ; sadece acar/kapatir.
/// </summary>
[System.Serializable]
public class TutorialAdimi
{
    [Tooltip("Sadece Inspector'da okumak icin (orn. 'Bugday ve domates ek'). Oyunu etkilemez.")]
    public string adimAdi = "Adim";

    [Tooltip("Acilip kapanacak obje. Panelin KENDISI olabilir, panelin altindaki IMAGE de olabilir. " +
             "BASLANGICTA KAPALI (SetActive false) birak, kod acar.\n" +
             "DIKKAT: Child bir objeyi verirsen PARENT'IN ACIK KALMASI gerekir, yoksa gorunmez.")]
    public GameObject panelObjesi;

    [Tooltip("Panelle BIRLIKTE acilip kapanacak ek objeler. Image ile yazi KARDES ise " +
             "(biri digerinin child'i degilse) yaziyi buraya ekle. Bos birakilabilir.")]
    public GameObject[] ekObjeler;

    [Tooltip("Bu panelin kapanmasi icin saglanmasi gereken sartlar. HEPSI saglanmali.")]
    public List<TutorialKosulu> kosullar = new List<TutorialKosulu>();

    [Header("Opsiyonel")]
    [Tooltip("Bir onceki panel kapandiktan kac saniye sonra bu panel acilsin?")]
    [Min(0f)] public float acilisGecikmesi = 0.5f;

    [Tooltip("Bu adimda ORTAK IKONUN gosterecegi dunya noktasi (bahce, olta standi, makine...). " +
             "Duvarlarin arkasindan da gorunur. BOS BIRAKIRSAN bu adimda ikon cikmaz.")]
    public Transform hedefNoktasi;

    [Tooltip("Panel acikken birlikte acilacak KENDI ozel isaret objen. " +
             "Ortak ikonu kullaniyorsan bunu bos birak.")]
    public GameObject hedefIsareti;

    [Tooltip("Panel acilirken calacak ses.")]
    public SesVerisi acilisSesi;

    [Tooltip("Panel kapanirken (adim tamamlaninca) calacak ses.")]
    public SesVerisi kapanisSesi;

    /// <summary>Tum sartlar saglandi mi?</summary>
    public bool TamamlandiMi
    {
        get
        {
            if (kosullar == null || kosullar.Count == 0) return true;

            for (int i = 0; i < kosullar.Count; i++)
                if (kosullar[i] != null && !kosullar[i].Saglandi) return false;

            return true;
        }
    }

    /// <summary>
    /// Duvar arkasi ikonunun SU AN gosterecegi nokta.
    ///
    /// Kural: sartlar sirayla gezilir, HENUZ SAGLANMAMIS ve hedef noktasi olan ILK sart
    /// kazanir. Boylece oyuncu bir isi bitirince ikon kendiliginden sonraki ise gecer,
    /// hicbiri kalmayinca kaybolur (panel hala acik olsa bile).
    ///
    /// Hicbir sartta hedef noktasi tanimli degilse adim seviyesindeki "hedefNoktasi" kullanilir.
    /// </summary>
    public Transform AktifHedefNoktasi()
    {
        bool kosuldaHedefVar = false;

        if (kosullar != null)
        {
            for (int i = 0; i < kosullar.Count; i++)
            {
                TutorialKosulu k = kosullar[i];
                if (k == null || k.hedefNoktasi == null) continue;

                kosuldaHedefVar = true;

                if (!k.Saglandi) return k.hedefNoktasi;
            }
        }

        // Sart bazli hedefler kullaniliyorsa ve hepsi bittiyse: ikon kalksin.
        if (kosuldaHedefVar) return null;

        return hedefNoktasi;
    }

    /// <summary>Durum tabanli sartlari kontrol eder.</summary>
    public void Guncelle()
    {
        if (kosullar == null) return;

        for (int i = 0; i < kosullar.Count; i++)
            if (kosullar[i] != null) kosullar[i].Guncelle();
    }

    /// <summary>
    /// Tus sartlarini kontrol eder. HER FRAME cagrilmali.
    /// Bir sart bu karede saglandiysa true doner.
    /// </summary>
    public bool TuslariKontrolEt()
    {
        if (kosullar == null) return false;

        bool degisti = false;

        for (int i = 0; i < kosullar.Count; i++)
            if (kosullar[i] != null && kosullar[i].TusKontrolEt()) degisti = true;

        return degisti;
    }

    /// <summary>Adim baslarken / akis sifirlanirken.</summary>
    public void Sifirla()
    {
        if (kosullar == null) return;

        for (int i = 0; i < kosullar.Count; i++)
        {
            if (kosullar[i] == null) continue;

            kosullar[i].Sifirla();

            // Bolge sartlari kendi ic durumunu da sifirlamali.
            // (Inspector'dan yanlis bilesen surukIenmis olabilir -> objede de ara.)
            MonoBehaviour h = kosullar[i].hedef;
            if (h != null)
            {
                TutorialBolgesi bolge = h as TutorialBolgesi;
                if (bolge == null) bolge = h.GetComponent<TutorialBolgesi>();
                if (bolge != null) bolge.Sifirla();
            }
        }
    }

    /// <summary>Paneli (ve varsa ek objeleri, hedef isaretini) ac/kapat.</summary>
    public void Goster(bool goster)
    {
        if (panelObjesi != null) panelObjesi.SetActive(goster);

        if (ekObjeler != null)
        {
            for (int i = 0; i < ekObjeler.Length; i++)
                if (ekObjeler[i] != null) ekObjeler[i].SetActive(goster);
        }

        if (hedefIsareti != null) hedefIsareti.SetActive(goster);
    }
}

/// <summary>
/// BIR GUNUN TUTORIAL AKISI. Panel sirasi burada durur.
///
/// NOT: Canvas panelleri SAHNE objesi oldugu icin bu bir ScriptableObject DEGIL,
/// sahnede duran bir bilesendir (ScriptableObject sahne referansi tutamaz).
///
/// KURULUM:
/// 1. Sahnede bos bir GameObject olustur, adini "TutorialAkisi_Gun1" yap.
/// 2. Bu scripti ekle.
/// 3. "Adimlar" listesine her panel icin bir eleman ekle:
///    - Panel Objesi  -> Canvas'taki panelin kendisi (KAPALI birak)
///    - Kosullar      -> paneli kapatacak sartlar
/// 4. TutorialYoneticisi'nin "Gun Akislari" listesinde bu objeyi 1. gune bagla.
/// </summary>
public class TutorialAkisi : MonoBehaviour
{
    [Tooltip("Bu akis hangi gun icin? Sadece Inspector'da okumak icin; asil eslesme " +
             "TutorialYoneticisi'nde yapilir.")]
    [SerializeField] private string aciklama = "Gun 1 akisi";

    /// <summary>Inspector'a yazilan serbest aciklama (sadece okumak icin).</summary>
    public string Aciklama => aciklama;

    [Tooltip("Panel sirasi. Yukaridan asagiya oynatilir.")]
    [SerializeField] private List<TutorialAdimi> adimlar = new List<TutorialAdimi>();

    /// <summary>Akistaki adim sayisi.</summary>
    public int AdimSayisi => adimlar != null ? adimlar.Count : 0;

    /// <summary>Sirasi verilen adimi dondurur (yoksa null).</summary>
    public TutorialAdimi AdimAl(int index)
    {
        if (adimlar == null || index < 0 || index >= adimlar.Count) return null;
        return adimlar[index];
    }

    /// <summary>Tum panelleri kapatir ve sartlari sifirlar.</summary>
    public void Sifirla()
    {
        if (adimlar == null) return;

        for (int i = 0; i < adimlar.Count; i++)
        {
            if (adimlar[i] == null) continue;

            adimlar[i].Sifirla();
            adimlar[i].Goster(false);
        }
    }

    /// <summary>Olay bildirimlerini SADECE aktif adima iletmek icin yoneticiden cagrilir.</summary>
    public TutorialAdimi AktifAdim(int index) => AdimAl(index);

    void Awake()
    {
        // Guvenlik: sahnede yanlislikla acik birakilmis panel varsa kapat.
        Sifirla();
    }
}
// ===== TUTORIAL SONU =====
