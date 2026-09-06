using UnityEngine;

/// <summary>
/// E tuþuyla kaðýda/ekmeðe bakýnca sarma iþlemini tetikler.
/// Kaðýt kaybolur, sarýlmýþ kaðýt görseli ekmek'in üstüne eklenir.
/// Ekmek + malzemeler olduðu gibi kalýr.
/// 
/// KURULUM:
/// 1. Oyuncuya bu scripti ekle
/// 2. RaycastSistemi sahnede olmalý
/// 3. Kaðýt objesinde EkmekYerlestirmeSistemi olmalý ve sarma ayarlarý yapýlmýþ olmalý
/// </summary>
public class EkmekSarmaSistemi : MonoBehaviour
{
    [Header("Sarma Ayarlarý")]
    [Tooltip("Sarma tuþu")]
    [SerializeField] private KeyCode sarmaTusu = KeyCode.E;

    private RaycastSistemi raycastSistemi;
    private NesneYerlestirmeSistemi yerlestirmeSistemi;
    private BalikCevirmeSistemi cevirmeSistemi;

    void Start()
    {
        raycastSistemi = FindObjectOfType<RaycastSistemi>();
        yerlestirmeSistemi = FindObjectOfType<NesneYerlestirmeSistemi>();
        cevirmeSistemi = FindObjectOfType<BalikCevirmeSistemi>();

        if (raycastSistemi == null)
            Debug.LogError("[EkmekSarma] RaycastSistemi bulunamadý!");
    }

    void Update()
    {
        if (raycastSistemi == null) return;
        if (yerlestirmeSistemi != null && yerlestirmeSistemi.YerlesimModuAktif) return;
        if (cevirmeSistemi != null && cevirmeSistemi.CevirmeAktif) return;

        if (Input.GetKeyDown(sarmaTusu))
        {
            SarmaDene();
        }
    }

    void SarmaDene()
    {
        if (!raycastSistemi.ObjeyeBakiyorMu) return;

        GameObject hedef = raycastSistemi.BakilanObje;
        if (hedef == null) return;

        EkmekYerlestirmeSistemi ekmekSistemi = hedef.GetComponent<EkmekYerlestirmeSistemi>();
        if (ekmekSistemi == null)
            ekmekSistemi = hedef.GetComponentInParent<EkmekYerlestirmeSistemi>();
        if (ekmekSistemi == null)
            ekmekSistemi = hedef.GetComponentInChildren<EkmekYerlestirmeSistemi>();

        if (ekmekSistemi == null) return;

        if (!ekmekSistemi.SarmaYapilabilirMi())
        {
            Debug.Log("[EkmekSarma] Sarma yapýlamaz (ekmek yok veya sarma prefab atanmamýþ).");
            return;
        }

        ekmekSistemi.SarmaYap();
    }
}