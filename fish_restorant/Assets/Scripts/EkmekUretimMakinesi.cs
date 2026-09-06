using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Ekmek üretim makinesi - UretimMakinesiSistemi'nden türetilir.
/// 
/// Çalýþma prensibi:
/// 1. Giriþ kasasýnda buðdaylar var (Kasa > Budaylar > buday1, buday2... yapýsý)
/// 2. Her döngüde: 1 buðday sil ? bekle
/// 3. Çýkýþ kasasý yoksa makinedeki sabit noktada spawn et
/// 4. Önce ALT ekmek spawn ? kýsa bekle ? ÜST ekmek spawn (çýkýþ kasasýnýn child'ý olur)
/// 5. Buðday görseli yükselip iner
/// </summary>
public class EkmekUretimMakinesi : UretimMakinesiSistemi
{
    [Header("Ekmek Makinesi - Çýkýþ Kasasý")]
    [Tooltip("Çýkýþ kasasý spawn noktasý (boþ Transform, makinede sabit)")]
    [SerializeField] private Transform cikisKasaSpawnNoktasi;

    [Tooltip("Çýkýþ kasasý prefab'ý")]
    [SerializeField] private GameObject cikisKasaPrefab;

    [Tooltip("Çýkýþ kasasý spawn edildikten sonra ilk ekmek spawn'a kadar bekleme")]
    [SerializeField] private float kasadanSonraBekleme = 0.5f;

    [Header("Ekmek Makinesi - Ekmek Spawn Noktalarý")]
    [Tooltip("Alt ekmek spawn noktalarý (makinede sabit, sýrayla doldurulur)")]
    [SerializeField] private Transform[] altEkmekSpawnNoktalari;

    [Tooltip("Üst ekmek spawn noktalarý (makinede sabit, sýrayla doldurulur)")]
    [SerializeField] private Transform[] ustEkmekSpawnNoktalari;

    [Header("Ekmek Makinesi - Ekmek Prefab'larý")]
    [Tooltip("Piþmiþ alt ekmek prefab'ý")]
    [SerializeField] private GameObject altEkmekPrefab;

    [Tooltip("Piþmiþ üst ekmek prefab'ý")]
    [SerializeField] private GameObject ustEkmekPrefab;

    [Tooltip("Alt ekmek spawn ile üst ekmek spawn arasý bekleme")]
    [SerializeField] private float altUstArasiBekleme = 0.3f;

    [Header("Ekmek Makinesi - Buðday Görseli")]
    [Tooltip("Buðday görseli için SiviModeliSistemi (opsiyonel)")]
    [SerializeField] private SiviModeliSistemi bugdaySistemi;

    [Header("Ekmek Makinesi - Üretim")]
    [Tooltip("Her bir buðdayýn tükenme süresi")]
    [SerializeField] private float bugdayTuketimSuresi = 2f;

    // Runtime
    private GameObject mevcutCikisKasasi;
    private int ekmekIndex;

    // ============================================================
    // ÜRETÝM SÜRECÝ - OVERRIDE
    // ============================================================

    protected override IEnumerator UretimSureci()
    {
        makineCalisiyor = true;
        ekmekIndex = 0;

        // Üretim partikül ve döngü sesi
        UretimSesiBaslat();

        // Buðday yükselsin
        if (bugdaySistemi != null) bugdaySistemi.YukselmeBaslat();

        // Buðdaylarý bul (Kasa > Budaylar > buday1, buday2... yapýsý)
        List<GameObject> bugdayListesi = BugdaylariBul();

        for (int i = 0; i < bugdayListesi.Count; i++)
        {
            // 1) Buðdayý sil
            if (bugdayListesi[i] != null)
            {
                bugdayListesi[i].SetActive(false);
                Destroy(bugdayListesi[i], 0.5f);
            }

            // 2) Bekle
            yield return new WaitForSeconds(bugdayTuketimSuresi);

            // 3) Çýkýþ kasasý yoksa veya alýndýysa yeni spawn et
            if (mevcutCikisKasasi == null || !mevcutCikisKasasi.transform.IsChildOf(cikisKasaSpawnNoktasi))
            {
                mevcutCikisKasasi = null;
                YeniCikisKasasiSpawnla();
                UrunSpawnSesiCal(); // çýkýþ kasasý spawn sesi
                yield return new WaitForSeconds(kasadanSonraBekleme);
            }

            // 4) ÖNCE alt ekmek spawn
            AltEkmekSpawnla();
            UrunSpawnSesiCal();

            // 5) Kýsa bekle
            yield return new WaitForSeconds(altUstArasiBekleme);

            // 6) SONRA üst ekmek spawn
            UstEkmekSpawnla();
            UrunSpawnSesiCal();

            ekmekIndex++;
        }

        // Buðday düþsün
        if (bugdaySistemi != null) bugdaySistemi.DusmeBaslat();

        UretimiTamamla();
    }

