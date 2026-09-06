using UnityEngine;

/// <summary>
/// Para prefabina eklenir. Musteri yiyip kalkinca masanin para noktasinda belirir.
/// Oyuncu bu objeye bakip SOL TIK yapinca fiyati hesaba eklenir ve obje yok olur.
///
/// KURULUM:
/// 1. Para prefabina (model) bu scripti ekle.
/// 2. Prefabda bir Collider olmali ve layer'i RaycastSistemi'nin etkilesim layer'inda olmali.
/// 3. raycastSistemi bos birakilirsa sahnede otomatik bulunur.
/// Fiyat, spawn edilirken ParaYoneticisi tarafindan atanir (elle girme).
/// </summary>
public class ParaObjesi : MonoBehaviour
{
    [Tooltip("Bos birakirsan sahnede otomatik bulunur.")]
    [SerializeField] private RaycastSistemi raycastSistemi;

    [Header("Ses (opsiyonel)")]
    [Tooltip("Para alininca calan ses. Bos birakabilirsin.")]
    [SerializeField] private SesVerisi toplamaSesi;

    private int fiyat;
    private bool toplandi;

    /// <summary>ParaYoneticisi spawn ederken cagirir: bu paranin degeri.</summary>
    public void FiyatAyarla(int deger) => fiyat = deger;

    void Awake()
    {
        if (raycastSistemi == null)
            raycastSistemi = FindObjectOfType<RaycastSistemi>();
    }

    void Update()
    {
        if (toplandi || raycastSistemi == null) return;
        if (!Input.GetMouseButtonDown(0)) return;

        // Bu paraya (veya child'ina) bakiyor muyuz?
        GameObject bakilan = raycastSistemi.BakilanObje;
        if (bakilan == null) return;
        if (bakilan != gameObject && !bakilan.transform.IsChildOf(transform)) return;

        Topla();
    }

    private void Topla()
    {
        toplandi = true;

        if (EkonomiYoneticisi.Instance != null)
            EkonomiYoneticisi.Instance.Kazan(fiyat);

        if (toplamaSesi != null && SesYoneticisi.Instance != null)
            SesYoneticisi.Instance.SesCal2D(toplamaSesi);

        Destroy(gameObject);
    }
}
