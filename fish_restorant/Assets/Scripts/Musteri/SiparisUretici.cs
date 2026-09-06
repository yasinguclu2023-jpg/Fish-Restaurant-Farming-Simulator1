using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Tek bir siparisin verisi (MonoBehaviour degil, duz sinif).
/// malzemeler: secilen malzemeler (Balik -> Sebze(ler) -> Icecek -> YanUrun sirasiyla).
/// masaNo: atanan masa numarasi (-1 = henuz masa yok).
/// </summary>
public class Siparis
{
    public readonly List<Malzeme> malzemeler = new List<Malzeme>();
    public int masaNo = -1;

    /// <summary>Siparisin toplam fiyati = icindeki tum malzemelerin fiyatlarinin toplami.</summary>
    public int ToplamFiyat
    {
        get
        {
            int toplam = 0;
            for (int i = 0; i < malzemeler.Count; i++)
                if (malzemeler[i] != null) toplam += malzemeler[i].Fiyat;
            return toplam;
        }
    }
}

/// <summary>
/// Agirlikli malzeme: bir malzeme + cikma siklik agirligi (slider).
/// Agirlik buyukse o malzeme daha sik secilir. 0 = hic secilmez.
/// </summary>
[System.Serializable]
public class AgirlikliMalzeme
{
    public Malzeme malzeme;
    [Range(0f, 1f)] public float agirlik = 1f;

    [Header("Bu baligin ekmegi (secilince siparise eklenir)")]
    [Tooltip("Bu baliga/ahtapota ozel ekmek ALT parcasi (Malzeme, kategori Ekmek).")]
    public Malzeme ekmekAlt;
    [Tooltip("Bu baliga/ahtapota ozel ekmek UST parcasi (Malzeme, kategori Ekmek).")]
    public Malzeme ekmekUst;
}

/// <summary>
/// Agirlikli sebze: bir sebze + cikma siklik agirligi (slider).
/// Agirlik buyukse o sebze daha sik secilir. AGIRLIK 0 = O SEBZE HIC SECILMEZ (siparise girmez).
/// </summary>
[System.Serializable]
public class AgirlikliSebze
{
    public Malzeme malzeme;
    [Range(0f, 1f)] public float agirlik = 1f;
}

/// <summary>
/// Olasilikli urun: bir malzeme + BAGIMSIZ cikma olasiligi (slider).
/// Her siparıste bu olasilikla eklenir. Icecek/YanUrun icin (bira, sarap ayri ayri).
/// 0 = hic cikmaz, 1 = her siparıste cikar.
/// </summary>
[System.Serializable]
public class OlasilikliUrun
{
    public Malzeme malzeme;
    [Range(0f, 1f)] public float olasilik = 0.5f;
}

/// <summary>
/// Rastgele AMA mantikli siparis uretir.
///
/// Mantik: malzemeler kategorilere ayrilir, siparis kurallı secilir:
///   1) Tam 1 Balik (zorunlu).
///   2) min..max arasi Sebze (tekrarsiz).
///   3) Belirli olasilikla 1 Icecek.
///   4) Belirli olasilikla 1 YanUrun.
/// Boylece asla sacma kombinasyon cikmaz, her seferinde farkli olur.
///
/// KURULUM: Sahneye bos GameObject + bu script. Havuzlari Inspector'dan doldur
/// (her kategoriye ait Malzeme asset'lerini surukle).
/// </summary>
public class SiparisUretici : MonoBehaviour
{
    [Header("Baliklar (agirlikli - slider ile siklik ayari)")]
    [Tooltip("Her balik icin agirlik (slider). Buyuk olan daha sik cikar.")]
    [SerializeField] private List<AgirlikliMalzeme> baliklar = new List<AgirlikliMalzeme>();

    [Header("Sebzeler (agirlikli - slider ile siklik ayari)")]
    [Tooltip("Her sebze icin agirlik (slider). Buyuk olan daha sik cikar.")]
    [SerializeField] private List<AgirlikliSebze> sebzeler = new List<AgirlikliSebze>();

