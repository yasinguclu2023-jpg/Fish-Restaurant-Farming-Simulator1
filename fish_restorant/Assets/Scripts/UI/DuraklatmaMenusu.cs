using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ESC ile oyunu DURDURAN ve secilen paneli acan duraklatma menusu.
///
/// NE YAPAR:
/// - ESC -> Time.timeScale = 0 (musteriler, pisirme, bitkiler, saat... hepsi durur)
///   + secilen panel acilir + imlec serbest kalir + oyuncu kontrolleri kapanir.
/// - ESC tekrar / Devam butonu -> oyun kaldigi yerden devam eder.
/// - Ayarlar paneli acikken ESC once ayarlari kapatir, oyunu baslatmaz.
///
/// ENTEGRASYON:
/// Menu acikken UIYoneticisi.HerhangiBirUIAcikMi true olur, boylece mevcut
/// raycast / alma / sulama / pisirme sistemleri kendiliginden kilitlenir.
/// Hicbir baska scriptte degisiklik gerekmez.
///
/// KURULUM (tek adim):
/// 1. Sahnede bos bir GameObject olustur, adini "DuraklatmaMenusu" yap -> bu scripti ekle.
/// 2. "Duraklatma Paneli" alanina kendi panelini surukle (BASLANGICTA KAPALI birak).
///    BASKA HICBIR SEY ATAMANA GEREK YOK.
/// 3. Butonlar ve sesler opsiyoneldir, bos birakilabilir.
///
/// Durdurulacak scriptler OTOMATIK bulunur: kodda gomulu varsayilan listedeki
/// isimler sahnede taranir ve o an ACIK olanlar kapatilir. Devam edilince
/// sadece kapatilanlar geri acilir (baska sebeple kapali olanlara dokunulmaz).
/// </summary>
public class DuraklatmaMenusu : MonoBehaviour
{
    public static DuraklatmaMenusu Instance { get; private set; }

    [Header("Paneller")]
    [Tooltip("ESC ile acilacak ANA panel. BASLANGICTA KAPALI (SetActive false) birak.")]
    [SerializeField] private GameObject duraklatmaPaneli;

    [Tooltip("Opsiyonel alt panel (ayarlar/kontroller). Acikken ESC once bunu kapatir.")]
    [SerializeField] private GameObject ayarlarPaneli;

    [Header("Tus Ayari")]
    [Tooltip("Menuyu acip kapatan tus.")]
    [SerializeField] private KeyCode acKapaTusu = KeyCode.Escape;

    [Header("Duraklatilacak Scriptler (OTOMATIK)")]
    [Tooltip("Acik: asagidaki isim listesindeki tum scriptler sahnede OTOMATIK bulunup kapatilir. " +
             "Elle atama yapmana gerek yoktur.")]
    [SerializeField] private bool otomatikBul = true;

    [Tooltip("Listede OLMAYAN ek script isimleri (SINIF adi). Bos birakilabilir. " +
             "Asagidaki varsayilan liste zaten kodda gomulu, buraya tekrar yazmana gerek yok.")]
    [SerializeField] private string[] ekScriptAdlari;

    [Tooltip("Acik: duraklatmada hangi scriptlerin kapatildigini Console'a yazar. " +
             "Bir sey durmuyorsa bunu acip adini gor, sonra 'Ek Script Adlari' listesine ekle.")]
    [SerializeField] private bool teshisLogu = false;

