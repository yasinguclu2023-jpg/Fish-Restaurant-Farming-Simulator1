using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EldeNesneKamera : MonoBehaviour
{
    [Header("Kamera Ayarlarý")]
    [SerializeField] private Camera anaKamera;

    [Header("Layer Ayarlarý")]
    [SerializeField] private string eldeNesneLayerAdi = "EldeNesne";

    [Header("El Kamerasý Ayarlarý")]
    [SerializeField] private float elKameraFOV = 70f;

    private Camera elKamerasi;
    private int eldeNesneLayer;

    void Awake()
    {
        eldeNesneLayer = LayerMask.NameToLayer(eldeNesneLayerAdi);

        if (eldeNesneLayer == -1)
        {
            Debug.LogError($"'{eldeNesneLayerAdi}' layer'ý bulunamadý! Layer oluþturun.");
            enabled = false;
            return;
        }

        if (anaKamera == null)
            anaKamera = Camera.main;

        ElKamerasiOlustur();
        KameralariAyarla();
    }

    void ElKamerasiOlustur()
    {
        GameObject elKamObj = new GameObject("ElKamerasi");
        elKamObj.transform.SetParent(anaKamera.transform, false);
        elKamObj.transform.localPosition = Vector3.zero;
        elKamObj.transform.localRotation = Quaternion.identity;

        elKamerasi = elKamObj.AddComponent<Camera>();

        // URP Camera Stack - Overlay kamera olarak ayarla
        var cameraData = elKamerasi.GetUniversalAdditionalCameraData();
        cameraData.renderType = CameraRenderType.Overlay;

        // Ana kameranýn stack'ine ekle
        var mainCameraData = anaKamera.GetUniversalAdditionalCameraData();
        mainCameraData.cameraStack.Add(elKamerasi);
    }

    void KameralariAyarla()
    {
        // Ana kamera - elde nesne layer'ýný GÖRMEZ
        anaKamera.cullingMask &= ~(1 << eldeNesneLayer);

        // El kamerasý - SADECE elde nesne layer'ýný görür
        elKamerasi.cullingMask = 1 << eldeNesneLayer;
        elKamerasi.fieldOfView = elKameraFOV;
        elKamerasi.nearClipPlane = 0.01f;
        elKamerasi.farClipPlane = 10f;
    }

    public void NesneLayerAyarla(GameObject nesne)
    {
        if (nesne == null) return;
        SetLayerRecursive(nesne, eldeNesneLayer);
    }

    public void NesneLayerSifirla(GameObject nesne, int eskiLayer)
    {
        if (nesne == null) return;
        SetLayerRecursive(nesne, eskiLayer);
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    public int EldeNesneLayer => eldeNesneLayer;
}