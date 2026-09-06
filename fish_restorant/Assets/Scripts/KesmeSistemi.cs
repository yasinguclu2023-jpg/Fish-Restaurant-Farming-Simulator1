using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Kesme sistemi. Kesme tahtasına bak + E tuşunu BASILI TUT = bıçak animasyonu + nesne dönüşümü.
///
/// E'ye basılı tutulduğu sürece ekranda "Kesme" kanalına ait radial (360°) ilerleme dolar. Tutma
/// süresi (varsayılan 1.19 sn, kesme animasyonunun süresi) dolunca kesim tamamlanır. Süre dolmadan
/// E bırakılırsa veya oyuncu tahtadan bakışını çekerse kesim iptal olur, ilerleme sıfırlanır ve
/// kesme sesi anında susar.
///
/// Radial: Paylaşılan <see cref="BasiliTutmaYoneticisi"/> üzerinden "Kesme" kanalıyla çalışır.
/// Sahnede bir <see cref="RadialIlerlemeUI"/> Image'ının "Hangi Kaynak = Kesme" seçili olması gerekir.
/// Böylece nesne alma (NesneAlma kanalı) radial'ından bağımsızdır, karışmaz.
///
/// Kurulum:
/// 1. Bu scripti oyuncuya veya boş bir manager objesine ekle
/// 2. Bıçak modelini sahneye koy, Animator ekle
/// 3. Inspector'dan bıçak Animator'ı ve kesme tariflerini ata
/// 4. Kesme tarifleri: Project > Create > Kesme Sistemi > Kesme Tarifi
/// 5. Canvas'ta kesmeye özel bir Radial360 Image + RadialIlerlemeUI (Hangi Kaynak = Kesme) oluştur
///
/// Bıçak Animator kurulumu:
///   - Idle state (default) — bıçak sabit durur
///   - "Kes" Bool parametresi: true olunca kesme animasyonuna geçiş
///   - "Kes" false olunca Idle'a dönüş
///   - Kesme animasyonunda Loop Time KAPALI olmalı
/// </summary>
public class KesmeSistemi : MonoBehaviour
{
    [Header("Tuş Ayarı")]
    [SerializeField] private KeyCode kesmeTusu = KeyCode.E;

    [Header("Bıçak")]
    [Tooltip("Bıçak modelinin Animator'ı")]
    [SerializeField] private Animator bicakAnimator;

    [Tooltip("Animator'daki bool parametresinin adı (true=kes, false=idle)")]
    [SerializeField] private string animasyonBool = "Kes";

    [Header("Basılı Tutma")]
    [Tooltip("E'ye kaç saniye basılı tutulunca kesim tamamlansın. Kesme animasyonunun süresiyle aynı olmalı.")]
    [SerializeField] private float tutmaSuresi = 1.19f;

    [Header("Bıçak Görünürlük")]
    [Tooltip("Bıçak modeli objesi (her zaman sahnede görünür kalır)")]
    [SerializeField] private GameObject bicakModeli;

    [Header("Kesme Tarifleri")]
    [Tooltip("Tüm kesme tariflerini buraya ekle")]
    [SerializeField] private KesmeTarifi[] tarifler;

    // Cache
    private RaycastSistemi raycastSistemi;
    private BasiliTutmaYoneticisi tutmaYoneticisi;
    private Dictionary<string, KesmeTarifi> tarifCache;

    // Basılı tutma durumu (kesim beklerken tutulan hedef)
    private bool kesimBekliyor;
    private KesmeTahtasiSistemi bekleyenTahta;
    private GameObject bekleyenKaynak;
    private KesmeTarifi bekleyenTarif;

    // Çalan kesme sesi (iptalde durdurmak için)
    private AudioSource kesmeSesKaynagi;
    private AudioClip kesmeSesKlip;

    void Awake()
    {
        // Tarif lookup'ı Dictionary'ye çevir - O(1) arama
        tarifCache = new Dictionary<string, KesmeTarifi>();
        if (tarifler != null)
        {
            for (int i = 0; i < tarifler.Length; i++)
            {
                if (tarifler[i] != null && !string.IsNullOrEmpty(tarifler[i].girisTag))
                    tarifCache[tarifler[i].girisTag] = tarifler[i];
            }
        }
    }

