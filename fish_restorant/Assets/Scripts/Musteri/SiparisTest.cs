using UnityEngine;

/// <summary>
/// GECICI TEST SCRIPTI - sadece "siparis uretiliyor ve board'da gozukuyor mu" testi.
/// Musteri/sira/masa sistemine ihtiyac duymaz.
///
/// KURULUM:
/// 1. Sahnede herhangi bir objeye (orn. yeni bos GameObject "SiparisTest") bu scripti ekle.
/// 2. "uretici"   -> SiparisUretici'nin oldugu objeyi surukle.
/// 3. "board"     -> test edecegin SiparisGosterici'yi (board) surukle.
/// 4. Play'e bas -> X tusuna bas: her basista yeni rastgele siparis board'da gozukur.
///    (C tusu: board'u temizler.)
///
/// Test bitince bu scripti silebilirsin.
/// </summary>
public class SiparisTest : MonoBehaviour
{
    [SerializeField] private SiparisUretici uretici;
    [SerializeField] private SiparisGosterici board;

    [Header("Mutfak Ekranlari (V = fis ekle, B = en eskiyi sil)")]
    [Tooltip("Siparisin gonderilecegi 2 monitor (MutfakEkrani).")]
    [SerializeField] private MutfakEkrani[] mutfakEkranlari;

    [Header("Test masa no araligi (dahil)")]
    [SerializeField] private int minMasaNo = 1;
    [SerializeField] private int maxMasaNo = 13;

    // X ile uretilen son siparis (V ile monitorlere gonderilir)
    private Siparis sonSiparis;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
            TestSiparisGoster();

        // V = en eski siparisi temizle (kapasite ac) - tum masalari doldurmak icin test
        if (Input.GetKeyDown(KeyCode.V))
            EnEskiFisiSil();

        if (Input.GetKeyDown(KeyCode.C) && board != null)
            board.Temizle();
    }

    [ContextMenu("Test Siparis Goster")]
    public void TestSiparisGoster()
    {
        if (uretici == null) { Debug.LogWarning("[SiparisTest] uretici atanmamis!", this); return; }
        if (board == null) { Debug.LogWarning("[SiparisTest] board atanmamis!", this); return; }

        Siparis s = uretici.SiparisUret();
        s.masaNo = Random.Range(minMasaNo, maxMasaNo + 1); // her seferinde 1-13 arasi rastgele
        sonSiparis = s;

        board.Goster(s);
    }

    /// <summary>X ile uretilen son siparisi 2 mutfak monitorune fis olarak ekler (V tusu).</summary>
    [ContextMenu("Monitorlere Gonder")]
    public void MonitorlereGonder()
    {
        if (sonSiparis == null || mutfakEkranlari == null) return;

        for (int i = 0; i < mutfakEkranlari.Length; i++)
            if (mutfakEkranlari[i] != null) mutfakEkranlari[i].FisEkle(sonSiparis);

        if (board != null) board.Temizle(); // ana board'u temizle (gercek akistaki gibi)
    }

    /// <summary>En eski fisi siler (B tusu). Gercek akista kapasiteyi acmak icin.</summary>
    [ContextMenu("En Eski Fisi Sil")]
    public void EnEskiFisiSil()
    {
        // Gercek akis: SiparisYonetimi uzerinden sil (sayac senkron kalsin)
        if (SiparisYonetimi.Instance != null)
        {
            SiparisYonetimi.Instance.EnEskiyiKaldir();
            return;
        }

        // Yoksa: dogrudan monitorlerden sil (sadece izole test)
        if (mutfakEkranlari == null) return;
        for (int i = 0; i < mutfakEkranlari.Length; i++)
            if (mutfakEkranlari[i] != null) mutfakEkranlari[i].EnEskiFisiKaldir();
    }
}

