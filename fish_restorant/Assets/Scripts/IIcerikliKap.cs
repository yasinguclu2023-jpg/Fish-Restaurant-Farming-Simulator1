using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ICERIK BARINDIRAN KAP arayuzu (kasa, kuvet, balik kasasi...).
///
/// Gun yeniden baslarken, DEPO DISINDA kalan kaplarin sadece ICI bosaltilir;
/// kabin kendisi asla silinmez.
///
/// ICopEtkilesimi ile ayni desen: her sistem kendi icerigini nasil sakladigini ve
/// nasil sildigini KENDISI bilir. Boylece kaplarda hicbir hiyerarsi/kurulum
/// degisikligi gerekmez (urunler eskisi gibi kabin altinda dizili kalir).
/// </summary>
public interface IIcerikliKap
{
    /// <summary>Kabin sadece ICINI bosaltir. Kap objesi silinmez.</summary>
    void IciniBosalt();

    /// <summary>Depo icinde mi diye bakilacak dunya konumu (genelde kabin kendi konumu).</summary>
    Vector3 KapKonumu { get; }
}

/// <summary>
/// Sahnedeki tum icerikli kaplarin kayit defteri.
///
/// Kaplar OnEnable'da kaydolur, OnDisable'da dusulur. Boylece gun sonunda
/// sahne taramasi (FindObjectsOfType) yapilmaz -> optimize.
/// (DepoAlani.Aktifler ile ayni desen.)
/// </summary>
public static class IcerikliKapDefteri
{
    public static readonly List<IIcerikliKap> Kaplar = new List<IIcerikliKap>();

    public static void Kaydet(IIcerikliKap kap)
    {
        if (kap == null) return;
        if (!Kaplar.Contains(kap)) Kaplar.Add(kap);
    }

    public static void Sil(IIcerikliKap kap)
    {
        if (kap == null) return;
        Kaplar.Remove(kap);
    }
}

/// <summary>
/// Gun sonu temizliginin KURALLARI burada:
///
///   1. KAPLAR ASLA SILINMEZ. Sadece icleri bosalir.
///      Bir kabin icindeki parca da bir KAP olabilir (orn. rafta duran kuvetler);
///      o parca da silinmez, yerinde kalir, sadece kendi icerigi bosaltilir.
///
///   2. BITKILER ASLA SILINMEZ. Bahcede ekili bitkiler (BitkiBuyumeSistemi) gunler
///      arasi yasamaya devam eder; tag'i ne olursa olsun temizlige takilmaz.
///      (Hasat edilmis urun ayri bir prefabtir; o normal urun sayilir.)
///
/// Bunlarin disindaki sıradan urunler (balik, kesilmis domates...) yok edilir.
/// </summary>
public static class KapYardimcisi
{
    /// <summary>Bu obje (ya da altindaki bir obje) bir KAP mi?</summary>
    public static bool KapMi(GameObject o)
    {
        if (o == null) return false;
        return o.GetComponentInChildren<IIcerikliKap>(true) != null;
    }

    /// <summary>Bu obje (ya da altindaki bir obje) ekili bir BITKI mi?</summary>
    public static bool BitkiMi(GameObject o)
    {
        if (o == null) return false;
        return o.GetComponentInChildren<BitkiBuyumeSistemi>(true) != null;
    }

    /// <summary>Gun sonu temizliginde ASLA silinmemesi gereken obje mi? (kap ya da bitki)</summary>
    public static bool KorunmaliMi(GameObject o)
    {
        return KapMi(o) || BitkiMi(o);
    }

    /// <summary>
    /// Bir kabin icindeki TEK parcayi gun sonu kuralina gore isler.
    ///
    /// - Parca bir KAP ise  : SILINMEZ, sadece ici bosaltilir -> true doner
    ///                        (cagiran taraf onu slotunda/yerinde birakmalidir).
    /// - Parca sıradan urun : yok edilir -> false doner.
    /// </summary>
    public static bool ParcayiIsle(GameObject parca)
    {
        if (parca == null) return false;

        // BITKILER: asla silinmez, icleri de bosaltilmaz. Oldugu gibi kalir.
        if (BitkiMi(parca)) return true;

        IIcerikliKap[] icKaplar = parca.GetComponentsInChildren<IIcerikliKap>(true);

        if (icKaplar != null && icKaplar.Length > 0)
        {
            // Parca bir kap: kendisi korunur, sadece icerigi bosalir.
            for (int i = 0; i < icKaplar.Length; i++)
                if (icKaplar[i] != null) icKaplar[i].IciniBosalt();

            return true;
        }

        Object.Destroy(parca);
        return false;
    }
}
