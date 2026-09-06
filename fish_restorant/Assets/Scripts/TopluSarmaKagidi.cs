using UnityEngine;

/// <summary>
/// Toplu sarma kağıdı objesi.
/// Sol tık → Inspector'da belirlenen kağıt prefab'ı eline gelir
/// Sağ tık → toplu kağıt objesinin kendisini alırsın (normal nesne gibi)
/// 
/// KURULUM:
/// 1. Toplu sarma kağıdı modeline bu scripti ekle
/// 2. Inspector'da kagitPrefab'a tek sarma kağıdı prefab'ını ata
/// 3. NesneAlmaSistemi'nde hem toplu kağıdın tag'i hem tek kağıdın tag'i için el pozisyonu olmalı
/// 4. Objenin collider'ı ve RaycastSistemi'nin etkilesimLayer'ında olmalı
/// </summary>
public class TopluSarmaKagidi : MonoBehaviour
{
    [Header("Kağıt Ayarları")]
    [Tooltip("Sol tıkla eline gelecek tek kağıt prefab'ı")]
    [SerializeField] private GameObject kagitPrefab;

    [Header("Ses")]
    [Tooltip("Kağıt alınırken çalan ses")]
    [SerializeField] private AudioClip kagitAlmaSesi;

    [Tooltip("Ses seviyesi")]
    [Range(0f, 1f)]
    [SerializeField] private float sesSeviyesi = 1f;

    /// <summary>
    /// Kağıt prefab'ı atanmış mı?
    /// </summary>
    public bool KagitVarMi => kagitPrefab != null;

    /// <summary>
    /// Tek kağıt prefab'ı spawn edip döndür.
    /// NesneAlmaSistemi tarafından çağrılır.
    /// </summary>
    public GameObject KagitAl()
    {
        if (kagitPrefab == null)
        {
            Debug.LogError("[TopluSarmaKagidi] kagitPrefab atanmamış!");
            return null;
        }

        GameObject kagit = Instantiate(kagitPrefab);
        kagit.name = kagitPrefab.name;

        // Ses çal
        if (kagitAlmaSesi != null)
            AudioSource.PlayClipAtPoint(kagitAlmaSesi, transform.position, sesSeviyesi);

        Debug.Log($"[TopluSarmaKagidi] Kağıt alındı: {kagit.name}");
        return kagit;
    }
}