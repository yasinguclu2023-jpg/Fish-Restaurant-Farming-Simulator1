using UnityEngine;
using OutlineFx;

public class RaycastSistemi : MonoBehaviour
{
    [Header("Raycast Ayarları")]
    [SerializeField] private Camera oyuncuKamerasi;
    [SerializeField] private float rayMenzili = 3f;
    [SerializeField] private LayerMask etkilesimLayer;
    [SerializeField] private LayerMask engelLayer;

    [Header("Görsel Ayarlar")]
    [SerializeField] private bool rayGoster = true;
    [SerializeField] private Color vurduRenk = Color.green;
    [SerializeField] private Color engelliRenk = Color.red;
    [SerializeField] private Color vurmadıRenk = Color.yellow;

    // Cache
    private RaycastHit hit;
    private GameObject mevcutObje;
    private GameObject oncekiObje;
    private OutlineFx.OutlineFx[] oncekiOutlines;
    private Ray ray;
    private bool engellendi;
    private static readonly Vector3 ViewportCenter = new Vector3(0.5f, 0.5f, 0f);

    /// <summary>
    /// Sahnedeki aktif raycast sistemi. Yerleştirme sistemleri, bakılan nesnenin
    /// hiyerarşisini değiştirdiğinde outline'ı tazelemek için buraya erişir.
    /// </summary>
    public static RaycastSistemi Aktif { get; private set; }

    void Awake()
    {
        Aktif = this;
    }

    void OnDestroy()
    {
        if (Aktif == this) Aktif = null;
    }

    void Start()
    {
        if (oyuncuKamerasi == null)
            oyuncuKamerasi = Camera.main;
    }

    void Update()
    {
        RaycastYap();
    }

    void RaycastYap()
    {
        ray = oyuncuKamerasi.ViewportPointToRay(ViewportCenter);

        mevcutObje = null;
        engellendi = false;

        LayerMask tumLayerlar = etkilesimLayer | engelLayer;

        if (Physics.Raycast(ray, out hit, rayMenzili, tumLayerlar))
        {
            GameObject vurulanObje = hit.collider.gameObject;
            int vurulanLayer = vurulanObje.layer;

            bool engelMi = ((1 << vurulanLayer) & engelLayer) != 0;
            bool etkilesimMi = ((1 << vurulanLayer) & etkilesimLayer) != 0;

            if (engelMi)
            {
                engellendi = true;
                mevcutObje = null;
            }
            else if (etkilesimMi)
            {
                mevcutObje = vurulanObje;
            }
        }

        OutlineGuncelle();

        if (rayGoster)
        {
            Color renk;
            float mesafe;

            if (mevcutObje != null)
            {
                renk = vurduRenk;
                mesafe = hit.distance;
            }
            else if (engellendi)
            {
                renk = engelliRenk;
                mesafe = hit.distance;
            }
            else
            {
                renk = vurmadıRenk;
                mesafe = rayMenzili;
            }

            Debug.DrawRay(ray.origin, ray.direction * mesafe, renk);
        }
    }

    void OutlineGuncelle()
    {
        if (mevcutObje == oncekiObje) return;

        // Önceki outline'ları kapat
        if (oncekiOutlines != null)
        {
            for (int i = 0; i < oncekiOutlines.Length; i++)
            {
                if (oncekiOutlines[i] != null)
                    oncekiOutlines[i].enabled = false;
            }
            oncekiOutlines = null;
        }

        // Yeni obje için outline bul
        if (mevcutObje != null)
        {
            // Root parent'ı bul
            Transform root = mevcutObje.transform;
            while (root.parent != null)
            {
                // Eğer parent da etkileşim layer'ındaysa yukarı çık
                if (((1 << root.parent.gameObject.layer) & etkilesimLayer) != 0)
                    root = root.parent;
                else
                    break;
            }

            // Root ve tüm child'lardaki OutlineFx'leri bul
            OutlineFx.OutlineFx[] outlines = root.GetComponentsInChildren<OutlineFx.OutlineFx>(true);

            // Kendisinde veya parent'ta da olabilir
            OutlineFx.OutlineFx parentOutline = root.GetComponent<OutlineFx.OutlineFx>();
            if (parentOutline == null)
                parentOutline = root.GetComponentInParent<OutlineFx.OutlineFx>();

            if (outlines.Length > 0)
            {
                for (int i = 0; i < outlines.Length; i++)
                {
                    outlines[i].enabled = true;
                }
                oncekiOutlines = outlines;
            }
            else if (parentOutline != null)
            {
                parentOutline.enabled = true;
                oncekiOutlines = new OutlineFx.OutlineFx[] { parentOutline };
            }
        }

        oncekiObje = mevcutObje;
    }

    /// <summary>
    /// Bakılan nesnenin hiyerarşisi değiştiğinde çağrılır (ekmeğe malzeme konması,
    /// tepsiye/tahtaya nesne konması vb.).
    ///
    /// Yeni gelen parçanın prefab'dan gelen açık outline'ı kapatılır, ardından outline
    /// listesi yeniden kurulur. Böylece oyuncu o an ana nesneye bakıyorsa yeni parça da
    /// aynı karede enable edilir ve tek birleşik outline olarak görünür.
    /// </summary>
    /// <param name="yeniNesne">Hiyerarşiye yeni eklenen nesne (yoksa null geçilebilir).</param>
    public static void HiyerarsiDegisti(GameObject yeniNesne = null)
    {
        if (yeniNesne != null)
        {
            OutlineFx.OutlineFx[] yeniOutlines = yeniNesne.GetComponentsInChildren<OutlineFx.OutlineFx>(true);
            for (int i = 0; i < yeniOutlines.Length; i++)
                yeniOutlines[i].enabled = false;
        }

        if (Aktif != null)
            Aktif.OutlineYenile();
    }

    /// <summary>
    /// Outline cache'ini geçersiz kılıp yeniden hesaplar.
    /// Bakılan obje değişmese bile child listesi baştan taranır.
    /// </summary>
    public void OutlineYenile()
    {
        oncekiObje = null;
        OutlineGuncelle();
    }

    /// <summary>
    /// Bakılan objede veya parent'larında BitkiBuyumeSistemi var mı kontrol eder
    /// </summary>
    public BitkiBuyumeSistemi BakilanBitki
    {
        get
        {
            if (mevcutObje == null) return null;

            // Önce kendisinde ara
            var bitki = mevcutObje.GetComponent<BitkiBuyumeSistemi>();
            if (bitki != null) return bitki;

            // Parent'larda ara
            return mevcutObje.GetComponentInParent<BitkiBuyumeSistemi>();
        }
    }

    public GameObject BakilanObje => mevcutObje;
    public bool ObjeyeBakiyorMu => mevcutObje != null;
    public float Mesafe => mevcutObje != null ? hit.distance : -1f;
    public Vector3 VurusNoktasi => mevcutObje != null ? hit.point : Vector3.zero;
    public string BakilanTag => mevcutObje != null ? mevcutObje.tag : "";
    public bool Engellendi => engellendi;
    public LayerMask EngelLayer => engelLayer;
}