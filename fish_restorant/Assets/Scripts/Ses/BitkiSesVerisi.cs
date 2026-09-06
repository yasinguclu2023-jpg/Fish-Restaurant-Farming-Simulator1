using UnityEngine;

/// <summary>
/// BitkiBuyumeSistemi olan objelere ekle.
/// Hasat, büyüme ve sulama sesleri için.
/// </summary>
public class BitkiSesVerisi : MonoBehaviour
{
    [Header("Hasat Sesleri")]
    [Tooltip("Hasat yapıldığında çalınacak ses")]
    [SerializeField] private SesVerisi hasatSesi;

    [Header("Sulama Sesleri")]
    [Tooltip("Bitki suya doyduğunda (sulama tamamlanınca) çalınacak ses. " +
             "Suyun AKMA sesi kovadadır (SulamaKovasi), buradaki ses bitkinin 'sulandım' sesidir.")]
    [SerializeField] private SesVerisi sulamaSesi;

    [Header("Büyüme Sesleri (Opsiyonel)")]
    [Tooltip("Yeni aşamaya geçildiğinde çalınacak ses")]
    [SerializeField] private SesVerisi buyumeAsamaSesi;

    [Tooltip("Bitki tamamen büyüdüğünde (hasat hazır)")]
    [SerializeField] private SesVerisi buyumeTamamSesi;

    /// <summary>
    /// Hasat yapıldığında çağır
    /// </summary>
    public void HasatSesiCal()
    {
        if (hasatSesi != null && SesYoneticisi.Instance != null)
        {
            SesYoneticisi.Instance.SesCal(hasatSesi, transform.position);
        }
    }

    /// <summary>
    /// Sulama tamamlandığında çağır
    /// </summary>
    public void SulamaSesiCal()
    {
        if (sulamaSesi != null && SesYoneticisi.Instance != null)
        {
            SesYoneticisi.Instance.SesCal(sulamaSesi, transform.position);
        }
    }

    /// <summary>
    /// Büyüme aşaması değiştiğinde çağır
    /// </summary>
    public void BuyumeAsamaSesiCal()
    {
        if (buyumeAsamaSesi != null && SesYoneticisi.Instance != null)
        {
            SesYoneticisi.Instance.SesCal(buyumeAsamaSesi, transform.position);
        }
    }

    /// <summary>
    /// Büyüme tamamlandığında çağır
    /// </summary>
    public void BuyumeTamamSesiCal()
    {
        if (buyumeTamamSesi != null && SesYoneticisi.Instance != null)
        {
            SesYoneticisi.Instance.SesCal(buyumeTamamSesi, transform.position);
        }
    }

    // Dışarıdan erişim
    public SesVerisi HasatSesi => hasatSesi;
    public SesVerisi SulamaSesi => sulamaSesi;
    public SesVerisi BuyumeAsamaSesi => buyumeAsamaSesi;
    public SesVerisi BuyumeTamamSesi => buyumeTamamSesi;
}
