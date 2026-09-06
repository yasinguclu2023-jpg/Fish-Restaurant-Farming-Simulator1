using UnityEngine;

/// <summary>
/// Oyuncuya (veya kameraya) eklenir.
/// Sepet elindeyken kýzartma makinesine bakýp sol klick'le kalamar yerleþtirir.
///
/// Mevcut RaycastSistemi'ni kullanýr (BakilanObje property'sini okur).
///
/// Kurulum:
/// 1. Oyuncu/kamera objesine bu scripti ekle
/// 2. RaycastSistemi referansýný ata
/// 3. 'eldekiSepet'i:
///    - Statik tutuyorsan inspector'dan ata
///    - NesneAlmaSistemi ile dinamikse, alýndýðýnda EldekiSepetiAyarla(sepet) çaðýr
/// </summary>
public class KalamarPisirmeEtkilesim : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private RaycastSistemi raycastSistemi;

    [Tooltip("Oyuncunun elindeki sepet. Runtime'da NesneAlmaSistemi'nden de set edilebilir.")]
    [SerializeField] private KalamarSepeti eldekiSepet;

    [Header("Giriþ")]
    [Tooltip("Yerleþtirme tuþu (varsayýlan: sol fare týklama)")]
    [SerializeField] private KeyCode yerlestirmeTusu = KeyCode.Mouse0;

    void Update()
    {
        if (raycastSistemi == null) return;
        if (!Input.GetKeyDown(yerlestirmeTusu)) return;

        // 1) Elimizde sepet var mý ve içinde kalamar var mý?
        if (eldekiSepet == null || !eldekiSepet.KalamarVarMi) return;

        // 2) Bakýlan objede kýzartma makinesi var mý?
        GameObject bakilan = raycastSistemi.BakilanObje;
        if (bakilan == null) return;

        KizartmaMakinesi makine = bakilan.GetComponent<KizartmaMakinesi>();
        if (makine == null) makine = bakilan.GetComponentInParent<KizartmaMakinesi>();
        if (makine == null) return;

        // 3) Boþ slot var mý?
        if (!makine.BosSlotVar) return;

        // 4) Yerleþtir ? baþarýlýysa sepetten 1 azalt
        if (makine.KalamarYerlestir())
            eldekiSepet.KalamarTuket();
    }

    /// <summary>
    /// Runtime'da sepeti ayarla (NesneAlmaSistemi entegrasyonu için).
    /// Sepet eline alýndýðýnda: etkilesim.EldekiSepetiAyarla(sepet);
    /// Sepet býrakýldýðýnda:    etkilesim.EldekiSepetiAyarla(null);
    /// </summary>
    public void EldekiSepetiAyarla(KalamarSepeti sepet)
    {
        eldekiSepet = sepet;
    }
}