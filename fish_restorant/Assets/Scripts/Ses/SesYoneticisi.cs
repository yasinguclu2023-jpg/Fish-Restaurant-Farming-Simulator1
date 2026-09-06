using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Merkezi ses y�neticisi. Sahnede bo� bir GameObject'e ekle.
/// AudioSource pooling ile GC allocation'� minimize eder.
/// </summary>
public class SesYoneticisi : MonoBehaviour
{
    public static SesYoneticisi Instance { get; private set; }

    [Header("Pool Ayarlar�")]
    [Tooltip("Ba�lang��ta olu�turulacak AudioSource say�s�")]
    [SerializeField] private int baslangicPoolBoyutu = 10;

    [Tooltip("Gerekti�inde pool'a eklenecek maksimum AudioSource")]
    [SerializeField] private int maksimumPoolBoyutu = 30;

    [Header("Genel Ayarlar")]
    [Range(0f, 1f)]
    [SerializeField] private float genelSesSeviyesi = 1f;

    // Pool
    private List<AudioSource> audioSourcePool;
    private Transform poolParent;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            PoolOlustur();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void PoolOlustur()
    {
        // Pool i�in parent obje
        poolParent = new GameObject("SesPool").transform;
        poolParent.SetParent(transform);

        audioSourcePool = new List<AudioSource>(baslangicPoolBoyutu);

        for (int i = 0; i < baslangicPoolBoyutu; i++)
        {
            AudioSourceOlustur();
        }
    }

    AudioSource AudioSourceOlustur()
    {
        GameObject obj = new GameObject("PooledAudio");
        obj.transform.SetParent(poolParent);

        AudioSource source = obj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        obj.SetActive(false);

        audioSourcePool.Add(source);
        return source;
    }

    AudioSource MusaitAudioSourceAl()
    {
        // M�sait olan� bul
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            if (!audioSourcePool[i].gameObject.activeInHierarchy)
            {
                return audioSourcePool[i];
            }
        }

        // M�sait yoksa ve limit a��lmad�ysa yeni olu�tur
        if (audioSourcePool.Count < maksimumPoolBoyutu)
        {
            return AudioSourceOlustur();
        }

        // Limit a��ld�ysa en eski �alan� kullan
        return audioSourcePool[0];
    }

    /// <summary>
    /// 3D pozisyonda ses �al. �alan AudioSource d�ner (erken durdurmak isteyen �a��ranlar i�in).
    /// Sesin do�al bitiminde havuz otomatik temizler; d�nen kayna�� saklay�p Stop() �a��rabilirsin.
    /// </summary>
    public AudioSource SesCal(SesVerisi sesVerisi, Vector3 pozisyon)
    {
        if (sesVerisi == null || !sesVerisi.GecerliMi) return null;

        AudioClip klip = sesVerisi.RastgeleKlipAl();
        if (klip == null) return null;

        AudioSource source = MusaitAudioSourceAl();

        // Pozisyona ta��
        source.transform.position = pozisyon;
        source.gameObject.SetActive(true);

        // Ayarlar� uygula
        source.clip = klip;
        source.volume = sesVerisi.SesSeviyesi * genelSesSeviyesi;
        source.pitch = sesVerisi.RastgelePitch;
        source.spatialBlend = sesVerisi.Uzamsallik;
        source.minDistance = sesVerisi.MinMesafe;
        source.maxDistance = sesVerisi.MaxMesafe;
        source.rolloffMode = AudioRolloffMode.Linear;

        source.Play();

        // Ses bitince pool'a geri d�n
        StartCoroutine(SesBitinceKapat(source, klip.length / source.pitch));

        return source;
    }

    /// <summary>
    /// 2D ses �al (UI sesleri, m�zik vs.)
    /// </summary>
    public void SesCal2D(SesVerisi sesVerisi)
    {
        if (sesVerisi == null || !sesVerisi.GecerliMi) return;

        AudioClip klip = sesVerisi.RastgeleKlipAl();
        if (klip == null) return;

        AudioSource source = MusaitAudioSourceAl();
        source.gameObject.SetActive(true);

        source.clip = klip;
        source.volume = sesVerisi.SesSeviyesi * genelSesSeviyesi;
        source.pitch = sesVerisi.RastgelePitch;
        source.spatialBlend = 0f; // 2D

        source.Play();

        StartCoroutine(SesBitinceKapat(source, klip.length / source.pitch));
    }

    /// <summary>
    /// Belirli bir Transform'dan ses �al (takip etmez, anl�k pozisyon)
    /// </summary>
    public void SesCal(SesVerisi sesVerisi, Transform hedef)
    {
        if (hedef == null) return;
        SesCal(sesVerisi, hedef.position);
    }

    System.Collections.IEnumerator SesBitinceKapat(AudioSource source, float sure)
    {
        yield return new WaitForSeconds(sure + 0.1f);

        if (source != null)
        {
            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Genel ses seviyesini ayarla (0-1)
    /// </summary>
    public void GenelSesAyarla(float seviye)
    {
        genelSesSeviyesi = Mathf.Clamp01(seviye);
    }

    /// <summary>
    /// T�m sesleri durdur
    /// </summary>
    public void TumSesleriDurdur()
    {
        foreach (var source in audioSourcePool)
        {
            if (source.isPlaying)
            {
                source.Stop();
                source.gameObject.SetActive(false);
            }
        }
    }

    public float GenelSesSeviyesi => genelSesSeviyesi;
}