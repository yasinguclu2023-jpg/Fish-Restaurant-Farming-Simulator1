using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// KAYIT YONETICISI (Singleton).
///
/// Gun yeniden baslarken (gun sonu objesine basilinca) DEPO DISINDA kalanlari temizler:
///   1. Kaplarin (kasa / kuvet / balik kasasi / GunSonuIcerik) sadece ICI bosalir -> KAPLAR SILINMEZ.
///   2. Yerde serbest duran urunler (kaba girmemis balik, domates...) silinir.
/// Depo (DepoAlani kupu) ICINDE olanlara hic dokunulmaz.
///
/// ILERIDE: gercek diske kayit (para, gun, depodaki esyalar -> dosya) burada yapilacak.
///
/// KURULUM:
/// 1. Sahnede bos bir GameObject olustur -> bu scripti ekle.
/// 2. "gunYoneticisi" bos birakilabilir (otomatik bulunur).
/// 3. "serbestUrunTagleri" listesine, yerde tek basina kalinca silinmesini istedigin
///    urunlerin tag'lerini yaz. Bos birakirsan sadece kaplarin ici bosalir.
/// 4. Sorun ararken "teshisLogu" acik birak -> Console'a ne oldugunu yazar.
/// </summary>
public class KayitYoneticisi : MonoBehaviour
{
    public static KayitYoneticisi Instance { get; private set; }

    [Header("Referanslar")]
    [Tooltip("Bos birakirsan sahnede otomatik bulunur.")]
    [SerializeField] private GunYoneticisi gunYoneticisi;

    [Header("Serbest Urunler")]
    [Tooltip("Yerde tek basina (kaba girmemis) kalinca yeni gunde silinecek urun tag'leri. " +
             "Depo icindekiler silinmez. Bos birakirsan bu tarama yapilmaz.")]
    [SerializeField] private string[] serbestUrunTagleri;

    [Header("Teshis")]
    [Tooltip("Gun gecisinde Console'a detayli rapor yazar (hangi kap bulundu, depo icinde mi, ne yapildi).")]
    [SerializeField] private bool teshisLogu = true;

