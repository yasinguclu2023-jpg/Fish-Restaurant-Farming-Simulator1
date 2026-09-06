using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GECICI FRAGMAN (trailer) SISTEMI - Singleton.
///
/// "Aktif" tiki ACIKKEN: tabela acildiktan sonra gelen ilk musteriler asagidaki
/// KODA GOMULU senaryoya gore gelir (belirli prefab, belirli siparis, belirli masa).
/// Senaryo bitince (veya tik KAPALIYKEN) her sey normal/rastgele sisteme doner.
///
/// KURULUM: Sahnede bos bir GameObject -> bu scripti ekle -> "Aktif" tikini ac.
/// Baska hicbir sey doldurman gerekmiyor; senaryo asagida hazir.
///
/// KALDIRMA: Tiki kapat (yeter). Tamamen silmek istersen bu dosyayi ve diger
/// scriptlerdeki kucuk "FRAGMAN" if bloklarini sil.
/// </summary>
public class FragmanSistemi : MonoBehaviour
{
    public static FragmanSistemi Instance { get; private set; }

    /// <summary>Tek bir senaryo musterisi (koda gomulu).</summary>
    public class FragmanMusteri
    {
        /// <summary>MusteriUretici'deki "Musteri Prefablari" listesindeki index.</summary>
        public int prefabIndex;

        /// <summary>Ana urun asset adi (ekmek alt/ust OTOMATIK eklenir).</summary>
        public string balikAdi;

        /// <summary>Sebze / yan urun / icecek asset adlari.</summary>
        public string[] digerAdlar;

        /// <summary>MasaYonetimi'ndeki "Masalar" listesindeki index.</summary>
        public int masaIndex;

        /// <summary>O masanin "Oturma Noktalari" dizisindeki index.</summary>
        public int koltukIndex;
    }

    [Header("Fragman")]
    [Tooltip("ACIK ise asagidaki senaryo calisir. Kapatirsan sistem tamamen devre disi (normal oyun).")]
    [SerializeField] private bool aktif = false;

    // ============================================================
    // SENARYO (koda gomulu - Inspector'dan doldurmaya gerek yok)
    // Malzeme adlari Assets/Mazemeler/ altindaki asset adlariyla eslesir.
    // ============================================================
    private static readonly FragmanMusteri[] senaryo =
    {
        // 1. musteri: prefab 9 -> balik + domates, sogan, marul, kalamar, sarap -> masa 8 / koltuk 0
        new FragmanMusteri
        {
            prefabIndex = 9,
            balikAdi    = "Balık",
            digerAdlar  = new[] { "domates", "sogan", "marul", "kalamar", "sarap" },
            masaIndex   = 8,
            koltukIndex = 0
        },

        // 2. musteri: prefab 4 -> octopus + marul, domates, sogan, bira -> masa 2 / koltuk 0
        new FragmanMusteri
        {
            prefabIndex = 4,
            balikAdi    = "octopus",
            digerAdlar  = new[] { "marul", "domates", "sogan", "bira" },
            masaIndex   = 2,
            koltukIndex = 0
        },
    };

    // Sirada bekleyen senaryo index'i
    private int sonrakiSenaryo;

    // Spawn edilen musteri -> senaryosu (sayac yerine instance'a baglanir ki sira karismasin)
    private readonly Dictionary<MusteriAI, FragmanMusteri> atananlar = new Dictionary<MusteriAI, FragmanMusteri>();

    /// <summary>Sistem devrede mi?</summary>
    public bool Aktif => aktif;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[Fragman] Sahnede birden fazla FragmanSistemi var. Fazlasi yok sayildi.", this);
            Destroy(this);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Uretici cagirir: sirada senaryo musterisi varsa prefab index'ini verir.</summary>
    public bool SonrakiPrefabIndex(out int prefabIndex)
    {
        prefabIndex = -1;
        if (!Aktif) return false;
        if (sonrakiSenaryo < 0 || sonrakiSenaryo >= senaryo.Length) return false;

        prefabIndex = senaryo[sonrakiSenaryo].prefabIndex;
        return true;
    }

    /// <summary>Uretici cagirir: spawn edilen musteriyi siradaki senaryoya baglar, sirayi ilerletir.</summary>
    public void MusteriyiBagla(MusteriAI musteri)
    {
        if (!Aktif || musteri == null) return;
        if (sonrakiSenaryo < 0 || sonrakiSenaryo >= senaryo.Length) return;

        atananlar[musteri] = senaryo[sonrakiSenaryo];
        sonrakiSenaryo++;
    }

    /// <summary>Bu musterinin senaryosu var mi? Yoksa null (normal/rastgele sistem calisir).</summary>
    public FragmanMusteri SenaryoAl(MusteriAI musteri)
    {
        if (!Aktif || musteri == null) return null;
        atananlar.TryGetValue(musteri, out FragmanMusteri s);
        return s;
    }

    /// <summary>Senaryo kullanildi, kayittan dus.</summary>
    public void MusteriyiUnut(MusteriAI musteri)
    {
        if (musteri != null) atananlar.Remove(musteri);
    }
}
