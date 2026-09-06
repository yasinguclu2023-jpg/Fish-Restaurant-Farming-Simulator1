using UnityEngine;

/// <summary>
/// Tab (veya seçilen tuş) ile açılıp kapanan UI paneli yöneticisi.
///
/// UI açıkken oyun içi etkileşimler (sulama, nesne alma, hasat...) durur:
/// diğer sistemler UIYoneticisi.HerhangiBirUIAcikMi kontrolünü kullanır.
/// Sahnede birden fazla UI paneli olabileceği için sayaç mantığı ile çalışır.
/// </summary>
public class UIYoneticisi : MonoBehaviour
{
    [Header("UI Paneli")]
    [SerializeField] private GameObject uiPaneli;

    [Header("Tuş Ayarı")]
    [SerializeField] private KeyCode acKapaTusu = KeyCode.Tab;

    [Header("Referanslar (Opsiyonel)")]
    [SerializeField] private MonoBehaviour[] kapatilacakScriptler;

    private bool uiAcik = false;

    // Sahnedeki AÇIK panel sayısı. 0'dan büyükse oyun içi etkileşimler kilitlenir.
    private static int acikUISayisi = 0;

    /// <summary>Sahnede şu an açık bir UI paneli var mı? (Sulama/alma sistemleri buna bakar.)</summary>
    public static bool HerhangiBirUIAcikMi => acikUISayisi > 0;

    void Start()
    {
        if (uiPaneli != null)
            uiPaneli.SetActive(false);

        // Başlangıçta mouse kilitli
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(acKapaTusu))
        {
            UIDurumunuDegistir();
        }
    }

    void OnDestroy()
    {
        // Sahne değişiminde sayaç asılı kalmasın
        if (uiAcik)
        {
            uiAcik = false;
            acikUISayisi = Mathf.Max(0, acikUISayisi - 1);
        }
    }

    public void UIDurumunuDegistir()
    {
        uiAcik = !uiAcik;

        if (uiPaneli != null)
            uiPaneli.SetActive(uiAcik);

        if (uiAcik)
        {
            acikUISayisi++;

            // UI açık - mouse serbest
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Oyuncu kontrollerini kapat
            ScriptleriAyarla(false);
        }
        else
        {
            acikUISayisi = Mathf.Max(0, acikUISayisi - 1);

            // UI kapalı - mouse kilitli
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Oyuncu kontrollerini aç
            ScriptleriAyarla(true);
        }
    }

    void ScriptleriAyarla(bool aktif)
    {
        if (kapatilacakScriptler == null) return;

        foreach (var script in kapatilacakScriptler)
        {
            if (script != null)
                script.enabled = aktif;
        }
    }

    // Diğer scriptlerden kontrol için
    public bool UIAcikMi => uiAcik;
}
