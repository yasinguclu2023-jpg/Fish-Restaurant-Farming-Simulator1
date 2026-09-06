using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Her sebze tag'i için görsel ve çıkış prefabı eşleştirmesi.
/// Inspector'da liste olarak görünür.
/// </summary>
[System.Serializable]
public class KuvetUrunTanimi
{
    [Tooltip("Kabul edilen sebze tag'i (ör: KesikDomates)")]
    public string tag;

    [Tooltip("Bu tag koyulunca spawn olacak görsel prefab (içinde modeller var)")]
    public GameObject gorselPrefab;

    [Header("Çıkış Prefabları")]
    [Tooltip("Küvetten alınca elde beliren prefab (zorunlu - varsayılan çıkış)")]
    public GameObject cikisPrefab;

    [Tooltip("Çıkış prefabının tag'i (el pozisyonu bulmak için)")]
    public string cikisTag;

    [Header("Alternatif Alma Prefabı (Opsiyonel)")]
    [Tooltip("Küvetten alınca farklı bir prefab elde edilsin mi? Boş bırakırsan cikisPrefab kullanılır.")]
    public GameObject almaPrefabi;

    [Tooltip("Alma prefabının tag'i (el pozisyonu bulmak için). Boş bırakırsan cikisTag kullanılır.")]
    public string almaTag;

    [Header("Ürüne Özel Miktar (0 = genel ayarı kullan)")]
    [Tooltip("Bu ürün koyulunca kaç model açılsın. 0 bırakırsan genel koymaMiktari kullanılır.")]
    public int koymaMiktari = 0;

    [Tooltip("Bu üründen alınca kaç model kapansın. 0 bırakırsan genel almaMiktari kullanılır.")]
    public int almaMiktari = 0;

    [Tooltip("Bu ürün için maksimum kapasite. 0 bırakırsan genel kapasite kullanılır.")]
    public int maksimumKapasite = 0;

    /// <summary>Alma prefabı tanımlı mı?</summary>
    public bool AlmaPrefabiVar => almaPrefabi != null;

    /// <summary>Efektif alma prefabı — almaPrefabi varsa onu, yoksa cikisPrefab döner</summary>
    public GameObject EfektifAlmaPrefab => almaPrefabi != null ? almaPrefabi : cikisPrefab;

    /// <summary>Efektif alma tag'i — almaTag doluysa onu, yoksa cikisTag döner</summary>
    public string EfektifAlmaTag => !string.IsNullOrEmpty(almaTag) ? almaTag : cikisTag;
}

/// <summary>
/// Küvet Sistemi - Kesilmiş sebzeleri toplamak için kullanılır.
/// Aynı anda sadece TEK BİR sebze türü barındırır.
/// İlk koyulan sebzenin türüne kilitlenir, tamamen boşalınca serbest kalır.
/// 
/// Prefab sistemi:
/// - cikisPrefab (zorunlu): Küvetten alınca elde beliren varsayılan prefab
/// - almaPrefabi (opsiyonel): Küvetten alınca farklı bir prefab isteniyorsa
///   Boş bırakılırsa → cikisPrefab kullanılır (eski davranış)
///   Dolu ise → almaPrefabi kullanılır
/// </summary>
public class KuvetSistemi : MonoBehaviour, ICopEtkilesimi, IIcerikliKap
{
    [Header("Ürün Tanımları")]
    [Tooltip("Her sebze tag'i için görsel ve çıkış prefabı eşleştirmesi")]
    [SerializeField] private List<KuvetUrunTanimi> urunTanimlari = new List<KuvetUrunTanimi>();

    [Header("Spawn Ayarları")]
    [Tooltip("Prefablar nerede spawn olacak? Boş bırakırsan küvetin pozisyonunda.")]
    [SerializeField] private Transform spawnPozisyonu;

    [Header("Genel Miktar Ayarları (Fallback)")]
    [Tooltip("Ürün tanımında 0 bırakılırsa bu değer kullanılır")]
    [SerializeField] private int koymaMiktari = 4;

    [Tooltip("Ürün tanımında 0 bırakılırsa bu değer kullanılır")]
    [SerializeField] private int almaMiktari = 1;

    [Header("Genel Kapasite (Fallback)")]
    [Tooltip("Ürün tanımında 0 bırakılırsa bu değer kullanılır. Görsel prefab'daki child sayısını geçemez.")]
    [SerializeField] private int maksimumKapasite = 20;

