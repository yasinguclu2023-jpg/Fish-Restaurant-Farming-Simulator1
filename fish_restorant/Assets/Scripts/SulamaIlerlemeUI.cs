using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sulama süresince dolan fill bar. Ekrandaki Image objesine ekle.
///
/// RadialIlerlemeUI'dan farkı: fill METODUNA karışmaz.
/// Radial mı, yatay bar mı, dikey mi -> Image üzerinden SEN seçersin.
/// SetActive kullanmaz, fillAmount 0 ile gizlenir.
///
/// KURULUM:
/// 1. Canvas altında bir Image oluştur, Image Type = Filled yap, fill method'u seç.
/// 2. Bu scripti o Image'a ekle. Başka ayar gerekmez.
/// </summary>
[RequireComponent(typeof(Image))]
public class SulamaIlerlemeUI : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Sulama başladıktan kaç saniye sonra bar görünmeye başlasın.")]
    [SerializeField] private float gostermeGecikmesi = 0.05f;

    [Tooltip("Sulama bittiğinde bar dolu kalıp kaç saniye sonra sıfırlansın.")]
    [SerializeField] private float bitisteBeklemeSuresi = 0.15f;

    private Image image;
    private BitkiSulamaSistemi sistem;
    private float baslamaZamani;
    private bool bekliyor;

    void Awake()
    {
        image = GetComponent<Image>();

        // Fill method / origin AYARLANMAZ -> inspector'daki seçimin korunur
        image.type = Image.Type.Filled;
        image.fillAmount = 0f;
    }

    void Start()
    {
        sistem = BitkiSulamaSistemi.Instance;

        if (sistem == null)
        {
            Debug.LogError("SulamaIlerlemeUI: BitkiSulamaSistemi bulunamadı! Sahnede olduğundan emin ol.");
            return;
        }

        sistem.OnSulamaBasladi += Basladi;
        sistem.OnSulamaIlerleme += IlerlemeGuncelle;
        sistem.OnSulamaBitti += Bitti;
        sistem.OnSulamaIptal += IptalEdildi;
    }

    void OnDestroy()
    {
        if (sistem != null)
        {
            sistem.OnSulamaBasladi -= Basladi;
            sistem.OnSulamaIlerleme -= IlerlemeGuncelle;
            sistem.OnSulamaBitti -= Bitti;
            sistem.OnSulamaIptal -= IptalEdildi;
        }
    }

    void Basladi()
    {
        baslamaZamani = Time.time;
        bekliyor = true;
        image.fillAmount = 0f;
    }

    void IlerlemeGuncelle(float ilerleme)
    {
        if (bekliyor && Time.time - baslamaZamani >= gostermeGecikmesi)
            bekliyor = false;

        if (!bekliyor)
            image.fillAmount = ilerleme;
    }

    void Bitti()
    {
        bekliyor = false;

        if (bitisteBeklemeSuresi <= 0f)
        {
            image.fillAmount = 0f;
            return;
        }

        // Bar dolu kalıp kısa süre sonra sıfırlansın (tamamlandığı görülsün)
        CancelInvoke(nameof(Gizle));
        Invoke(nameof(Gizle), bitisteBeklemeSuresi);
    }

    /// <summary>Tuş erken bırakıldı: bar beklemeden anında sıfırlanır.</summary>
    void IptalEdildi()
    {
        bekliyor = false;
        CancelInvoke(nameof(Gizle));
        image.fillAmount = 0f;
    }

    void Gizle()
    {
        image.fillAmount = 0f;
    }
}
