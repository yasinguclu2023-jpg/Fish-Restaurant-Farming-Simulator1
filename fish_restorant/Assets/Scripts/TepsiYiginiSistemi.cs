using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Üst üste dizilmiş tepsi yığını.
/// Sol tık → en üstteki aktif tepsi child'ı kapanır (SetActive false), eline tepsi prefab'ı gelir
/// Sağ tık → tüm yığını (parent objeyi) alıp taşırsın
/// 
/// KURULUM:
/// 1. Boş bir parent GameObject oluştur → bu scripti ekle
/// 2. Altına tepsi modellerini child olarak diz (en alttaki = index 0, en üstteki = son child)
/// 3. Inspector'da tepsiPrefab'a eline gelecek tepsi prefab'ını ata
/// 4. Parent objeye collider ekle (tüm yığını kapsasın)
/// 5. RaycastSistemi'nin etkilesimLayer'ında olmalı
/// 
/// HİYERARŞİ:
/// TepsiYigini (boş GO)     ← TepsiYiginiSistemi + BoxCollider
///   ├── TepsiModel_01      ← sadece görsel (child)
///   ├── TepsiModel_02      ← sadece görsel (child)
///   ├── TepsiModel_03      ← sadece görsel (child)
///   └── ... (15'e kadar)
/// </summary>
public class TepsiYiginiSistemi : MonoBehaviour
{
    [Header("Tepsi Ayarları")]
    [Tooltip("Sol tıkla eline gelecek tepsi prefab'ı")]
    [SerializeField] private GameObject tepsiPrefab;

    [Tooltip("Geri konabilecek tepsi tag'leri (tepsi prefab'ının tag'i)")]
    [SerializeField] private string[] kabulEdilenTagler;

    [Header("Ses")]
    [Tooltip("Tepsi alınırken çalan ses")]
    [SerializeField] private AudioClip tepsiAlmaSesi;

    [Tooltip("Tepsi geri konulurken çalan ses")]
    [SerializeField] private AudioClip tepsiKoymaSesi;

    [Tooltip("Ses seviyesi")]
    [Range(0f, 1f)]
    [SerializeField] private float sesSeviyesi = 1f;

    // Runtime
    // Tüm child'lar (kapalı olanlar dahil) - başlangıç sırasıyla
    private List<GameObject> tumTepsiler = new List<GameObject>();
    // Şu an aktif olan child sayısı
    private int aktifSayisi;

    void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            tumTepsiler.Add(transform.GetChild(i).gameObject);
        }
        // Başlangıçta aktif olanları say
        aktifSayisi = 0;
        for (int i = 0; i < tumTepsiler.Count; i++)
        {
            if (tumTepsiler[i].activeSelf)
                aktifSayisi++;
        }

        Debug.Log($"[TepsiYigini] {aktifSayisi}/{tumTepsiler.Count} tepsi aktif.");
    }

    /// <summary>
    /// Yığında alınabilir tepsi var mı?
    /// </summary>
    public bool TepsiVarMi => aktifSayisi > 0 && tepsiPrefab != null;

    /// <summary>
    /// Yığındaki aktif tepsi sayısı
    /// </summary>
    public int TepsiSayisi => aktifSayisi;

    /// <summary>
    /// En üstteki aktif tepsi child'ını kapat, tepsi prefab'ı spawn edip döndür.
    /// </summary>
    public GameObject TepsiAl()
    {
        if (aktifSayisi == 0 || tepsiPrefab == null)
        {
            Debug.Log("[TepsiYigini] Tepsi yok veya prefab atanmamış!");
            return null;
        }

        // En üstteki aktif = aktifSayisi - 1 index'i
        aktifSayisi--;
        tumTepsiler[aktifSayisi].SetActive(false);

        // Prefab spawn
        GameObject tepsi = Instantiate(tepsiPrefab);
        tepsi.name = tepsiPrefab.name;

        if (tepsiAlmaSesi != null)
            AudioSource.PlayClipAtPoint(tepsiAlmaSesi, transform.position, sesSeviyesi);

        Debug.Log($"[TepsiYigini] Tepsi alındı. Kalan: {aktifSayisi}");
        return tepsi;
    }

    /// <summary>
    /// Elde tutulan tepsi bu yığına geri konabilir mi?
    /// </summary>
    public bool TepsiGeriKonabilirMi(GameObject nesne)
    {
        if (nesne == null) return false;
        if (aktifSayisi >= tumTepsiler.Count) return false; // Zaten hepsi açık

        // Tepside ekmek varsa geri konamaz
        TepsiSistemi tepsiSistemi = nesne.GetComponent<TepsiSistemi>();
        if (tepsiSistemi == null)
            tepsiSistemi = nesne.GetComponentInChildren<TepsiSistemi>();
        if (tepsiSistemi != null && tepsiSistemi.EkmekVarMi) return false;

        // Tek kullanımlık tepsi (makineden çıkan) bu yığına ait değil → sessizce reddet
        if (tepsiSistemi != null && tepsiSistemi.TekKullanimlik) return false;

        // Tag kontrolü
        if (kabulEdilenTagler != null && kabulEdilenTagler.Length > 0)
        {
            string tag = nesne.tag;
            bool bulundu = false;
            for (int i = 0; i < kabulEdilenTagler.Length; i++)
            {
                if (kabulEdilenTagler[i] == tag) { bulundu = true; break; }
            }
            if (!bulundu) return false;
        }

        return true;
    }

    /// <summary>
    /// Tepsiyi yığına geri koy: elde tutulan tepsi yok edilir,
    /// sıradaki child açılır (index = aktifSayisi, böylece sıra korunur).
    /// </summary>
    public bool TepsiGeriKoy(GameObject nesne)
    {
        if (!TepsiGeriKonabilirMi(nesne)) return false;

        // aktifSayisi = şu an açık olan tepsi sayısı
        // index 0'dan aktifSayisi-1'e kadar açık, aktifSayisi index'indeki kapalı → onu aç
        if (aktifSayisi < tumTepsiler.Count)
        {
            tumTepsiler[aktifSayisi].SetActive(true);
            aktifSayisi++;
        }

        if (tepsiKoymaSesi != null)
            AudioSource.PlayClipAtPoint(tepsiKoymaSesi, transform.position, sesSeviyesi);

        // Elde tutulan tepsi prefab'ını yok et
        Destroy(nesne);

        Debug.Log($"[TepsiYigini] Tepsi geri konuldu. Toplam: {aktifSayisi}");
        return true;
    }
}