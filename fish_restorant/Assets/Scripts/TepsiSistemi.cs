using UnityEngine;

/// <summary>
/// Tek bir tepsi prefab'�na eklenir.
/// Herhangi bir nesne tepsinin �st�ne serbest b�rak�l�r (vuru� noktas�nda kal�r).
/// Nesne otomatik olarak tepsinin child'� olur.
/// Tepsi al�n�rken t�m child nesneler birlikte gelir.
/// Nesne tepsiden al�n�rsa (child'dan ��karsa) tepsi otomatik "bo�" olarak g�ncellenir.
/// 
/// KURULUM:
/// 1. Tepsi prefab'�na bu scripti ekle
/// 2. Inspector'da kabulEdilenTagler'i ayarla (bo� b�rak�rsan hepsini kabul eder)
/// 3. Tepside collider olmal�
/// </summary>
public class TepsiSistemi : MonoBehaviour, ICopEtkilesimi
{
    [Header("Tepsi Ayarlar�")]
    [Tooltip("Kabul edilen tag'ler.\nBo� b�rak�l�rsa her �eyi kabul eder.")]
    [SerializeField] private string[] kabulEdilenTagler;

    [Header("Tepsi Tipi")]
    [Tooltip("Isaretliyse: TEK KULLANIMLIK tepsi (makineden cikan bardak tepsisi gibi).\n" +
             "- Cop kovasina atilinca icindekilerle birlikte KOMPLE yok olur\n" +
             "- Tepsi yiginina geri konamaz (sessizce reddedilir)")]
    [SerializeField] private bool tekKullanimlik = false;

    [Header("Ses")]
    [Tooltip("Nesne konuldu�unda �alan ses")]
    [SerializeField] private AudioClip nesneKonulmaSesi;

    [Tooltip("Ses seviyesi")]
    [Range(0f, 1f)]
    [SerializeField] private float sesSeviyesi = 1f;

    // Ba�lang��taki child say�s� (tepsi'nin kendi mesh/collider child'lar�)
    private int baslangicChildSayisi;

    void Start()
    {
        // Tepsi'nin kendi child'lar�n� say (mesh, collider vb.)
        // Bunlar "yerle�en nesne" de�il, tepsi'nin par�as�
        baslangicChildSayisi = transform.childCount;
    }

    /// <summary>
    /// Tepsiye sonradan eklenmi� nesne var m�?
    /// (Child say�s� ba�lang��tan fazlaysa nesne var demektir)
    /// </summary>
    public bool EkmekVarMi => transform.childCount > baslangicChildSayisi;

    /// <summary>
    /// Tepsideki eklenen nesne say�s�
    /// </summary>
    public int NesneSayisi => transform.childCount - baslangicChildSayisi;

    /// <summary>
    /// Tek kullanimlik tepsi mi? (Cope atilinca komple yok olur, yigina geri konamaz)
    /// </summary>
    public bool TekKullanimlik => tekKullanimlik;

    /// <summary>
    /// Bu nesne tepsiye konabilir mi?
    /// </summary>
    public bool NesneKabulEdilirMi(GameObject nesne)
    {
        if (nesne == null) return false;

        if (kabulEdilenTagler == null || kabulEdilenTagler.Length == 0)
            return true;

        string nesneTag = nesne.tag;
        for (int i = 0; i < kabulEdilenTagler.Length; i++)
        {
            if (kabulEdilenTagler[i] == nesneTag)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Nesneyi tepsiye koy (vuru� noktas�na, �zg�r pozisyon, child olur)
    /// </summary>
    public bool NesneYerlestir(GameObject nesne, Vector3 orijinalScale, int orijinalLayer, Vector3 vurusNoktasi, Quaternion rotasyon)
    {
        if (!NesneKabulEdilirMi(nesne)) return false;

        // Fizik
        if (nesne.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        if (nesne.TryGetComponent(out Collider col))
        {
            col.enabled = true;
            col.isTrigger = false;
        }

        // �nce world pozisyona koy
        nesne.transform.SetParent(null);
        nesne.transform.position = vurusNoktasi;
        nesne.transform.rotation = rotasyon;
        nesne.transform.localScale = orijinalScale;

        // Mesh'in alt noktas�n� hesapla (tepsiye g�m�lmesin)
        // Nesnenin kendi yerlesim ofseti: ince/duz objeler (fis gibi) tepsiyle ayni
        // duzlemde olup titremesin (z-fighting) diye YerlestirilebilirNesne.yereOfset
        // ile biraz yukari kaldirilir. Ofset 0 ise (varsayilan) hicbir sey degismez.
        Vector3 ekOfset = Vector3.zero;
        if (nesne.TryGetComponent(out YerlestirilebilirNesne yerlesimData)) ekOfset = yerlesimData.YereOfset;

        Renderer[] rendererlar = nesne.GetComponentsInChildren<Renderer>();
        if (rendererlar.Length > 0)
        {
            Bounds toplamBounds = rendererlar[0].bounds;
            for (int r = 1; r < rendererlar.Length; r++)
            {
                if (rendererlar[r] is ParticleSystemRenderer) continue;
                toplamBounds.Encapsulate(rendererlar[r].bounds);
            }
            float meshAltNokta = toplamBounds.min.y;
            float pivotY = nesne.transform.position.y;
            float offset = pivotY - meshAltNokta;
            nesne.transform.position = vurusNoktasi + Vector3.up * offset + ekOfset;
        }
        else
        {
            nesne.transform.position = vurusNoktasi + ekOfset;
        }

        // Tepsinin child'� yap
        nesne.transform.SetParent(transform, true);

        SetLayerRecursive(nesne, orijinalLayer);

        // Nesne artık tepsinin child'ı → outline'ı tepsiyle birleştir
        RaycastSistemi.HiyerarsiDegisti(nesne);

        // Ses
        if (nesneKonulmaSesi != null)
            AudioSource.PlayClipAtPoint(nesneKonulmaSesi, nesne.transform.position, sesSeviyesi);

        Debug.Log($"[Tepsi] Nesne tepsiye konuldu: {nesne.name} (Toplam: {NesneSayisi})");
        return true;
    }

    /// <summary>
    /// Cop kovasina atilinca: tepsiye eklenen nesneleri (baslangic child'larindan fazlasini)
    /// yok eder, tepsi kalir -> true doner (tepsi silinmez, sadece bosalir).
    /// Tek kullanimlik tepside ise false doner -> cop kovasi tepsiyi komple siler.
    /// </summary>
    public bool CopeAtildi()
    {
        // Tek kullanimlik tepsi: false donunce cop kovasi tepsiyi KOMPLE siler.
        // Icindeki urunler zaten child oldugu icin onlar da beraber gider.
        if (tekKullanimlik) return false;

        for (int i = transform.childCount - 1; i >= baslangicChildSayisi; i--)
            Destroy(transform.GetChild(i).gameObject);
        return true;
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}