using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DEPO ALANI. Sahnedeki bir kupun (ya da herhangi bir kutu bolgesinin) uzerine eklenir.
/// Bir noktanin / objenin bu kutunun ICINDE olup olmadigini soyler.
///
/// Tespit fizik/trigger KULLANMAZ: noktayi kupun kendi koordinat sistemine cevirir
/// (InverseTransformPoint) ve kutunun yari-boyutunu asip asmadigina bakar. Boylece kup
/// dondurulse/olceklense bile dogru calisir ve sadece sorulunca (gun sonunda) hesaplanir.
///
/// ONEMLI: Obje testi (NesneIcerdeMi) sadece pivot noktasina bakmaz; objenin GOVDE
/// merkezine (renderer/collider bounds) de bakar. Bir kasanin pivotu tabanda ya da
/// disarida olsa bile, govdesi kutunun icindeyse ICERIDE sayilir.
///
/// KURULUM:
/// 1. Sahneye bir kup koy, bu scripti ekle.
/// 2. Kupun COLLIDER'INI SIL (ya da Is Trigger yap) -> hem oyuncu hem esyalar icine girer.
///    Collider yoksa kutu boyutu "elleBoyut" x transform olcegi olur (1,1,1 = standart kup).
/// 3. Sahnede YESIL kutuyu (gizmo) gorursun. Esyalarin bu kutunun ICINDE kaldigindan emin ol.
/// 4. Kenarda kalanlar icin "pay" degerini artirabilirsin.
/// </summary>
public class DepoAlani : MonoBehaviour
{
    /// <summary>Sahnedeki aktif tum depolar (KayitYoneticisi bunlari sorgular).</summary>
    public static readonly List<DepoAlani> Aktifler = new List<DepoAlani>();

    [Header("Kutu Olcusu")]
    [Tooltip("Varsa buradan okunur. Bos birakirsan ayni objedeki BoxCollider aranir; " +
             "o da yoksa asagidaki elle degerler kullanilir.")]
    [SerializeField] private BoxCollider kutu;

    [Tooltip("Collider YOKKEN kullanilacak kutu boyutu (local). Transform olcegiyle carpilir. " +
             "1,1,1 = standart Unity kupu.")]
    [SerializeField] private Vector3 elleBoyut = Vector3.one;

    [Tooltip("Collider YOKKEN kullanilacak kutu merkezi (local).")]
    [SerializeField] private Vector3 elleMerkez = Vector3.zero;

    [Header("Tolerans")]
    [Tooltip("Kutunun her yonune eklenen pay (local birim). Kenarda kalan esyalar da " +
             "icerde sayilsin istiyorsan artir.")]
    [SerializeField] private float pay = 0.1f;

    [Header("Gizmo (Editor gorseli)")]
    [SerializeField] private bool gizmoGoster = true;
    [SerializeField] private Color gizmoRenk = new Color(0f, 1f, 0f, 0.20f);

    void Awake()
    {
        if (kutu == null) kutu = GetComponent<BoxCollider>();
    }

    void OnEnable()
    {
        if (!Aktifler.Contains(this)) Aktifler.Add(this);
    }

    void OnDisable()
    {
        Aktifler.Remove(this);
    }

    /// <summary>Kutunun merkezi (local). Collider varsa ondan, yoksa elle degerden.</summary>
    public Vector3 Merkez => kutu != null ? kutu.center : elleMerkez;

    /// <summary>Kutunun boyutu (local). Collider varsa ondan, yoksa elle degerden.</summary>
    public Vector3 Boyut => kutu != null ? kutu.size : elleBoyut;

    /// <summary>Kutunun DUNYA olcusundeki boyutu (teshis icin).</summary>
    public Vector3 DunyaBoyut => Vector3.Scale(Boyut, transform.lossyScale);

    /// <summary>Kutunun DUNYA merkezi (teshis icin).</summary>
    public Vector3 DunyaMerkez => transform.TransformPoint(Merkez);

    /// <summary>Verilen dunya noktasinin kutu merkezine gore LOCAL konumu (teshis icin).</summary>
    public Vector3 LokalKonum(Vector3 dunyaNoktasi)
    {
        return transform.InverseTransformPoint(dunyaNoktasi) - Merkez;
    }

    /// <summary>Verilen dunya noktasi bu kutunun icinde mi?</summary>
    public bool IcerdeMi(Vector3 dunyaNoktasi)
    {
        Vector3 local = LokalKonum(dunyaNoktasi);
        Vector3 yariBoyut = Boyut * 0.5f + Vector3.one * pay;

        return Mathf.Abs(local.x) <= yariBoyut.x
            && Mathf.Abs(local.y) <= yariBoyut.y
            && Mathf.Abs(local.z) <= yariBoyut.z;
    }

    /// <summary>
    /// Obje bu kutunun icinde mi? Once GOVDE merkezine (renderer/collider bounds),
    /// o yoksa pivota bakar. Ikisinden biri icerdeyse ICERIDE sayilir (bagislayici).
    /// </summary>
    public bool NesneIcerdeMi(GameObject o)
    {
        if (o == null) return false;

        if (IcerdeMi(o.transform.position)) return true;

        if (GovdeMerkezi(o, out Vector3 merkez))
            return IcerdeMi(merkez);

        return false;
    }

    /// <summary>Verilen nokta sahnedeki HERHANGI bir deponun icinde mi?</summary>
    public static bool NoktaBirDepodaMi(Vector3 dunyaNoktasi)
    {
        for (int i = 0; i < Aktifler.Count; i++)
            if (Aktifler[i] != null && Aktifler[i].IcerdeMi(dunyaNoktasi))
                return true;
        return false;
    }

    /// <summary>Obje sahnedeki HERHANGI bir deponun icinde mi? (govde merkezi dahil)</summary>
    public static bool NesneBirDepodaMi(GameObject o)
    {
        for (int i = 0; i < Aktifler.Count; i++)
            if (Aktifler[i] != null && Aktifler[i].NesneIcerdeMi(o))
                return true;
        return false;
    }

    /// <summary>
    /// Objenin (ve child'larinin) gorsel govde merkezini bulur.
    /// Once Renderer, yoksa Collider bounds kullanilir. Gunde bir kez calisir.
    /// </summary>
    public static bool GovdeMerkezi(GameObject o, out Vector3 merkez)
    {
        merkez = Vector3.zero;
        if (o == null) return false;

        Bounds b = new Bounds();
        bool ilk = true;

        Renderer[] renderers = o.GetComponentsInChildren<Renderer>(false);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            if (ilk) { b = renderers[i].bounds; ilk = false; }
            else b.Encapsulate(renderers[i].bounds);
        }

        if (ilk)
        {
            Collider[] colliders = o.GetComponentsInChildren<Collider>(false);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null) continue;
                if (ilk) { b = colliders[i].bounds; ilk = false; }
                else b.Encapsulate(colliders[i].bounds);
            }
        }

        if (ilk) return false;

        merkez = b.center;
        return true;
    }

    void OnDrawGizmos()
    {
        if (!gizmoGoster) return;

        // Editor'de (play disinda) Awake calismamis olabilir; collider'i burada da ara.
        BoxCollider bc = kutu != null ? kutu : GetComponent<BoxCollider>();
        Vector3 merkez = bc != null ? bc.center : elleMerkez;
        Vector3 boyut = bc != null ? bc.size : elleBoyut;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = gizmoRenk;
        Gizmos.DrawCube(merkez, boyut);
        Gizmos.color = new Color(gizmoRenk.r, gizmoRenk.g, gizmoRenk.b, 1f);
        Gizmos.DrawWireCube(merkez, boyut);
    }
}
