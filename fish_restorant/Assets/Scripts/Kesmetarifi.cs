using UnityEngine;

/// <summary>
/// Kesme tarifi. Her sebze için giriş tag'i ve çıkış prefab'ı tanımlar.
/// 
/// Oluşturma: Project > Sağ Tık > Create > Kesme Sistemi > Kesme Tarifi
/// 
/// Örnek:
///   Giriş Tag: "domates"    → Çıkış Prefab: dilimlenmisDomatesPrefab
///   Giriş Tag: "sogan"      → Çıkış Prefab: dilimlenmisSoganPrefab
///   Giriş Tag: "marul"      → Çıkış Prefab: dilimlenmisMalrulPrefab
/// </summary>
[CreateAssetMenu(fileName = "YeniKesmeTarifi", menuName = "Kesme Sistemi/Kesme Tarifi")]
public class KesmeTarifi : ScriptableObject
{
    [Header("Giriş")]
    [Tooltip("Kesilecek nesnenin tag'i")]
    public string girisTag;

    [Header("Çıkış")]
    [Tooltip("Kesme sonrası oluşacak prefab")]
    public GameObject cikisPrefab;

    [Header("Zamanlama")]
    [Tooltip("Animasyon başladıktan kaç saniye sonra nesne değişsin (0 = animasyon bitince)")]
    public float degisimGecikmesi = 0f;

    [Header("Ses (Opsiyonel)")]
    [Tooltip("Kesme sırasında çalınacak özel ses")]
    public SesVerisi kesmeSesi;
}