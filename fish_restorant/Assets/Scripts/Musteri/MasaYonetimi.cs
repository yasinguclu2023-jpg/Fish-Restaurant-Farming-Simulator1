using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Masa ve oturma yeri yoneticisi (Singleton).
///
/// - Her masanin kendi numarasi + birden fazla oturma noktasi (empty) vardir.
/// - DOLULUK MASA BAZINDA: bir masada sadece BIR kisi oturur. Masadaki 2 noktadan
///   biri rastgele secilir, ama o masa artik komple dolu sayilir.
/// - Masa secimi RASTGELE yapilir (sirayla degil).
///
/// KURULUM:
/// 1. Sahnede bos GameObject -> bu script.
/// 2. "masalar" listesine her masa icin: numara + oturma noktalarini (2 empty) gir.
/// </summary>
public class MasaYonetimi : MonoBehaviour
{
    public static MasaYonetimi Instance { get; private set; }

    [System.Serializable]
    public class Masa
    {
        [Tooltip("Bu masanin numarasi (siparis numarasi olarak gosterilir).")]
        public int numara = 1;

        [Tooltip("Oturma noktalari (empty). Biri rastgele secilir; masa tek kisilik sayilir.")]
        public Transform[] oturmaNoktalari;

        [Tooltip("Musteri yiyip kalkinca paranin belirecegi nokta (empty). Bos olursa para direkt hesaba eklenir.")]
        public Transform paraNoktasi;
    }

    [Header("Masalar (Inspector'dan diz)")]
    [SerializeField] private List<Masa> masalar = new List<Masa>();

    // Dolu masalar (masa bazinda doluluk)
    private readonly HashSet<Masa> doluMasalar = new HashSet<Masa>();

    // GC'siz yardimcilar
    private readonly List<Masa> bosMasalarGecici = new List<Masa>();
    private readonly List<Transform> koltukGecici = new List<Transform>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[MasaYonetimi] Sahnede birden fazla MasaYonetimi var. Fazlasi yok sayildi.", this);
            Destroy(this);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>GUN YENIDEN BASLARKEN cagrilir: tum masalari bos konuma alir.</summary>
    public void Sifirla()
    {
        doluMasalar.Clear();
    }

    /// <summary>Bos masa var mi?</summary>
    public bool BosYerVarMi
    {
        get
        {
            for (int i = 0; i < masalar.Count; i++)
                if (MasaUygunMu(masalar[i])) return true;
            return false;
        }
    }

    /// <summary>
    /// RASTGELE bos bir masa secer, masayi dolu isaretler ve o masadan RASTGELE bir koltuk dondurur.
    /// Basariliysa true; oturmaNoktasi ve masaNo doldurulur.
    /// </summary>
    public bool YerAyir(out Transform oturmaNoktasi, out int masaNo)
    {
        // Bos masalari topla
        bosMasalarGecici.Clear();
        for (int i = 0; i < masalar.Count; i++)
            if (MasaUygunMu(masalar[i])) bosMasalarGecici.Add(masalar[i]);

        if (bosMasalarGecici.Count == 0)
        {
            oturmaNoktasi = null;
            masaNo = -1;
            return false;
        }

        // Rastgele masa sec
        Masa secilen = bosMasalarGecici[Random.Range(0, bosMasalarGecici.Count)];
        doluMasalar.Add(secilen);

        oturmaNoktasi = RastgeleKoltuk(secilen);
        masaNo = secilen.numara;
        return true;
    }

    /// <summary>
    /// FRAGMAN/senaryo icin: BELIRLI bir masa ve koltugu ayirir.
    /// Masa doluysa veya index gecersizse false doner (cagiran taraf rastgeleye dusebilir).
    /// </summary>
    public bool YerAyirBelirli(int masaIndex, int koltukIndex, out Transform oturmaNoktasi, out int masaNo)
    {
        oturmaNoktasi = null;
        masaNo = -1;

        if (masalar == null || masaIndex < 0 || masaIndex >= masalar.Count) return false;

        Masa m = masalar[masaIndex];
        if (m == null || m.oturmaNoktalari == null) return false;
        if (doluMasalar.Contains(m)) return false;
        if (koltukIndex < 0 || koltukIndex >= m.oturmaNoktalari.Length) return false;
        if (m.oturmaNoktalari[koltukIndex] == null) return false;

        doluMasalar.Add(m);
        oturmaNoktasi = m.oturmaNoktalari[koltukIndex];
        masaNo = m.numara;
        return true;
    }

    /// <summary>Belirtilen masa numarasinin para noktasini dondurur (yoksa null).</summary>
    public Transform ParaNoktasiAl(int masaNo)
    {
        for (int i = 0; i < masalar.Count; i++)
            if (masalar[i] != null && masalar[i].numara == masaNo)
                return masalar[i].paraNoktasi;
        return null;
    }

    /// <summary>Bu koltugun ait oldugu masayi tekrar bosaltir (musteri kalkinca).</summary>
    public void YeriBosalt(Transform oturmaNoktasi)
    {
        if (oturmaNoktasi == null) return;

        for (int i = 0; i < masalar.Count; i++)
        {
            Masa m = masalar[i];
            if (m == null || m.oturmaNoktalari == null) continue;

            for (int j = 0; j < m.oturmaNoktalari.Length; j++)
            {
                if (m.oturmaNoktalari[j] == oturmaNoktasi)
                {
                    doluMasalar.Remove(m);
                    return;
                }
            }
        }
    }

    // Masa kullanilabilir mi? (dolu degil + en az bir gecerli koltugu var)
    private bool MasaUygunMu(Masa m)
    {
        if (m == null || m.oturmaNoktalari == null) return false;
        if (doluMasalar.Contains(m)) return false;

        for (int j = 0; j < m.oturmaNoktalari.Length; j++)
            if (m.oturmaNoktalari[j] != null) return true;
        return false;
    }

    // Masadaki gecerli koltuklardan rastgele birini dondur
    private Transform RastgeleKoltuk(Masa m)
    {
        koltukGecici.Clear();
        for (int j = 0; j < m.oturmaNoktalari.Length; j++)
            if (m.oturmaNoktalari[j] != null) koltukGecici.Add(m.oturmaNoktalari[j]);

        if (koltukGecici.Count == 0) return null;
        return koltukGecici[Random.Range(0, koltukGecici.Count)];
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (masalar == null) return;

        for (int i = 0; i < masalar.Count; i++)
        {
            Masa m = masalar[i];
            if (m == null || m.oturmaNoktalari == null) continue;

            bool masaDolu = Application.isPlaying && doluMasalar.Contains(m);

            for (int j = 0; j < m.oturmaNoktalari.Length; j++)
            {
                Transform k = m.oturmaNoktalari[j];
                if (k == null) continue;

                Gizmos.color = masaDolu ? Color.red : Color.cyan;
                Gizmos.DrawWireSphere(k.position, 0.2f);
                Gizmos.DrawLine(k.position, k.position + k.forward * 0.3f);

                UnityEditor.Handles.Label(k.position + Vector3.up * 0.25f, "Masa " + m.numara);
            }
        }
    }
#endif
}
