using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class YerlestirilebilirNesneData
{
    public string nesneAdi;
    public GameObject nesnePrefab;
    public Sprite nesneIkonu;

    [Tooltip("Satın alma fiyatı. 0 = bedava. Para, nesne YERE KONDUĞUNDA düşer; " +
             "yerleştirmeyi iptal edersen para gitmez.")]
    public int fiyat = 0;

    [Tooltip("Fiyatın yazılacağı TextMeshPro. Boş bırakılırsa butonun altında adında " +
             "'fiyat' geçen bir yazı otomatik aranır.")]
    public TMP_Text fiyatYazisi;

    // ===== DEMO GUN SISTEMI (kaldirilabilir) =====
    [Header("Demo: Açılış Günü")]
    [Tooltip("Bu ürün kaçıncı günde satın alınabilir olsun? 1 = baştan açık.\n" +
             "Sahnede DemoGunSistemi YOKSA bu alan yok sayılır (her şey açık gelir).")]
    [Min(1)]
    public int acilisGunu = 1;

    [Tooltip("Kilitliyken açılacak obje (kilit ikonu, karartma paneli...). Opsiyonel.")]
    public GameObject kilitKaplamasi;

    [Tooltip("Kilitliyken 'Gün 3' yazacak TextMeshPro. Boş bırakılırsa butonun altında " +
             "adında 'kilit' geçen bir yazı otomatik aranır.")]
    public TMP_Text kilitYazisi;
    // ===== DEMO GUN SISTEMI SONU =====

    [TextArea(1, 3)]
    public string aciklama;
}

/// <summary>
/// Bir kategori (Sebzeler, Makineler vb.) için tüm bilgileri tutar
/// </summary>
[System.Serializable]
public class KategoriData
{
    [Tooltip("Kategori adı (örn: Sebzeler, Makineler)")]
    public string kategoriAdi;

    [Tooltip("Bu kategoriye ait paneli buraya sürükle (içinde butonlar olan panel)")]
    public GameObject kategoriPaneli;

    [Tooltip("Üstteki sekme butonu (Sebzeler/Makineler tuşu)")]
    public Button sekmeButonu;

    [Tooltip("Bu kategoriye ait yerleştirilebilir nesneler")]
    public YerlestirilebilirNesneData[] nesneler;

    [Tooltip("Bu kategorideki nesne butonları (panel içindeki butonlar - sırayla)")]
    public Button[] nesneButonlari;
}

public class UIYerlestirmeSistemi : MonoBehaviour
{
    [Header("Kategoriler")]
    [SerializeField] private KategoriData[] kategoriler;

    [Header("Başlangıç Ayarları")]
    [Tooltip("UI ilk açıldığında hangi kategori gösterilsin? (0 = ilk kategori)")]
    [SerializeField] private int baslangicKategorisi = 0;

    [Header("Referanslar")]
    [SerializeField] private UIYoneticisi uiYoneticisi;

    [Header("Elde Nesne Uyarısı")]
    [Tooltip("Elde/yerleştirmede nesne varken yeni seçim yapılınca gösterilecek uyarı paneli")]
    [SerializeField] private GameObject uyariPaneli;

    [Tooltip("Uyarı panelinin ekranda kalma süresi (saniye)")]
    [SerializeField] private float uyariGosterimSuresi = 2f;

    [Header("Satın Alma")]
    [Tooltip("Fiyat yazısı formatı. {0} fiyatın geleceği yerdir. Örnek: \"{0} TL\"")]
    [SerializeField] private string fiyatFormati = "{0} TL";

    [Tooltip("Fiyat 0 ise yazıyı gizle (bedava ürün).")]
    [SerializeField] private bool bedavaysaFiyatiGizle = true;

    [Tooltip("Para yeterliyken fiyat yazısının rengi.")]
    [SerializeField] private Color fiyatNormalRenk = Color.white;

    [Tooltip("Para yetmezken fiyat yazısının rengi.")]
    [SerializeField] private Color fiyatYetersizRenk = new Color(1f, 0.25f, 0.25f, 1f);

    [Tooltip("Para yetmiyorsa butonu tıklanamaz yap (soluklaşır).")]
    [SerializeField] private bool parasiYetmezseButonKapat = true;

    [Tooltip("Para yetmediğinde gösterilecek uyarı paneli (opsiyonel).")]
    [SerializeField] private GameObject yetersizParaPaneli;

    [Tooltip("Para yetmediğinde çalacak ses (2D, opsiyonel).")]
    [SerializeField] private SesVerisi yetersizParaSesi;

