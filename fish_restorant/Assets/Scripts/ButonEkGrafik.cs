using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Unity Button'un renk gecisini (Normal / Uzerinde / Basili / Pasif) butonun
/// ALTINDAKI ek grafiklere de uygular. Button bileseni sadece tek bir
/// "Target Graphic" boyar; ikon Image'lari ve yazilar bu yuzden degismez.
///
/// Renkler grafigin KENDI rengiyle carpilir (Unity'nin yaptiginin aynisi).
/// Yani ikonun kendi rengi beyazsa, buradaki renk aynen gorunur.
///
/// KURULUM:
/// 1. Bu scripti BUTONUN KENDISINE ekle (Button bileseni olan objeye).
/// 2. Hedef Grafikler listesine boyanmasini istedigin Image / Text objelerini surukle.
///    Bos birakirsan ve "Otomatik Bul" aciksa, tum cocuk grafikleri kendisi bulur.
/// 3. Renkleri Button'daki degerlerle ayni yaparsan tam senkron calisir.
/// 4. (Opsiyonel) Olcek ve ses ayarlarini doldur.
///
/// NOT: Ek grafik butonun RectTransform alani DISINDA kalirsa fare o grafigin
/// uzerindeyken buton "uzerinde" sayilmaz. Grafikler butonun alani icinde olmali.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
public class ButonEkGrafik : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    private enum Durum { Normal, Uzerinde, Basili, Pasif }

    [Header("Hedef Grafikler")]
    [Tooltip("Butonla birlikte renk degistirecek Image / Text objeleri.")]
    [SerializeField] private Graphic[] hedefGrafikler;

    [Tooltip("Liste bossa tum cocuk grafikleri otomatik bul (butonun kendi Target Graphic'i haric).")]
    [SerializeField] private bool otomatikBul = true;

    [Header("Renkler")]
    [Tooltip("Grafigin kendi rengiyle carpilir. Beyaz = degisiklik yok.")]
    [SerializeField] private Color normalRenk = Color.white;
    [SerializeField] private Color uzerindeRenk = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color basiliRenk = new Color(0.65f, 0.65f, 0.65f, 1f);
    [SerializeField] private Color pasifRenk = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Tooltip("Renk gecis suresi (saniye). 0 = anlik.")]
    [Range(0f, 1f)]
    [SerializeField] private float gecisSuresi = 0.1f;

    [Header("Olcek (opsiyonel)")]
    [Tooltip("Uzerine gelince / basinca buton buyuyup kuculsun mu?")]
    [SerializeField] private bool olcekDegistir = false;
    [SerializeField] private float uzerindeOlcek = 1.05f;
    [SerializeField] private float basiliOlcek = 0.95f;
    [Tooltip("Olcek gecis hizi. Buyudukce daha hizli.")]
    [SerializeField] private float olcekHizi = 12f;

    [Header("Ses (opsiyonel)")]
    [Tooltip("Fare butonun uzerine ilk geldiginde calar.")]
    [SerializeField] private SesVerisi uzerineGelmeSesi;
    [Tooltip("Butona basildiginda calar.")]
    [SerializeField] private SesVerisi tiklamaSesi;

    private Selectable buton;
    private RectTransform rectTransform;
    private Vector3 baslangicOlcegi;
    private Vector3 hedefOlcek;

    private bool uzerinde;
    private bool basili;
    private bool sonInteractable = true;

    void Awake()
    {
        buton = GetComponent<Selectable>();
        rectTransform = transform as RectTransform;
        baslangicOlcegi = transform.localScale;
        hedefOlcek = baslangicOlcegi;

        GrafikleriHazirla();
    }

    void OnEnable()
    {
        uzerinde = false;
        basili = false;
        sonInteractable = buton.interactable;
        hedefOlcek = baslangicOlcegi;
        transform.localScale = baslangicOlcegi;

        DurumUygula(true);
    }

    void OnDisable()
    {
        uzerinde = false;
        basili = false;
        transform.localScale = baslangicOlcegi;
    }

    void Update()
    {
        // Buton kod tarafindan pasif/aktif edilmis olabilir -> yakala.
        if (buton.interactable != sonInteractable)
        {
            sonInteractable = buton.interactable;
            if (!sonInteractable)
            {
                uzerinde = false;
                basili = false;
            }
            DurumUygula(false);
        }

        if (olcekDegistir)
            OlcekGuncelle();
    }

    /// <summary>Liste bossa cocuk grafikleri toplar, butonun kendi hedefini disarida birakir.</summary>
    private void GrafikleriHazirla()
    {
        if (hedefGrafikler != null && hedefGrafikler.Length > 0) return;
        if (!otomatikBul)
        {
            hedefGrafikler = new Graphic[0];
            return;
        }

        Graphic[] hepsi = GetComponentsInChildren<Graphic>(true);
        var liste = new System.Collections.Generic.List<Graphic>(hepsi.Length);

        for (int i = 0; i < hepsi.Length; i++)
        {
            Graphic g = hepsi[i];
            if (g == null) continue;
            if (g == buton.targetGraphic) continue;            // Button zaten bunu boyuyor
            if (g.GetComponent<Selectable>() != null) continue; // Ic ice buton varsa karisma

            liste.Add(g);
        }

        hedefGrafikler = liste.ToArray();
    }

    /// <summary>Aktif duruma gore tum hedef grafikleri boyar.</summary>
    private void DurumUygula(bool anlik)
    {
        Durum durum = AktifDurum();
        Color hedefRenk = DurumRengi(durum);
        float sure = anlik ? 0f : gecisSuresi;

        if (hedefGrafikler != null)
        {
            for (int i = 0; i < hedefGrafikler.Length; i++)
            {
                Graphic g = hedefGrafikler[i];
                if (g == null) continue;

                // CrossFadeColor CanvasRenderer rengini degistirir:
                // grafigin kendi Color'i korunur, ustune carpilir.
                g.CrossFadeColor(hedefRenk, sure, true, true);
            }
        }

        if (olcekDegistir)
            hedefOlcek = baslangicOlcegi * DurumOlcegi(durum);
    }

    private Durum AktifDurum()
    {
        if (!buton.interactable) return Durum.Pasif;
        if (basili && uzerinde) return Durum.Basili;
        if (uzerinde) return Durum.Uzerinde;
        return Durum.Normal;
    }

    private Color DurumRengi(Durum durum)
    {
        switch (durum)
        {
            case Durum.Uzerinde: return uzerindeRenk;
            case Durum.Basili: return basiliRenk;
            case Durum.Pasif: return pasifRenk;
            default: return normalRenk;
        }
    }

    private float DurumOlcegi(Durum durum)
    {
        switch (durum)
        {
            case Durum.Uzerinde: return uzerindeOlcek;
            case Durum.Basili: return basiliOlcek;
            default: return 1f;
        }
    }

    private void OlcekGuncelle()
    {
        if (transform.localScale == hedefOlcek) return;

        // UI zaman olceginden etkilenmemeli (oyun duraklatilsa bile calissin).
        transform.localScale = Vector3.Lerp(
            transform.localScale, hedefOlcek, Time.unscaledDeltaTime * olcekHizi);

        if ((transform.localScale - hedefOlcek).sqrMagnitude < 0.000001f)
            transform.localScale = hedefOlcek;
    }

    private void SesCal(SesVerisi ses)
    {
        if (ses == null || SesYoneticisi.Instance == null) return;
        SesYoneticisi.Instance.SesCal2D(ses);
    }

    // --- Pointer olaylari (cocuk objelerden de buraya ulasir) ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!buton.interactable) return;

        uzerinde = true;
        DurumUygula(false);
        SesCal(uzerineGelmeSesi);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        uzerinde = false;
        DurumUygula(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!buton.interactable) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        basili = true;
        DurumUygula(false);
        SesCal(tiklamaSesi);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        basili = false;
        DurumUygula(false);
    }

    /// <summary>
    /// Grafik listesini kod tarafindan degistirmek icin (dinamik olusturulan butonlar).
    /// </summary>
    public void GrafikleriAyarla(params Graphic[] grafikler)
    {
        hedefGrafikler = grafikler;
        DurumUygula(true);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Editorde renk degistirirken canli onizleme
        if (!Application.isPlaying || buton == null) return;
        DurumUygula(true);
    }
#endif
}
