using System;
using UnityEngine;

/// <summary>
/// Oyun ekonomisinin merkezi (Singleton). Paranin TEK sahibi.
///
/// - Kazanma (dogru servis) buradan gecer: Kazan().
/// - Harcama (magaza / satin alma) ILERIDE buradan gececek: Harca() / YeterliMi().
///   (Su an Harca hicbir yerde cagrilmiyor; ekonomi/magaza sonra eklenince kullanilacak.)
/// - UI, ParaDegisti event'ine abone olup otomatik guncellenir (gevsek bagli, optimize).
///
/// KURULUM:
/// 1. Sahnede bos bir GameObject olustur -> bu scripti ekle.
/// 2. "baslangicParasi" alanina oyunun basindaki parayi gir.
/// </summary>
public class EkonomiYoneticisi : MonoBehaviour
{
    public static EkonomiYoneticisi Instance { get; private set; }

    [Header("Baslangic")]
    [Tooltip("Oyun basinda oyuncunun sahip oldugu para.")]
    [SerializeField] private int baslangicParasi = 100;

    /// <summary>Mevcut para (disaridan salt-okunur).</summary>
    public int Para { get; private set; }

    /// <summary>Para her degistiginde yeni miktarla tetiklenir. UI buna abone olur.</summary>
    public event Action<int> ParaDegisti;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Para = baslangicParasi;
        }
        else
        {
            Debug.LogWarning("[EkonomiYoneticisi] Sahnede birden fazla EkonomiYoneticisi var. Fazlasi yok sayildi.", this);
            Destroy(this);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Para ekler (dogru servis vb.). 0/negatif miktar yok sayilir.</summary>
    public void Kazan(int miktar)
    {
        if (miktar <= 0) return;
        Para += miktar;
        ParaDegisti?.Invoke(Para);
    }

    /// <summary>
    /// Yeterli para varsa harcar ve true doner; yoksa dokunmaz, false doner.
    /// (ILERIDE magaza/satin alma tarafindan kullanilacak.)
    /// </summary>
    public bool Harca(int miktar)
    {
        if (miktar <= 0) return true;
        if (Para < miktar) return false;
        Para -= miktar;
        ParaDegisti?.Invoke(Para);
        return true;
    }

    /// <summary>Belirtilen miktar icin yeterli para var mi?</summary>
    public bool YeterliMi(int miktar) => Para >= miktar;
}
