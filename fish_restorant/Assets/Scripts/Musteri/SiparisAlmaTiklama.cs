using UnityEngine;

/// <summary>
/// Register (kasa) objesine eklenir. Bu objeye bakip SOL TIK yapinca,
/// o an ana board'da bekleyen siparisi "alir" (SiparisYonetimi.AktifSiparisiGonder):
/// siparis mutfak monitorlerine fis olarak gider, musteri masasina yurur, sira ilerler.
///
/// Optimize: input sadece ekranda bekleyen siparis VARKEN kontrol edilir.
///
/// KURULUM:
/// 1. Register objesine bu scripti ekle.
/// 2. Register'da bir Collider olmali ve layer'i RaycastSistemi'nin etkilesimLayer'inda olmali.
/// 3. raycastSistemi bos birakilirsa sahnede otomatik bulunur.
/// </summary>
public class SiparisAlmaTiklama : MonoBehaviour
{
    [Tooltip("Bos birakirsan sahnede otomatik bulunur.")]
    [SerializeField] private RaycastSistemi raycastSistemi;

    void Awake()
    {
        if (raycastSistemi == null)
            raycastSistemi = FindObjectOfType<RaycastSistemi>();
    }

    void Update()
    {
        if (raycastSistemi == null) return;

        // Bekleyen siparis yoksa hic ugrasma (optimize)
        if (SiparisYonetimi.Instance == null || !SiparisYonetimi.Instance.EkrandaSiparisVar) return;

        if (!Input.GetMouseButtonDown(0)) return;

        // Bu register'a (veya child'ina) bakiyor muyuz?
        GameObject bakilan = raycastSistemi.BakilanObje;
        if (bakilan == null) return;
        if (bakilan != gameObject && !bakilan.transform.IsChildOf(transform)) return;

        SiparisYonetimi.Instance.AktifSiparisiGonder();
    }
}
