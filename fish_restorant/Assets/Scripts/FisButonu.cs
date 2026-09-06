using UnityEngine;

/// <summary>
/// Rafta duran tek bir tiklanabilir fis modeli.
/// Sol tik -> kendi fisPrefab'inin bir KOPYASI uretilir ve ele gelir.
/// Raftaki model oldugu yerde kalir (sonsuz kaynak, hicbir sey eksilmez).
///
/// 13 farkli fis icin: 13 ayri model, her birine bu script + kendi fisPrefab'i.
///
/// KURULUM:
/// 1. Raftaki her fis modeline bu scripti + bir Collider ekle.
/// 2. Model, RaycastSistemi'nin etkilesimLayer'inda olmali (bakinca outline/yesil ray).
/// 3. fisPrefab'a ELE GELECEK fis prefab'ini ata (Collider + Rigidbody + tag'i olan).
///    Not: ele gelen prefab'in tag'i, NesneAlmaSistemi'ndeki tagPozisyonlari'nda
///    bir el pozisyonuyla eslesmeli; yoksa fis ele gelmez.
/// </summary>
public class FisButonu : MonoBehaviour
{
    [Header("Fis")]
    [Tooltip("Sol tikla eline gelecek fis prefab'i (bu numaranin modeli).")]
    [SerializeField] private GameObject fisPrefab;

    [Header("Ses (opsiyonel)")]
    [Tooltip("Fis alinirken calan ses (SesVerisi asset'i).")]
    [SerializeField] private SesVerisi fisAlmaSesi;

    /// <summary>Alinabilir fis prefab'i atanmis mi?</summary>
    public bool FisVarMi => fisPrefab != null;

    /// <summary>Fis prefab'inin bir kopyasini uretip dondurur (raftaki model kalir).</summary>
    public GameObject FisAl()
    {
        if (fisPrefab == null)
        {
            Debug.LogWarning("[FisButonu] fisPrefab ATANMAMIS!", this);
            return null;
        }

        GameObject fis = Instantiate(fisPrefab);
        fis.name = fisPrefab.name;

        if (fisAlmaSesi != null && SesYoneticisi.Instance != null)
            SesYoneticisi.Instance.SesCal(fisAlmaSesi, transform.position);

        return fis;
    }
}