    void Start()
    {
        raycastSistemi = FindObjectOfType<RaycastSistemi>();
        tutmaYoneticisi = BasiliTutmaYoneticisi.Instance;

#if UNITY_EDITOR
        if (raycastSistemi == null)
            Debug.LogError("[KesmeSistemi] RaycastSistemi bulunamadı!");
        if (tutmaYoneticisi == null)
            Debug.LogError("[KesmeSistemi] BasiliTutmaYoneticisi bulunamadı! Radial UI çalışmaz.");
        if (bicakAnimator == null)
            Debug.LogError("[KesmeSistemi] Bıçak Animator atanmamış!");
        if (tarifler == null || tarifler.Length == 0)
            Debug.LogWarning("[KesmeSistemi] Kesme tarifi atanmamış!");
#endif

        // Bıçak her zaman görünür
        if (bicakModeli != null)
            bicakModeli.SetActive(true);
    }

    void Update()
    {
        // Kesim için basılı tutma sürüyorsa onu işle
        if (kesimBekliyor)
        {
            BasiliTutmaDevam();
            return;
        }

        // Yeni kesim başlatma denemesi
        if (Input.GetKeyDown(kesmeTusu))
            KesmeDenemesiYap();
    }

    private void KesmeDenemesiYap()
    {
        if (raycastSistemi == null || !raycastSistemi.ObjeyeBakiyorMu) return;

        // Başka bir basılı tutma (ör. nesne alma) devam ediyorsa kesim başlatma
        if (tutmaYoneticisi != null && tutmaYoneticisi.Aktif) return;

        GameObject bakilan = raycastSistemi.BakilanObje;
        if (bakilan == null) return;

        // Tahtayı bul - null coalescing zinciri
        KesmeTahtasiSistemi tahta = bakilan.GetComponent<KesmeTahtasiSistemi>()
            ?? bakilan.GetComponentInParent<KesmeTahtasiSistemi>()
            ?? bakilan.GetComponentInChildren<KesmeTahtasiSistemi>();

        if (tahta == null || !tahta.NesneVarMi) return;

        GameObject tahtadakiNesne = tahta.SonDoluSlotNesnesi;
        if (tahtadakiNesne == null) return;

        // Dictionary ile O(1) tarif arama
        if (!tarifCache.TryGetValue(tahtadakiNesne.tag, out KesmeTarifi tarif))
        {
#if UNITY_EDITOR
            Debug.Log($"[KesmeSistemi] '{tahtadakiNesne.tag}' için kesme tarifi yok!");
#endif
            return;
        }

        // Basılı tutmayı başlat - Kesme kanalının radial'ı dolmaya başlasın
        BasiliTutmaBaslat(tahta, tahtadakiNesne, tarif);
    }

    /// <summary>
    /// Kesim için basılı tutmayı başlatır: animasyon oynar, ses çalar, Kesme radial'ı dolmaya başlar.
    /// </summary>
    private void BasiliTutmaBaslat(KesmeTahtasiSistemi tahta, GameObject kaynak, KesmeTarifi tarif)
    {
        bekleyenTahta = tahta;
        bekleyenKaynak = kaynak;
        bekleyenTarif = tarif;
        kesimBekliyor = true;

        // Bıçak animasyonunu başlat (tutmaSuresi ile senkron oynar)
        if (bicakAnimator != null)
            bicakAnimator.SetBool(animasyonBool, true);

        // Kesme sesini çal - iptalde durdurmak için kaynağı sakla
        if (tarif.kesmeSesi != null && SesYoneticisi.Instance != null)
        {
            kesmeSesKaynagi = SesYoneticisi.Instance.SesCal(tarif.kesmeSesi, kaynak.transform.position);
            kesmeSesKlip = kesmeSesKaynagi != null ? kesmeSesKaynagi.clip : null;
        }

        // Radial ilerlemeyi "Kesme" kanalında başlat
        tutmaYoneticisi?.Baslat(tutmaSuresi, BasiliTutmaYoneticisi.TutmaKaynagi.Kesme);
    }

