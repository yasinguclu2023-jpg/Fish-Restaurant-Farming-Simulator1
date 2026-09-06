using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Musteri uretici (spawner).
///
/// - Spawn noktalari Inspector'dan ayarlanir (liste, istedigin kadar ekleyebilirsin).
/// - Karakterler spawn noktalarindan SIRAYLA (round-robin) uretilir:
///   1. nokta -> 2. nokta -> 1. nokta -> 2. nokta ...
/// - Her "spawnAraligi" saniyede bir karakter uretilir.
/// - Sira dolu oldugunda (SiraYonetimi.SiraDoluMu) uretim DURUR;
///   sirada yer acilinca kaldigi yerden devam eder.
///
/// KURULUM:
/// 1. Sahnede bos bir GameObject olustur -> bu scripti ekle.
/// 2. "musteriPrefab" -> NavMeshAgent + MusteriAI iceren prefab (simdilik Capsule olabilir).
/// 3. "spawnNoktalari" -> sehirde karakterlerin doguacagi bos Transform'lar.
/// 4. SiraYonetimi sahnede olmali.
/// </summary>
public class MusteriUretici : MonoBehaviour
{
    [Header("Musteri Prefablari")]
    [Tooltip("NavMeshAgent + MusteriAI iceren karakter prefablari (variant'lar). " +
             "Istedigin kadar ekle; her spawn'da rastgele biri secilir (ust uste ayni gelmez).")]
    [SerializeField] private List<GameObject> musteriPrefablari = new List<GameObject>();

    [Header("Spawn Noktalari (sirayla kullanilir)")]
    [Tooltip("Istedigin kadar nokta ekleyebilirsin. Karakterler sirayla bu noktalardan dogar.")]
    [SerializeField] private List<Transform> spawnNoktalari = new List<Transform>();

    [Header("Zamanlama")]
    [Tooltip("Oyun basladiktan sonra ilk uretime kadar gecen sure (saniye).")]
    [SerializeField] private float ilkGecikme = 1f;
    [Tooltip("Kac saniyede bir karakter uretilsin.")]
    [SerializeField] private float spawnAraligi = 3f;
    [Tooltip("Kapali ise otomatik uretim olmaz (sadece HemenUret ile manuel).")]
    [SerializeField] private bool otomatikUret = true;

    [Header("Gun Sonu")]
    [Tooltip("Bitis saatine ulasinca (gun bitince) yeni musteri uretilmez. Bos birakirsan sahnede otomatik bulunur.")]
    [SerializeField] private ZamanSistemi zamanSistemi;

    private int sonrakiSpawnIndex;        // spawn noktasi round-robin
    private GameObject sonPrefab;         // tekrarsiz rastgele icin son secilen prefab (referans)
    private float kalanSure;              // sonraki uretime kalan sure (geri sayim)
    private bool uretimAcik;              // tabela ile kontrol: acikken sayac ilerler, kapaliyken donar
    private readonly List<GameObject> secenekler = new List<GameObject>(); // RastgelePrefab icin (GC'siz)

    // Su an sahnede CANLI olan musteriler: prefab -> instance.
    // Ayni prefabtan ikinci bir musteri, oncekisi yok olana kadar uretilmez.
    private readonly Dictionary<GameObject, MusteriAI> canliMusteriler = new Dictionary<GameObject, MusteriAI>();
    private readonly List<GameObject> olenPrefablar = new List<GameObject>(); // temizlik icin (GC'siz)

    void Start()
    {
        kalanSure = ilkGecikme;
        if (zamanSistemi == null) zamanSistemi = FindObjectOfType<ZamanSistemi>();
    }

