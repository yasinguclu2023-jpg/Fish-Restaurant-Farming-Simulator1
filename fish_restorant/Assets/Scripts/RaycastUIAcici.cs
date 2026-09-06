using UnityEngine;

public class RaycastUIAcici : MonoBehaviour
{
    [Header("UI Ayarlarý")]
    [Tooltip("UI'larýn baðlý olduðu Canvas")]
    [SerializeField] private string canvasAdi = "Canvas";

    [Tooltip("Canvas içindeki UI objelerinin isimleri")]
    [SerializeField] private string[] uiIsimleri;

    private RaycastSistemi raycastSistemi;
    private GameObject[] bulunanUIler;
    private bool uiAcik = false;

    void Start()
    {
        raycastSistemi = FindObjectOfType<RaycastSistemi>();

        // Canvas'ý bul
        GameObject canvas = GameObject.Find(canvasAdi);
        if (canvas == null)
        {
            Debug.LogError($"'{canvasAdi}' isimli Canvas bulunamadý!");
            return;
        }

        // UI'larý bul (kapalý olsalar bile)
        if (uiIsimleri != null && uiIsimleri.Length > 0)
        {
            bulunanUIler = new GameObject[uiIsimleri.Length];

            for (int i = 0; i < uiIsimleri.Length; i++)
            {
                Transform bulunan = BulRecursive(canvas.transform, uiIsimleri[i]);
                if (bulunan != null)
                    bulunanUIler[i] = bulunan.gameObject;
                else
                    Debug.LogWarning($"'{uiIsimleri[i]}' bulunamadý!");
            }
        }

        UIleriKapat();
    }

    Transform BulRecursive(Transform parent, string isim)
    {
        foreach (Transform child in parent)
        {
            if (child.name == isim)
                return child;

            Transform sonuc = BulRecursive(child, isim);
            if (sonuc != null)
                return sonuc;
        }
        return null;
    }

    void Update()
    {
        if (raycastSistemi == null || bulunanUIler == null) return;

        bool bakiyor = raycastSistemi.BakilanObje == gameObject;

        if (bakiyor && !uiAcik)
        {
            UIleriAc();
            uiAcik = true;
        }
        else if (!bakiyor && uiAcik)
        {
            UIleriKapat();
            uiAcik = false;
        }
    }

    void UIleriAc()
    {
        for (int i = 0; i < bulunanUIler.Length; i++)
        {
            if (bulunanUIler[i] != null)
                bulunanUIler[i].SetActive(true);
        }
    }

    void UIleriKapat()
    {
        for (int i = 0; i < bulunanUIler.Length; i++)
        {
            if (bulunanUIler[i] != null)
                bulunanUIler[i].SetActive(false);
        }
    }

    void OnDisable()
    {
        if (uiAcik)
        {
            UIleriKapat();
            uiAcik = false;
        }
    }
}