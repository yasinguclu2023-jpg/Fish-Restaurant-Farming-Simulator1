using UnityEngine;

/// <summary>
/// Bu nesne hangi tuşla eline alınır?
/// </summary>
public enum AlmaTusuTipi
{
    /// <summary>Bitki/makine ise taşıma tuşu (X), değilse sağ tık. Varsayılan.</summary>
    Otomatik,
    /// <summary>Her zaman sağ tık ile alınır.</summary>
    SagTik,
    /// <summary>Her zaman taşıma tuşu (X) ile alınır.</summary>
    TasimaTusu
}

/// <summary>
/// Bir nesnenin alınabilmesi için basılı tutma gerektiren marker component.
///
/// Kullanım:
/// - Bitki için: Bitki root objesine ekle, "parentTanCalissin" = true yap
///   (Oyuncu bitkinin child meshlerine tıklasa bile bulunur)
/// - Makine için: Makine objesine ekle, "parentTanCalissin" = false bırak
///   (Sadece makineye doğrudan tıklanırsa çalışır, üstündeki tepsi/kasa için tetiklenmez)
///
/// TUŞ: Bitki ve makineler taşıma tuşu (X) ile, diğer her şey (kasa, tepsi yığını,
/// toplu kağıt vb.) SAĞ TIK ile alınır. Otomatik tespit edilir; gerekirse
/// "almaTusu" alanından elle zorlanabilir.
/// </summary>
public class BasiliTutmaGerekli : MonoBehaviour
{
    [Header("Basılı Tutma Ayarları")]
    [Tooltip("Bu nesne alınabilir mi?")]
    [SerializeField] private bool alinabilir = true;

    [Tooltip("Basılı tutma süresi (saniye)")]
    [SerializeField] private float sure = 1.5f;

    [Header("Hedef Arama Davranışı")]
    [Tooltip("Eğer bu component child'da değil parent'ta ise, oyuncu child'a tıklasa bile bulunur. " +
             "BİTKİ için TRUE yap (mesh child'da, marker root'ta). " +
             "MAKİNE için FALSE bırak (üstündeki tepsi/kasaya tıklayınca tetiklenmesin).")]
    [SerializeField] private bool parentTanCalissin = false;

    [Header("Alma Tuşu")]
    [Tooltip("Otomatik: bitki (BitkiBuyumeSistemi) veya makine (UretimMakinesiSistemi / " +
             "KizartmaMakinesi) ise TAŞIMA TUŞU (X), değilse SAĞ TIK ile alınır. " +
             "İstisna gerekiyorsa buradan elle seç.")]
    [SerializeField] private AlmaTusuTipi almaTusu = AlmaTusuTipi.Otomatik;

    // Otomatik tespit cache'i (her frame GetComponent aramasın)
    private bool tusCozuldu;
    private bool tasimaTusuIleAlinir;

    public bool Alinabilir => alinabilir;
    public float Sure => sure;
    public bool ParentTanCalissin => parentTanCalissin;

    /// <summary>
    /// true  → taşıma tuşu (X) ile alınır (bitki, makine)
    /// false → sağ tık ile alınır (kasa, tepsi yığını, toplu kağıt vb.)
    /// </summary>
    public bool TasimaTusuIleAlinir
    {
        get
        {
            if (!tusCozuldu)
            {
                tasimaTusuIleAlinir = TusuCoz();
                tusCozuldu = true;
            }
            return tasimaTusuIleAlinir;
        }
    }

    bool TusuCoz()
    {
        if (almaTusu == AlmaTusuTipi.SagTik) return false;
        if (almaTusu == AlmaTusuTipi.TasimaTusu) return true;
        return BitkiVeyaMakineMi();
    }

    bool BitkiVeyaMakineMi()
    {
        if (GetComponentInChildren<BitkiBuyumeSistemi>(true) != null) return true;
        if (GetComponentInChildren<UretimMakinesiSistemi>(true) != null) return true;
        if (GetComponentInChildren<KizartmaMakinesi>(true) != null) return true;
        return false;
    }

    public void AlinabilirAyarla(bool deger) => alinabilir = deger;
}
