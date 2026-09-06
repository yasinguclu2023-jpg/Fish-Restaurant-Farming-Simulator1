using UnityEngine;

/// <summary>
/// Para birakma yoneticisi (Singleton).
/// Musteri yiyip kalkinca cagrilir: o masanin para noktasinda bir PARA prefabi olusturur
/// ve fiyatini atar. Oyuncu paraya tiklayinca (ParaObjesi) fiyat hesaba eklenir.
///
/// KURULUM:
/// 1. Sahnede bos bir GameObject -> bu scripti ekle.
/// 2. "Para Prefab" alanina ParaObjesi iceren para prefabini surukle.
/// 3. Masa noktalari MasaYonetimi'nde her masanin "Para Noktasi" alanindan gelir.
/// </summary>
public class ParaYoneticisi : MonoBehaviour
{
    public static ParaYoneticisi Instance { get; private set; }

    [Header("Referans")]
    [Tooltip("Masada belirecek para prefabi (ParaObjesi + Collider icermeli).")]
    [SerializeField] private ParaObjesi paraPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Belirtilen masanin noktasinda, verilen fiyatli bir para birakir.
    /// Nokta veya prefab yoksa parayi kaybetmemek icin direkt hesaba ekler.
    /// </summary>
    public void ParaBirak(int masaNo, int fiyat)
    {
        if (fiyat <= 0) return;

        Transform nokta = MasaYonetimi.Instance != null
            ? MasaYonetimi.Instance.ParaNoktasiAl(masaNo)
            : null;

        // Prefab ya da nokta yoksa: para kaybolmasin, direkt kazan.
        if (paraPrefab == null || nokta == null)
        {
            if (paraPrefab == null)
                Debug.LogWarning("[ParaYoneticisi] paraPrefab atanmamis; para direkt hesaba eklendi.", this);
            else
                Debug.LogWarning($"[ParaYoneticisi] Masa {masaNo} icin para noktasi yok; para direkt hesaba eklendi.", this);

            if (EkonomiYoneticisi.Instance != null) EkonomiYoneticisi.Instance.Kazan(fiyat);
            return;
        }

        ParaObjesi para = Instantiate(paraPrefab, nokta.position, nokta.rotation);
        para.FiyatAyarla(fiyat);
    }
}
