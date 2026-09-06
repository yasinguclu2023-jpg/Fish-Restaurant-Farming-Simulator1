using UnityEngine;
using FishingGameTool.Fishing;

/// <summary>
/// Olta ses sistemi. FishingSystem ile ayn� objeye (Player) ekleyin.
/// 3 ses: At��, �eki� (loop), Yakalama
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class OltaSesSistemi : MonoBehaviour
{
    [Header("Ses Klipleri")]
    [Tooltip("Olta at�l�rken �alan ses")]
    [SerializeField] private AudioClip atisSesi;

    [Tooltip("Sa� t�k bas�l� tutarken �alan ses (loop)")]
    [SerializeField] private AudioClip cekisSesi;

    [Tooltip("Bal�k oltaya ilk tak�ld��� (�s�rd���) an �alan ses")]
    [SerializeField] private AudioClip balikTakildiSesi;

    [Tooltip("Bal�k yakaland���nda (tamamen �ekildi�inde) �alan ses")]
    [SerializeField] private AudioClip yakalamaSesi;

    [Header("Ses Ayarlar�")]
    [Range(0f, 1f)]
    [SerializeField] private float atisSesSeviyes = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float cekisSesSeviyes = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float balikTakildiSesSeviyes = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float yakalamaSesSeviyes = 1f;

    private AudioSource audioSource;
    private FishingSystem fishingSystem;

    private bool oncekiCekis = false;
    private bool oncekiSamandiraVar = false;
    private bool oncekiBalikTutuldu = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        fishingSystem = GetComponent<FishingSystem>();
        if (fishingSystem == null)
            fishingSystem = FindObjectOfType<FishingSystem>();
    }

    void Update()
    {
        if (fishingSystem == null || !fishingSystem.enabled)
        {
            CekisSesiDurdur();
            return;
        }

        AtisKontrol();
        CekisKontrol();
        YakalamaKontrol();
    }

    void AtisKontrol()
    {
        bool samandiraVar = fishingSystem._fishingRod != null &&
                            fishingSystem._fishingRod._fishingFloat != null;

        if (samandiraVar && !oncekiSamandiraVar)
            SesCalTekSefer(atisSesi, atisSesSeviyes);

        oncekiSamandiraVar = samandiraVar;
    }

    void CekisKontrol()
    {
        // Sadece �amand�ra varken �eki� sesi �als�n
        bool samandiraVar = fishingSystem._fishingRod != null &&
                            fishingSystem._fishingRod._fishingFloat != null;
        bool cekisAktif = fishingSystem._attractInput && samandiraVar;

        if (cekisAktif && !oncekiCekis)
            CekisSesiBaslat();
        else if (!cekisAktif && oncekiCekis)
            CekisSesiDurdur();

        oncekiCekis = cekisAktif;
    }

    void YakalamaKontrol()
    {
        bool balikTutuldu = fishingSystem._advanced != null && fishingSystem._advanced._caughtLoot;

        // false -> true: bal�k oltaya tak�ld� (�s�rd�) = "bal�k tuttuk" an�
        if (!oncekiBalikTutuldu && balikTutuldu)
        {
            SesCalTekSefer(balikTakildiSesi, balikTakildiSesSeviyes);
        }
        // true -> false: bal�k tamamen �ekildi/yakaland�
        else if (oncekiBalikTutuldu && !balikTutuldu)
        {
            SesCalTekSefer(yakalamaSesi, yakalamaSesSeviyes);
            CekisSesiDurdur();
        }

        oncekiBalikTutuldu = balikTutuldu;
    }

    void CekisSesiBaslat()
    {
        if (cekisSesi == null) return;

        audioSource.clip = cekisSesi;
        audioSource.volume = cekisSesSeviyes;
        audioSource.loop = true;
        audioSource.Play();
    }

    void CekisSesiDurdur()
    {
        if (audioSource.loop && audioSource.isPlaying)
        {
            audioSource.loop = false;
            audioSource.Stop();
        }
    }

    void SesCalTekSefer(AudioClip clip, float seviye)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip, seviye);
    }
}