    /// <summary>
    /// Basılı tutma devam ederken her frame çağrılır. Tuş bırakılırsa/hedef kaybolursa iptal,
    /// süre dolunca kesim tamamlanır.
    /// </summary>
    private void BasiliTutmaDevam()
    {
        // Tuş bırakıldıysa iptal
        if (!Input.GetKey(kesmeTusu))
        {
            Iptal();
            return;
        }

        // Kaynak nesne yok olduysa (Destroy vb.) iptal
        if (bekleyenKaynak == null)
        {
            Iptal();
            return;
        }

        // Artık aynı tahtaya bakılmıyorsa iptal
        if (raycastSistemi == null || !raycastSistemi.ObjeyeBakiyorMu || !AyniTahtayaBakiliyorMu())
        {
            Iptal();
            return;
        }

        // Yönetici yoksa süreyi sürdüremeyiz - güvenli iptal
        if (tutmaYoneticisi == null)
        {
            Iptal();
            return;
        }

        // Süre dolduysa kesimi tamamla (radial'ı yönetici günceller)
        if (tutmaYoneticisi.Guncelle())
            KesimiTamamla();
    }

    /// <summary>
    /// Oyuncunun hâlâ kesime başladığı tahtaya bakıp bakmadığını kontrol eder.
    /// </summary>
    private bool AyniTahtayaBakiliyorMu()
    {
        GameObject bakilan = raycastSistemi.BakilanObje;
        if (bakilan == null) return false;

        KesmeTahtasiSistemi tahta = bakilan.GetComponent<KesmeTahtasiSistemi>()
            ?? bakilan.GetComponentInParent<KesmeTahtasiSistemi>()
            ?? bakilan.GetComponentInChildren<KesmeTahtasiSistemi>();

        return tahta != null && tahta == bekleyenTahta;
    }

    /// <summary>
    /// Süre dolunca kesimi tamamla: nesneyi dönüştür, animasyonu durdur.
    /// Radial'ı yönetici (OnTamamlandi) gizler. Ses doğal olarak biter (durdurulmaz).
    /// </summary>
    private void KesimiTamamla()
    {
        NesneyiDonustur(bekleyenTahta, bekleyenKaynak, bekleyenTarif);

        if (bicakAnimator != null)
            bicakAnimator.SetBool(animasyonBool, false);

        Temizle();
    }

    /// <summary>
    /// Kesimi iptal et: kesme sesini durdur, animasyon Idle'a dönsün, radial gizlensin.
    /// </summary>
    private void Iptal()
    {
        KesmeSesiniDurdur();

        tutmaYoneticisi?.Iptal();

        if (bicakAnimator != null)
            bicakAnimator.SetBool(animasyonBool, false);

        Temizle();
    }

    /// <summary>
    /// Yarıda bırakılınca çalan kesme sesini susturur.
    /// Sadece hâlâ bizim klibimizi çalıyorsa durdurur (havuzdaki kaynak başka sese verilmişse dokunmaz).
    /// </summary>
    private void KesmeSesiniDurdur()
    {
        if (kesmeSesKaynagi != null && kesmeSesKaynagi.isPlaying && kesmeSesKaynagi.clip == kesmeSesKlip)
            kesmeSesKaynagi.Stop();

        kesmeSesKaynagi = null;
        kesmeSesKlip = null;
    }

    private void Temizle()
    {
        kesimBekliyor = false;
        bekleyenTahta = null;
        bekleyenKaynak = null;
        bekleyenTarif = null;
        kesmeSesKaynagi = null;
        kesmeSesKlip = null;
    }

    private void NesneyiDonustur(KesmeTahtasiSistemi tahta, GameObject kaynak, KesmeTarifi tarif)
    {
        if (tarif.cikisPrefab == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[KesmeSistemi] '{tarif.name}' tarifinde çıkış prefab'ı yok!");
#endif
            return;
        }

        Transform kaynakParent = kaynak.transform.parent;
        Vector3 pos = kaynak.transform.position;
        Quaternion rot = kaynak.transform.rotation;

        // Yeni nesneyi oluştur
        GameObject yeniNesne = Instantiate(tarif.cikisPrefab, pos, rot);

        // Aynı parent'a bağla (slot)
        if (kaynakParent != null)
        {
            yeniNesne.transform.SetParent(kaynakParent, true);
            yeniNesne.transform.localPosition = Vector3.zero;
            yeniNesne.transform.localRotation = Quaternion.identity;
        }

        // Fizik - tahtada kalsın
        if (yeniNesne.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Slot güncelle sonra kaynağı sil
        tahta.SlottakiNesneyiDegistir(kaynak, yeniNesne);
        Destroy(kaynak);

#if UNITY_EDITOR
        Debug.Log($"[KesmeSistemi] '{kaynak.name}' → '{yeniNesne.name}' dönüştürüldü.");
#endif
    }

    public bool KesimDevamEdiyorMu => kesimBekliyor;
}
