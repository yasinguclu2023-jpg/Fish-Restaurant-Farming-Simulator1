using UnityEngine;

// ===== TUTORIAL (kaldirilabilir) =====

/// <summary>
/// TUTORIAL HEDEF IKONU - "su tarafa git" isareti.
///
/// Bu script SADECE konum isini yapar:
///   - Ikonu hedefin uzerine tasir ve hedefi takip eder,
///   - Havada asagi yukari salinir (goze carpsin diye),
///   - Oyuncu cok yaklasinca gizler,
///   - Tutorial gerektiginde acar/kapatir.
///
/// Diger iki is BASKA YERDEN gelir, burada kod yok:
///   KAMERAYA DONME  -> ayni objeye eklenen "KamerayaDonukIkon" bileseni
///                      (IkonYoneticisi hepsini tek Update'te dondurur - grass mantigi)
///   DUVAR ARKASINDAN GORUNME -> objenin "HedefIsareti" layer'inda olmasi
///                      + ana kamerada "HedefIsaretiKamera" scripti
///
/// KURULUM:
/// 1. Bir Quad olustur, materyalini ver, Mesh Collider'ini SIL.
/// 2. Layer'ini "HedefIsareti" yap.
/// 3. Bu scripti + "KamerayaDonukIkon" bilesenini ekle.
/// 4. Objeyi KAPALI birak.
/// 5. TutorialYoneticisi > "Ortak Isaret" alanina bu objeyi surukle.
/// </summary>
public class HedefIsareti : MonoBehaviour
{
    [Header("Konum")]
    [Tooltip("Hedefin tam uzerine degil, biraz yukarisina koymak icin ofset (metre).")]
    [SerializeField] private Vector3 dunyaOfset = new Vector3(0f, 2f, 0f);

    [Tooltip("ACIK: Hedef hareket ederse isaret pesinden gider.")]
    [SerializeField] private bool hedefiTakipEt = true;

    [Header("Salinim")]
    [Tooltip("Isaret asagi yukari suzulsun mu? Goze daha cok carpar.")]
    [SerializeField] private bool salinsin = true;

    [Tooltip("Salinim yuksekligi (metre).")]
    [SerializeField] private float salinimYuksekligi = 0.15f;

    [Tooltip("Salinim hizi.")]
    [SerializeField] private float salinimHizi = 2f;

    [Header("Gorunurluk")]
    [Tooltip("Oyuncu hedefe bu kadar yaklasinca isaret gizlenir (oraya zaten vardi). " +
             "0 = hicbir zaman gizlenmesin.")]
    [Min(0f)]
    [SerializeField] private float yakinsaGizlenmeMesafesi = 2.5f;

    [Header("Teshis")]
    [SerializeField] private bool teshisLogu = false;

    private Transform hedef;
    private Transform govde;
    private Camera kamera;
    private Renderer[] rendererlar;
    private Vector3 temelKonum;
    private bool cizimKapali;

    /// <summary>Su an bir hedefi isaret ediyor mu?</summary>
    public bool Aktif => hedef != null && gameObject.activeSelf;

    void Awake()
    {
        govde = transform;
        rendererlar = GetComponentsInChildren<Renderer>(true);

        if (rendererlar == null || rendererlar.Length == 0)
            Debug.LogWarning("[HedefIsareti] Objede hic Renderer yok; ikon gorunmez. " +
                             "Quad/Sprite kullandigindan emin ol.", this);
    }

    // ================== DISARIYA ACIK ==================

    /// <summary>Verilen noktayi isaretlemeye basla ve gorunur ol.</summary>
    public void Hedefe(Transform yeniHedef)
    {
        if (yeniHedef == null)
        {
            Gizle();
            return;
        }

        // AYNI hedef tekrar verildiyse dokunma: yoksa "yakinsa gizlenme" durumu
        // her cagrida sifirlanir ve ikon yanip soner.
        if (hedef == yeniHedef && gameObject.activeSelf) return;

        hedef = yeniHedef;

        if (govde == null) govde = transform;

        if (!gameObject.activeSelf) gameObject.SetActive(true);

        cizimKapali = false;
        CizimiAyarla(true);
        KonumuGuncelle(); // ilk kareyi hemen dogru yere koy, bir kare yanlis yerde gorunmesin

        if (teshisLogu)
            Debug.Log($"[HedefIsareti] Hedef: '{hedef.name}'", this);
    }

    /// <summary>Isareti tamamen kapat (hedefi birak).</summary>
    public void Gizle()
    {
        hedef = null;

        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    // ================== DONGU ==================

    void LateUpdate()
    {
        // LateUpdate: oyuncu/kamera hareketi bittikten SONRA -> isaret titremez.
        if (hedef == null) return;

        if (hedefiTakipEt || salinsin) KonumuGuncelle();

        MesafeKontrol();
    }

    void KonumuGuncelle()
    {
        if (hedef == null || govde == null) return;

        temelKonum = hedef.position + dunyaOfset;

        if (salinsin && salinimYuksekligi > 0f)
        {
            float y = Mathf.Sin(Time.time * salinimHizi) * salinimYuksekligi;
            govde.position = temelKonum + new Vector3(0f, y, 0f);
        }
        else
        {
            govde.position = temelKonum;
        }
    }

    /// <summary>Oyuncu hedefe cok yaklastiysa cizimi kapatir (obje aktif kalir).</summary>
    void MesafeKontrol()
    {
        if (yakinsaGizlenmeMesafesi <= 0f) return;

        if (kamera == null)
        {
            // Camera.main bu projede null olabilir (MainCamera tag'i yok) -> yedekli arama
            kamera = HedefIsaretiKamera.AnaKamerayiBul();
            if (kamera == null) return;
        }

        float mesafeKare = (kamera.transform.position - temelKonum).sqrMagnitude;
        bool cokYakin = mesafeKare <= yakinsaGizlenmeMesafesi * yakinsaGizlenmeMesafesi;

        if (cokYakin == cizimKapali) return;

        cizimKapali = cokYakin;
        CizimiAyarla(!cokYakin);
    }

    void CizimiAyarla(bool acik)
    {
        if (rendererlar == null) return;

        for (int i = 0; i < rendererlar.Length; i++)
            if (rendererlar[i] != null) rendererlar[i].enabled = acik;
    }
}
// ===== TUTORIAL SONU =====
