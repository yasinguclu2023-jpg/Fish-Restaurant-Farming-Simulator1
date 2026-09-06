using System.Collections.Generic;
using UnityEngine;

// ===== DEMO GUN SISTEMI (kaldirilabilir) =====
// Bu dosyanin TAMAMI demoya aittir. Demo bitince "Gun" klasorunu silmen yeterli.

/// <summary>
/// Bir malzemenin o gunku orani (0-100 slider).
///
/// DIKKAT - kategoriye gore ANLAMI DEGISIR:
///   SEBZE              -> AGIRLIK (goreli siklik). Once minSebze..maxSebze arasi KAC sebze
///                         olacagi secilir, sonra agirliga gore HANGILERI secilir.
///   ICECEK / YAN URUN  -> GERCEK YUZDE. 50 yazarsan siparislerin %50'sinde cikar.
///
/// Her iki durumda da 0 = o malzeme o gun HIC cikmaz.
/// </summary>
[System.Serializable]
public class MalzemeOrani
{
    [Tooltip("Hangi malzeme? (Malzeme asset'ini surukle)")]
    public Malzeme malzeme;

    [Tooltip("Sebzede AGIRLIK (goreli siklik), icecek/yan urunde GERCEK YUZDE. 0 = o gun hic cikmaz.")]
    [Range(0, 100)] public int oran = 60;
}

/// <summary>
/// TEK BIR GUNUN ayarlari. Listede kacinci sirada ise o gundur (0. eleman = 1. gun).
/// </summary>
[System.Serializable]
public class GunAyari
{
    [Tooltip("Sadece Inspector'da okumak icin. Oyunu etkilemez.")]
    public string gunAdi = "Gun";

    [Tooltip("Kendi notun (o gun ne aciliyor vb.). Oyunu etkilemez.")]
    [TextArea(1, 3)] public string notlar;

    [Header("Sebze Adedi (siparis basina)")]
    [Tooltip("En az kac sebze. 0 = bazi siparisler SADE BALIK olur.")]
    [Min(0)] public int minSebze = 0;

    [Tooltip("En fazla kac sebze. Ornek: min 0 / max 1 -> %50 sade balik, %50 tek sebze.")]
    [Min(0)] public int maxSebze = 2;

    [Header("Musteri")]
    [Tooltip("Kac saniyede bir musteri gelsin. 0 = bu gune DOKUNMA (MusteriUretici'nin kendi degeri kalir).")]
    [Min(0f)] public float spawnAraligi = 0f;

    [Header("Siparis Oranlari (0-100)")]
    [Tooltip("SEBZELER - AGIRLIK: buyuk olan daha sik secilir. Listede OLMAYAN sebze o gun hic cikmaz.")]
    public List<MalzemeOrani> sebzeAgirliklari = new List<MalzemeOrani>();

    [Tooltip("ICECEKLER - GERCEK YUZDE. En fazla 1 icecek eklenir (bira+sarap ayni anda cikmaz).")]
    public List<MalzemeOrani> icecekOranlari = new List<MalzemeOrani>();

    [Tooltip("YAN URUNLER - GERCEK YUZDE. Her biri bagimsiz, birden fazla cikabilir.")]
    public List<MalzemeOrani> yanUrunOranlari = new List<MalzemeOrani>();
}

/// <summary>
/// DEMO GUN PLANI (ScriptableObject).
///
/// Demonun gun gun siparis/musteri dengesi buradan ayarlanir. Gun sayisi = liste uzunlugu,
/// yani 5'i 7 yapmak icin sadece eleman eklersin, kod degismez.
///
/// BURADA OLMAYAN SEY: hangi gun ne ACILACAGI. O bilgi magazada durur ->
/// UIYerlestirmeSistemi > Kategoriler > Nesneler > "Acilis Gunu" alani.
///
/// KURULUM:
/// 1. Project penceresinde sag tik -> Create -> Restoran -> Gun Plani
/// 2. "Gunler" listesine 5 eleman ekle, her gunu doldur.
/// 3. Sahnedeki DemoGunSistemi objesinin "Gun Plani" alanina bu asset'i surukle.
/// </summary>
[CreateAssetMenu(fileName = "GunPlani", menuName = "Restoran/Gun Plani")]
public class GunPlani : ScriptableObject
{
    [Tooltip("Her eleman bir gundur. 1. eleman = 1. gun.")]
    [SerializeField] private List<GunAyari> gunler = new List<GunAyari>();

    /// <summary>Plandaki gun sayisi.</summary>
    public int GunSayisi => gunler != null ? gunler.Count : 0;

    /// <summary>
    /// 1'den baslayan gun numarasinin ayarini dondurur.
    /// Plan disi gunlerde (orn. plan 5 gun ama 7. gundeyiz) SON gunun ayari kullanilir.
    /// Plan bossa null doner -> sistem o gun hicbir sey uygulamaz.
    /// </summary>
    public GunAyari GunAl(int gun)
    {
        if (gunler == null || gunler.Count == 0) return null;

        int index = Mathf.Clamp(gun - 1, 0, gunler.Count - 1);
        return gunler[index];
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (gunler == null) return;

        for (int i = 0; i < gunler.Count; i++)
        {
            GunAyari g = gunler[i];
            if (g == null) continue;

            // max, min'in altina dusemez
            if (g.maxSebze < g.minSebze) g.maxSebze = g.minSebze;

            // Bos isim birakildiysa okunakli bir ad ver
            if (string.IsNullOrWhiteSpace(g.gunAdi) || g.gunAdi == "Gun")
                g.gunAdi = "Gun " + (i + 1);
        }
    }
#endif
}
// ===== DEMO GUN SISTEMI SONU =====