    /// <summary>
    /// Duraklatmada kapatilacak scriptlerin VARSAYILAN listesi.
    /// KODDA gomulu (serialize EDILMEZ): boylece bu liste guncellendiginde
    /// sahnedeki mevcut component da otomatik yeni listeyi kullanir.
    /// DIKKAT: DOSYA adi degil, SINIF adi yazilir.
    /// (Ornek: CharacterController.cs dosyasindaki sinifin adi FPSController'dir.)
    /// </summary>
    private static readonly string[] VarsayilanScriptAdlari =
    {
        // --- Oyuncu kontrol / etkilesim ---
        "FirstPersonController",
        "FPSController",
        "RaycastSistemi",
        "NesneAlmaSistemi",
        "NesneYerlestirmeSistemi",
        "BasiliTutmaYoneticisi",
        "EldeNesneKamera",
        "EkmekSarmaSistemi",
        "KesmeSistemi",
        "BalikCevirmeSistemi",
        "BitkiSulamaSistemi",
        "BakisGorseliYoneticisi",

        // --- Olta / balik tutma ---
        "OltaAtisRotasyonu",
        "FishingSystem",
        "CharacterMovement",
        "FPPCameraSystem",
        "TPPCamera",
        "InteractionSystem",
        "SimpleUIManager",

        // --- Dunyadaki tiklanabilir sistemler ---
        "KapiSistemi",
        "TabelaSistemi",
        "ParaObjesi",
        "SiparisGosterici",
        "SiparisAlmaTiklama",
        "GunSonuEtkilesimi",
        "KalamarPisirmeEtkilesim",
        "MaterialGecis",

        // --- Diger UI / kamera ---
        "UIYoneticisi",
        "SinematikKamera",
        "GelistiriciHileleri"
    };

    [Tooltip("Listede olmayan, elle eklemek istedigin scriptler. Bos birakilabilir.")]
    [SerializeField] private MonoBehaviour[] ekstraScriptler;

    [Header("Butonlar (Opsiyonel)")]
    [Tooltip("Oyuna donus butonu.")]
    [SerializeField] private Button devamButonu;
    [Tooltip("Ayarlar panelini acan buton.")]
    [SerializeField] private Button ayarlarButonu;
    [Tooltip("Ayarlar panelinden ana menuye donus butonu.")]
    [SerializeField] private Button ayarlardanGeriButonu;
    [Tooltip("Oyundan cikis butonu.")]
    [SerializeField] private Button cikisButonu;

    [Header("Ses (Opsiyonel)")]
    [Tooltip("Menu acilirken calan ses.")]
    [SerializeField] private SesVerisi acilisSesi;
    [Tooltip("Menu kapanirken calan ses.")]
    [SerializeField] private SesVerisi kapanisSesi;
    [Tooltip("Butonlara basilinca calan ses.")]
    [SerializeField] private SesVerisi butonSesi;

    [Header("Ses Davranisi")]
    [Tooltip("Acik: menu acikken oyunun TUM sesleri susar (menu sesleri haric).")]
    [SerializeField] private bool oyunSesleriDeSussun = false;

    [Header("Durum (sadece izlemek icin)")]
    [SerializeField] private bool duraklatildi = false;

    /// <summary>Oyun su an duraklatilmis mi? Baska sistemler bunu kontrol edebilir.</summary>
    public static bool OyunDuraklatildiMi => Instance != null && Instance.duraklatildi;

    /// <summary>Bu ornegin duraklatma durumu.</summary>
    public bool DuraklatildiMi => duraklatildi;

    // Duraklatmadan onceki timeScale (SinematikKamera agir cekim yapiyorsa bozulmasin)
    private float oncekiTimeScale = 1f;

    // Menu sesleri icin OZEL kaynak: timeScale = 0 ve AudioListener.pause durumlarindan
    // etkilenmez. Bu yuzden havuzlu SesYoneticisi yerine burasi kullanilir.
    private AudioSource menuSesKaynagi;

    // Hizli arama icin isim listesinin HashSet hali (her duraklatmada yeniden kurulmaz)
    private readonly HashSet<string> aranacakAdlar = new HashSet<string>();

