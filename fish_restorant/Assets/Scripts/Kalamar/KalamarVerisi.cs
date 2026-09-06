using UnityEngine;

/// <summary>
/// Kalamar prefab'ýna eklenir.
/// Tek yüzlü piþme ilerlemesi ve shader slider'ýný yönetir.
/// MaterialPropertyBlock kullanýr ? her instance baðýmsýz piþer, material instance oluþmaz.
///
/// Kurulum:
/// 1. Kalamar prefab'ýnda bu script olsun
/// 2. Renderer referansýný ata (boþsa child'lardan otomatik bulur)
/// 3. Shader'da piþme slider property'sinin adýný gir (varsayýlan: _PismeSlider)
/// </summary>
public class KalamarVerisi : MonoBehaviour
{
    [Header("Renderer")]
    [Tooltip("Kalamarýn renderer'ý. Boþ býrakýrsan child'lardan otomatik bulur.")]
    [SerializeField] private Renderer kalamarRenderer;

    [Header("Shader")]
    [Tooltip("Shader'daki piþme slider property adý")]
    [SerializeField] private string shaderSliderAdi = "_PismeSlider";

    private float pismeIlerleme = 0f;
    private int shaderSliderID;
    private MaterialPropertyBlock propBlock;
    private bool hazir;

    // === Public ===
    public float PismeIlerleme => pismeIlerleme;
    public bool Pisti => pismeIlerleme >= 1f;
    public Renderer Renderer => kalamarRenderer;

    void Awake()
    {
        if (kalamarRenderer == null)
            kalamarRenderer = GetComponentInChildren<Renderer>();

        shaderSliderID = Shader.PropertyToID(shaderSliderAdi);
        propBlock = new MaterialPropertyBlock();
        hazir = kalamarRenderer != null;

        if (hazir) SliderUygula(0f);
        else Debug.LogWarning($"[KalamarVerisi] '{name}' üzerinde Renderer bulunamadý!");
    }

    /// <summary>
    /// Piþme ilerlemesini ayarla (0-1 arasý). Shader otomatik güncellenir.
    /// </summary>
    public void IlerlemeyiAyarla(float yeniIlerleme)
    {
        pismeIlerleme = Mathf.Clamp01(yeniIlerleme);
        if (hazir) SliderUygula(pismeIlerleme);
    }

    void SliderUygula(float deger)
    {
        kalamarRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(shaderSliderID, deger);
        kalamarRenderer.SetPropertyBlock(propBlock);
    }
}