    // ============================================================
    // YARDIMCI METODLAR
    // ============================================================

    /// <summary>
    /// Kasanýn içindeki buðdaylarý bulur.
    /// Kasa > Container > buðdaylar yapýsýný destekler (UretimMakinesiSistemi'ndekiyle ayný mantýk).
    /// </summary>
    private List<GameObject> BugdaylariBul()
    {
        List<GameObject> liste = new List<GameObject>();
        if (mevcutKasa == null) return liste;

        Transform kasa = mevcutKasa.transform;
        Transform urunParent = kasa;

        // Kasa > Budaylar > buday1, buday2... yapýsý için
        for (int i = 0; i < kasa.childCount; i++)
        {
            Transform child = kasa.GetChild(i);
            if (child.childCount > 0)
            {
                urunParent = child;
                break;
            }
        }

        for (int i = 0; i < urunParent.childCount; i++)
        {
            GameObject child = urunParent.GetChild(i).gameObject;
            if (child.activeSelf)
                liste.Add(child);
        }

        return liste;
    }

    /// <summary>
    /// Yeni çýkýþ kasasý spawn et
    /// </summary>
    private void YeniCikisKasasiSpawnla()
    {
        if (cikisKasaPrefab == null || cikisKasaSpawnNoktasi == null) return;

        mevcutCikisKasasi = Instantiate(cikisKasaPrefab, cikisKasaSpawnNoktasi.position, cikisKasaSpawnNoktasi.rotation);
        mevcutCikisKasasi.transform.SetParent(cikisKasaSpawnNoktasi);

        Rigidbody rb = mevcutCikisKasasi.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        ekmekIndex = 0;
    }

    /// <summary>
    /// Alt ekmek spawn et
    /// </summary>
    private void AltEkmekSpawnla()
    {
        if (altEkmekPrefab == null || altEkmekSpawnNoktalari == null) return;
        if (ekmekIndex >= altEkmekSpawnNoktalari.Length) return;
        if (mevcutCikisKasasi == null) return;

        Transform nokta = altEkmekSpawnNoktalari[ekmekIndex];
        if (nokta == null) return;

        GameObject ekmek = Instantiate(altEkmekPrefab, nokta.position, nokta.rotation);
        ekmek.transform.SetParent(mevcutCikisKasasi.transform, true);

        Rigidbody rb = ekmek.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    /// <summary>
    /// Üst ekmek spawn et
    /// </summary>
    private void UstEkmekSpawnla()
    {
        if (ustEkmekPrefab == null || ustEkmekSpawnNoktalari == null) return;
        if (ekmekIndex >= ustEkmekSpawnNoktalari.Length) return;
        if (mevcutCikisKasasi == null) return;

        Transform nokta = ustEkmekSpawnNoktalari[ekmekIndex];
        if (nokta == null) return;

        GameObject ekmek = Instantiate(ustEkmekPrefab, nokta.position, nokta.rotation);
        ekmek.transform.SetParent(mevcutCikisKasasi.transform, true);

        Rigidbody rb = ekmek.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    // ============================================================
    // EDITOR GIZMO
    // ============================================================

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (cikisKasaSpawnNoktasi != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(cikisKasaSpawnNoktasi.position, new Vector3(0.4f, 0.3f, 0.4f));
            UnityEditor.Handles.Label(cikisKasaSpawnNoktasi.position + Vector3.up * 0.25f, "Cikis Kasa");
        }

        if (altEkmekSpawnNoktalari != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < altEkmekSpawnNoktalari.Length; i++)
            {
                if (altEkmekSpawnNoktalari[i] != null)
                {
                    Gizmos.DrawWireSphere(altEkmekSpawnNoktalari[i].position, 0.04f);
                    UnityEditor.Handles.Label(altEkmekSpawnNoktalari[i].position + Vector3.up * 0.08f, $"Alt {i}");
                }
            }
        }

        if (ustEkmekSpawnNoktalari != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < ustEkmekSpawnNoktalari.Length; i++)
            {
                if (ustEkmekSpawnNoktalari[i] != null)
                {
                    Gizmos.DrawWireSphere(ustEkmekSpawnNoktalari[i].position, 0.04f);
                    UnityEditor.Handles.Label(ustEkmekSpawnNoktalari[i].position + Vector3.up * 0.08f, $"Ust {i}");
                }
            }
        }
    }
#endif
}