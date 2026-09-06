using UnityEngine;

/// <summary>
/// Bitki büyüme + sulama döngüsü.
///
/// AKIŞ:
/// Ekildiğinde SUSUZ başlar -> sulanana kadar hiç büyümez ->
/// sulanınca aşama aşama büyür (1 -> 2 -> 3) -> son aşamada hasat hazır ->
/// hasat edilince ilk aşamaya döner ve TEKRAR SULAMA ister.
/// </summary>
public class BitkiBuyumeSistemi : MonoBehaviour
{
    [Header("Modeller")]
    [Tooltip("Sürekli görünür kalacak model (saksı, toprak vb.)")]
    [SerializeField] private GameObject sabitModel;

    [Tooltip("Sırayla açılacak büyüme aşamaları")]
    [SerializeField] private GameObject asamaModel1;
    [SerializeField] private GameObject asamaModel2;
    [SerializeField] private GameObject asamaModel3;

    [Header("Büyüme Ayarları")]
    [Tooltip("Her aşama arası geçen süre (saniye). Susuzken sayaç DURUR.")]
    [SerializeField] private float asamaSuresi = 10f;

    [Header("Hasat Ayarları")]
    [Tooltip("Hasat edildiğinde ele geçecek prefab")]
    [SerializeField] private GameObject hasatPrefab;

    [Tooltip("Hasat sonrası aşamalar arası süre (saniye)")]
    [SerializeField] private float yenidenBuyumeSuresi = 15f;

    [Tooltip("Hasattan sonra hangi aşamaya dönsün? 0 = boş saksı, 1 = 1. aşama modeli, 2 = 2. aşama")]
    [Range(0, 2)]
    [SerializeField] private int hasatSonrasiAsama = 1;

    [Header("Sulama Ayarları")]
    [Tooltip("Kapalı: bitki sulama beklemeden kendi kendine büyür (eski davranış).")]
    [SerializeField] private bool suGerekli = true;

    [Tooltip("Bu bitkiyi BİR KEZ sulamanın parası. Para yetmezse sulama başlamaz.")]
    [SerializeField] private int sulamaBedeli = 5;

    [Tooltip("Kapalı: bir sulama hasada kadar yeter. Açık: HER aşama için ayrı sulama ister.")]
    [SerializeField] private bool herAsamadaSulamaGerekli = false;

    [Tooltip("Bitki susuzken açılacak opsiyonel işaret (su damlası ikonu, solmuş model vb.)")]
    [SerializeField] private GameObject susuzIkonu;

    // Referanslar
    private NesneAlmaSistemi almaSistemi;
    private NesneYerlestirmeSistemi yerlestirmeSistemi;
    private EldeNesneKamera eldeNesneKamera;
    private BitkiSesVerisi bitkiSesVerisi;
    private Transform elPozisyonu;

    // Durum
    private int mevcutAsama = 0;
    private float zamanlayici = 0f;
    private float aktifAsamaSuresi;
    private bool hasatHazir = false;
    private bool buyumeAktif = true;
    private bool susuz = false;

    // Aşama listesi
    private GameObject[] asamalar;

    void Start()
    {
        almaSistemi = FindObjectOfType<NesneAlmaSistemi>();
        yerlestirmeSistemi = FindObjectOfType<NesneYerlestirmeSistemi>();
        eldeNesneKamera = FindObjectOfType<EldeNesneKamera>();

        TryGetComponent(out bitkiSesVerisi);

        ElPozisyonunuBul();

        asamalar = new GameObject[] { asamaModel1, asamaModel2, asamaModel3 };

        BaslangicAyarla();
    }

