using System;
using UnityEngine;

// ===== DEMO GUN SISTEMI (kaldirilabilir) =====
// Bu dosyanin TAMAMI demoya aittir. Demo bitince "Gun" klasorunu silmen yeterli.

/// <summary>
/// DEMONUN GUN BEYNI.
///
/// GunYoneticisi'ne HIC DOKUNMAZ; onun mevcut "GunYenidenBaslatildi" event'ine abone olur.
/// Yani bu obje sahneden silinirse oyun eski haliyle calismaya devam eder:
///   - Siparis oranlari SiparisUretici'nin kendi Inspector degerlerinde kalir,
///   - Magazada hicbir buton kilitli olmaz.
///
/// NE YAPAR:
/// 1. Oyun basinda 1. gunun ayarlarini uygular.
/// 2. Her yeni gunde o gunun ayarlarini uygular (siparis oranlari + spawn araligi).
/// 3. Magazadaki kilitleri tazeler (hangi urun kacinci gunde acilir -> magazada yazar).
/// 4. Son gun bitince DemoBitti event'ini tetikler (final ekrani buna abone olabilir).
///
/// KURULUM:
/// 1. Sahnede bos bir GameObject olustur, adini "DemoGunSistemi" yap -> bu scripti ekle.
/// 2. "Gun Plani" alanina GunPlani asset'ini surukle. (Bos birakirsan siparis oranlari
///    degismez, ama magaza kilitleri yine calisir.)
/// 3. Diger referanslari bos birakabilirsin, sahnede otomatik bulunur.
/// 4. "Toplam Gun" = 5. Sinirsiz istiyorsan 0 yaz.
/// </summary>
public class DemoGunSistemi : MonoBehaviour
{
    public static DemoGunSistemi Instance { get; private set; }

    [Header("Plan")]
    [Tooltip("Gunluk siparis/musteri ayarlarinin durdugu asset. BOS BIRAKILABILIR: " +
             "o zaman siparis oranlari degismez, sadece magaza kilitleri calisir.")]
    [SerializeField] private GunPlani gunPlani;

    [Header("Bagli Sistemler (bos birakirsan otomatik bulunur)")]
    [Tooltip("Gun sayacini okudugumuz cekirdek sistem. Bu scriptin ona hicbir etkisi yok.")]
    [SerializeField] private GunYoneticisi gunYoneticisi;

    [Tooltip("Gunluk siparis oranlarinin uygulanacagi uretici.")]
    [SerializeField] private SiparisUretici siparisUretici;

    [Tooltip("Gunluk spawn araliginin uygulanacagi uretici.")]
    [SerializeField] private MusteriUretici musteriUretici;

    [Tooltip("Kilitlerin tazelenecegi magaza UI'i.")]
    [SerializeField] private UIYerlestirmeSistemi magaza;

    [Header("Demo Siniri")]
    [Tooltip("Demo kac gun surecek? Son gun bitince DemoBitti tetiklenir. 0 = SINIRSIZ (demo siniri yok).")]
    [Min(0)] [SerializeField] private int toplamGun = 5;

    [Header("Sesler (opsiyonel)")]
    [Tooltip("Yeni gun basladiginda calar. Oyunun ilk aciliskinda CALMAZ.")]
    [SerializeField] private SesVerisi yeniGunSesi;

    [Tooltip("Son gun bitince (demo tamamlaninca) calar.")]
    [SerializeField] private SesVerisi demoBittiSesi;

    [Header("Teshis")]
    [Tooltip("Her gun gecisinde Console'a hangi ayarlarin uygulandigini yazar.")]
    [SerializeField] private bool teshisLogu = true;

    /// <summary>Kacinci gundeyiz. GunYoneticisi yoksa guvenli varsayilan: 1.</summary>
    public int Gun => gunYoneticisi != null ? gunYoneticisi.Gun : 1;

    /// <summary>Demo kac gun? 0 = sinirsiz.</summary>
    public int ToplamGun => toplamGun;

    /// <summary>Su an demonun SON gunu mu? (Sinirsiz modda hep false.)</summary>
    public bool SonGunMu => toplamGun > 0 && Gun >= toplamGun;

    /// <summary>Demo bitti mi? (son gun de tamamlandi)</summary>
    public bool DemoBittiMi => toplamGun > 0 && Gun > toplamGun;

    /// <summary>Yeni gunun ayarlari uygulandiginda tetiklenir (gun numarasiyla). UI abone olabilir.</summary>
    public event Action<int> GunAyariUygulandi;

    /// <summary>Son gun de bitince BIR KEZ tetiklenir. Final ekrani buna abone olur.</summary>
    public event Action DemoBitti;