    // Runtime - aktif tür verisi
    private KuvetUrunTanimi aktifTanim;
    private GameObject gorselInstance;
    private Transform[] modeller;
    private int aktifModelSayisi;
    private bool kilitli;

    // Runtime - aktif ürünün efektif değerleri
    private int aktifKoymaMiktari;
    private int aktifAlmaMiktari;
    private int aktifMaksKapasite;

    private int EfektifDeger(int urunDegeri, int genelDeger)
    {
        return urunDegeri > 0 ? urunDegeri : genelDeger;
    }

    void Awake()
    {
        DurumuGeriYukle();
    }

    /// <summary>
    /// Prefab'a gömülü görsel child'dan küvetin doluluk durumunu yeniden kurar.
    /// Dolu bir küvetten prefab yapılıp sahneye eklenince runtime alanları (kilitli,
    /// aktifModelSayisi, aktifTanim, modeller) sıfırlanır; görsel modeller ise child
    /// olarak prefab'a gömülü kalır. Bu metot durumu o child hiyerarşisinden geri yükler,
    /// böylece UrunVarMi tekrar doğru döner ve domatesler alınabilir.
    /// (Bkz. KasaSistemi.Awake / ChildSlotlariOlustur — aynı desen.)
    /// </summary>
    void DurumuGeriYukle()
    {
        if (kilitli) return; // Zaten kurulu (runtime'da doldurulmuş) - dokunma

        for (int c = 0; c < transform.childCount; c++)
        {
            Transform child = transform.GetChild(c);
            KuvetUrunTanimi tanim = TanimBulByGorselAdi(child.name);
            if (tanim == null) continue;

            // Prefab'a gömülü görsel instance bulundu - PrefabSpawnEt ile aynı mantık
            aktifTanim = tanim;
            gorselInstance = child.gameObject;
            aktifKoymaMiktari = EfektifDeger(tanim.koymaMiktari, koymaMiktari);
            aktifAlmaMiktari = EfektifDeger(tanim.almaMiktari, almaMiktari);
            aktifMaksKapasite = EfektifDeger(tanim.maksimumKapasite, maksimumKapasite);

            int childCount = child.childCount;
            int modelSayisi = Mathf.Min(childCount, aktifMaksKapasite);
            modeller = new Transform[modelSayisi];

            // Modeller alttan (0'dan) yukarı açılıp yukarıdan aşağı kapandığı için aktif
            // olanlar modeller[0..n-1] aralığında bitişiktir. Mevcut aktif/pasif durumu KORUNUR.
            int aktifSayac = 0;
            for (int i = 0; i < modelSayisi; i++)
            {
                modeller[i] = child.GetChild(i);
                if (modeller[i].gameObject.activeSelf)
                    aktifSayac++;
            }

            if (aktifMaksKapasite > childCount)
                aktifMaksKapasite = childCount;

            aktifModelSayisi = aktifSayac;
            kilitli = true;

            Debug.Log($"[Kuvet] Prefab durumu geri yüklendi: '{aktifTanim.tag}'. Model: {aktifModelSayisi}/{aktifMaksKapasite}");
            break;
        }
    }

    /// <summary>
    /// Sebzeyi küvete koy. Sebze yok edilir, modeller açılır.
    /// </summary>
    public bool SebzeKoy(GameObject sebze)
    {
        if (sebze == null) return false;
        if (!NesneKabulEdilirMi(sebze)) return false;

        string tag = sebze.tag;
        KuvetUrunTanimi tanim = TanimBul(tag);
        if (tanim == null) return false;

        // İlk sebze - türe kilitle ve prefab spawn et
        if (!kilitli)
        {
            aktifTanim = tanim;
            aktifKoymaMiktari = EfektifDeger(tanim.koymaMiktari, koymaMiktari);
            aktifAlmaMiktari = EfektifDeger(tanim.almaMiktari, almaMiktari);
            aktifMaksKapasite = EfektifDeger(tanim.maksimumKapasite, maksimumKapasite);

            if (!PrefabSpawnEt()) return false;
            kilitli = true;
        }

        if (DoluMu) return false;

        // Küvetle daha önce temas etmiş mi kontrol et
        int eklenecekMiktar;
        KuvetDilimVerisi dilimVerisi = sebze.GetComponent<KuvetDilimVerisi>();

        if (dilimVerisi != null)
            eklenecekMiktar = dilimVerisi.Miktar;
        else
            eklenecekMiktar = aktifKoymaMiktari;

        // Modelleri aç
        int acilacak = Mathf.Min(eklenecekMiktar, aktifMaksKapasite - aktifModelSayisi);
        for (int i = 0; i < acilacak; i++)
        {
            if (aktifModelSayisi < modeller.Length)
            {
                modeller[aktifModelSayisi].gameObject.SetActive(true);
                aktifModelSayisi++;
            }
        }

        // Sebzeyi yok et
        Destroy(sebze);

        Debug.Log($"[Kuvet] '{tag}' eklendi. Model: {aktifModelSayisi}/{aktifMaksKapasite}");
        return true;
    }

