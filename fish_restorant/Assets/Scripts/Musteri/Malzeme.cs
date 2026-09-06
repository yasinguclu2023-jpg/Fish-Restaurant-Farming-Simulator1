using UnityEngine;

/// <summary>
/// Siparis malzemesi kategorileri.
/// Board'da her kategori kendi slotlarinda gosterilir.
/// </summary>
public enum MalzemeKategorisi
{
    Balik,
    Sebze,
    Icecek,
    YanUrun,
    Ekmek
}

/// <summary>
/// Tek bir malzeme tanimi (ScriptableObject).
/// Project > sag tik > Restoran > Malzeme ile her urun icin bir asset olustur.
/// Inspector'da kategoriye gore (balik / sebze / icecek / yan urun) doldur.
/// </summary>
[CreateAssetMenu(fileName = "YeniMalzeme", menuName = "Restoran/Malzeme")]
public class Malzeme : ScriptableObject
{
    [Header("Tanim")]
    [SerializeField] private string malzemeAdi = "Malzeme";
    [SerializeField] private MalzemeKategorisi kategori = MalzemeKategorisi.Balik;

    [Header("Fiyat")]
    [Tooltip("Bu malzemenin fiyati. Siparis toplami tum malzemelerin fiyatlarinin toplamidir.")]
    [SerializeField] private int fiyat = 0;

    [Header("Gorsel")]
    [Tooltip("Board'da gosterilecek ikon (Sprite).")]
    [SerializeField] private Sprite ikon;

    [Header("Mutfak Eslesmesi (opsiyonel, ileride)")]
    [Tooltip("Bu malzemenin mutfaktaki urun tag/id'si. Servis eslesmesinde kullanilacak.")]
    [SerializeField] private string urunTag;

    public string MalzemeAdi => malzemeAdi;
    public MalzemeKategorisi Kategori => kategori;
    public int Fiyat => fiyat;
    public Sprite Ikon => ikon;
    public string UrunTag => urunTag;
}
