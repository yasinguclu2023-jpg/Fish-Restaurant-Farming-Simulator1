using UnityEngine;

/// <summary>
/// Alýnabilir/yerleþtirilebilir nesnelere ekle.
/// Inspector'dan ses verilerini ata.
/// </summary>
public class NesneSesVerisi : MonoBehaviour
{
    [Header("Alma/Býrakma Sesleri")]
    [Tooltip("Nesne alýndýðýnda çalýnacak ses")]
    [SerializeField] private SesVerisi alinirkenkiSes;

    [Tooltip("Nesne býrakýldýðýnda/yerleþtirildiðinde çalýnacak ses")]
    [SerializeField] private SesVerisi birakilirkenkiSes;

    [Header("Ek Sesler (Opsiyonel)")]
    [Tooltip("Nesne ile etkileþime girildiðinde (ör: üzerine týklama)")]
    [SerializeField] private SesVerisi etkilesimSesi;

    [Tooltip("Nesne taþýnýrken döngüsel ses (opsiyonel)")]
    [SerializeField] private SesVerisi tasinirkenkiSes;

    /// <summary>
    /// Nesne alýndýðýnda çaðýr
    /// </summary>
    public void AlmaSesiCal()
    {
        if (alinirkenkiSes != null && SesYoneticisi.Instance != null)
        {
            SesYoneticisi.Instance.SesCal(alinirkenkiSes, transform.position);
        }
    }

    /// <summary>
    /// Nesne býrakýldýðýnda/yerleþtirildiðinde çaðýr
    /// </summary>
    public void BirakmaSesiCal()
    {
        if (birakilirkenkiSes != null && SesYoneticisi.Instance != null)
        {
            SesYoneticisi.Instance.SesCal(birakilirkenkiSes, transform.position);
        }
    }

    /// <summary>
    /// Belirli bir pozisyonda býrakma sesi çal
    /// </summary>
    public void BirakmaSesiCal(Vector3 pozisyon)
    {
        if (birakilirkenkiSes != null && SesYoneticisi.Instance != null)
        {
            SesYoneticisi.Instance.SesCal(birakilirkenkiSes, pozisyon);
        }
    }

    /// <summary>
    /// Etkileþim sesi çal
    /// </summary>
    public void EtkilesimSesiCal()
    {
        if (etkilesimSesi != null && SesYoneticisi.Instance != null)
        {
            SesYoneticisi.Instance.SesCal(etkilesimSesi, transform.position);
        }
    }

    // Dýþarýdan eriþim için
    public SesVerisi AlinirkenkiSes => alinirkenkiSes;
    public SesVerisi BirakilirkenkiSes => birakilirkenkiSes;
    public SesVerisi EtkilesimSesi => etkilesimSesi;
    public SesVerisi TasinirkenkiSes => tasinirkenkiSes;

    public bool AlmaSesiVar => alinirkenkiSes != null;
    public bool BirakmaSesiVar => birakilirkenkiSes != null;
}