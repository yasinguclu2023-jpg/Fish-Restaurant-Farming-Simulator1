using System.Collections;
using UnityEngine;

/// <summary>
/// GUN SONU OBJESI. Bu scriptin bulundugu objeye (kendisi ya da child'i) crosshair ile
/// bakip sol tiklayinca:
///   - Gun BITTIYSE (saat bitisSaati'ne ulastiysa): onay sesi calar + gunu yeniden baslatir.
///   - Gun BITMEDIYSE: uyari sesi calar + (opsiyonel) kisa bir uyari mesaji gosterir.
///
/// Mevcut RaycastSistemi ile calisir (first-person kilitli imlec uyumlu).
/// ILERIDE: gunu bitirme ayni zamanda KAYIT (save) tetikleyecek (GunYoneticisi icinde TODO).
///
/// KURULUM:
/// 1. Basilacak 3D objenin (collider'i olan, RaycastSistemi'nin etkilesimLayer'inda) uzerine ekle.
/// 2. Sahnede GunYoneticisi olmali (bos birakirsan otomatik bulunur).
/// 3. Sesleri (onay/uyari) ve istersen uyariMesajiObjesi'ni ata.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class GunSonuEtkilesimi : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Bos birakirsan sahnede otomatik bulunur.")]
    [SerializeField] private GunYoneticisi gunYoneticisi;
    [Tooltip("Bos birakirsan sahnede otomatik bulunur.")]
    [SerializeField] private RaycastSistemi raycastSistemi;
    [Tooltip("Elde nesne var mi kontrolu icin. Bos birakirsan sahnede otomatik bulunur.")]
    [SerializeField] private NesneYerlestirmeSistemi yerlestirmeSistemi;

    [Header("Etkilesim")]
    [Tooltip("Bu objeye bakarken basilacak tus (varsayilan: sol fare).")]
    [SerializeField] private KeyCode etkilesimTusu = KeyCode.Mouse0;

    [Header("Sesler")]
    [Tooltip("Gun bittiginde basilinca calan onay sesi.")]
    [SerializeField] private AudioClip onaySesi;
    [Tooltip("Gun bitmeden basilinca calan uyari sesi.")]
    [SerializeField] private AudioClip uyariSesi;
    [Range(0f, 1f)]
    [SerializeField] private float sesYuksekligi = 1f;

    [Header("Uyari Mesaji (opsiyonel)")]
    [Tooltip("Gun bitmeden basilinca kisa sure gosterilecek UI/obje (orn. 'Gun henuz bitmedi'). Bos birakilabilir.")]
    [SerializeField] private GameObject uyariMesajiObjesi;
    [Tooltip("Uyari mesaji kac saniye ekranda kalsin.")]
    [SerializeField] private float uyariMesajiSuresi = 1.5f;

    private AudioSource sesKaynagi;
    private Coroutine uyariRutini;

    void Awake()
    {
        sesKaynagi = GetComponent<AudioSource>();
        sesKaynagi.playOnAwake = false;

        if (gunYoneticisi == null) gunYoneticisi = FindObjectOfType<GunYoneticisi>();
        if (raycastSistemi == null) raycastSistemi = FindObjectOfType<RaycastSistemi>();
        if (yerlestirmeSistemi == null) yerlestirmeSistemi = FindObjectOfType<NesneYerlestirmeSistemi>();

        if (uyariMesajiObjesi != null) uyariMesajiObjesi.SetActive(false);
    }

    void Update()
    {
        if (raycastSistemi == null || gunYoneticisi == null) return;
        if (!Input.GetKeyDown(etkilesimTusu)) return;

        // Elde nesne varken bu objeye etkilesim YOK (sessizce yok sayilir).
        if (EldeNesneVar()) return;

        if (!BuObjeyeMiBakiliyor()) return;

        if (gunYoneticisi.GunSonuMu)
        {
            SesCal(onaySesi);
            gunYoneticisi.GunuBitirVeBaslat();
        }
        else
        {
            // Gun daha bitmedi: basma engellenir, uyari verilir.
            SesCal(uyariSesi);
            UyariGoster();
        }
    }

    /// <summary>Oyuncunun elinde bir nesne (ya da aktif yerlestirme modu) var mi?</summary>
    bool EldeNesneVar()
    {
        if (yerlestirmeSistemi == null) return false;
        return yerlestirmeSistemi.YerlesimModuAktif || yerlestirmeSistemi.EldeNesneVarMi;
    }

    bool BuObjeyeMiBakiliyor()
    {
        GameObject bakilan = raycastSistemi.BakilanObje;
        if (bakilan == null) return false;
        return bakilan == gameObject || bakilan.transform.IsChildOf(transform);
    }

    void SesCal(AudioClip clip)
    {
        if (clip != null) sesKaynagi.PlayOneShot(clip, sesYuksekligi);
    }

    void UyariGoster()
    {
        if (uyariMesajiObjesi == null) return;
        if (uyariRutini != null) StopCoroutine(uyariRutini);
        uyariRutini = StartCoroutine(UyariRutini());
    }

    IEnumerator UyariRutini()
    {
        uyariMesajiObjesi.SetActive(true);
        yield return new WaitForSeconds(uyariMesajiSuresi);
        uyariMesajiObjesi.SetActive(false);
        uyariRutini = null;
    }
}
