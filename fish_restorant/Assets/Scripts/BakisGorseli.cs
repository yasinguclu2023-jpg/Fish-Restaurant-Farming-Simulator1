using UnityEngine;

/// <summary>
/// Bu nesneye bakılınca açılacak UI objesini belirler.
///
/// NE ZAMAN KULLANILIR?
/// Prefablar ve runtime'da doğan nesneler için (makineler, taşınan kasalar, bitkiler...).
/// Sahnede sabit duran nesneleri tek yerden yönetmek istersen bunun yerine
/// <see cref="BakisGorseliYoneticisi"/> üzerindeki "Eşleşmeler" listesini kullan.
///
/// Bu scriptin UPDATE'İ YOKTUR; sadece kendini kayıt defterine yazar,
/// gösterimi merkezî yönetici yapar.
///
/// ÖNEMLİ - PREFAB ÜSTÜNDEYSE:
/// Unity prefablara sahne referansı verdirmez. Bu yüzden prefab üstünde "Gösterilecek UI"
/// alanına Project'teki bir UI PREFABI sürüklemelisin; yönetici onu Canvas altına bir kez
/// kopyalar ve o kopyayı açıp kapatır. Sahnedeki hazır Image'ı ancak sahne nesnelerinde
/// (prefab olmayan) kullanabilirsin.
///
/// KURULUM:
/// 1. Sahnede bir BakisGorseliYoneticisi olduğundan emin ol.
/// 2. Bu scripti nesnenin köküne ekle (collider'ı etkileşim layer'ında olmalı).
/// 3. "Gösterilecek UI" alanına Image/panel objesini (veya UI prefabını) sürükle. Bitti.
/// </summary>
[DisallowMultipleComponent]
public class BakisGorseli : MonoBehaviour
{
    [Header("Görsel")]
    [Tooltip("Bu nesneye bakınca açılacak UI objesi.\n" +
             "• Sahne nesnesindeysen: Canvas altındaki hazır Image/panel'i sürükle.\n" +
             "• Prefab üstündeysen: Project'teki bir UI prefabını sürükle.")]
    [SerializeField] private GameObject gosterilecekUI;

    [Header("Sahip")]
    [Tooltip("Görselin ait olduğu kök nesne. Boş bırakılırsa bu obje kullanılır. " +
             "Bu nesneye VEYA child'larına bakmak görseli açar.")]
    [SerializeField] private Transform sahip;

    private Transform _kayitliSahip;

    /// <summary>Şu an atanmış UI.</summary>
    public GameObject GosterilecekUI => gosterilecekUI;

    void OnEnable()
    {
        _kayitliSahip = sahip != null ? sahip : transform;

        if (gosterilecekUI == null)
        {
            Debug.LogWarning($"[BakisGorseli] '{name}' üzerinde 'Gösterilecek UI' atanmamış.", this);
            return;
        }

        BakisGorseliYoneticisi.Kaydet(_kayitliSahip, gosterilecekUI);
    }

    void OnDisable()
    {
        if (_kayitliSahip != null)
            BakisGorseliYoneticisi.Sil(_kayitliSahip);

        _kayitliSahip = null;
    }

    /// <summary>
    /// Gösterilecek UI'ı oyun sırasında değiştirir (örn. makine dolu/boş durumuna göre farklı panel).
    /// null verilirse bu nesnenin görseli kapanır.
    /// </summary>
    public void UIDegistir(GameObject yeniUI)
    {
        gosterilecekUI = yeniUI;

        if (!isActiveAndEnabled) return;

        if (_kayitliSahip == null)
            _kayitliSahip = sahip != null ? sahip : transform;

        if (gosterilecekUI == null)
            BakisGorseliYoneticisi.Sil(_kayitliSahip);
        else
            BakisGorseliYoneticisi.Kaydet(_kayitliSahip, gosterilecekUI);
    }
}