    // ===== DEMO GUN SISTEMI (kaldirilabilir) =====
    [Header("Demo: Gün Kilidi")]
    [Tooltip("Kilit yazısı formatı. {0} açılış gününün geleceği yerdir. Örnek: \"Gün {0}\"")]
    [SerializeField] private string kilitFormati = "Gün {0}";

    [Tooltip("Ürün kilitliyken fiyat yazısı gizlensin mi? (Yerine kilit yazısı görünür.)")]
    [SerializeField] private bool kilitliykenFiyatiGizle = true;

    [Tooltip("Kilitli butona basılınca gösterilecek uyarı paneli (opsiyonel).")]
    [SerializeField] private GameObject kilitliPaneli;

    [Tooltip("Kilitli butona basılınca çalacak ses (2D, opsiyonel).")]
    [SerializeField] private SesVerisi kilitliSesi;
    // ===== DEMO GUN SISTEMI SONU =====

    private NesneYerlestirmeSistemi yerlestirmeSistemi;
    private int aktifKategoriIndex = -1;
    private Coroutine uyariCoroutine;
    private bool ekonomiyeAbone;

    void Start()
    {
        yerlestirmeSistemi = FindObjectOfType<NesneYerlestirmeSistemi>();

        if (yerlestirmeSistemi == null)
            Debug.LogError("NesneYerlestirmeSistemi bulunamadı!");

        if (uyariPaneli != null)
            uyariPaneli.SetActive(false);

        if (yetersizParaPaneli != null)
            yetersizParaPaneli.SetActive(false);

        // ===== DEMO GUN SISTEMI (kaldirilabilir) =====
        if (kilitliPaneli != null)
            kilitliPaneli.SetActive(false);
        // ===== DEMO GUN SISTEMI SONU =====

        SekmeButonlariniBagla();
        NesneButonlariniBagla();
        FiyatYazilariniBul();
        KilitYazilariniBul(); // DEMO GUN SISTEMI (kaldirilabilir)

        EkonomiyeAboneOl();
        FiyatlariGuncelle();

        // Başlangıçta sadece bir kategoriyi göster
        if (kategoriler != null && kategoriler.Length > 0)
        {
            int gosterIndex = Mathf.Clamp(baslangicKategorisi, 0, kategoriler.Length - 1);
            KategoriGoster(gosterIndex);
        }
    }

    void OnDestroy()
    {
        if (ekonomiyeAbone && EkonomiYoneticisi.Instance != null)
        {
            EkonomiYoneticisi.Instance.ParaDegisti -= ParaDegistiginde;
            ekonomiyeAbone = false;
        }
    }

    void EkonomiyeAboneOl()
    {
        if (ekonomiyeAbone || EkonomiYoneticisi.Instance == null) return;

        EkonomiYoneticisi.Instance.ParaDegisti += ParaDegistiginde;
        ekonomiyeAbone = true;
    }

    void ParaDegistiginde(int yeniPara)
    {
        FiyatlariGuncelle(yeniPara);
    }

    // ================== BUTON BAĞLAMA ==================

    /// <summary>
    /// Üstteki sekme butonlarını (Sebzeler/Makineler) bağlar
    /// </summary>
    void SekmeButonlariniBagla()
    {
        if (kategoriler == null) return;

        for (int i = 0; i < kategoriler.Length; i++)
        {
            if (kategoriler[i].sekmeButonu == null) continue;

            int kategoriIndex = i; // closure için kopya
            kategoriler[i].sekmeButonu.onClick.RemoveAllListeners();
            kategoriler[i].sekmeButonu.onClick.AddListener(() => KategoriGoster(kategoriIndex));
        }
    }

    /// <summary>
    /// Her kategorinin içindeki nesne butonlarını bağlar
    /// </summary>
    void NesneButonlariniBagla()
    {
        if (kategoriler == null) return;

        for (int k = 0; k < kategoriler.Length; k++)
        {
            var kategori = kategoriler[k];
            if (kategori.nesneButonlari == null) continue;

            for (int n = 0; n < kategori.nesneButonlari.Length; n++)
            {
                if (kategori.nesneButonlari[n] == null) continue;

                int kategoriIndex = k;
                int nesneIndex = n;

                kategori.nesneButonlari[n].onClick.RemoveAllListeners();
                kategori.nesneButonlari[n].onClick.AddListener(() =>
                    NesneYerlestirmeBaslat(kategoriIndex, nesneIndex));
            }
        }
    }

    // ================== FİYAT ==================