    [Header("Icecekler (her biri olasilik - EN FAZLA 1 secilir)")]
    [Tooltip("Her icecek kendi olasiligiyla ADAY olur; en fazla 1 icecek eklenir (bira+sarap ayni anda cikmaz).")]
    [SerializeField] private List<OlasilikliUrun> icecekler = new List<OlasilikliUrun>();

    [Header("Yan Urunler (her biri BAGIMSIZ olasilik - slider)")]
    [Tooltip("Her yan urun kendi olasiligiyla eklenir. Birden fazla cikabilir.")]
    [SerializeField] private List<OlasilikliUrun> yanUrunler = new List<OlasilikliUrun>();

    [Header("Sebze Adedi")]
    [SerializeField] private int minSebze = 1;
    [SerializeField] private int maxSebze = 3;

    [Header("Cesitlilik")]
    [Tooltip("Acikken bir onceki siparisle birebir ayni kombinasyon cikmaz.")]
    [SerializeField] private bool oncekiyleAyniOlmasin = true;

    // GC'siz yardimcilar
    private readonly List<AgirlikliSebze> sebzeGecici = new List<AgirlikliSebze>();
    private readonly List<Malzeme> icecekAdaylari = new List<Malzeme>(); // en fazla 1 icecek secimi icin
    private readonly StringBuilder imzaSb = new StringBuilder(64);
    private string sonImza;

    /// <summary>Rastgele mantikli bir siparis uretir.</summary>
    public Siparis SiparisUret()
    {
        Siparis s = TekUret();

        // Ust uste ayni kombinasyon olmasin (birkac deneme)
        if (oncekiyleAyniOlmasin)
        {
            int guvenlik = 0;
            while (Imza(s) == sonImza && guvenlik++ < 8)
                s = TekUret();
        }

        sonImza = Imza(s);
        return s;
    }

    /// <summary>
    /// FRAGMAN/senaryo icin: verilen malzemelerle SABIT bir siparis olusturur.
    /// Balik verilirse ONA AIT ekmek alt/ust otomatik eklenir (baliklar listesinden bulunur),
    /// boylece servis eslesmesi (MasaServisTetikleyici) bozulmaz.
    /// </summary>
    public Siparis SiparisOlustur(Malzeme balik, Malzeme[] digerMalzemeler)
    {
        Siparis s = new Siparis();

        if (balik != null)
        {
            s.malzemeler.Add(balik);

            AgirlikliMalzeme tanim = BalikTanimBul(balik);
            if (tanim != null)
            {
                if (tanim.ekmekAlt != null) s.malzemeler.Add(tanim.ekmekAlt);
                if (tanim.ekmekUst != null) s.malzemeler.Add(tanim.ekmekUst);
            }
            else
            {
                Debug.LogWarning($"[SiparisUretici] '{balik.name}' baliklar listesinde yok; ekmek eklenemedi.", this);
            }
        }

        if (digerMalzemeler != null)
        {
            for (int i = 0; i < digerMalzemeler.Length; i++)
                if (digerMalzemeler[i] != null) s.malzemeler.Add(digerMalzemeler[i]);
        }

        return s;
    }

    /// <summary>
    /// FRAGMAN: malzeme ADLARIYLA sabit siparis olusturur (adlar Malzeme asset adlaridir).
    /// Ekmek alt/ust otomatik eklenir.
    /// </summary>
    public Siparis SiparisOlusturAdaGore(string balikAdi, string[] digerAdlar)
    {
        Malzeme balik = MalzemeBulAdaGore(balikAdi);
        if (balik == null && !string.IsNullOrEmpty(balikAdi))
            Debug.LogWarning($"[SiparisUretici] Fragman: '{balikAdi}' havuzlarda bulunamadi!", this);

        Malzeme[] digerleri = null;
        if (digerAdlar != null)
        {
            digerleri = new Malzeme[digerAdlar.Length];
            for (int i = 0; i < digerAdlar.Length; i++)
            {
                digerleri[i] = MalzemeBulAdaGore(digerAdlar[i]);
                if (digerleri[i] == null)
                    Debug.LogWarning($"[SiparisUretici] Fragman: '{digerAdlar[i]}' havuzlarda bulunamadi!", this);
            }
        }

        return SiparisOlustur(balik, digerleri);
    }