    // Duraklatmada KAPATILAN scriptler. Devam edince sadece bunlar geri acilir.
    private readonly List<MonoBehaviour> durdurulanScriptler = new List<MonoBehaviour>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[DuraklatmaMenusu] Sahnede birden fazla ornek var, fazlasi kapatildi.");
            enabled = false;
            return;
        }

        MenuSesKaynaginiHazirla();
        AranacakAdlariHazirla();
    }

    void AranacakAdlariHazirla()
    {
        aranacakAdlar.Clear();

        // Once kodda gomulu varsayilan liste
        for (int i = 0; i < VarsayilanScriptAdlari.Length; i++)
            aranacakAdlar.Add(VarsayilanScriptAdlari[i]);

        // Sonra Inspector'dan eklenen ekstra isimler
        if (ekScriptAdlari == null) return;

        for (int i = 0; i < ekScriptAdlari.Length; i++)
        {
            string ad = ekScriptAdlari[i];
            if (!string.IsNullOrWhiteSpace(ad))
                aranacakAdlar.Add(ad.Trim());
        }
    }

    void Start()
    {
        // Paneller baslangicta kapali olsun
        if (duraklatmaPaneli != null) duraklatmaPaneli.SetActive(false);
        if (ayarlarPaneli != null) ayarlarPaneli.SetActive(false);

        ButonlariBagla();

        if (duraklatmaPaneli == null)
            Debug.LogWarning("[DuraklatmaMenusu] Duraklatma Paneli atanmamis! Inspector alanina panelini surukle.");
    }

    void Update()
    {
        if (Input.GetKeyDown(acKapaTusu))
        {
            // Ayarlar panelindeysek once oraya geri don, oyunu baslatma
            if (duraklatildi && ayarlarPaneli != null && ayarlarPaneli.activeSelf)
            {
                AyarlardanGeri();
                return;
            }

            if (duraklatildi) Devam();
            else Duraklat();
        }
    }

    void OnDestroy()
    {
        if (Instance != this) return;

        // Sahne degisiminde oyun donuk kalmasin / sayac asili kalmasin
        if (duraklatildi)
        {
            duraklatildi = false;
            Time.timeScale = oncekiTimeScale;
            AudioListener.pause = false;
            UIYoneticisi.UIKapandiBildir();
        }

        Instance = null;
    }

    // ===================== ANA ISLEMLER =====================

    /// <summary>Oyunu durdurur ve paneli acar. Butonlardan da cagrilabilir.</summary>
    public void Duraklat()
    {
        if (duraklatildi) return;

        duraklatildi = true;

        // Mevcut hizi sakla (agir cekim vb. bozulmasin)
        oncekiTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;

        if (duraklatmaPaneli != null) duraklatmaPaneli.SetActive(true);
        if (ayarlarPaneli != null) ayarlarPaneli.SetActive(false);

        // Diger sistemler (alma, sulama, pisirme...) kendiliginden kilitlensin
        UIYoneticisi.UIAcildiBildir();

        // ONEMLI: Scriptler timeScale'den ONCE kapatilir.
        // SinematikKamera gibi bazi scriptler OnDisable icinde timeScale = 1 yaziyor;
        // sirayi ters yaparsak duraklatma aninda oyun geri baslar.
        ScriptleriDurdur();

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (oyunSesleriDeSussun) AudioListener.pause = true;

        MenuSesiCal(acilisSesi);
    }

    /// <summary>Oyunu kaldigi yerden devam ettirir. Devam butonuna bagla.</summary>
    public void Devam()
    {
        if (!duraklatildi) return;

        MenuSesiCal(kapanisSesi);

        duraklatildi = false;

        Time.timeScale = oncekiTimeScale;
        AudioListener.pause = false;

        if (duraklatmaPaneli != null) duraklatmaPaneli.SetActive(false);
        if (ayarlarPaneli != null) ayarlarPaneli.SetActive(false);

        UIYoneticisi.UIKapandiBildir();

        ScriptleriDevamEttir();

        // Baska bir UI paneli (Tab menusu vb.) hala acik olabilir; oyleyse imlec serbest kalsin
        if (!UIYoneticisi.HerhangiBirUIAcikMi)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>Duraklatma durumunu tersine cevirir (tus veya buton icin).</summary>
    public void DurumuDegistir()
    {
        if (duraklatildi) Devam();
        else Duraklat();
    }

    // ===================== AYARLAR PANELI =====================

    /// <summary>Ayarlar panelini acar, ana paneli gizler.</summary>
    public void AyarlariAc()
    {
        if (ayarlarPaneli == null) return;

        MenuSesiCal(butonSesi);
        ayarlarPaneli.SetActive(true);
        if (duraklatmaPaneli != null) duraklatmaPaneli.SetActive(false);
    }

    /// <summary>Ayarlar panelinden ana duraklatma paneline doner.</summary>
    public void AyarlardanGeri()
    {
        if (ayarlarPaneli == null) return;

        MenuSesiCal(butonSesi);
        ayarlarPaneli.SetActive(false);
        if (duraklatmaPaneli != null) duraklatmaPaneli.SetActive(true);
    }

    // ===================== CIKIS =====================

    /// <summary>Oyundan cikar. Cikis butonuna bagla.</summary>
    public void OyundanCik()
    {
        MenuSesiCal(butonSesi);

        // Editorde calisirken donuk kalmasin
        Time.timeScale = 1f;
        AudioListener.pause = false;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ===================== YARDIMCILAR =====================

    void ButonlariBagla()
    {
        if (devamButonu != null)
        {
            devamButonu.onClick.RemoveListener(Devam);
            devamButonu.onClick.AddListener(Devam);
        }

        if (ayarlarButonu != null)
        {
            ayarlarButonu.onClick.RemoveListener(AyarlariAc);
            ayarlarButonu.onClick.AddListener(AyarlariAc);
        }

        if (ayarlardanGeriButonu != null)
        {
            ayarlardanGeriButonu.onClick.RemoveListener(AyarlardanGeri);
            ayarlardanGeriButonu.onClick.AddListener(AyarlardanGeri);
        }

        if (cikisButonu != null)
        {
            cikisButonu.onClick.RemoveListener(OyundanCik);
            cikisButonu.onClick.AddListener(OyundanCik);
        }
    }

    /// <summary>
    /// Girdi okuyan scriptleri sahnede bulup kapatir ve hangilerini kapattigini saklar.
    /// Sadece O AN ACIK olanlar kapatilir; boylece devam ederken baska bir sebeple
    /// zaten kapali olan bir script yanlislikla acilmaz.
    /// </summary>
    void ScriptleriDurdur()
    {
        durdurulanScriptler.Clear();

        if (otomatikBul && aranacakAdlar.Count > 0)
        {
            MonoBehaviour[] hepsi = FindObjectsOfType<MonoBehaviour>();

            for (int i = 0; i < hepsi.Length; i++)
            {
                MonoBehaviour mb = hepsi[i];
                if (mb == null || mb == this || !mb.enabled) continue;

                if (aranacakAdlar.Contains(mb.GetType().Name))
                {
                    mb.enabled = false;
                    durdurulanScriptler.Add(mb);
                }
            }
        }

        if (ekstraScriptler != null)
        {
            for (int i = 0; i < ekstraScriptler.Length; i++)
            {
                MonoBehaviour mb = ekstraScriptler[i];
                if (mb == null || mb == this || !mb.enabled) continue;

                mb.enabled = false;
                durdurulanScriptler.Add(mb);
            }
        }

        if (teshisLogu)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[DuraklatmaMenusu] Kapatilan script sayisi: ")
              .Append(durdurulanScriptler.Count).Append(" -> ");

            for (int i = 0; i < durdurulanScriptler.Count; i++)
            {
                sb.Append(durdurulanScriptler[i].GetType().Name);
                if (i < durdurulanScriptler.Count - 1) sb.Append(", ");
            }

            Debug.Log(sb.ToString());
        }
    }

    /// <summary>Duraklatmada kapatilan scriptleri geri acar.</summary>
    void ScriptleriDevamEttir()
    {
        for (int i = 0; i < durdurulanScriptler.Count; i++)
        {
            if (durdurulanScriptler[i] != null)
                durdurulanScriptler[i].enabled = true;
        }

        durdurulanScriptler.Clear();
    }

    void MenuSesKaynaginiHazirla()
    {
        menuSesKaynagi = gameObject.AddComponent<AudioSource>();
        menuSesKaynagi.playOnAwake = false;
        menuSesKaynagi.spatialBlend = 0f;           // 2D
        menuSesKaynagi.ignoreListenerPause = true;  // AudioListener.pause iken de calar
    }

    void MenuSesiCal(SesVerisi ses)
    {
        if (ses == null || !ses.GecerliMi || menuSesKaynagi == null) return;

        AudioClip klip = ses.RastgeleKlipAl();
        if (klip == null) return;

        menuSesKaynagi.pitch = ses.RastgelePitch;
        menuSesKaynagi.PlayOneShot(klip, ses.SesSeviyesi);
    }
}