    /// <summary>
    /// Küvetten ürün al. Model kapanır, çıkış prefabı döner.
    /// almaPrefabi tanımlıysa o kullanılır, yoksa cikisPrefab kullanılır.
    /// Tamamen boşalırsa küvet serbest kalır.
    /// </summary>
    public GameObject UrunAl()
    {
        if (!kilitli || aktifModelSayisi <= 0 || aktifTanim == null)
            return null;

        // Efektif prefab ve tag'i al (almaPrefabi varsa onu, yoksa cikisPrefab)
        GameObject prefab = aktifTanim.EfektifAlmaPrefab;
        string efektifTag = aktifTanim.EfektifAlmaTag;

        if (prefab == null)
        {
            Debug.LogError($"[Kuvet] '{aktifTanim.tag}' için çıkış prefabı atanmamış!");
            return null;
        }

        int almaM = aktifAlmaMiktari;

        // Modelleri kapat (alttan yukarı)
        int kapanacak = Mathf.Min(almaM, aktifModelSayisi);
        for (int i = 0; i < kapanacak; i++)
        {
            aktifModelSayisi--;
            if (aktifModelSayisi >= 0 && aktifModelSayisi < modeller.Length)
            {
                modeller[aktifModelSayisi].gameObject.SetActive(false);
            }
        }

        // Çıkış prefabı oluştur
        GameObject cikis = Instantiate(prefab);
        cikis.name = prefab.name;

        if (!string.IsNullOrEmpty(efektifTag))
            cikis.tag = efektifTag;

        // Dilim verisini ekle
        KuvetDilimVerisi dilim = cikis.AddComponent<KuvetDilimVerisi>();
        dilim.MiktarAyarla(almaM);

        Debug.Log($"[Kuvet] Ürün alındı: '{cikis.name}' (tag: {efektifTag}). Kalan: {aktifModelSayisi}/{aktifMaksKapasite}");

        // Tamamen boşaldıysa serbest bırak
        if (aktifModelSayisi <= 0)
        {
            KuvetiSerbestBirak();
        }

        return cikis;
    }

    /// <summary>
    /// Bu nesne küvete konabilir mi?
    /// </summary>
    public bool NesneKabulEdilirMi(GameObject nesne)
    {
        if (nesne == null) return false;

        // Parent altındaysa (depoda) sebze kabul etme
        if (transform.parent != null) return false;

        // Tanım listesinde var mı?
        KuvetUrunTanimi tanim = TanimBul(nesne.tag);
        if (tanim == null) return false;

        // Kilitli değilse ve tanım varsa kabul et
        if (!kilitli) return true;

        // Kilitliyse sadece aynı türü kabul et
        if (aktifTanim.tag != nesne.tag) return false;

        // Dolu mu?
        return !DoluMu;
    }

    KuvetUrunTanimi TanimBul(string tag)
    {
        for (int i = 0; i < urunTanimlari.Count; i++)
        {
            if (urunTanimlari[i].tag == tag)
                return urunTanimlari[i];
        }
        return null;
    }

    /// <summary>
    /// Görsel instance'ın adına göre tanım bulur.
    /// İsim formatı PrefabSpawnEt ile aynı: gorselPrefab.name + "_" + tag.
    /// Prefab'a gömülü görsel child'ı geri tanımak için kullanılır.
    /// </summary>
    KuvetUrunTanimi TanimBulByGorselAdi(string childAdi)
    {
        for (int i = 0; i < urunTanimlari.Count; i++)
        {
            KuvetUrunTanimi t = urunTanimlari[i];
            if (t.gorselPrefab == null) continue;
            if (childAdi == t.gorselPrefab.name + "_" + t.tag)
                return t;
        }
        return null;
    }

