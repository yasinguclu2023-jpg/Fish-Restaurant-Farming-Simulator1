using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Masadaki gorunmez servis bolgesi (trigger). Tepsi/yemek bu bolgeye girince,
/// uzerindeki yemeklerin (UrunKimligi -> Malzeme) bu masanin siparisiyle TAM ESLESIP
/// eslesmedigini kontrol eder ve Debug ile "DOGRU/YANLIS" yazar.
///
/// Tam eslesme: siparisteki her malzeme tepside birebir (sayi dahil) olmali; fazla/eksik = YANLIS.
///
/// KURULUM:
/// 1. Masanin uzerine bos bir obje koy (gorunmez) -> bu scripti + bir Collider (Is Trigger ✓) ekle.
/// 2. "masaNo" alanini bu masanin numarasiyla AYNI yap (MasaYonetimi'ndeki numara).
/// 3. Tepsi/yemek objelerinde Collider olmali ki trigger algilasin.
/// 4. Yemek prefab'larinda UrunKimligi olmali.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MasaServisTetikleyici : MonoBehaviour
{
    [Tooltip("Bu bolgenin ait oldugu masa numarasi (MasaYonetimi'ndeki numara ile ayni).")]
    [SerializeField] private int masaNo = 1;

    [Header("Pisme Kontrolu")]
    [Tooltip("Balik pismisligini (Gorunum-Bitme arasi mi) kontrol etmek icin izgara. " +
             "Bos birakirsan sahnede otomatik bulunur.")]
    [SerializeField] private PisirmeSistemi pisirmeSistemi;

    // Ayni tepsinin birden fazla collider'i girince tekrar islememek icin
    private readonly HashSet<Transform> islenenKokler = new HashSet<Transform>();

    // GC'siz yardimcilar
    private readonly List<Malzeme> tepsidekiler = new List<Malzeme>();
    private readonly List<Malzeme> gecici = new List<Malzeme>();
    private readonly List<Malzeme> eksikler = new List<Malzeme>();
    private readonly List<Malzeme> fazlalar = new List<Malzeme>();
    private readonly System.Text.StringBuilder sb = new System.Text.StringBuilder(64);

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void Awake()
    {
        if (pisirmeSistemi == null)
            pisirmeSistemi = FindObjectOfType<PisirmeSistemi>();
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[MasaServis] TRIGGER girdi: {other.name}", this); // TEST

        Transform kok = KokBul(other);
        if (kok == null) return;
        if (islenenKokler.Contains(kok)) return; // bu tepsi zaten islendi

        // Kok altindaki tum AKTIF yemekleri (UrunKimligi) topla (kapali olanlari sayma)
        UrunKimligi[] urunler = kok.GetComponentsInChildren<UrunKimligi>(false);
        if (urunler == null || urunler.Length == 0) return; // yemek degil, yok say

        tepsidekiler.Clear();
        for (int i = 0; i < urunler.Length; i++)
        {
            if (urunler[i] == null || urunler[i].Malzeme == null) continue;
            tepsidekiler.Add(urunler[i].Malzeme);
            // TEST: her yemegin objesi ve nereden geldigi
            Debug.Log($"[MasaServis] Bulundu: {urunler[i].Malzeme.name}  <-  obje: {HiyerarsiYolu(urunler[i].transform)}", urunler[i]);
        }

        if (tepsidekiler.Count == 0) return;

        islenenKokler.Add(kok);

        // Bu masanin siparisi
        Siparis siparis = SiparisYonetimi.Instance != null
            ? SiparisYonetimi.Instance.MasaSiparisiniAl(masaNo)
            : null;

        if (siparis == null)
        {
            Debug.Log($"[MasaServis] Masa {masaNo}: bu masada aktif siparis yok.");
            return;
        }

        EksikFazlaHesapla(siparis.malzemeler, tepsidekiler);
        bool malzemelerDogru = eksikler.Count == 0 && fazlalar.Count == 0;

        // Balik pismisligi: iki yuz de PISMIS araliginda (Gorunum-Bitme) olmali; cig/yanmis => yanlis
        bool baliklarIyi = BaliklarIyiPismisMi(kok, out string pismeSorunu);

        bool dogru = malzemelerDogru && baliklarIyi;

        if (dogru)
        {
            Debug.Log($"[MasaServis] Masa {masaNo}: Siparis DOGRU ✓");

            // O masadaki musteri yemeye baslasin (siparisteki ana urune gore el objesi acilir)
            MusteriAI musteri = SiparisYonetimi.Instance != null
                ? SiparisYonetimi.Instance.MasaMusterisiniAl(masaNo)
                : null;

            // Icecek/YanUrun kaplarinin (bardak/kasa) ALT objelerini topla; musteri
            // kalkarken (yemeSuresi sonunda) silinecek. Kaplar masada kalir.
            List<GameObject> masadanSilinecekler = AltObjeleriTopla(urunler);
            if (musteri != null) musteri.Ye(AnaUrunBul(siparis), masadanSilinecekler, siparis.ToplamFiyat);

            // Masadaki ekmek arasi(lar)ini yok et (altindaki tum malzemelerle birlikte)
            EkmekArasi[] ekmekArasilari = kok.GetComponentsInChildren<EkmekArasi>(false);
            for (int i = 0; i < ekmekArasilari.Length; i++)
                if (ekmekArasilari[i] != null) Destroy(ekmekArasilari[i].gameObject);

            // Siparisi ekrandan (monitorlerden) kaldir + tamamlanmis say
            if (SiparisYonetimi.Instance != null)
            {
                SiparisYonetimi.Instance.SiparisiKaldir(siparis);
                SiparisYonetimi.Instance.MasaSiparisiTamamla(masaNo);
            }
        }
        else
        {
            string mesaj = $"[MasaServis] Masa {masaNo}: Siparis YANLIS ✗";
            mesaj += $"\n  Siparis: {Adlar(siparis.malzemeler)}";
            mesaj += $"\n  Tepsi:   {Adlar(tepsidekiler)}";
            if (eksikler.Count > 0) mesaj += $"\n  EKSIK (siparişte var, tepside yok): {Adlar(eksikler)}";
            if (fazlalar.Count > 0) mesaj += $"\n  FAZLA (tepside var, siparişte yok): {Adlar(fazlalar)}";
            if (!baliklarIyi) mesaj += $"\n  PIŞME: {pismeSorunu}";
            Debug.LogWarning(mesaj);

            // Siparisi ekrandan (monitorlerden) kaldir
            if (SiparisYonetimi.Instance != null)
                SiparisYonetimi.Instance.SiparisiKaldir(siparis);

            // O masadaki musteri: bir sure bekleyip yemeden kalksin gitsin
            MusteriAI musteri = SiparisYonetimi.Instance != null
                ? SiparisYonetimi.Instance.MasaMusterisiniAl(masaNo)
                : null;
            if (musteri != null) musteri.YanlisServis();
        }
    }

    void OnTriggerExit(Collider other)
    {
        Transform kok = KokBul(other);
        if (kok != null) islenenKokler.Remove(kok);
    }

    // TEST: bir transform'un hiyerarsi yolunu yazar (Tepsi/Yemek gibi)
    private string HiyerarsiYolu(Transform t)
    {
        if (t == null) return "(null)";
        string yol = t.name;
        Transform p = t.parent;
        int guvenlik = 0;
        while (p != null && guvenlik++ < 10)
        {
            yol = p.name + "/" + yol;
            p = p.parent;
        }
        return yol;
    }

    // Tepsinin kok transform'unu bul (rigidbody varsa onun, yoksa root)
    private Transform KokBul(Collider other)
    {
        if (other == null) return null;
        if (other.attachedRigidbody != null) return other.attachedRigidbody.transform;
        return other.transform.root;
    }

    // Siparis ile tepsi arasindaki farki hesaplar:
    // eksikler = siparişte olup tepside olmayanlar, fazlalar = tepside olup siparişte olmayanlar.
    private void EksikFazlaHesapla(List<Malzeme> siparis, List<Malzeme> tepsi)
    {
        eksikler.Clear();
        fazlalar.Clear();

        gecici.Clear();
        gecici.AddRange(tepsi);

        // Siparisteki her ogeyi tepsiden dusurmeye calis; dusemezsek EKSIK
        for (int i = 0; i < siparis.Count; i++)
        {
            int idx = gecici.IndexOf(siparis[i]);
            if (idx < 0) eksikler.Add(siparis[i]);
            else gecici.RemoveAt(idx);
        }

        // Tepside artakalanlar = FAZLA
        fazlalar.AddRange(gecici);
    }

    // Icecek ve YanUrun kaplarinin (UrunKimligi tasiyan bardak/kasa) alt objelerini toplar.
    // Kap masada kalir; sadece alt obje (icecek/kalamar) musteri kalkinca silinir.
    // Hicbir sey bulunmazsa null doner (musteri de null'i sorunsuz karsilar).
    private List<GameObject> AltObjeleriTopla(UrunKimligi[] urunler)
    {
        if (urunler == null) return null;

        List<GameObject> liste = null;
        for (int i = 0; i < urunler.Length; i++)
        {
            UrunKimligi u = urunler[i];
            if (u == null || u.Malzeme == null) continue;

            MalzemeKategorisi k = u.Malzeme.Kategori;
            if (k != MalzemeKategorisi.Icecek && k != MalzemeKategorisi.YanUrun) continue;

            Transform kap = u.transform;
            for (int c = 0; c < kap.childCount; c++)
            {
                if (liste == null) liste = new List<GameObject>();
                liste.Add(kap.GetChild(c).gameObject);
            }
        }
        return liste;
    }

    // Tepsideki tum balik(lar)in iki yuzu de (alt+ust) PISMIS araliginda mi?
    // Cig veya yanmis bir yuz varsa false (yanlis siparis). Pisirilebilir balik yoksa
    // veya izgara referansi yoksa true doner (pismisligi bloklamadan malzeme kararina birakir).
    private bool BaliklarIyiPismisMi(Transform kok, out string sorun)
    {
        sorun = null;
        if (kok == null) return true;

        PisirilebilirNesne[] baliklar = kok.GetComponentsInChildren<PisirilebilirNesne>(false);
        if (baliklar == null || baliklar.Length == 0) return true; // pisirilebilir balik yok

        if (pisirmeSistemi == null)
        {
            Debug.LogWarning("[MasaServis] pisirmeSistemi atanmamis; balik pismisligi kontrol edilemiyor.", this);
            return true; // bloklamadan gec
        }

        for (int i = 0; i < baliklar.Length; i++)
        {
            PisirilebilirNesne b = baliklar[i];
            if (b == null) continue;

            PisirmeSistemi.PismeDurumu alt = pisirmeSistemi.DurumHesapla(b.AltPismeIlerleme);
            PisirmeSistemi.PismeDurumu ust = pisirmeSistemi.DurumHesapla(b.UstPismeIlerleme);

            if (alt != PisirmeSistemi.PismeDurumu.Pismis || ust != PisirmeSistemi.PismeDurumu.Pismis)
            {
                sorun = $"{b.name} -> Alt: {alt}, Ust: {ust} (ikisi de PISMIS olmali)";
                return false;
            }
        }
        return true;
    }

    // Siparisteki ana urunu (Balik kategorisi) bulur - el objesi secimi icin
    private Malzeme AnaUrunBul(Siparis siparis)
    {
        for (int i = 0; i < siparis.malzemeler.Count; i++)
            if (siparis.malzemeler[i] != null && siparis.malzemeler[i].Kategori == MalzemeKategorisi.Balik)
                return siparis.malzemeler[i];
        return null;
    }

    // Malzeme listesini okunur isimlere cevirir (debug icin)
    private string Adlar(List<Malzeme> liste)
    {
        sb.Clear();
        for (int i = 0; i < liste.Count; i++)
        {
            if (liste[i] == null) continue;
            if (sb.Length > 0) sb.Append(", ");
            string ad = liste[i].MalzemeAdi;
            if (string.IsNullOrEmpty(ad) || ad == "Malzeme") ad = liste[i].name; // asset adi (Levrek, Domates...)
            sb.Append(ad);
        }
        return sb.Length == 0 ? "(bos)" : sb.ToString();
    }
}
