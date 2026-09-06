using UnityEngine;
using TMPro;

/// <summary>
/// Para miktarini bir TMP text'te gosterir. EkonomiYoneticisi.ParaDegisti event'ine
/// abone olur; para her degistiginde text otomatik guncellenir (Update kullanmaz -> optimize).
///
/// KURULUM:
/// 1. Para yazan TMP text objesine (veya bos bir UI objesine) bu scripti ekle.
/// 2. "paraText" alanina para gosterecek TextMeshProUGUI'yi ata.
/// 3. Onek/sonek opsiyonel (varsayilan basta "$").
/// </summary>
public class ParaUI : MonoBehaviour
{
    [Header("Referans")]
    [Tooltip("Para miktarinin yazilacagi text.")]
    [SerializeField] private TextMeshProUGUI paraText;

    [Header("Format")]
    [Tooltip("Rakamin onune eklenecek (orn. \"$\").")]
    [SerializeField] private string onEk = "$";
    [Tooltip("Rakamin sonuna eklenecek (orn. \" TL\"). Bos birakabilirsin.")]
    [SerializeField] private string sonEk = "";

    void Start()
    {
        // Awake'ler Start'lardan once bittigi icin Instance burada hazirdir.
        if (EkonomiYoneticisi.Instance != null)
        {
            EkonomiYoneticisi.Instance.ParaDegisti += Guncelle;
            Guncelle(EkonomiYoneticisi.Instance.Para); // baslangic degerini hemen yaz
        }
        else
        {
            Debug.LogWarning("[ParaUI] Sahnede EkonomiYoneticisi yok; para gosterilemez.", this);
        }
    }

    void OnDestroy()
    {
        if (EkonomiYoneticisi.Instance != null)
            EkonomiYoneticisi.Instance.ParaDegisti -= Guncelle;
    }

    private void Guncelle(int para)
    {
        if (paraText != null) paraText.text = onEk + para + sonEk;
    }
}