    void Update()
    {
        if (!otomatikUret) return;
        if (!uretimAcik) return; // tabela pasif: sayac ilerlemez (oldugu yerde donar)

        // Gun bitti (saat bitisSaati'ne ulasti): yeni musteri uretme. Mevcutlar isini bitirir.
        if (zamanSistemi != null && zamanSistemi.GunBittiMi()) return;

        // Sira doluysa uretme; sayaci ilerletme ki yer acilinca hemen uretsin.
        if (SiraYonetimi.Instance != null && SiraYonetimi.Instance.SiraDoluMu)
            return;

        // Tum karakterlerden sahnede canli kopya varsa uretme; sayaci ilerletme ki
        // biri gidince hemen uretsin.
        if (!UygunPrefabVarMi()) return;

        kalanSure -= Time.deltaTime;
        if (kalanSure > 0f) return;

        Uret();
        kalanSure = spawnAraligi;
    }

    /// <summary>Tabela aktif olunca cagrilir: uretimi ac. Sayaci SIFIRLAMAZ, kaldigi yerden devam eder.</summary>
    public void UretimiBaslat()
    {
        uretimAcik = true;
    }

    /// <summary>Tabela pasif olunca cagrilir: uretimi durdur. Sayac oldugu yerde donar.</summary>
    public void UretimiDurdur()
    {
        uretimAcik = false;
    }

    // ===== DEMO GUN SISTEMI (kaldirilabilir) =====
    /// <summary>
    /// O gunun spawn araligini disaridan ayarlar (DemoGunSistemi cagirir).
    /// 0 veya negatif verirsen MEVCUT DEGER KORUNUR.
    /// Bu metodu kimse cagirmazsa Inspector'daki deger aynen kullanilir.
    /// </summary>
    public void SpawnAraligiAyarla(float yeniAralik)
    {
        if (yeniAralik <= 0f) return;
        spawnAraligi = yeniAralik;
    }
    // ===== DEMO GUN SISTEMI SONU =====

    /// <summary>
    /// GUN YENIDEN BASLARKEN cagrilir (GunYoneticisi).
    /// Sahnedeki TUM musterileri temizler, uretim durumunu ve sayaclari sifirlar.
    /// </summary>
    public void GunuSifirla()
    {
        // Sahnedeki tum musterileri aninda yok et (masa/icecek gibi kalintilari da temizler).
        MusteriAI[] hepsi = FindObjectsOfType<MusteriAI>();
        for (int i = 0; i < hepsi.Length; i++)
            if (hepsi[i] != null) hepsi[i].AninadaYokEt();

        canliMusteriler.Clear();
        sonPrefab = null;
        sonrakiSpawnIndex = 0;
        uretimAcik = false;        // yeni gun: dukkan kapali, tabela ile tekrar acilir
        kalanSure = ilkGecikme;    // sayaci basa al
    }

    /// <summary>Bir musteri uretir (round-robin spawn noktasindan). Test icin de cagrilabilir.</summary>
    [ContextMenu("Hemen Uret")]
    public MusteriAI Uret()
    {
        // FRAGMAN: sirada senaryo musterisi varsa onun prefab'ini kullan
        bool fragmanMusterisi = false;
        GameObject prefab = null;
        if (FragmanSistemi.Instance != null &&
            FragmanSistemi.Instance.SonrakiPrefabIndex(out int fragmanIndex))
        {
            if (musteriPrefablari != null && fragmanIndex >= 0 && fragmanIndex < musteriPrefablari.Count
                && musteriPrefablari[fragmanIndex] != null)
            {
                prefab = musteriPrefablari[fragmanIndex];
                fragmanMusterisi = true;
            }
            else
            {
                Debug.LogWarning($"[MusteriUretici] Fragman prefab index gecersiz: {fragmanIndex}", this);
            }
        }

        if (prefab == null) prefab = RastgelePrefab();
        if (prefab == null)
        {
            Debug.LogWarning("[MusteriUretici] musteriPrefablari bos veya gecersiz!", this);
            return null;
        }
        if (spawnNoktalari == null || spawnNoktalari.Count == 0)
        {
            Debug.LogWarning("[MusteriUretici] spawnNoktalari bos!", this);
            return null;
        }

        Transform spawn = SonrakiSpawnNoktasi();
        if (spawn == null) return null;

        GameObject obj = Instantiate(prefab, spawn.position, spawn.rotation);

        MusteriAI musteri = obj.GetComponent<MusteriAI>();
        if (musteri == null)
        {
            Debug.LogWarning("[MusteriUretici] Prefab'da MusteriAI bileseni yok!", obj);
            return null;
        }

        // Bu prefabtan artik sahnede canli biri var; o yok olana kadar tekrar uretilmeyecek.
        canliMusteriler[prefab] = musteri;

        // FRAGMAN: bu musteriyi senaryosuna bagla (akis baslamadan once)
        if (fragmanMusterisi && FragmanSistemi.Instance != null)
            FragmanSistemi.Instance.MusteriyiBagla(musteri);

        musteri.AkisiBaslat(spawn); // spawn = geri donecegi nokta
        return musteri;
    }

