using UnityEngine;

/// <summary>
/// Bu component, bir nesnenin üzerine baþka nesnelerin yerleþtirilebileceðini belirtir.
/// Masa, tezgah, raf gibi yüzeylere eklenir.
/// 
/// Kurulum:
/// 1. Üzerine nesne býrakýlacak objeye bu scripti ekle
/// 2. Collider olduðundan emin ol
/// 3. Layer'ý RaycastSistemi'nin etkilesimLayer'ýna ekle
/// </summary>
public class YerlestirilebilirNesne : MonoBehaviour
{
    [Header("Yerleþtirme Ayarlarý")]
    [Tooltip("Sadece bu taglere sahip nesneler yerleþtirilebilir. Boþ býrakýrsan hepsini kabul eder.")]
    [SerializeField] public string[] kabulEdilenTagler;

    [Tooltip("Yerleþtirilen nesne bu layerlara döner. Boþ býrakýrsan orijinal layer korunur.")]
    [SerializeField] public LayerMask yerlesebilecegiLayerlar;

    [Tooltip("Yere göre offset (yerleþtirme noktasý ayarý)")]
    [SerializeField] public Vector3 yereOfset = Vector3.zero;

    [Header("Fizik Ayarlarý")]
    [Tooltip("Yerleþtirilen nesne kinematic yapýlsýn mý?")]
    [SerializeField] public bool kinematicYap = true;

    [Tooltip("Yerleþtirilen nesnenin collider'ý açýlsýn mý?")]
    [SerializeField] public bool colliderAc = true;

    /// <summary>
    /// Bu nesne yerleþtirilebilir mi kontrol eder
    /// </summary>
    public bool NesneKabulEdilirMi(GameObject nesne)
    {
        if (nesne == null) return false;

        // Tag listesi boþsa her þeyi kabul et
        if (kabulEdilenTagler == null || kabulEdilenTagler.Length == 0)
            return true;

        // Tag kontrolü
        string nesneTag = nesne.tag;
        for (int i = 0; i < kabulEdilenTagler.Length; i++)
        {
            if (kabulEdilenTagler[i] == nesneTag)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Nesneyi bu yüzeye yerleþtir
    /// </summary>
    public bool NesneYerlestir(GameObject nesne, Vector3 orijinalScale, int orijinalLayer)
    {
        if (!NesneKabulEdilirMi(nesne)) return false;

        // Parent'ý kaldýr
        nesne.transform.SetParent(null);

        // Orijinal scale'i geri ver
        nesne.transform.localScale = orijinalScale;

        // Layer ayarla
        int hedefLayer = orijinalLayer;
        if (yerlesebilecegiLayerlar.value != 0)
        {
            // Ýlk aktif layer'ý bul
            for (int i = 0; i < 32; i++)
            {
                if ((yerlesebilecegiLayerlar.value & (1 << i)) != 0)
                {
                    hedefLayer = i;
                    break;
                }
            }
        }
        SetLayerRecursive(nesne, hedefLayer);

        // Fizik ayarlarý
        if (nesne.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = kinematicYap;
            rb.useGravity = !kinematicYap;
        }

        // Collider ayarlarý
        if (nesne.TryGetComponent(out Collider col))
        {
            col.enabled = colliderAc;
            col.isTrigger = false;
        }

        // Býrakma sesi
        if (nesne.TryGetComponent(out NesneSesVerisi sesVerisi))
        {
            sesVerisi.BirakmaSesiCal(nesne.transform.position);
        }

        return true;
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    // Public propertyler
    public Vector3 YereOfset => yereOfset;
    public LayerMask YerlesebilecegiLayerlar => yerlesebilecegiLayerlar;
    public bool KinematicYap => kinematicYap;
    public bool ColliderAc => colliderAc;
}