    void ElPozisyonunuBul()
    {
        if (hasatPrefab != null && almaSistemi != null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                foreach (Transform child in cam.transform)
                {
                    if (child.name.ToLower().Contains("el") || child.name.ToLower().Contains("hand"))
                    {
                        elPozisyonu = child;
                        break;
                    }
                }

                if (elPozisyonu == null)
                    elPozisyonu = cam.transform;
            }
        }
    }

    void BaslangicAyarla()
    {
        if (sabitModel != null)
            sabitModel.SetActive(true);

        mevcutAsama = 0;
        AsamaGoster(0);

        zamanlayici = 0f;
        aktifAsamaSuresi = asamaSuresi;
        hasatHazir = false;
        buyumeAktif = true;

        // Ekildiği an susuz: sulanmadan büyümeye başlamaz
        susuz = suGerekli;
        IkonGuncelle();
    }

    void Update()
    {
        if (!buyumeAktif) return;

        // Susuzken büyüme sayacı durur
        if (suGerekli && susuz) return;

        zamanlayici += Time.deltaTime;

        if (zamanlayici >= aktifAsamaSuresi)
        {
            zamanlayici = 0f;
            SonrakiAsama();
        }
    }

    /// <summary>0 = hiçbir aşama modeli açık değil (boş saksı), 1..3 = ilgili aşama modeli.</summary>
    void AsamaGoster(int asama)
    {
        if (asamalar == null) return;

        for (int i = 0; i < asamalar.Length; i++)
        {
            if (asamalar[i] != null)
                asamalar[i].SetActive(i == asama - 1);
        }
    }

    void SonrakiAsama()
    {
        if (mevcutAsama >= asamalar.Length) return;

        mevcutAsama++;
        AsamaGoster(mevcutAsama);

        // ===== SES ÇALMA - BÜYÜME AŞAMASI =====
        if (bitkiSesVerisi != null)
            bitkiSesVerisi.BuyumeAsamaSesiCal();
        // =======================================

        if (mevcutAsama >= asamalar.Length)
        {
            hasatHazir = true;
            buyumeAktif = false;

            // ===== SES ÇALMA - BÜYÜME TAMAM =====
            if (bitkiSesVerisi != null)
                bitkiSesVerisi.BuyumeTamamSesiCal();
            // ====================================
        }
        else if (herAsamadaSulamaGerekli)
        {
            SusuzYap();
        }
    }

    // ================== SULAMA ==================

    void SusuzYap()
    {
        if (!suGerekli) return;

        susuz = true;
        zamanlayici = 0f;
        IkonGuncelle();
    }

    /// <summary>
    /// BitkiSulamaSistemi sulama süresi dolunca çağırır. Büyüme sayacı buradan sonra işler.
    /// </summary>
    public void Sulandi()
    {
        if (!susuz) return;

        susuz = false;
        zamanlayici = 0f;
        IkonGuncelle();

        // ===== SES ÇALMA - SULAMA =====
        if (bitkiSesVerisi != null)
            bitkiSesVerisi.SulamaSesiCal();
        // ==============================
    }

    void IkonGuncelle()
    {
        if (susuzIkonu != null)
            susuzIkonu.SetActive(suGerekli && susuz);
    }

    // ================== HASAT ==================

    public bool HasatEt(Transform hedefElPozisyonu)
    {
        if (!hasatHazir || hasatPrefab == null) return false;

        if (yerlestirmeSistemi != null && yerlestirmeSistemi.YerlesimModuAktif)
            return false;

        if (hedefElPozisyonu != null)
            elPozisyonu = hedefElPozisyonu;

        if (elPozisyonu == null)
        {
            Debug.LogWarning("El pozisyonu bulunamadı!");
            return false;
        }

        // ===== SES ÇALMA - HASAT =====
        if (bitkiSesVerisi != null)
            bitkiSesVerisi.HasatSesiCal();
        // =============================

        GameObject hasat = Instantiate(hasatPrefab);

        Vector3 orijinalScale = hasat.transform.lossyScale;
        int orijinalLayer = hasat.layer;

        if (hasat.TryGetComponent(out Rigidbody rb))
            rb.isKinematic = true;

        if (hasat.TryGetComponent(out Collider col))
            col.enabled = false;

        hasat.transform.SetParent(elPozisyonu, false);
        hasat.transform.localPosition = Vector3.zero;
        hasat.transform.localRotation = Quaternion.identity;

        Vector3 parentScale = elPozisyonu.lossyScale;
        hasat.transform.localScale = new Vector3(
            orijinalScale.x / parentScale.x,
            orijinalScale.y / parentScale.y,
            orijinalScale.z / parentScale.z
        );

        if (eldeNesneKamera != null)
            eldeNesneKamera.NesneLayerAyarla(hasat);

        if (yerlestirmeSistemi != null)
            yerlestirmeSistemi.YerlestirmeyeBasla(hasat, orijinalScale, elPozisyonu, orijinalLayer);

        hasatHazir = false;
        YenidenBuyumeyeBasla();

        return true;
    }

    void YenidenBuyumeyeBasla()
    {
        mevcutAsama = Mathf.Clamp(hasatSonrasiAsama, 0, asamalar.Length);
        AsamaGoster(mevcutAsama);

        aktifAsamaSuresi = yenidenBuyumeSuresi;
        zamanlayici = 0f;
        buyumeAktif = true;

        // Hasattan sonra bitki tekrar susuz: oyuncu sulamadan büyümeye başlamaz
        if (suGerekli)
            SusuzYap();
        else
            IkonGuncelle();
    }

    // ================== DIŞARIYA AÇIK ==================

    public bool HasatHazirMi => hasatHazir;
    public int MevcutAsama => mevcutAsama;

    /// <summary>Şu an sulanabilir mi? (susuz + hasat aşamasında değil)</summary>
    public bool SulanabilirMi => suGerekli && susuz && !hasatHazir;
    public bool SusuzMu => susuz;
    public int SulamaBedeli => sulamaBedeli;
}
