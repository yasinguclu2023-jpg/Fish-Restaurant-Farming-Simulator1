using UnityEngine;

/// <summary>
/// Bir GostermelikSekans'ı tetikler. Bağımsız showcase için tasarlandı.
/// Tetikleme yöntemi seçilebilir: bakıp tıklama, tuş, trigger alanı veya manuel.
/// </summary>
[RequireComponent(typeof(GostermelikSekans))]
public class GostermelikTetikleyici : MonoBehaviour
{
    public enum TetiklemeTipi
    {
        BakipTikla,      // Ekran merkezinden ray; bu objeye bakıp sol tık
        TusaBas,         // Belirlenen tuşa basınca
        TetikleyiciAlan, // Belirli tag'li bir collider içeri girince (OnTriggerEnter)
        Manuel           // Sadece kod/UnityEvent ile (Tetikle() çağrısı)
    }

    [Header("Tetikleme")]
    [SerializeField] private TetiklemeTipi tetiklemeTipi = TetiklemeTipi.BakipTikla;

    [Header("BakipTikla Ayarları")]
    [SerializeField] private Camera oyuncuKamerasi;
    [Tooltip("Maksimum bakış/tıklama mesafesi")]
    [SerializeField] private float menzil = 3f;
    [Tooltip("Ray'in hangi layer'lara çarpacağı (boş bırakılırsa hepsi)")]
    [SerializeField] private LayerMask vurabilecegiLayerlar = ~0;

    [Header("TusaBas Ayarları")]
    [SerializeField] private KeyCode tus = KeyCode.E;

    [Header("TetikleyiciAlan Ayarları")]
    [Tooltip("Boş bırakılırsa her collider tetikler")]
    [SerializeField] private string gerekliTag = "Player";

    private GostermelikSekans sekans;
    private Collider kendiCollider;

    private static readonly Vector3 EkranMerkezi = new Vector3(0.5f, 0.5f, 0f);

    void Awake()
    {
        sekans = GetComponent<GostermelikSekans>();
        kendiCollider = GetComponent<Collider>();
    }

    void Start()
    {
        if (oyuncuKamerasi == null)
            oyuncuKamerasi = Camera.main;
    }

    void Update()
    {
        switch (tetiklemeTipi)
        {
            case TetiklemeTipi.BakipTikla:
                if (Input.GetMouseButtonDown(0) && BakilanObjeBuMu())
                    Tetikle();
                break;

            case TetiklemeTipi.TusaBas:
                if (Input.GetKeyDown(tus))
                    Tetikle();
                break;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (tetiklemeTipi != TetiklemeTipi.TetikleyiciAlan) return;
        if (!string.IsNullOrEmpty(gerekliTag) && !other.CompareTag(gerekliTag)) return;
        Tetikle();
    }

    /// <summary>
    /// Sekansı çalıştırır. Manuel modda da bu çağrılır.
    /// </summary>
    public void Tetikle()
    {
        if (sekans != null)
            sekans.Oynat();
    }

    bool BakilanObjeBuMu()
    {
        if (oyuncuKamerasi == null) return false;

        Ray ray = oyuncuKamerasi.ViewportPointToRay(EkranMerkezi);
        if (!Physics.Raycast(ray, out RaycastHit hit, menzil, vurabilecegiLayerlar))
            return false;

        // Çarpılan collider bu obje mi ya da çocuğu mu?
        if (kendiCollider != null && hit.collider == kendiCollider) return true;
        return hit.collider.transform.IsChildOf(transform);
    }
}