    /// <summary>
    /// Rastgele bir prefab secer.
    /// SERT kural: sahnede CANLI kopyasi olan prefab secilmez (ayni karakterden 2 tane olmaz).
    /// YUMUSAK kural: mumkunse bir onceki spawn'la ayni karakter ust uste gelmez.
    /// Musait prefab yoksa null doner (uretim beklemeye gecer).
    /// </summary>
    private GameObject RastgelePrefab()
    {
        if (musteriPrefablari == null || musteriPrefablari.Count == 0)
            return null;

        OluleriTemizle();

        // 1) Sahnede canli kopyasi OLMAYAN (musait) prefablari topla
        secenekler.Clear();
        for (int i = 0; i < musteriPrefablari.Count; i++)
        {
            GameObject p = musteriPrefablari[i];
            if (p == null) continue;
            if (canliMusteriler.ContainsKey(p)) continue; // bu karakterden sahnede zaten var
            secenekler.Add(p);
        }

        if (secenekler.Count == 0) return null; // hepsi sahnede: uretme

        // 2) Baska secenek varsa ust uste ayni karakteri verme
        if (secenekler.Count > 1 && sonPrefab != null)
            secenekler.Remove(sonPrefab);

        GameObject secilen = secenekler[Random.Range(0, secenekler.Count)];
        sonPrefab = secilen;
        return secilen;
    }

    /// <summary>Yok edilmis (Destroy) musterileri kayittan duser; o prefablar tekrar musait olur.</summary>
    private void OluleriTemizle()
    {
        if (canliMusteriler.Count == 0) return;

        olenPrefablar.Clear();
        foreach (KeyValuePair<GameObject, MusteriAI> kv in canliMusteriler)
            if (kv.Value == null) olenPrefablar.Add(kv.Key); // Unity: yok edilmis obje == null

        for (int i = 0; i < olenPrefablar.Count; i++)
            canliMusteriler.Remove(olenPrefablar[i]);
    }

    /// <summary>Sahnede canli kopyasi olmayan (uretilebilir) en az bir prefab var mi?</summary>
    private bool UygunPrefabVarMi()
    {
        if (musteriPrefablari == null || musteriPrefablari.Count == 0) return false;

        OluleriTemizle();

        for (int i = 0; i < musteriPrefablari.Count; i++)
        {
            GameObject p = musteriPrefablari[i];
            if (p != null && !canliMusteriler.ContainsKey(p)) return true;
        }
        return false;
    }

    private Transform SonrakiSpawnNoktasi()
    {
        // Bos olabilecek elemanlari atlayarak siradaki gecerli noktayi sec
        int denenen = 0;
        while (denenen < spawnNoktalari.Count)
        {
            if (sonrakiSpawnIndex >= spawnNoktalari.Count)
                sonrakiSpawnIndex = 0;

            Transform nokta = spawnNoktalari[sonrakiSpawnIndex];
            sonrakiSpawnIndex++;

            if (nokta != null) return nokta;
            denenen++;
        }
        return null;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (spawnNoktalari == null) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < spawnNoktalari.Count; i++)
        {
            Transform n = spawnNoktalari[i];
            if (n == null) continue;

            Gizmos.DrawWireCube(n.position, Vector3.one * 0.5f);
            Gizmos.DrawLine(n.position, n.position + n.forward * 0.5f);
            UnityEditor.Handles.Label(n.position + Vector3.up * 0.4f, "Spawn " + i);
        }
    }
#endif
}
