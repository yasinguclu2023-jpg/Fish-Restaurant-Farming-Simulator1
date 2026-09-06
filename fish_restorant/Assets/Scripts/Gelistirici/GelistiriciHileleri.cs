using UnityEngine;

// ===== GELISTIRICI HILELERI (kaldirilabilir) =====
// Bu dosyanin TAMAMI test icindir. Silmek icin "Gelistirici" klasorunu ve
// UIYerlestirmeSistemi'ndeki isaretli blogu sil.

/// <summary>
/// TEST ICIN HIZLI HILE TUSU.
///
/// "Aktif" tiki ACIKKEN oyunda hile tusuna (varsayilan P) basinca:
///   1. Hesaba para eklenir,
///   2. Magazadaki TUM GUN KILITLERI acilir -> sogan, marul, bira makinesi,
///      sarap makinesi vb. aninda satin alinabilir olur.
///
/// Esyalar BEDAVA OLMAZ, sadece kilitleri kalkar; parayla alinir.
/// Zaten eklenen para tam bunun icindir.
///
/// KURULUM:
/// 1. Sahnede bos bir GameObject olustur -> adini "GelistiriciHileleri" yap.
/// 2. Bu scripti ekle.
/// 3. "Aktif" tikini ac. (Kapatirsan tus hicbir sey yapmaz.)
/// 4. Oyunda P'ye bas.
///
/// GUVENLIK: "Sadece Editorde" tiki acikken hile SADECE Unity Editor'de calisir,
/// build'e sizmaz. Demoyu birine gonderirken tiki acik birak.
/// </summary>
public class GelistiriciHileleri : MonoBehaviour
{
    [Header("Ana Ayar")]
    [Tooltip("KAPALI ise hile tusu hicbir sey yapmaz.")]
    [SerializeField] private bool aktif = true;

    [Tooltip("Bu tusa basinca hile calisir.")]
    [SerializeField] private KeyCode hileTusu = KeyCode.P;

    [Header("Ne Yapsin?")]
    [Tooltip("Basinca eklenecek para. 0 = para ekleme.")]
    [Min(0)]
    [SerializeField] private int eklenecekPara = 1000;

    [Tooltip("Magazadaki gun kilitlerini kaldirir (sogan, marul, bira/sarap makinesi...). " +
             "Bir kez acilinca oturum boyunca acik kalir.")]
    [SerializeField] private bool tumKilitleriAc = true;

    [Tooltip("Musteriler de butun malzemeleri istesin (sogan, marul, bira, sarap). " +
             "Gun plani ne derse desin gecerli olur, gun degisse bile kapanmaz.")]
    [SerializeField] private bool siparisleriDeAc = true;

    [Tooltip("Acilan malzemelerin siparise girme orani (0-100). " +
             "Sebzede agirlik, icecek/yan urunde gercek yuzde.")]
    [Range(0, 100)]
    [SerializeField] private int siparisOrani = 60;

    [Tooltip("Siparis basina en fazla kac sebze olsun. 0 = gun planindaki degere dokunma. " +
             "(Gun 1'de bu deger 1 oldugu icin tek sebze cikar; 3 yaparsan cesitlenir.)")]
    [Min(0)]
    [SerializeField] private int siparistekiMaxSebze = 3;

    [Header("Guvenlik")]
    [Tooltip("ACIK: Hile sadece Unity Editor'de calisir, alinan build'de calismaz. " +
             "Demoyu paylasirken ACIK birak.")]
    [SerializeField] private bool sadeceEditorde = true;

    [Header("Teshis")]
    [Tooltip("Hile calisinca Console'a ne yapildigini yazar.")]
    [SerializeField] private bool teshisLogu = true;

    void Update()
    {
        if (!aktif) return;

        // Build'de calismasin (istenirse)
        if (sadeceEditorde && !Application.isEditor) return;

        if (!Input.GetKeyDown(hileTusu)) return;

        HileyiUygula();
    }

    /// <summary>Hileyi uygular. Tusa basmadan da (baska bir yerden) cagrilabilir.</summary>
    [ContextMenu("Hileyi Simdi Uygula")]
    public void HileyiUygula()
    {
        int eklenen = 0;
        int acilanMagaza = 0;

        // 1) Para
        if (eklenecekPara > 0)
        {
            if (EkonomiYoneticisi.Instance != null)
            {
                EkonomiYoneticisi.Instance.Kazan(eklenecekPara);
                eklenen = eklenecekPara;
            }
            else if (teshisLogu)
            {
                Debug.LogWarning("[Hile] EkonomiYoneticisi bulunamadi, para eklenemedi.", this);
            }
        }

        // 2) Magaza kilitleri (sahnede birden fazla magaza olabilir, hepsini ac)
        if (tumKilitleriAc)
        {
            UIYerlestirmeSistemi[] magazalar = FindObjectsOfType<UIYerlestirmeSistemi>();

            for (int i = 0; i < magazalar.Length; i++)
            {
                if (magazalar[i] == null) continue;

                magazalar[i].TumKilitleriAc();
                acilanMagaza++;
            }

            if (acilanMagaza == 0 && teshisLogu)
                Debug.LogWarning("[Hile] Sahnede UIYerlestirmeSistemi (magaza) bulunamadi.", this);
        }

        // 3) Siparis havuzu: musteriler de butun malzemeleri istesin
        bool siparisAcildi = false;

        if (siparisleriDeAc)
        {
            SiparisUretici uretici = FindObjectOfType<SiparisUretici>();

            if (uretici != null)
            {
                uretici.TumMalzemeleriAc(siparisOrani, siparistekiMaxSebze);
                siparisAcildi = true;
            }
            else if (teshisLogu)
            {
                Debug.LogWarning("[Hile] SiparisUretici bulunamadi, siparisler acilamadi.", this);
            }
        }

        if (teshisLogu)
        {
            int para = EkonomiYoneticisi.Instance != null ? EkonomiYoneticisi.Instance.Para : -1;

            Debug.Log($"[Hile] UYGULANDI\n" +
                      $"  eklenen para   : {eklenen}   (yeni bakiye: {para})\n" +
                      $"  acilan magaza  : {acilanMagaza}  (tum gun kilitleri kalkti)\n" +
                      $"  siparis havuzu : {(siparisAcildi ? $"acildi (%{siparisOrani}, max {siparistekiMaxSebze} sebze)" : "dokunulmadi")}", this);
        }
    }
}
// ===== GELISTIRICI HILELERI SONU =====