    bool PrefabSpawnEt()
    {
        if (aktifTanim == null || aktifTanim.gorselPrefab == null)
        {
            Debug.LogError($"[Kuvet] Görsel prefab atanmamış!");
            return false;
        }

        Vector3 pos = spawnPozisyonu != null ? spawnPozisyonu.position : transform.position;
        Quaternion rot = spawnPozisyonu != null ? spawnPozisyonu.rotation : transform.rotation;

        gorselInstance = Instantiate(aktifTanim.gorselPrefab, pos, rot);
        gorselInstance.name = aktifTanim.gorselPrefab.name + "_" + aktifTanim.tag;
        gorselInstance.transform.SetParent(transform);

        int childCount = gorselInstance.transform.childCount;
        int modelSayisi = Mathf.Min(childCount, aktifMaksKapasite);
        modeller = new Transform[modelSayisi];

        for (int i = 0; i < modelSayisi; i++)
        {
            modeller[i] = gorselInstance.transform.GetChild(i);
            modeller[i].gameObject.SetActive(false);
        }

        if (aktifMaksKapasite > childCount)
            aktifMaksKapasite = childCount;

        aktifModelSayisi = 0;

        Debug.Log($"[Kuvet] '{aktifTanim.tag}' prefabı spawn edildi. Model: {modelSayisi}, Kapasite: {aktifMaksKapasite}");
        return true;
    }

    void KuvetiSerbestBirak()
    {
        if (gorselInstance != null)
        {
            Destroy(gorselInstance);
            gorselInstance = null;
        }

        modeller = null;
        aktifModelSayisi = 0;
        aktifTanim = null;
        kilitli = false;
        aktifKoymaMiktari = 0;
        aktifAlmaMiktari = 0;
        aktifMaksKapasite = 0;

        Debug.Log("[Kuvet] Küvet boşaldı, serbest bırakıldı. Artık başka tür kabul edebilir.");
    }

    // ===== GUN SONU (IIcerikliKap) =====

    void OnEnable()
    {
        IcerikliKapDefteri.Kaydet(this);
    }

    void OnDisable()
    {
        IcerikliKapDefteri.Sil(this);
    }

    /// <summary>Depo icinde mi kontrolu icin kabin dunya konumu.</summary>
    public Vector3 KapKonumu => transform.position;

    /// <summary>
    /// Gun sonu temizligi: kuvetin icini (gorsel instance + modeller) siler, kuvet kalir.
    /// Kuvet tekrar serbest kalir; ertesi gun baska bir sebzeye kilitlenebilir.
    /// </summary>
    public void IciniBosalt()
    {
        KuvetiSerbestBirak();
    }

    // ===== COP KOVASI =====

    /// <summary>
    /// Cop kovasina atilinca: kuvetin icini (gorsel + modeller) temizler, kuvet kalir.
    /// Her zaman true (kuvet silinmez, sadece bosalir).
    /// </summary>
    public bool CopeAtildi()
    {
        KuvetiSerbestBirak();
        return true;
    }

    // ===== PUBLIC PROPERTIES =====

    /// <summary>Küvette ürün var mı?</summary>
    public bool UrunVarMi => kilitli && aktifModelSayisi > 0;

    /// <summary>Küvet dolu mu?</summary>
    public bool DoluMu => aktifModelSayisi >= aktifMaksKapasite;

    /// <summary>Küvet bir türe kilitli mi?</summary>
    public bool Kilitli => kilitli;

    /// <summary>Aktif model sayısı</summary>
    public int AktifModelSayisi => aktifModelSayisi;

    /// <summary>Maksimum kapasite (aktif ürünün efektif kapasitesi)</summary>
    public int MaksKapasite => kilitli ? aktifMaksKapasite : maksimumKapasite;

    /// <summary>Aktif türün çıkış tag'i (almaPrefabi varsa almaTag, yoksa cikisTag)</summary>
    public string CikisTag => aktifTanim != null ? aktifTanim.EfektifAlmaTag : "";

    /// <summary>Aktif türün tag'i (hangi sebzeye kilitli)</summary>
    public string AktifTag => aktifTanim != null ? aktifTanim.tag : "";
}