    // Defterin kopyasi (GC'siz): temizlik sirasinda defter degisebildigi icin kopya uzerinde gezilir.
    private readonly List<IIcerikliKap> gecici = new List<IIcerikliKap>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }

        if (gunYoneticisi == null) gunYoneticisi = FindObjectOfType<GunYoneticisi>();

        if (gunYoneticisi != null)
            gunYoneticisi.GunYenidenBaslatildi += GunGecisiTemizligi;
        else
            Debug.LogWarning("[KayitYoneticisi] GunYoneticisi bulunamadi; gun gecisi temizligi calismaz.", this);
    }

    void OnDestroy()
    {
        if (gunYoneticisi != null)
            gunYoneticisi.GunYenidenBaslatildi -= GunGecisiTemizligi;

        if (Instance == this) Instance = null;
    }

    /// <summary>Gun yeniden baslarken cagrilir: depo disinda kalanlari temizler.</summary>
    void GunGecisiTemizligi()
    {
        if (teshisLogu)
        {
            Debug.Log($"[KayitYoneticisi] ===== GUN GECISI TEMIZLIGI =====\n" +
                      $"Kayitli kap sayisi : {IcerikliKapDefteri.Kaplar.Count}\n" +
                      $"Depo alani sayisi  : {DepoAlani.Aktifler.Count}", this);

            if (IcerikliKapDefteri.Kaplar.Count == 0)
                Debug.LogWarning("[KayitYoneticisi] Hic kap kayitli degil! Kasa/kuvet objelerinde " +
                                 "ilgili script var mi ve AKTIF mi kontrol et.", this);

            if (DepoAlani.Aktifler.Count == 0)
                Debug.LogWarning("[KayitYoneticisi] Hic DepoAlani yok! Her sey 'depo disi' sayilir " +
                                 "ve tum kaplar bosaltilir.", this);

            // Depo kutularinin gercek dunya olcusu: esyalarin bu kutunun icinde kalmasi gerekir.
            for (int d = 0; d < DepoAlani.Aktifler.Count; d++)
            {
                DepoAlani depo = DepoAlani.Aktifler[d];
                if (depo == null) continue;

                Debug.Log($"[KayitYoneticisi] Depo '{depo.gameObject.name}'\n" +
                          $"  dunya merkez: {depo.DunyaMerkez}\n" +
                          $"  dunya boyut : {depo.DunyaBoyut}", depo);
            }
        }

        KaplariBosalt();
        SerbestUrunleriTemizle();

        // ILERIDE: gercek diske kayit (para, gun, depodaki esyalar) burada yapilacak.
        // TODO: Diske kaydet.
    }

    /// <summary>Depo DISINDAKI kaplarin icini bosaltir. Kaplarin kendisi silinmez.</summary>
    void KaplariBosalt()
    {
        var kaplar = IcerikliKapDefteri.Kaplar;

        // 1) Yok edilmis (olu) kayitlari defterden dus.
        for (int i = kaplar.Count - 1; i >= 0; i--)
        {
            // Arayuz referansinda Unity'nin "yok edilmis obje" kontrolu icin MonoBehaviour'a bak.
            MonoBehaviour olu = kaplar[i] as MonoBehaviour;
            if (kaplar[i] == null || olu == null) kaplar.RemoveAt(i);
        }

        // 2) ONEMLI: IciniBosalt() sirasinda defter DEGISEBILIR (bir obje kapanabilir/yok olabilir
        //    -> OnDisable defterden duser). Bu yuzden defter uzerinde degil, KOPYASI uzerinde gez.
        gecici.Clear();
        gecici.AddRange(kaplar);

        for (int i = 0; i < gecici.Count; i++)
        {
            IIcerikliKap kap = gecici[i];
            MonoBehaviour mb = kap as MonoBehaviour;
            if (kap == null || mb == null) continue; // arada yok edilmis olabilir

            // Once pivot noktasi, olmazsa objenin GOVDE merkezi kontrol edilir (bagislayici).
            bool depoIcinde = DepoAlani.NoktaBirDepodaMi(kap.KapKonumu)
                           || DepoAlani.NesneBirDepodaMi(mb.gameObject);

            if (teshisLogu)
            {
                string govde = DepoAlani.GovdeMerkezi(mb.gameObject, out Vector3 gm)
                    ? gm.ToString() : "(govde bulunamadi)";

                Debug.Log($"[KayitYoneticisi] Kap: '{mb.gameObject.name}' ({mb.GetType().Name})\n" +
                          $"  pivot konum : {kap.KapKonumu}\n" +
                          $"  govde merkez: {govde}\n" +
                          $"  icerik adedi: {IcerikAdedi(mb)}\n" +
                          $"  depo icinde : {depoIcinde}\n" +
                          $"  sonuc       : {(depoIcinde ? "KORUNDU" : "ICI BOSALTILDI")}", mb);
            }

            if (!depoIcinde)
                kap.IciniBosalt();
        }

        gecici.Clear();
    }

    /// <summary>Teshis icin: kabin su an kac urun tuttugu (bilinmiyorsa -1).</summary>
    int IcerikAdedi(MonoBehaviour mb)
    {
        if (mb is KasaSistemi kasa) return kasa.MevcutUrunSayisi;
        if (mb is BalikKasaSistemi balikKasa) return balikKasa.MevcutUrunSayisi;
        if (mb is KuvetSistemi kuvet) return kuvet.AktifModelSayisi;
        return -1;
    }

    /// <summary>
    /// Yerde serbest duran (kaba girmemis) urunlerden depo DISINDA kalanlari siler.
    /// Depo icindeki kaplarin icindeki urunler konum olarak da depo icinde oldugu icin korunur.
    /// </summary>
    void SerbestUrunleriTemizle()
    {
        if (serbestUrunTagleri == null || serbestUrunTagleri.Length == 0) return;

        int toplamSilinen = 0;

        for (int t = 0; t < serbestUrunTagleri.Length; t++)
        {
            string tag = serbestUrunTagleri[t];
            if (string.IsNullOrEmpty(tag)) continue;

            GameObject[] bulunanlar;
            try
            {
                bulunanlar = GameObject.FindGameObjectsWithTag(tag);
            }
            catch (UnityException)
            {
                Debug.LogWarning($"[KayitYoneticisi] '{tag}' tag'i Unity'de tanimli degil, atlandi.", this);
                continue;
            }

            for (int i = 0; i < bulunanlar.Length; i++)
            {
                GameObject o = bulunanlar[i];
                if (o == null) continue;

                // KAPLAR ve BITKILER ASLA SILINMEZ (tag'i listede olsa bile).
                if (KapYardimcisi.KorunmaliMi(o)) continue;

                // Depo icindekiler korunur (pivot ya da govde merkezi kutunun icindeyse).
                if (DepoAlani.NesneBirDepodaMi(o)) continue;

                Destroy(o);
                toplamSilinen++;
            }
        }

        if (teshisLogu && toplamSilinen > 0)
            Debug.Log($"[KayitYoneticisi] Serbest urun temizligi: {toplamSilinen} obje silindi.", this);
    }
}
