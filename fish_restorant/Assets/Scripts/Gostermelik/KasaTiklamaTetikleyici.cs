using UnityEngine;

/// <summary>
/// Doðrudan KASANIN üstüne eklenir. First-person (kilitli imleç) için
/// OnMouseDown çalýþmadýðýndan, mevcut RaycastSistemi'ni kullanýr:
/// crosshair bu objedeyken sol týk -> ses çalar + karakter sekansýný baþlatýr.
/// Opsiyonel olarak atanan bir resmi (GameObject) kapatýr.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class KasaTiklamaTetikleyici : MonoBehaviour
{
    [SerializeField] private KasaYuruyusSekansi karakterSekansi;
    [SerializeField] private AudioClip tiklamaSesi;
    [Range(0f, 1f)]
    [SerializeField] private float sesYuksekligi = 1f;

    [Tooltip("Týklanýnca kapatýlacak resim/obje (opsiyonel).")]
    [SerializeField] private GameObject kapatilacakResim;

    private AudioSource sesKaynagi;
    private RaycastSistemi raycast;

    void Awake()
    {
        sesKaynagi = GetComponent<AudioSource>();
        sesKaynagi.playOnAwake = false;
        raycast = FindObjectOfType<RaycastSistemi>();
    }

    void Update()
    {
        if (raycast == null || karakterSekansi == null) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (karakterSekansi.SekansAktif) return;
        if (!raycast.ObjeyeBakiyorMu) return;

        // Bakýlan obje bu kasa mý (kendisi ya da child'ý)?
        GameObject bakilan = raycast.BakilanObje;
        bool banaMiBakiyor = bakilan == gameObject ||
                             (bakilan != null && bakilan.transform.IsChildOf(transform));
        if (!banaMiBakiyor) return;

        if (tiklamaSesi != null)
            sesKaynagi.PlayOneShot(tiklamaSesi, sesYuksekligi);

        if (kapatilacakResim != null)
            kapatilacakResim.SetActive(false);

        karakterSekansi.SekansiBaslat();
    }

    void OnValidate()
    {
        if (sesKaynagi != null) sesKaynagi.volume = sesYuksekligi;
    }
}