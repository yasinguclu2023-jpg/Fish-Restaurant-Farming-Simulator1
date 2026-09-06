using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Image objesine at.
/// SetActive kullanmaz, fillAmount ile gizler/g�sterir.
/// </summary>
[RequireComponent(typeof(Image))]
public class RadialIlerlemeUI : MonoBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private float gostermeGecikmesi = 0.1f;

    [Header("Kaynak Filtresi")]
    [Tooltip("Bu radial hangi sistem için dolsun? NesneAlma = bitki/nesne alma, Kesme = kesme tahtası. " +
             "Farklı kaynaklar birbirine karışmaz.")]
    [SerializeField] private BasiliTutmaYoneticisi.TutmaKaynagi hangiKaynak = BasiliTutmaYoneticisi.TutmaKaynagi.NesneAlma;

    private Image image;
    private BasiliTutmaYoneticisi yonetici;
    private float baslamaZamani;
    private bool bekliyor;

    void Awake()
    {
        image = GetComponent<Image>();

        // Image ayarlar�
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Radial360;
        image.fillOrigin = (int)Image.Origin360.Top;
        image.fillClockwise = true;
        image.fillAmount = 0f;
    }

    void Start()
    {
        yonetici = BasiliTutmaYoneticisi.Instance;

        if (yonetici == null)
        {
            Debug.LogError("RadialIlerlemeUI: BasiliTutmaYoneticisi bulunamad�! Sahnede oldu�undan emin ol.");
            return;
        }

        // Eventlere abone ol
        yonetici.OnBasladi += Basladi;
        yonetici.OnIlerleme += IlerlemeGuncelle;
        yonetici.OnTamamlandi += Gizle;
        yonetici.OnIptal += Gizle;
    }

    void OnDestroy()
    {
        if (yonetici != null)
        {
            yonetici.OnBasladi -= Basladi;
            yonetici.OnIlerleme -= IlerlemeGuncelle;
            yonetici.OnTamamlandi -= Gizle;
            yonetici.OnIptal -= Gizle;
        }
    }

    void Basladi(BasiliTutmaYoneticisi.TutmaKaynagi kaynak)
    {
        if (kaynak != hangiKaynak) return;

        baslamaZamani = Time.time;
        bekliyor = true;
        image.fillAmount = 0f;
    }

    void IlerlemeGuncelle(BasiliTutmaYoneticisi.TutmaKaynagi kaynak, float ilerleme)
    {
        if (kaynak != hangiKaynak) return;

        // Gecikme sonras� g�ster
        if (bekliyor && Time.time - baslamaZamani >= gostermeGecikmesi)
        {
            bekliyor = false;
        }

        if (!bekliyor)
        {
            image.fillAmount = ilerleme;
        }
    }

    void Gizle(BasiliTutmaYoneticisi.TutmaKaynagi kaynak)
    {
        if (kaynak != hangiKaynak) return;

        image.fillAmount = 0f;
        bekliyor = false;
    }
}