    /// <summary>
    /// Fiyat yazısı atanmamış ürünler için butonun altında adında "fiyat"/"price"
    /// geçen TextMeshPro'yu bulur. Bir kez Start'ta çalışır.
    /// </summary>
    void FiyatYazilariniBul()
    {
        if (kategoriler == null) return;

        for (int k = 0; k < kategoriler.Length; k++)
        {
            var kategori = kategoriler[k];
            if (kategori.nesneler == null || kategori.nesneButonlari == null) continue;

            for (int n = 0; n < kategori.nesneler.Length; n++)
            {
                var veri = kategori.nesneler[n];
                if (veri == null || veri.fiyatYazisi != null) continue;
                if (n >= kategori.nesneButonlari.Length || kategori.nesneButonlari[n] == null) continue;

                TMP_Text[] yazilar = kategori.nesneButonlari[n].GetComponentsInChildren<TMP_Text>(true);
                for (int y = 0; y < yazilar.Length; y++)
                {
                    string ad = yazilar[y].gameObject.name.ToLower();
                    if (ad.Contains("fiyat") || ad.Contains("price"))
                    {
                        veri.fiyatYazisi = yazilar[y];
                        break;
                    }
                }
            }
        }
    }

    void FiyatlariGuncelle()
    {
        int para = EkonomiYoneticisi.Instance != null ? EkonomiYoneticisi.Instance.Para : int.MaxValue;
        FiyatlariGuncelle(para);
    }

    /// <summary>
    /// Tüm butonların fiyat yazısını, kilit durumunu ve tıklanabilirliğini günceller.
    /// Her frame DEĞİL; sadece para değiştiğinde ve gün değiştiğinde çalışır.
    /// </summary>
    void FiyatlariGuncelle(int para)
    {
        if (kategoriler == null) return;

        for (int k = 0; k < kategoriler.Length; k++)
        {
            var kategori = kategoriler[k];
            if (kategori.nesneler == null) continue;

            for (int n = 0; n < kategori.nesneler.Length; n++)
            {
                var veri = kategori.nesneler[n];
                if (veri == null) continue;

                bool bedava = veri.fiyat <= 0;
                bool yeterli = bedava || para >= veri.fiyat;

                // ===== DEMO GUN SISTEMI (kaldirilabilir) =====
                bool kilitli = KilitliMi(veri);

                if (veri.kilitKaplamasi != null)
                    veri.kilitKaplamasi.SetActive(kilitli);

                if (veri.kilitYazisi != null)
                {
                    veri.kilitYazisi.enabled = kilitli;
                    if (kilitli) veri.kilitYazisi.SetText(KilitMetni(veri.acilisGunu));
                }
                // ===== DEMO GUN SISTEMI SONU =====

                if (veri.fiyatYazisi != null)
                {
                    if (kilitli && kilitliykenFiyatiGizle) // DEMO GUN SISTEMI (kaldirilabilir)
                    {
                        veri.fiyatYazisi.enabled = false;
                    }
                    else if (bedava && bedavaysaFiyatiGizle)
                    {
                        veri.fiyatYazisi.enabled = false;
                    }
                    else
                    {
                        veri.fiyatYazisi.enabled = true;
                        veri.fiyatYazisi.SetText(FiyatMetni(veri.fiyat));
                        veri.fiyatYazisi.color = yeterli ? fiyatNormalRenk : fiyatYetersizRenk;
                    }
                }

                if (kategori.nesneButonlari != null &&
                    n < kategori.nesneButonlari.Length &&
                    kategori.nesneButonlari[n] != null)
                {
                    Button buton = kategori.nesneButonlari[n];

                    if (kilitli)
                        buton.interactable = false;              // DEMO GUN SISTEMI
                    else if (parasiYetmezseButonKapat)
                        buton.interactable = yeterli;
                    else if (veri.acilisGunu > 1)
                        buton.interactable = true;               // DEMO GUN SISTEMI: kilit açıldı, butonu geri aç
                }
            }
        }
    }

    /// <summary>
    /// string.Format KULLANILMAZ: inspector'a hatalı format yazılırsa exception fırlatırdı.
    /// </summary>
    string FiyatMetni(int deger)
    {
        if (string.IsNullOrEmpty(fiyatFormati))
            return deger.ToString();

        if (fiyatFormati.Contains("{0}"))
            return fiyatFormati.Replace("{0}", deger.ToString());

        return deger + " " + fiyatFormati;
    }

    // ===== DEMO GUN SISTEMI (kaldirilabilir) =====
    // Bu bölüm demoya aittir. Silersen mağaza eski haliyle (her şey açık) çalışır.

