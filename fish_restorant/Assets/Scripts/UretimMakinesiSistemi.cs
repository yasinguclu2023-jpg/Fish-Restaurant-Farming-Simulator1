using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UretimMakinesiSistemi : MonoBehaviour
{
    [Header("Kasa Yerleştirme")]
    [Tooltip("Kasanın konulacağı nokta (boş Transform)")]
    [SerializeField] private Transform kasaKoymaNoktasi;

    [Tooltip("Sadece bu tag'lere sahip kasalar kabul edilir. Boş bırakırsan hepsini alır.")]
    [SerializeField] private string[] kabulEdilenKasaTagleri;

    [Header("Ürün Tüketme")]
    [Tooltip("Her bir ürünün tükenme süresi (saniye)")]
    [SerializeField] private float urunTuketimSuresi = 2f;

    [Header("Çıkış - Tepsi")]
    [Tooltip("Tepsi spawn noktası (boş Transform)")]
    [SerializeField] private Transform tepsiSpawnNoktasi;

    [Tooltip("Tepsi prefab'ı")]
    [SerializeField] private GameObject tepsiPrefab;

    [Tooltip("Tepsi spawn edildikten sonra ilk bardak spawn'a kadar bekleme (saniye)")]
    [SerializeField] private float tepsidenSonraBekleme = 0.5f;

    [Header("Çıkış - Bardaklar/Ürünler")]
    [Tooltip("Bardak spawn noktaları (makinede boş Transform'lar)")]
    [SerializeField] private Transform[] bardakSpawnNoktalari;

    [Tooltip("Bardak/ürün prefab'ı")]
    [SerializeField] private GameObject bardakPrefab;

    [Header("Sıvı Modeli (Opsiyonel)")]
    [SerializeField] private SiviModeliSistemi siviSistemi;

    [Header("Sesler (Opsiyonel)")]
    [Tooltip("Sesin tam yüksek duyulduğu mesafe")]
    [SerializeField] private float sesMinMesafe = 2f;

    [Tooltip("Sesin duyulmamaya başladığı mesafe")]
    [SerializeField] private float sesMaxMesafe = 15f;

    [SerializeField] private AudioClip kasaKoymaSesi;
    [Range(0f, 1f)]
    [SerializeField] private float kasaKoymaSesiVolume = 1f;

    [SerializeField] private AudioClip uretimDonguSesi;
    [Range(0f, 1f)]
    [SerializeField] private float uretimDonguSesiVolume = 1f;

    [SerializeField] private AudioClip urunSpawnSesi;
    [Range(0f, 1f)]
    [SerializeField] private float urunSpawnSesiVolume = 1f;

    [SerializeField] private AudioClip tamamlanmaSesi;
    [Range(0f, 1f)]
    [SerializeField] private float tamamlanmaSesiVolume = 1f;

    [Header("Partikül (Opsiyonel)")]
    [SerializeField] private ParticleSystem uretimPartikul;

    // Runtime
    protected bool makineCalisiyor;
    protected bool kasaMevcut;
    protected GameObject mevcutKasa;
    protected GameObject mevcutTepsi;
    protected int bardakIndex;
    protected AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 3D ses ayarları (her durumda uygula, manuel eklenmiş AudioSource'a da)
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = sesMinMesafe;
        audioSource.maxDistance = sesMaxMesafe;
    }

    void Start()
    {
    }

    // ============================================================
    // KASA KOYMA
    // ============================================================

    public bool KasaKoy(GameObject kasa)
    {
        if (kasa == null || kasaMevcut || makineCalisiyor) return false;
        if (!KasaKabulEdilirMi(kasa)) return false;

        mevcutKasa = kasa;

        kasa.transform.SetParent(kasaKoymaNoktasi);
        kasa.transform.localPosition = Vector3.zero;
        kasa.transform.localRotation = Quaternion.identity;

        Rigidbody rb = kasa.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider[] cols = kasa.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = false;

        kasaMevcut = true;
        SesCal(kasaKoymaSesi, kasaKoymaSesiVolume);

        StartCoroutine(UretimSureci());
        return true;
    }

    public bool KasaKabulEdilirMi(GameObject kasa)
    {
        if (kasa == null) return false;
        if (kasaMevcut || makineCalisiyor) return false;

        if (kabulEdilenKasaTagleri == null || kabulEdilenKasaTagleri.Length == 0)
            return true;

        string kasaTag = kasa.tag;
        for (int i = 0; i < kabulEdilenKasaTagleri.Length; i++)
        {
            if (kabulEdilenKasaTagleri[i] == kasaTag)
                return true;
        }
        return false;
    }

    // ============================================================
    // ÜRETİM SÜRECİ
    // ============================================================

    protected virtual IEnumerator UretimSureci()
    {
        makineCalisiyor = true;
        bardakIndex = 0;

        if (uretimPartikul != null) uretimPartikul.Play();

        if (uretimDonguSesi != null && audioSource != null)
        {
            audioSource.clip = uretimDonguSesi;
            audioSource.loop = true;
            audioSource.volume = uretimDonguSesiVolume;
            audioSource.Play();
        }

        if (siviSistemi != null) siviSistemi.YukselmeBaslat();

        // Ürünleri bul
        List<GameObject> urunListesi = UrunleriBul();
        int toplamUrun = urunListesi.Count;

        // ===== EŞ ZAMANLI DÖNGÜ =====
        for (int i = 0; i < toplamUrun; i++)
        {
            // 1) Üzümü sil
            GameObject urun = urunListesi[i];
            if (urun != null)
            {
                urun.SetActive(false);
                Destroy(urun, 0.5f);
            }

            // 2) Bekle
            yield return new WaitForSeconds(urunTuketimSuresi);

            // 3) Tepsi yoksa veya alınmışsa yeni spawn et
            if (mevcutTepsi == null || !mevcutTepsi.transform.IsChildOf(tepsiSpawnNoktasi))
            {
                mevcutTepsi = null; // referansı temizle
                YeniTepsiSpawnla();
                yield return new WaitForSeconds(tepsidenSonraBekleme);
            }

            // 4) Bardak spawn et
            BardakSpawnla();
        }

        // Sıvı düşmeye başlasın
        if (siviSistemi != null) siviSistemi.DusmeBaslat();

        // Tamamlandı
        UretimiTamamla();
    }

    // ============================================================
    // TEPSİ & BARDAK SPAWN
    // ============================================================

    void YeniTepsiSpawnla()
    {
        mevcutTepsi = Instantiate(tepsiPrefab, tepsiSpawnNoktasi.position, tepsiSpawnNoktasi.rotation);
        mevcutTepsi.transform.SetParent(tepsiSpawnNoktasi);

        Rigidbody tepsiRb = mevcutTepsi.GetComponent<Rigidbody>();
        if (tepsiRb != null)
        {
            tepsiRb.isKinematic = true;
            tepsiRb.useGravity = false;
        }

        // Bardak index sıfırla — yeni tepsi, baştan dol
        bardakIndex = 0;

        SesCal(urunSpawnSesi, urunSpawnSesiVolume);
    }

    void BardakSpawnla()
    {
        if (bardakPrefab == null || bardakSpawnNoktalari == null) return;
        if (bardakIndex >= bardakSpawnNoktalari.Length) return;
        if (mevcutTepsi == null) return;

        Transform nokta = bardakSpawnNoktalari[bardakIndex];
        if (nokta != null)
        {
            // Noktanın WORLD pozisyonuna spawn et, sonra tepsinin child'ı yap
            GameObject bardak = Instantiate(bardakPrefab, nokta.position, nokta.rotation);
            bardak.transform.SetParent(mevcutTepsi.transform, true);

            Rigidbody bRb = bardak.GetComponent<Rigidbody>();
            if (bRb != null)
            {
                bRb.isKinematic = true;
                bRb.useGravity = false;
            }

            SesCal(urunSpawnSesi, urunSpawnSesiVolume);
        }

        bardakIndex++;
    }

    // ============================================================
    // ÜRETİM TAMAMLAMA
    // ============================================================

    protected virtual void UretimiTamamla()
    {
        makineCalisiyor = false;

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (uretimPartikul != null) uretimPartikul.Stop();

        SesCal(tamamlanmaSesi, tamamlanmaSesiVolume);

        if (mevcutKasa != null)
        {
            Destroy(mevcutKasa);
            mevcutKasa = null;
        }
        kasaMevcut = false;
    }

    // ============================================================
    // ÜRÜN BULMA
    // ============================================================

    protected virtual List<GameObject> UrunleriBul()
    {
        List<GameObject> liste = new List<GameObject>();
        if (mevcutKasa == null) return liste;

        Transform kasa = mevcutKasa.transform;
        Transform urunParent = kasa;

        // Kasa > uzum_toplu > uzum1, uzum2... yapısını destekle
        for (int i = 0; i < kasa.childCount; i++)
        {
            Transform child = kasa.GetChild(i);
            if (child.childCount > 0)
            {
                urunParent = child;
                break;
            }
        }

        for (int i = 0; i < urunParent.childCount; i++)
        {
            GameObject child = urunParent.GetChild(i).gameObject;
            if (child.activeSelf)
                liste.Add(child);
        }

        return liste;
    }

    // ============================================================
    // TEPSİ ALMA
    // ============================================================

    public GameObject TepsiAl()
    {
        if (mevcutTepsi == null) return null;

        GameObject tepsi = mevcutTepsi;
        tepsi.transform.SetParent(null);

        Collider[] cols = tepsi.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = true;

        mevcutTepsi = null;
        return tepsi;
    }

    // ============================================================
    // YARDIMCI
    // ============================================================

    protected void SesCal(AudioClip clip, float volume = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, volume);
    }

    // ============================================================
    // TÜRETİLMİŞ SINIFLAR İÇİN YARDIMCI SES METODLARI
    // ============================================================

    /// <summary>
    /// Üretim döngü sesini başlat (loop)
    /// </summary>
    protected void UretimSesiBaslat()
    {
        if (uretimDonguSesi != null && audioSource != null)
        {
            audioSource.clip = uretimDonguSesi;
            audioSource.loop = true;
            audioSource.volume = uretimDonguSesiVolume;
            audioSource.Play();
        }
    }

    /// <summary>
    /// Üretim döngü sesini durdur
    /// </summary>
    protected void UretimSesiDurdur()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    /// <summary>
    /// Kasa koyma sesini çal
    /// </summary>
    protected void KasaKoymaSesiCal()
    {
        SesCal(kasaKoymaSesi, kasaKoymaSesiVolume);
    }

    /// <summary>
    /// Ürün spawn sesini çal
    /// </summary>
    protected void UrunSpawnSesiCal()
    {
        SesCal(urunSpawnSesi, urunSpawnSesiVolume);
    }

    /// <summary>
    /// Tamamlanma sesini çal
    /// </summary>
    protected void TamamlanmaSesiCal()
    {
        SesCal(tamamlanmaSesi, tamamlanmaSesiVolume);
    }

    /// <summary>
    /// Üretim partikülünü başlat
    /// </summary>
    protected void UretimPartikulBaslat()
    {
        if (uretimPartikul != null) uretimPartikul.Play();
    }

    /// <summary>
    /// Üretim partikülünü durdur
    /// </summary>
    protected void UretimPartikulDurdur()
    {
        if (uretimPartikul != null) uretimPartikul.Stop();
    }

    // ============================================================
    // PUBLIC PROPERTIES
    // ============================================================

    public bool MakineCalisiyor => makineCalisiyor;
    public bool KasaMevcut => kasaMevcut;
    public bool KasaKoyulabilirMi => !kasaMevcut && !makineCalisiyor;
    public bool TepsiAlinabilirMi => mevcutTepsi != null;
    public Transform KasaKoymaNoktasi => kasaKoymaNoktasi;
    public string[] KabulEdilenKasaTagleri => kabulEdilenKasaTagleri;

    // ============================================================
    // EDITOR GIZMO
    // ============================================================

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (kasaKoymaNoktasi != null)
        {
            Gizmos.color = kasaMevcut ? Color.red : Color.cyan;
            Gizmos.DrawWireCube(kasaKoymaNoktasi.position, new Vector3(0.4f, 0.3f, 0.4f));
            UnityEditor.Handles.Label(kasaKoymaNoktasi.position + Vector3.up * 0.25f, "Kasa Noktasi");
        }

        if (tepsiSpawnNoktasi != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(tepsiSpawnNoktasi.position, new Vector3(0.5f, 0.05f, 0.3f));
            UnityEditor.Handles.Label(tepsiSpawnNoktasi.position + Vector3.up * 0.15f, "Tepsi Spawn");
        }

        if (bardakSpawnNoktalari != null)
        {
            Gizmos.color = Color.magenta;
            for (int i = 0; i < bardakSpawnNoktalari.Length; i++)
            {
                if (bardakSpawnNoktalari[i] != null)
                {
                    Gizmos.DrawWireSphere(bardakSpawnNoktalari[i].position, 0.03f);
                    UnityEditor.Handles.Label(bardakSpawnNoktalari[i].position + Vector3.up * 0.08f, $"Bardak {i}");
                }
            }
        }
    }
#endif
}