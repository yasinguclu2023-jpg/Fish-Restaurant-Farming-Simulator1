using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GENEL AMACLI GUN SONU ICERIK TEMIZLEYICI.
///
/// Yeni bir kap (dolap, raf, kova, tezgah...) ekledigin de KOD YAZMANA GEREK YOK:
/// sadece bu scripti o objenin uzerine surukle, Inspector'dan ayarla.
///
/// Gun yeniden baslarken (gun sonu objesine basilinca):
///   - Obje DEPO ICINDEYSE  -> hicbir sey olmaz.
///   - Obje DEPO DISINDAYSA -> "icerikKok" altindaki child'lar silinir.
/// Kabin kendisi ASLA silinmez.
///
/// KURULUM:
/// 1. Scripti kabin uzerine surukle.
/// 2. "Icerik Kok" -> icindekilerin durdugu transform. BOS BIRAKIRSAN bu objenin
///    kendi child'lari silinir (cogu durumda dogrusu budur).
/// 3. Kabin kendi parcalari (mesh, kapak, slot noktalari) da child ise, onlari
///    "Korunacaklar" listesine ekle -> asla silinmezler.
/// 4. Istersen "Sadece Su Tagler" ile sadece belirli urunleri sildir.
///
/// NOT: KasaSistemi / KuvetSistemi / BalikKasaSistemi zaten kendi temizligini yapar;
/// o objelerde bu scripte gerek yoktur (ikisi birden olursa iki kez temizlenir, zararsiz).
/// </summary>
public class GunSonuIcerik : MonoBehaviour, IIcerikliKap
{
    [Header("Ne Silinecek?")]
    [Tooltip("Altindaki child'lar silinecek kok. BOS BIRAKIRSAN bu objenin child'lari silinir.")]
    [SerializeField] private Transform icerikKok;

    [Tooltip("Sadece bu tag'lere sahip objeler silinsin. BOS BIRAKIRSAN tum child'lar silinir.")]
    [SerializeField] private string[] sadeceSuTagler;

    [Tooltip("Bu objeler (ve altindakiler) ASLA silinmez. Kabin kendi parcalari icin kullan.")]
    [SerializeField] private Transform[] korunacaklar;

    [Header("Tarama Derinligi")]
    [Tooltip("True: sadece DOGRUDAN child'lar taranir (varsayilan). " +
             "False: tum alt hiyerarsi taranir - tag filtresiyle birlikte kullanmak icin.")]
    [SerializeField] private bool sadeceDogrudanChildlar = true;

    [Header("Teshis")]
    [Tooltip("Temizlik yapilinca Console'a bilgi yazar.")]
    [SerializeField] private bool teshisLogu = false;

    // GC'siz yardimcilar
    private readonly List<GameObject> silinecekler = new List<GameObject>();
    private readonly List<Transform> gecici = new List<Transform>();

    void OnEnable()
    {
        IcerikliKapDefteri.Kaydet(this);
    }

    void OnDisable()
    {
        IcerikliKapDefteri.Sil(this);
    }

    /// <summary>Depo icinde mi kontrolu icin kullanilan dunya konumu.</summary>
    public Vector3 KapKonumu => icerikKok != null ? icerikKok.position : transform.position;

    /// <summary>Gun sonu temizligi: icerigi siler, kabin kendisi kalir.</summary>
    [ContextMenu("Test: Icini Bosalt")]
    public void IciniBosalt()
    {
        Transform kok = icerikKok != null ? icerikKok : transform;

        silinecekler.Clear();

        if (sadeceDogrudanChildlar)
        {
            for (int i = 0; i < kok.childCount; i++)
                Degerlendir(kok.GetChild(i), kok);
        }
        else
        {
            gecici.Clear();
            kok.GetComponentsInChildren(true, gecici); // kok'un kendisi de dahil
            for (int i = 0; i < gecici.Count; i++)
                Degerlendir(gecici[i], kok);
        }

        // Kaplar ASLA silinmez: onlar yerinde kalir, sadece icleri bosalir.
        int silinen = 0;
        for (int i = 0; i < silinecekler.Count; i++)
        {
            if (silinecekler[i] == null) continue;
            if (!KapYardimcisi.ParcayiIsle(silinecekler[i])) silinen++;
        }

        if (teshisLogu)
            Debug.Log($"[GunSonuIcerik] '{gameObject.name}': {silinen} obje silindi, " +
                      $"{silinecekler.Count - silinen} kap korundu.", this);
    }

    // Bu transform silinmeli mi? Uygunsa silinecekler listesine ekler.
    private void Degerlendir(Transform t, Transform kok)
    {
        if (t == null || t == kok) return;
        if (Korunuyor(t)) return;
        if (!TagUygun(t.gameObject)) return;

        silinecekler.Add(t.gameObject);
    }

    // Korunacaklar listesindeki bir obje ya da onun altinda mi?
    private bool Korunuyor(Transform t)
    {
        if (korunacaklar == null) return false;

        for (int i = 0; i < korunacaklar.Length; i++)
        {
            Transform k = korunacaklar[i];
            if (k == null) continue;
            if (t == k || t.IsChildOf(k)) return true;
        }
        return false;
    }

    // Tag filtresi bos ise her sey uygun; degilse tag eslesmeli.
    private bool TagUygun(GameObject o)
    {
        if (sadeceSuTagler == null || sadeceSuTagler.Length == 0) return true;

        for (int i = 0; i < sadeceSuTagler.Length; i++)
        {
            string tag = sadeceSuTagler[i];
            if (string.IsNullOrEmpty(tag)) continue;

            if (o.CompareTag(tag)) return true;
        }
        return false;
    }
}