    /// <summary>
    /// Bu ürün şu an kilitli mi? Sahnede DemoGunSistemi YOKSA hiçbir şey kilitli değildir.
    /// </summary>
    bool KilitliMi(YerlestirilebilirNesneData veri)
    {
        if (veri == null) return false;

        // ===== GELISTIRICI HILELERI (kaldirilabilir) =====
        if (hileKilitleriAcik) return false;                 // test hilesi: her şey açık
        // ===== GELISTIRICI HILELERI SONU =====

        if (veri.acilisGunu <= 1) return false;              // baştan açık ürün
        if (DemoGunSistemi.Instance == null) return false;   // demo sistemi yok -> kilit yok

        return DemoGunSistemi.Instance.Gun < veri.acilisGunu;
    }

    // ===== GELISTIRICI HILELERI (kaldirilabilir) =====
    /// <summary>Test hilesiyle açıldı mı? Açıksa gün kilitleri hiç uygulanmaz.</summary>
    private bool hileKilitleriAcik;

    /// <summary>
    /// TEST HİLESİ: mağazadaki tüm gün kilitlerini kaldırır (GelistiriciHileleri çağırır).
    /// Ürünler bedava OLMAZ, sadece satın alınabilir hale gelir.
    /// Bir kez açılınca oyun kapanana kadar açık kalır.
    /// </summary>
    public void TumKilitleriAc()
    {
        if (hileKilitleriAcik) return;

        hileKilitleriAcik = true;
        FiyatlariGuncelle(); // butonları hemen tazele
    }
    // ===== GELISTIRICI HILELERI SONU =====

    /// <summary>
    /// Kilit yazısı metni. string.Format KULLANILMAZ: inspector'a hatalı format
    /// yazılırsa exception fırlatırdı (FiyatMetni ile aynı mantık).
    /// </summary>
    string KilitMetni(int gun)
    {
        if (string.IsNullOrEmpty(kilitFormati))
            return gun.ToString();

        if (kilitFormati.Contains("{0}"))
            return kilitFormati.Replace("{0}", gun.ToString());

        return kilitFormati + " " + gun;
    }

    /// <summary>
    /// Gün değişince DemoGunSistemi çağırır: kilitleri ve fiyatları yeniden hesaplar.
    /// </summary>
    public void KilitleriTazele()
    {
        FiyatlariGuncelle();
    }

    /// <summary>
    /// Kilit yazısı atanmamış ürünler için butonun altında adında "kilit"/"lock"
    /// geçen TextMeshPro'yu bulur. Bir kez Start'ta çalışır (FiyatYazilariniBul ile aynı mantık).
    /// </summary>
    void KilitYazilariniBul()
    {
        if (kategoriler == null) return;

        for (int k = 0; k < kategoriler.Length; k++)
        {
            var kategori = kategoriler[k];
            if (kategori.nesneler == null || kategori.nesneButonlari == null) continue;

            for (int n = 0; n < kategori.nesneler.Length; n++)
            {
                var veri = kategori.nesneler[n];
                if (veri == null || veri.kilitYazisi != null) continue;
                if (n >= kategori.nesneButonlari.Length || kategori.nesneButonlari[n] == null) continue;

                TMP_Text[] yazilar = kategori.nesneButonlari[n].GetComponentsInChildren<TMP_Text>(true);
                for (int y = 0; y < yazilar.Length; y++)
                {
                    if (yazilar[y] == veri.fiyatYazisi) continue; // fiyat yazısını kapma

                    string ad = yazilar[y].gameObject.name.ToLower();
                    if (ad.Contains("kilit") || ad.Contains("lock"))
                    {
                        veri.kilitYazisi = yazilar[y];
                        break;
                    }
                }
            }
        }
    }
    // ===== DEMO GUN SISTEMI SONU =====

    // ================== KATEGORİ ==================

    /// <summary>
    /// Belirtilen kategoriyi gösterir, diğerlerini gizler
    /// </summary>
    public void KategoriGoster(int kategoriIndex)
    {
        if (kategoriler == null || kategoriIndex < 0 || kategoriIndex >= kategoriler.Length)
        {
            Debug.LogError($"Geçersiz kategori index: {kategoriIndex}");
            return;
        }

        // Tüm panelleri kapat
        for (int i = 0; i < kategoriler.Length; i++)
        {
            if (kategoriler[i].kategoriPaneli != null)
                kategoriler[i].kategoriPaneli.SetActive(i == kategoriIndex);
        }

        aktifKategoriIndex = kategoriIndex;
    }

