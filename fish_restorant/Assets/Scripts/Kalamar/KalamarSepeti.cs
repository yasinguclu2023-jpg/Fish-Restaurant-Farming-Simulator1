using UnityEngine;

/// <summary>
/// Kalamar sepetine eklenir (oyuncunun elinde tuttuðu sepet).
/// Ýçindeki kalamar sayýsýný yönetir.
///
/// Kurulum:
/// 1. Sepet prefab'ýna / objesine bu scripti ekle
/// 2. 'kalamarSayisi'ný inspector'dan ayarla
/// 3. KalamarPisirmeEtkilesim, bu sepet elindeyken kalamar tüketir
/// </summary>
public class KalamarSepeti : MonoBehaviour
{
    [Header("Sepet Ýçeriði")]
    [Tooltip("Sepette baþlangýçta kaç kalamar var")]
    [SerializeField] private int kalamarSayisi = 10;

    [Tooltip("Sýnýrsýz kalamar (debug/test için)")]
    [SerializeField] private bool sinirsiz = false;

    public int KalamarSayisi => kalamarSayisi;
    public bool Bos => !sinirsiz && kalamarSayisi <= 0;
    public bool KalamarVarMi => sinirsiz || kalamarSayisi > 0;

    /// <summary>
    /// Sepetten bir kalamar al (sayacý 1 azalt).
    /// </summary>
    public bool KalamarTuket()
    {
        if (Bos) return false;
        if (!sinirsiz) kalamarSayisi--;
        return true;
    }

    /// <summary>
    /// Sepete kalamar ekle (örn: doldurma istasyonunda).
    /// </summary>
    public void KalamarEkle(int miktar)
    {
        if (miktar > 0) kalamarSayisi += miktar;
    }
}