    private bool demoBittiBildirildi;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[DemoGunSistemi] Sahnede birden fazla DemoGunSistemi var. Fazlasi yok sayildi.", this);
            Destroy(this);
        }
    }

    void Start()
    {
        if (Instance != this) return; // fazlalik kopya: hicbir sey yapma

        ReferanslariBul();

        if (gunYoneticisi != null)
        {
            gunYoneticisi.GunYenidenBaslatildi += YeniGunBasladi;
        }
        else
        {
            Debug.LogWarning("[DemoGunSistemi] GunYoneticisi bulunamadi! Gun gecisleri izlenemeyecek, " +
                             "sadece 1. gun ayarlari uygulanacak.", this);
        }

        // Oyunun ilk acilisi: 1. gunun ayarlarini uygula (ses CALMAZ).
        GunuUygula(Gun, false);
    }

    void OnDestroy()
    {
        if (gunYoneticisi != null)
            gunYoneticisi.GunYenidenBaslatildi -= YeniGunBasladi;

        if (Instance == this) Instance = null;
    }

    void ReferanslariBul()
    {
        if (gunYoneticisi == null)
            gunYoneticisi = GunYoneticisi.Instance != null ? GunYoneticisi.Instance : FindObjectOfType<GunYoneticisi>();

        if (siparisUretici == null) siparisUretici = FindObjectOfType<SiparisUretici>();
        if (musteriUretici == null) musteriUretici = FindObjectOfType<MusteriUretici>();
        if (magaza == null) magaza = FindObjectOfType<UIYerlestirmeSistemi>();
    }

    /// <summary>GunYoneticisi yeni gunu baslattiginda cagrilir (event).</summary>
    void YeniGunBasladi()
    {
        GunuUygula(Gun, true);
    }

    /// <summary>
    /// Verilen gunun ayarlarini butun sistemlere uygular.
    /// Plan yoksa siparis oranlarina DOKUNULMAZ, magaza kilitleri yine tazelenir.
    /// </summary>
    void GunuUygula(int gun, bool sesCal)
    {
        // 1) Magaza kilitleri plandan BAGIMSIZ: her gun tazelenir.
        //    (Hangi urun kacinci gunde acilir bilgisi magazanin kendi icinde.)
        if (magaza != null) magaza.KilitleriTazele();

        // 2) Gunluk siparis / musteri ayarlari (plan varsa)
        GunAyari ayar = gunPlani != null ? gunPlani.GunAl(gun) : null;

        if (ayar != null)
        {
            if (siparisUretici != null) siparisUretici.GunAyariniUygula(ayar);

            if (musteriUretici != null && ayar.spawnAraligi > 0f)
                musteriUretici.SpawnAraligiAyarla(ayar.spawnAraligi);
        }

        if (sesCal && yeniGunSesi != null && SesYoneticisi.Instance != null)
            SesYoneticisi.Instance.SesCal2D(yeniGunSesi);

        if (teshisLogu)
        {
            string ad = ayar != null ? ayar.gunAdi : "(plan yok)";
            Debug.Log($"[DemoGunSistemi] GUN {gun}{(toplamGun > 0 ? " / " + toplamGun : "")} -> {ad}\n" +
                      $"  siparis oranlari : {(ayar != null && siparisUretici != null ? "uygulandi" : "DEGISMEDI")}\n" +
                      $"  spawn araligi    : {(ayar != null && ayar.spawnAraligi > 0f ? ayar.spawnAraligi + " sn" : "dokunulmadi")}\n" +
                      $"  magaza kilitleri : {(magaza != null ? "tazelendi" : "magaza bulunamadi")}", this);
        }

        GunAyariUygulandi?.Invoke(gun);

        // 3) Demo bitti mi? (son gun de tamamlandiysa bir kez haber ver)
        DemoBitisiniKontrolEt();
    }

    void DemoBitisiniKontrolEt()
    {
        if (demoBittiBildirildi || !DemoBittiMi) return;

        demoBittiBildirildi = true;

        if (demoBittiSesi != null && SesYoneticisi.Instance != null)
            SesYoneticisi.Instance.SesCal2D(demoBittiSesi);

        if (teshisLogu)
            Debug.Log($"[DemoGunSistemi] DEMO BITTI! ({toplamGun} gun tamamlandi)", this);

        DemoBitti?.Invoke();
    }

    // ================== TEST (sadece editorde) ==================

    /// <summary>Play modunda Inspector'dan sag tik -> gunu bitirip sonrakine gecer.</summary>
    [ContextMenu("Test: Gunu Atla")]
    void TestGunuAtla()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[DemoGunSistemi] Bu test sadece Play modunda calisir.", this);
            return;
        }

        if (gunYoneticisi == null) gunYoneticisi = FindObjectOfType<GunYoneticisi>();
        if (gunYoneticisi == null)
        {
            Debug.LogWarning("[DemoGunSistemi] GunYoneticisi yok, gun atlanamadi.", this);
            return;
        }

        gunYoneticisi.GunuBitirVeBaslat();
    }

    /// <summary>Ayarlari elle yeniden uygular (plan asset'ini Play modunda degistirdiysen).</summary>
    [ContextMenu("Test: Ayarlari Yeniden Uygula")]
    void TestYenidenUygula()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[DemoGunSistemi] Bu test sadece Play modunda calisir.", this);
            return;
        }

        GunuUygula(Gun, false);
    }
}
// ===== DEMO GUN SISTEMI SONU =====