    // ================== SEÇİM / SATIN ALMA ==================

    /// <summary>
    /// Belirtilen kategorideki belirtilen nesnenin yerleştirilmesini başlatır.
    /// Para BURADA DÜŞMEZ, sadece yeterli mi diye bakılır; ödeme nesne yere konunca yapılır.
    /// </summary>
    public void NesneYerlestirmeBaslat(int kategoriIndex, int nesneIndex)
    {
        // Zaten bir yerleştirme/elde nesne varken yeni seçime izin verme.
        // Aksi halde eski preview öksüz kalıp sahnede takılı kalıyor.
        if (yerlestirmeSistemi != null && yerlestirmeSistemi.YerlesimModuAktif)
        {
            UyariGoster(uyariPaneli, "Elinde nesne varken yeni nesne seçilemez!");
            return;
        }

        if (kategoriIndex < 0 || kategoriIndex >= kategoriler.Length)
        {
            Debug.LogError($"Geçersiz kategori index: {kategoriIndex}");
            return;
        }

        var kategori = kategoriler[kategoriIndex];

        if (nesneIndex < 0 || nesneIndex >= kategori.nesneler.Length)
        {
            Debug.LogError($"Geçersiz nesne index: {nesneIndex} ({kategori.kategoriAdi})");
            return;
        }

        var veri = kategori.nesneler[nesneIndex];

        // ===== DEMO GUN SISTEMI (kaldirilabilir) =====
        // Butonun kapalı olması yetmez: koddan da çağrılabilir, burada da kilidi kontrol et.
        if (KilitliMi(veri))
        {
            if (kilitliSesi != null && SesYoneticisi.Instance != null)
                SesYoneticisi.Instance.SesCal2D(kilitliSesi);

            UyariGoster(kilitliPaneli, $"'{veri.nesneAdi}' {veri.acilisGunu}. günde açılıyor!");
            return;
        }
        // ===== DEMO GUN SISTEMI SONU =====

        if (veri.nesnePrefab == null)
        {
            Debug.LogError($"'{veri.nesneAdi}' için prefab atanmamış!");
            return;
        }

        if (yerlestirmeSistemi == null)
        {
            Debug.LogError("Yerleştirme sistemi bulunamadı!");
            return;
        }

        // Para kontrolü (henüz harcanmıyor)
        if (!ParaYeterliMi(veri))
            return;

        // UI'ı kapat
        if (uiYoneticisi != null)
            uiYoneticisi.UIDurumunuDegistir();

        // Yerleştirmeyi başlat - fiyat da gider, ödeme yere konunca yapılır
        yerlestirmeSistemi.PrefabIleYerlestirmeBaslat(veri.nesnePrefab, veri.fiyat);
    }

    bool ParaYeterliMi(YerlestirilebilirNesneData veri)
    {
        if (veri.fiyat <= 0) return true;

        if (EkonomiYoneticisi.Instance == null)
        {
            Debug.LogWarning("[Satın Alma] EkonomiYoneticisi yok, ürün bedava verildi.");
            return true;
        }

        if (EkonomiYoneticisi.Instance.YeterliMi(veri.fiyat)) return true;

        Debug.Log($"[Satın Alma] Yetersiz para! '{veri.nesneAdi}' için {veri.fiyat} gerekli, " +
                  $"mevcut: {EkonomiYoneticisi.Instance.Para}");

        if (yetersizParaSesi != null && SesYoneticisi.Instance != null)
            SesYoneticisi.Instance.SesCal2D(yetersizParaSesi);

        UyariGoster(yetersizParaPaneli, "Yetersiz para!");
        return false;
    }

    // ================== UYARI ==================

    /// <summary>
    /// Uyarı panelini gösterir ve bir süre sonra gizler. Panel yoksa Console'a yazar.
    /// </summary>
    void UyariGoster(GameObject panel, string yedekMesaj)
    {
        if (panel == null)
        {
            Debug.LogWarning(yedekMesaj);
            return;
        }

        panel.SetActive(true);

        if (uyariCoroutine != null)
            StopCoroutine(uyariCoroutine);
        uyariCoroutine = StartCoroutine(UyariGizleRutini(panel));
    }

    IEnumerator UyariGizleRutini(GameObject panel)
    {
        yield return new WaitForSeconds(uyariGosterimSuresi);

        if (panel != null)
            panel.SetActive(false);

        uyariCoroutine = null;
    }

    // Public propertyler
    public KategoriData[] TumKategoriler => kategoriler;
    public int AktifKategoriIndex => aktifKategoriIndex;
}
