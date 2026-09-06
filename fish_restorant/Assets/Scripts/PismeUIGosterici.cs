using UnityEngine;
using TMPro;

/// <summary>
/// Balýk prefab'ýna eklenir. Oyuncu balýða baktýðýnda piþme bilgilerini gösterir.
/// Canvas her zaman kameraya döner (billboard).
/// 
/// Kurulum:
/// 1. Bu scripti balýk prefab'ýnýn parent objesine ekle
/// 2. Inspector'dan canvas, isimText, altOranText, ustOranText'i sürükle
/// 3. Canvas baþlangýçta deaktif olmalý
/// </summary>
public class PismeUIGosterici : MonoBehaviour
{
    [Header("UI Referanslarý")]
    [Tooltip("Piþme bilgilerini gösteren World Space Canvas")]
    [SerializeField] private GameObject canvas;

    [Tooltip("Balýk ismi")]
    [SerializeField] private TextMeshProUGUI isimText;

    [Tooltip("Alt piþme oraný texti")]
    [SerializeField] private TextMeshProUGUI altOranText;

    [Tooltip("Üst piþme oraný texti")]
    [SerializeField] private TextMeshProUGUI ustOranText;

    [Header("Ayarlar")]
    [Tooltip("Balýðýn ismi")]
    [SerializeField] private string balikIsmi = "Balýk";

    [Tooltip("Oyuncunun UI'ý görebileceði maksimum mesafe")]
    [SerializeField] private float gorunurlukMesafesi = 5f;

    private PisirilebilirNesne pisirilebilirData;
    private Camera oyuncuKamerasi;
    private RaycastSistemi raycastSistemi;
    private bool uiAktif = false;

    void Start()
    {
        pisirilebilirData = GetComponent<PisirilebilirNesne>();
        if (pisirilebilirData == null)
            pisirilebilirData = GetComponentInChildren<PisirilebilirNesne>();

        oyuncuKamerasi = Camera.main;
        raycastSistemi = FindObjectOfType<RaycastSistemi>();

        if (canvas != null)
            canvas.SetActive(false);

        if (isimText != null)
            isimText.text = balikIsmi;
    }

    void LateUpdate()
    {
        if (canvas == null) return;

        // Her zaman kameraya döndür (balýk dönse bile)
        if (oyuncuKamerasi != null)
            canvas.transform.rotation = oyuncuKamerasi.transform.rotation;

        if (raycastSistemi == null || pisirilebilirData == null) return;

        bool bakiyor = BaligaBakiyorMu();

        if (bakiyor && !uiAktif)
        {
            canvas.SetActive(true);
            uiAktif = true;
        }
        else if (!bakiyor && uiAktif)
        {
            canvas.SetActive(false);
            uiAktif = false;
        }

        if (uiAktif)
        {
            UIGuncelle();
        }
    }

    bool BaligaBakiyorMu()
    {
        if (raycastSistemi == null) return false;
        if (!raycastSistemi.ObjeyeBakiyorMu) return false;

        GameObject bakilan = raycastSistemi.BakilanObje;
        if (bakilan == null) return false;

        if (bakilan == gameObject) return true;
        if (bakilan.transform.IsChildOf(transform)) return true;

        if (oyuncuKamerasi == null) return false;
        float mesafe = Vector3.Distance(oyuncuKamerasi.transform.position, transform.position);
        if (mesafe > gorunurlukMesafesi) return false;

        return false;
    }

    void UIGuncelle()
    {
        int altYuzde = Mathf.RoundToInt(pisirilebilirData.AltPismeIlerleme * 100f);
        int ustYuzde = Mathf.RoundToInt(pisirilebilirData.UstPismeIlerleme * 100f);

        if (altOranText != null)
            altOranText.text = $"Bottom Doneness: %{altYuzde}";

        if (ustOranText != null)
            ustOranText.text = $"Top Doneness: %{ustYuzde}";
    }

    void BillboardUygula()
    {
        if (oyuncuKamerasi == null) return;

        // Canvas'ý kameraya doðru döndür
        canvas.transform.rotation = oyuncuKamerasi.transform.rotation;
    }
}