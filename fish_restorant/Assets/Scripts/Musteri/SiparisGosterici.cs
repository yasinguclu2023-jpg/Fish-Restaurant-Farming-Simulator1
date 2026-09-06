using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bir siparisi board'da gosterir. Hem ana board hem 2 mutfak ekrani bunu kullanir.
///
/// Yerlesim uc bolge (her kategori kendi slotlarina sirayla dizilir):
///   Balik   -> balikSlotlari
///   Sebze   -> sebzeSlotlari
///   Icecek  -> icecekSlotlari
/// Slotlari ONCEDEN yerlestir (board'a child Image'lar); kod sadece sprite atar / acar-kapar.
/// Boylece her sipariste Instantiate yok -> optimize.
///
/// TIKLAMA (sadece ana board): "tiklanabilir" acikken bu board'a bakip sol tik ->
/// SiparisYonetimi'ne siparisi gonderir. Optimize: input sadece board'da siparis
/// VARKEN kontrol edilir.
///
/// KURULUM (ana board):
/// - Board 3D obje + Collider, etkilesimLayer'da (RaycastSistemi gormeli).
/// - Bu script board'in (collider'in oldugu) objesinde olsun.
/// - World-space Canvas + child Image'lar slot olarak; bolgeA/bolgeB listelerine ata.
/// </summary>
public class SiparisGosterici : MonoBehaviour
{
    [Header("Balik Slotlari (sirayla dolar)")]
    [SerializeField] private Image[] balikSlotlari;

    [Header("Sebze Slotlari (sirayla dolar)")]
    [SerializeField] private Image[] sebzeSlotlari;

    [Header("Icecek Slotlari (sirayla dolar)")]
    [SerializeField] private Image[] icecekSlotlari;

    [Header("Yan Urun Slotlari (sirayla dolar)")]
    [SerializeField] private Image[] yanUrunSlotlari;

    [Header("Panel (opsiyonel - sadece ANA board)")]
    [Tooltip("Ikonlarin altinda durdugu parent Image/panel. Siparis gelince acilir (SetActive true), " +
             "alininca kapanir. Bos birakirsan hic dokunulmaz (monitorler/fisler icin bos birak).")]
    [SerializeField] private GameObject panelObjesi;

    [Header("Masa Numarasi (opsiyonel)")]
    [SerializeField] private TextMeshProUGUI masaNoText;
    [SerializeField] private string masaNoOnEki = "Masa ";

    [Header("Fiyat (opsiyonel - genelde sadece ANA board)")]
    [Tooltip("Siparis toplam fiyatinin yazilacagi text. Bos birakirsan fiyat gosterilmez.")]
    [SerializeField] private TextMeshProUGUI fiyatText;
    [Tooltip("Fiyatin onune eklenecek (orn. \"$\").")]
    [SerializeField] private string fiyatOnEki = "$";
    [Tooltip("Fiyatin sonuna eklenecek (orn. \" TL\"). Bos birakabilirsin.")]
    [SerializeField] private string fiyatSonEki = "";

    [Header("Tiklama (sadece ANA board acik olsun)")]
    [Tooltip("Acikken bu board'a bakip sol tiklayinca siparis mutfak ekranlarina gonderilir.")]
    [SerializeField] private bool tiklanabilir = false;
    [Tooltip("Bos birakirsan sahnede otomatik bulunur.")]
    [SerializeField] private RaycastSistemi raycastSistemi;

    private bool doluMu;

    void Awake()
    {
        if (tiklanabilir && raycastSistemi == null)
            raycastSistemi = FindObjectOfType<RaycastSistemi>();
        Temizle();
    }

    /// <summary>Siparisi board'a cizer.</summary>
    public void Goster(Siparis siparis)
    {
        if (siparis == null) { Temizle(); return; }

        // Panel'i ac (sadece atanmissa - ana board)
        if (panelObjesi != null) panelObjesi.SetActive(true);

        int balik = 0, sebze = 0, icecek = 0, yan = 0;
        for (int i = 0; i < siparis.malzemeler.Count; i++)
        {
            Malzeme m = siparis.malzemeler[i];
            if (m == null) continue;

            switch (m.Kategori)
            {
                case MalzemeKategorisi.Balik:   SlotAyarla(balikSlotlari, balik, m.Ikon); balik++; break;
                case MalzemeKategorisi.Sebze:   SlotAyarla(sebzeSlotlari, sebze, m.Ikon); sebze++; break;
                case MalzemeKategorisi.Icecek:  SlotAyarla(icecekSlotlari, icecek, m.Ikon); icecek++; break;
                case MalzemeKategorisi.YanUrun: SlotAyarla(yanUrunSlotlari, yan, m.Ikon); yan++; break;
            }
        }

        KalaniKapat(balikSlotlari, balik);
        KalaniKapat(sebzeSlotlari, sebze);
        KalaniKapat(icecekSlotlari, icecek);
        KalaniKapat(yanUrunSlotlari, yan);

        if (masaNoText != null)
            masaNoText.text = siparis.masaNo > 0 ? masaNoOnEki + siparis.masaNo : masaNoOnEki + "?";

        if (fiyatText != null)
            fiyatText.text = fiyatOnEki + siparis.ToplamFiyat + fiyatSonEki;

        doluMu = true;
    }

    /// <summary>Board'i bosaltir.</summary>
    public void Temizle()
    {
        KalaniKapat(balikSlotlari, 0);
        KalaniKapat(sebzeSlotlari, 0);
        KalaniKapat(icecekSlotlari, 0);
        KalaniKapat(yanUrunSlotlari, 0);
        if (masaNoText != null) masaNoText.text = "";
        if (fiyatText != null) fiyatText.text = "";

        // Panel'i kapat (sadece atanmissa - ana board)
        if (panelObjesi != null) panelObjesi.SetActive(false);

        doluMu = false;
    }

    void Update()
    {
        // Optimize: sadece tiklanabilir ana board'da ve siparis varken input kontrol et.
        if (!tiklanabilir || !doluMu || raycastSistemi == null) return;
        if (!Input.GetMouseButtonDown(0)) return;

        GameObject bakilan = raycastSistemi.BakilanObje;
        if (bakilan == null) return;
        if (bakilan != gameObject && !bakilan.transform.IsChildOf(transform)) return;

        if (SiparisYonetimi.Instance != null)
            SiparisYonetimi.Instance.AktifSiparisiGonder();
    }

    private void SlotAyarla(Image[] slotlar, int idx, Sprite ikon)
    {
        if (slotlar == null || idx < 0 || idx >= slotlar.Length) return;
        Image img = slotlar[idx];
        if (img == null) return;
        img.sprite = ikon;
        img.enabled = ikon != null;
    }

    private void KalaniKapat(Image[] slotlar, int baslangic)
    {
        if (slotlar == null) return;
        for (int i = baslangic; i < slotlar.Length; i++)
        {
            if (slotlar[i] == null) continue;
            slotlar[i].sprite = null;
            slotlar[i].enabled = false;
        }
    }
}
