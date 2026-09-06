using System;
using UnityEngine;
using TMPro;

// Oyun saatini tutar, dukkan acikken ilerletir ve ekrandaki UI'a yazar.
// Tabela tarafindan Ac() / Kapat() ile kontrol edilir.
//
// GUN SONU: Saat bitisSaati'ne ulasinca HICBIR SEY DURMAZ; saat de akis da aynen
// devam eder. Sadece "gunBitti" bayragi true olur (GunBitti event'i bir kez tetiklenir)
// ki gun sonu objesi basilabilir hale gelsin. Asil sifirlama, o objeye basilinca
// GunYoneticisi -> GunuSifirla() ile yapilir.
public class ZamanSistemi : MonoBehaviour
{
    [Header("Saat Ayarlari")]
    [Tooltip("Dukkan acildiginda baslayacak saat (10 = 10:00)")]
    public float baslangicSaati = 10f;

    [Tooltip("Bu saate ulasinca GUN BITER (gun sonu objesi basilabilir olur). Saat DURMAZ.")]
    public float bitisSaati = 21f;

    [Tooltip("Baslangictan bitise kadar GERCEK dunyada kac dakika sursun")]
    public float gercekSureDakika = 8f;

    [Header("UI")]
    [Tooltip("Saatin yazilacagi TextMeshPro objesi")]
    public TextMeshProUGUI saatText;

    [Header("Durum (sadece izlemek icin)")]
    [SerializeField] private float suankiSaat;
    [SerializeField] private bool acikMi = false;
    [SerializeField] private bool gunBitti = false;

    /// <summary>Saat bitisSaati'ne ilk kez ulastiginda bir kez tetiklenir (UI/ses vb. icin).</summary>
    public event Action GunBitti;

    // Optimizasyon: text'i her frame degil, sadece dakika degisince guncelle
    private int sonGosterilenDakika = -1;

    void Start()
    {
        suankiSaat = baslangicSaati;
        sonGosterilenDakika = -1; // ilk yazimi zorla
        SaatiYaz();
    }

    void Update()
    {
        if (!acikMi) return;

        // gercekSureDakika dakikada (baslangic -> bitis) kadar oyun saati ilerlesin.
        float toplamOyunSaati = bitisSaati - baslangicSaati;
        float gercekSureSaniye = Mathf.Max(0.01f, gercekSureDakika * 60f);
        float saatHizi = toplamOyunSaati / gercekSureSaniye;

        suankiSaat += saatHizi * Time.deltaTime;

        // Bitis saatine ulasildi: SAAT DONAR (bitisSaati'nde sabit kalir, ilerlemez).
        // Ama dukkan KAPANMAZ; musteri uretimi (tabelaya bagli) aynen devam eder.
        // gunBitti bayragi bir kez kalkar ki gun sonu objesi basilabilir olsun.
        if (suankiSaat >= bitisSaati)
        {
            suankiSaat = bitisSaati; // saati bitiste dondur

            if (!gunBitti)
            {
                gunBitti = true;
                GunBitti?.Invoke();
            }
        }

        SaatiYaz();
    }

    void SaatiYaz()
    {
        if (saatText == null) return;

        int saat = Mathf.FloorToInt(suankiSaat);
        int dakika = Mathf.FloorToInt((suankiSaat - saat) * 60f);

        // Ayni dakikadaysak bosuna guncelleme yapma
        if (dakika == sonGosterilenDakika) return;
        sonGosterilenDakika = dakika;

        saatText.text = string.Format("{0:00}:{1:00}", saat, dakika);
    }

    // --- Tabela tarafindan cagrilir ---

    public void Ac()
    {
        acikMi = true;
    }

    public void Kapat()
    {
        acikMi = false;
    }

    public bool AcikMi()
    {
        return acikMi;
    }

    /// <summary>Gun bitti mi? (saat bitisSaati'ne ulasti mi) Gun sonu objesi bunu okur.</summary>
    public bool GunBittiMi()
    {
        return gunBitti;
    }

    // Baska sistemler su anki saati float olarak okuyabilir (orn. musteri spawn)
    public float SuankiSaat()
    {
        return suankiSaat;
    }

    /// <summary>
    /// GUN YENIDEN BASLARKEN cagrilir (GunYoneticisi). Saati baslangica dondurur,
    /// gun-bitti bayragini sifirlar ve dukkani KAPALI birakir (oyuncu tabelayi tekrar acar).
    /// </summary>
    public void GunuSifirla()
    {
        suankiSaat = baslangicSaati;
        gunBitti = false;
        acikMi = false;            // yeni gun: dukkan kapali baslar
        sonGosterilenDakika = -1;  // UI'in yeniden yazilmasini zorla
        SaatiYaz();
    }
}
