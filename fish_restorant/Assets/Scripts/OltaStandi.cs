using UnityEngine;

/// <summary>
/// Olta stand�. Bu scripti stant objesine ekleyin.
/// ��ine bir bo� GameObject koyun (oltan�n duraca�� pozisyon).
/// </summary>
public class OltaStandi : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Oltan�n stantta duraca�� pozisyon (bo� GameObject)")]
    public Transform oltaPozisyonu;

    [Tooltip("Stantta duran olta objesi - ba�lang��ta buraya koyun")]
    public GameObject mevcutOlta;

    [Header("Sesler")]
    [Tooltip("Stanttan olta al�n�rken �alacak ses")]
    [SerializeField] private SesVerisi oltaAlmaSesi;

    [Tooltip("Stanta olta konulurken �alacak ses")]
    [SerializeField] private SesVerisi oltaKoymaSesi;

    // Stanttaki oltan�n ba�lang��taki (g�zel) d�nya �l�e�i - geri koyarken bunu kullan�r�z
    private Vector3 baslangicOltaScale = Vector3.one;

    /// <summary>
    /// Stantta olta var m�?
    /// </summary>
    public bool OltaVarMi => mevcutOlta != null;

    void Awake()
    {
        if (mevcutOlta != null)
            baslangicOltaScale = mevcutOlta.transform.lossyScale;
    }

    /// <summary>
    /// Stanttan oltay� al
    /// </summary>
    public GameObject OltaAl()
    {
        if (mevcutOlta == null) return null;

        // Stanttaki g�zel �l�e�i hat�rla ki geri koyunca ayn� g�z�ks�n
        baslangicOltaScale = mevcutOlta.transform.lossyScale;

        GameObject olta = mevcutOlta;
        mevcutOlta = null;

        if (oltaAlmaSesi != null && SesYoneticisi.Instance != null)
            SesYoneticisi.Instance.SesCal(oltaAlmaSesi, transform.position);

        return olta;
    }

    /// <summary>
    /// Stanta olta koy
    /// </summary>
    public void OltaKoy(GameObject olta)
    {
        if (olta == null || oltaPozisyonu == null) return;

        mevcutOlta = olta;

        // Oltay� stant pozisyonuna yerle�tir
        olta.transform.SetParent(null);
        olta.transform.position = oltaPozisyonu.position;
        olta.transform.rotation = oltaPozisyonu.rotation;
        olta.transform.localScale = baslangicOltaScale;

        // Rigidbody varsa kinematic yap
        if (olta.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }

        // Collider'lar� a� (raycast alg�las�n diye)
        Collider[] cols = olta.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = true;

        if (oltaKoymaSesi != null && SesYoneticisi.Instance != null)
            SesYoneticisi.Instance.SesCal(oltaKoymaSesi, transform.position);
    }
}