using TMPro;
using UnityEngine;

/// <summary>
/// Bitkinin üstündeki UI'da sulama bedelini yazar (ör. "5₺").
/// Bedeli parent'taki BitkiBuyumeSistemi'nden OTOMATİK okur -> her bitkiye elle yazmazsın.
///
/// Para yetmiyorsa yazı rengi değişir. Renk kontrolü HER FRAME DEĞİL, sadece
/// EkonomiYoneticisi.ParaDegisti tetiklendiğinde yapılır.
///
/// KURULUM:
/// 1. Bitki prefabındaki World Space Canvas'ın içine TextMeshPro - Text (UI) ekle.
/// 2. Bu scripti o Text objesine ekle. Gerisi otomatik.
/// </summary>
public class BitkiSulamaBedeliUI : MonoBehaviour
{
    [Header("Referanslar (boş bırakılırsa otomatik bulunur)")]
    [Tooltip("Yazının yazılacağı TextMeshPro alanı.")]
    [SerializeField] private TMP_Text yaziAlani;

    [Tooltip("Bedeli okunacak bitki. Boş bırakılırsa parent'larda aranır.")]
    [SerializeField] private BitkiBuyumeSistemi bitki;

    [Header("Yazı")]
    [Tooltip("Yazı formatı. {0} bedelin geleceği yerdir. Örnek: \"{0} TL\", \"{0}₺\", \"-{0}\"\n" +
             "İçinde {0} yoksa bedel yazının başına eklenir.")]
    [SerializeField] private string yaziFormati = "{0} TL";

    [Tooltip("Bedel 0 ise yazıyı tamamen gizle (bedava sulama).")]
    [SerializeField] private bool bedavaysaGizle = true;

    [Header("Renkler")]
    [Tooltip("Para yeterliyken yazı rengi.")]
    [SerializeField] private Color normalRenk = Color.white;

    [Tooltip("Para yetmezken yazı rengi.")]
    [SerializeField] private Color yetersizRenk = new Color(1f, 0.25f, 0.25f, 1f);

    private int bedel;
    private bool abone;

    void Awake()
    {
        if (yaziAlani == null)
            yaziAlani = GetComponent<TMP_Text>();

        if (yaziAlani == null)
            yaziAlani = GetComponentInChildren<TMP_Text>(true);

        if (bitki == null)
            bitki = GetComponentInParent<BitkiBuyumeSistemi>();

        if (yaziAlani == null)
            Debug.LogError("[BedelUI] TextMeshPro alanı bulunamadı!", this);

        if (bitki == null)
            Debug.LogError("[BedelUI] Parent'ta BitkiBuyumeSistemi bulunamadı!", this);
    }

    void OnEnable()
    {
        YaziyiGuncelle();
        AboneOl();
        RenkGuncelle();
    }

    void OnDisable()
    {
        AbonelikBirak();
    }

    void AboneOl()
    {
        if (abone || EkonomiYoneticisi.Instance == null) return;

        EkonomiYoneticisi.Instance.ParaDegisti += ParaDegistiginde;
        abone = true;
    }

    void AbonelikBirak()
    {
        if (!abone || EkonomiYoneticisi.Instance == null) return;

        EkonomiYoneticisi.Instance.ParaDegisti -= ParaDegistiginde;
        abone = false;
    }

    /// <summary>
    /// Bedeli bitkiden okuyup yazar. İkon her açıldığında çağrılır,
    /// böylece inspector'dan bedeli değiştirsen bile yazı doğru kalır.
    /// </summary>
    public void YaziyiGuncelle()
    {
        if (yaziAlani == null || bitki == null) return;

        bedel = bitki.SulamaBedeli;

        if (bedavaysaGizle && bedel <= 0)
        {
            yaziAlani.enabled = false;
            return;
        }

        yaziAlani.enabled = true;
        yaziAlani.SetText(MetniOlustur(bedel));
    }

    /// <summary>
    /// string.Format KULLANILMAZ: inspector'a {1} gibi hatalı bir format yazılırsa
    /// FormatException fırlatıp UI'ı çökertiyordu. Replace ile hiçbir girdi hata vermez.
    /// </summary>
    string MetniOlustur(int deger)
    {
        if (string.IsNullOrEmpty(yaziFormati))
            return deger.ToString();

        if (yaziFormati.Contains("{0}"))
            return yaziFormati.Replace("{0}", deger.ToString());

        // Format içinde {0} yoksa bedeli başa ekle: "TL" -> "5 TL"
        return deger + " " + yaziFormati;
    }

    void ParaDegistiginde(int yeniPara)
    {
        RenkGuncelle(yeniPara);
    }

    void RenkGuncelle()
    {
        int para = EkonomiYoneticisi.Instance != null ? EkonomiYoneticisi.Instance.Para : int.MaxValue;
        RenkGuncelle(para);
    }

    void RenkGuncelle(int para)
    {
        if (yaziAlani == null) return;

        Color hedef = para >= bedel ? normalRenk : yetersizRenk;

        if (yaziAlani.color != hedef)
            yaziAlani.color = hedef;
    }
}