    /// <summary>Havuzlardaki (balik/sebze/icecek/yanUrun) malzemeler icinde ada gore arar. Yoksa null.</summary>
    public Malzeme MalzemeBulAdaGore(string ad)
    {
        if (string.IsNullOrEmpty(ad)) return null;

        for (int i = 0; i < baliklar.Count; i++)
            if (baliklar[i] != null && baliklar[i].malzeme != null && AdEsit(baliklar[i].malzeme.name, ad))
                return baliklar[i].malzeme;

        for (int i = 0; i < sebzeler.Count; i++)
            if (sebzeler[i] != null && sebzeler[i].malzeme != null && AdEsit(sebzeler[i].malzeme.name, ad))
                return sebzeler[i].malzeme;

        for (int i = 0; i < icecekler.Count; i++)
            if (icecekler[i] != null && icecekler[i].malzeme != null && AdEsit(icecekler[i].malzeme.name, ad))
                return icecekler[i].malzeme;

        for (int i = 0; i < yanUrunler.Count; i++)
            if (yanUrunler[i] != null && yanUrunler[i].malzeme != null && AdEsit(yanUrunler[i].malzeme.name, ad))
                return yanUrunler[i].malzeme;

        return null;
    }

    private static bool AdEsit(string a, string b)
        => string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);

    // Verilen baligin "baliklar" listesindeki tanimini (ekmek bilgisiyle) bulur
    private AgirlikliMalzeme BalikTanimBul(Malzeme balik)
    {
        for (int i = 0; i < baliklar.Count; i++)
            if (baliklar[i] != null && baliklar[i].malzeme == balik) return baliklar[i];
        return null;
    }

    private Siparis TekUret()
    {
        Siparis s = new Siparis();

        // 1) Balik (zorunlu, agirlikli secim) + o baliga ait ekmek alt/ust
        AgirlikliMalzeme balikSecim = RastgeleAgirlikli(baliklar);
        if (balikSecim != null && balikSecim.malzeme != null)
        {
            s.malzemeler.Add(balikSecim.malzeme);
            if (balikSecim.ekmekAlt != null) s.malzemeler.Add(balikSecim.ekmekAlt);
            if (balikSecim.ekmekUst != null) s.malzemeler.Add(balikSecim.ekmekUst);
        }

        // 2) Sebze (min..max adet, tekrarsiz, AGIRLIKLI secim)
        if (sebzeler.Count > 0)
        {
            int istenen = Random.Range(minSebze, maxSebze + 1);

            sebzeGecici.Clear();
            for (int i = 0; i < sebzeler.Count; i++)
                if (sebzeler[i] != null && sebzeler[i].malzeme != null && sebzeler[i].agirlik > 0f)
                    sebzeGecici.Add(sebzeler[i]); // agirlik 0 = HIC secilmez (havuza girmez)

            int adet = Mathf.Clamp(istenen, 0, sebzeGecici.Count);
            for (int i = 0; i < adet; i++)
            {
                int idx = AgirlikliIndexSec(sebzeGecici);
                if (idx < 0) break;
                s.malzemeler.Add(sebzeGecici[idx].malzeme);
                sebzeGecici.RemoveAt(idx); // tekrarsiz
            }
        }

        // 3) Icecek: her biri kendi olasiligiyla ADAY olur; EN FAZLA 1 icecek eklenir.
        //    Birden fazla aday tutarsa aralarindan rastgele biri secilir (bira+sarap ayni anda cikmaz).
        icecekAdaylari.Clear();
        for (int i = 0; i < icecekler.Count; i++)
        {
            OlasilikliUrun u = icecekler[i];
            if (u != null && u.malzeme != null && u.olasilik > 0f && Random.value <= u.olasilik)
                icecekAdaylari.Add(u.malzeme);
        }
        if (icecekAdaylari.Count > 0)
            s.malzemeler.Add(icecekAdaylari[Random.Range(0, icecekAdaylari.Count)]);

        // 4) Yan urunler (her biri BAGIMSIZ olasilik ile eklenir)
        for (int i = 0; i < yanUrunler.Count; i++)
        {
            OlasilikliUrun u = yanUrunler[i];
            if (u != null && u.malzeme != null && u.olasilik > 0f && Random.value <= u.olasilik)
                s.malzemeler.Add(u.malzeme);
        }

        return s;
    }

    /// <summary>Agirliga gore rastgele bir index secer (buyuk agirlik = daha sik). Hepsi 0 ise esit. -1 = bos.</summary>
    private int AgirlikliIndexSec(List<AgirlikliSebze> liste)
    {
        if (liste == null || liste.Count == 0) return -1;

        float toplam = 0f;
        for (int i = 0; i < liste.Count; i++)
            toplam += Mathf.Max(0f, liste[i].agirlik);

        // Tum agirliklar 0 ise esit dagit
        if (toplam <= 0f) return Random.Range(0, liste.Count);

        float r = Random.value * toplam;
        for (int i = 0; i < liste.Count; i++)
        {
            float a = Mathf.Max(0f, liste[i].agirlik);
            if (r < a) return i;
            r -= a;
        }
        return liste.Count - 1; // guvenlik
    }

    /// <summary>Agirliga gore rastgele secer (agirlik buyukse daha sik). Tum agirliklar 0 ise esit dagitir.</summary>
    private AgirlikliMalzeme RastgeleAgirlikli(List<AgirlikliMalzeme> liste)
    {
        if (liste == null || liste.Count == 0) return null;

        float toplam = 0f;
        for (int i = 0; i < liste.Count; i++)
            if (liste[i] != null && liste[i].malzeme != null)
                toplam += Mathf.Max(0f, liste[i].agirlik);

        // Tum agirliklar 0 ise: ilk gecerli ogeyi dondur
        if (toplam <= 0f)
        {
            for (int i = 0; i < liste.Count; i++)
                if (liste[i] != null && liste[i].malzeme != null)
                    return liste[i];
            return null;
        }

        float r = Random.value * toplam;
        for (int i = 0; i < liste.Count; i++)
        {
            if (liste[i] == null || liste[i].malzeme == null) continue;
            float a = Mathf.Max(0f, liste[i].agirlik);
            if (r < a) return liste[i];
            r -= a;
        }

        // Guvenlik: son gecerliyi dondur
        for (int i = liste.Count - 1; i >= 0; i--)
            if (liste[i] != null && liste[i].malzeme != null)
                return liste[i];
        return null;
    }

    private string Imza(Siparis s)
    {
        imzaSb.Clear();
        for (int i = 0; i < s.malzemeler.Count; i++)
        {
            if (s.malzemeler[i] != null)
            {
                imzaSb.Append(s.malzemeler[i].name);
                imzaSb.Append('|');
            }
        }
        return imzaSb.ToString();
    }

    // ===== DEMO GUN SISTEMI (kaldirilabilir) =====
    // Asagidaki blok demoya aittir. Silersen bu script eski haliyle (Inspector
    // degerleriyle) calismaya devam eder; hicbir sey bozulmaz.

    /// <summary>
    /// O GUNUN ayarlarini uygular: sebze agirliklari, icecek/yan urun olasiliklari
    /// ve sebze adedi (min/max).
    ///
    /// Ayar listesinde GECMEYEN malzemenin degeri 0 olur -> o gun siparise HIC girmez.
    /// Boylece "acilmamis sebze sipariste cikmasin" garantisi otomatik saglanir.
    ///
    /// Bu metodu kimse cagirmazsa Inspector'daki degerler aynen kullanilir.
    /// </summary>
    public void GunAyariniUygula(GunAyari ayar)
    {
        if (ayar == null) return;

        // Sebze adedi
        minSebze = Mathf.Max(0, ayar.minSebze);
        maxSebze = Mathf.Max(minSebze, ayar.maxSebze);

        // Sebzeler -> AGIRLIK (goreli siklik)
        for (int i = 0; i < sebzeler.Count; i++)
        {
            if (sebzeler[i] == null) continue;
            sebzeler[i].agirlik = OranBul(ayar.sebzeAgirliklari, sebzeler[i].malzeme);
        }

        // Icecekler -> GERCEK OLASILIK
        for (int i = 0; i < icecekler.Count; i++)
        {
            if (icecekler[i] == null) continue;
            icecekler[i].olasilik = OranBul(ayar.icecekOranlari, icecekler[i].malzeme);
        }

        // Yan urunler -> GERCEK OLASILIK
        for (int i = 0; i < yanUrunler.Count; i++)
        {
            if (yanUrunler[i] == null) continue;
            yanUrunler[i].olasilik = OranBul(ayar.yanUrunOranlari, yanUrunler[i].malzeme);
        }

        // Yeni gun: "ust uste ayni siparis olmasin" hafizasini sifirla.
        sonImza = null;

        // Test hilesi aciksa gunluk ayar onu ezmesin, tekrar uygula.
        if (hileAcik) HileyiUygula(); // GELISTIRICI HILELERI (kaldirilabilir)
    }

    // ===== GELISTIRICI HILELERI (kaldirilabilir) =====

    private bool hileAcik;
    private int hileOran = 60;
    private int hileMaxSebze = 3;

    /// <summary>
    /// TEST HILESI: butun malzemeleri siparislere acar (sogan, marul, bira, sarap...).
    /// Gun plani ne derse desin gecerli olur; gun degisse bile kapanmaz.
    /// </summary>
    /// <param name="yuzde">Her malzemenin orani (0-100). Sebzede agirlik, icecek/yan urunde gercek yuzde.</param>
    /// <param name="maxSebzeDegeri">Siparis basina en fazla kac sebze. 0 = mevcut degere dokunma.</param>
    public void TumMalzemeleriAc(int yuzde, int maxSebzeDegeri)
    {
        hileAcik = true;
        hileOran = Mathf.Clamp(yuzde, 0, 100);
        hileMaxSebze = Mathf.Max(0, maxSebzeDegeri);

        HileyiUygula();
    }

    /// <summary>Hile degerlerini butun havuzlara yazar.</summary>
    private void HileyiUygula()
    {
        float deger = Mathf.Clamp01(hileOran / 100f);

        for (int i = 0; i < sebzeler.Count; i++)
            if (sebzeler[i] != null) sebzeler[i].agirlik = deger;

        for (int i = 0; i < icecekler.Count; i++)
            if (icecekler[i] != null) icecekler[i].olasilik = deger;

        for (int i = 0; i < yanUrunler.Count; i++)
            if (yanUrunler[i] != null) yanUrunler[i].olasilik = deger;

        // Gun 1'de max sebze 1 olabiliyor; hile acikken cesitlilik icin yukseltilir.
        if (hileMaxSebze > 0)
        {
            maxSebze = hileMaxSebze;
            if (minSebze > maxSebze) minSebze = maxSebze;
        }

        sonImza = null;
    }
    // ===== GELISTIRICI HILELERI SONU =====

    /// <summary>
    /// Malzemeyi gunluk oran listesinde arar. Bulursa 0-1 arasi degere cevirir,
    /// BULAMAZSA 0 dondurur (o gun kapali demektir).
    /// </summary>
    private static float OranBul(List<MalzemeOrani> liste, Malzeme malzeme)
    {
        if (liste == null || malzeme == null) return 0f;

        for (int i = 0; i < liste.Count; i++)
        {
            if (liste[i] != null && liste[i].malzeme == malzeme)
                return Mathf.Clamp01(liste[i].oran / 100f);
        }
        return 0f;
    }
    // ===== DEMO GUN SISTEMI SONU =====

    void OnValidate()
    {
        if (minSebze < 0) minSebze = 0;
        if (maxSebze < minSebze) maxSebze = minSebze;
    }
}
