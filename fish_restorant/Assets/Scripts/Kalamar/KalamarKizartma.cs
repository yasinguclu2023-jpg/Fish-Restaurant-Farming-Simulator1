using UnityEngine;

/// <summary>
/// Kýzartma makinesine eklenir.
/// Sepetten kalamar konulduðunda, sepet prefab'ýný boþ slot empty'sine instantiate eder.
/// Child kalamar mesh'ini bulup shader slider'ýný 0'dan 1'e çýkarýr.
/// Spawn olan sepet alýndýðýnda (destroy edilince veya parent deðiþince) particle/ses temizlenir.
/// </summary>
public class KalamarKizartma : MonoBehaviour
{
    [Header("Kalamar Prefab")]
    [Tooltip("Slot'a spawn edilecek prefab (içinde 'kalamar' isimli child mesh olmalý)")]
    [SerializeField] private GameObject kalamarPrefab;

    [Tooltip("Renderer aranýrken bu kelimeyi içeren child seçilir (büyük/küçük harf önemsiz)")]
    [SerializeField] private string rendererIsimAnahtari = "kalamar";

    [Header("Slot Noktalarý")]
    [Tooltip("Kalamarýn spawn olacaðý boþ empty transform'lar")]
    [SerializeField] private Transform[] slotNoktalari;

    [Header("Kabul Edilen Tag'ler")]
    [Tooltip("Elindeki nesnenin tag'i bunlardan biriyse yerleþtirilebilir. Boþ býrakýrsan hepsi.")]
    [SerializeField] private string[] kabulEdilenTagler;

    [Header("Piþme")]
    [Tooltip("Shader slider 0'dan 1'e kaç saniyede çýksýn")]
    [SerializeField] private float pismeSuresi = 5f;

    [Tooltip("Shader graph'taki piþme property'sinin Reference adý (örn. _kalamar)")]
    [SerializeField] private string shaderSliderAdi = "_kalamar";

    [Header("Efektler")]
    [SerializeField] private ParticleSystem particlePrefab;
    [SerializeField] private AudioClip konulmaSesi;
    [SerializeField] private AudioClip cizirtiSesi;

    [Header("Ses Ayarlarý")]
    [Range(0f, 1f)]
    [Tooltip("Loop cýzýrtý ses seviyesi")]
    [SerializeField] private float cizirtiSeviyesi = 0.5f;

    [Tooltip("Loop sesin tam ses çýkardýðý minimum mesafe")]
    [SerializeField] private float sesMinMesafe = 1f;

    [Tooltip("Loop sesin tamamen kesildiði maksimum mesafe")]
    [SerializeField] private float sesMaxMesafe = 15f;

    [Tooltip("Loop sesin uzaklýk eðrisi tipi")]
    [SerializeField] private AudioRolloffMode sesRolloff = AudioRolloffMode.Linear;

    [Tooltip("Loop ses 3D mi (1) yoksa 2D mi (0)")]
    [Range(0f, 1f)]
    [SerializeField] private float sesUzaysal = 1f;

    // Ýç durum
    private class SlotDurumu
    {
        public GameObject spawnEdilen;
        public Renderer renderer;
        public ParticleSystem particle;
        public float ilerleme;
        public bool aktif;
    }
    private SlotDurumu[] slotlar;

    private int shaderID;
    private MaterialPropertyBlock propBlock;
    private AudioSource loopAudio;
    private int doluSayisi;

    void Awake()
    {
        shaderID = Shader.PropertyToID(shaderSliderAdi);
        propBlock = new MaterialPropertyBlock();

        int n = slotNoktalari != null ? slotNoktalari.Length : 0;
        slotlar = new SlotDurumu[n];
        for (int i = 0; i < n; i++) slotlar[i] = new SlotDurumu();

        loopAudio = gameObject.AddComponent<AudioSource>();
        loopAudio.clip = cizirtiSesi;
        loopAudio.loop = true;
        loopAudio.playOnAwake = false;
        loopAudio.volume = cizirtiSeviyesi;
        loopAudio.spatialBlend = sesUzaysal;
        loopAudio.minDistance = sesMinMesafe;
        loopAudio.maxDistance = sesMaxMesafe;
        loopAudio.rolloffMode = sesRolloff;
    }

    void Update()
    {
        // Her zaman slotlarý kontrol et (alýnmýþ olabilir)
        bool degisiklikVar = false;
        for (int i = 0; i < slotlar.Length; i++)
        {
            var s = slotlar[i];
            if (!s.aktif) continue;

            // Spawn obje yok edildi veya parent'ý deðiþti (alýndý)
            if (s.spawnEdilen == null || s.spawnEdilen.transform.parent != slotNoktalari[i])
            {
                SlotuTemizle(s);
                degisiklikVar = true;
                continue;
            }

            // Piþme
            if (s.ilerleme < 1f && s.renderer != null)
            {
                s.ilerleme = Mathf.Clamp01(s.ilerleme + Time.deltaTime / pismeSuresi);
                s.renderer.GetPropertyBlock(propBlock);
                propBlock.SetFloat(shaderID, s.ilerleme);
                s.renderer.SetPropertyBlock(propBlock);
            }
        }

        if (degisiklikVar) LoopSesGuncelle();
    }

    void SlotuTemizle(SlotDurumu s)
    {
        // Particle'ý durdur ve yok et
        if (s.particle != null)
        {
            s.particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(s.particle.gameObject);
            s.particle = null;
        }
        s.spawnEdilen = null;
        s.renderer = null;
        s.ilerleme = 0f;
        s.aktif = false;
        doluSayisi = Mathf.Max(0, doluSayisi - 1);
    }

    void LoopSesGuncelle()
    {
        if (loopAudio == null || cizirtiSesi == null) return;

        if (doluSayisi > 0 && !loopAudio.isPlaying)
            loopAudio.Play();
        else if (doluSayisi == 0 && loopAudio.isPlaying)
            loopAudio.Stop();
    }

    public bool NesneKabulEdilirMi(GameObject nesne)
    {
        if (nesne == null) return false;
        if (kabulEdilenTagler == null || kabulEdilenTagler.Length == 0) return true;

        for (int i = 0; i < kabulEdilenTagler.Length; i++)
            if (kabulEdilenTagler[i] == nesne.tag) return true;
        return false;
    }

    public bool BosSlotVar
    {
        get
        {
            if (slotlar == null) return false;
            for (int i = 0; i < slotlar.Length; i++)
                if (!slotlar[i].aktif) return true;
            return false;
        }
    }

    /// <summary>
    /// Ýlk boþ slot'a prefab'ý spawn eder, içindeki kalamar mesh'inin shader'ýný baþlatýr.
    /// </summary>
    public bool KalamarYerlestir()
    {
        if (kalamarPrefab == null)
        {
            Debug.LogError("[Kizartma] Kalamar prefab atanmamýþ!");
            return false;
        }
        if (slotlar == null || slotlar.Length == 0)
        {
            Debug.LogError("[Kizartma] Slot noktalarý atanmamýþ!");
            return false;
        }

        int idx = -1;
        for (int i = 0; i < slotlar.Length; i++)
        {
            if (!slotlar[i].aktif) { idx = i; break; }
        }
        if (idx < 0) return false;

        Transform nokta = slotNoktalari[idx];
        var s = slotlar[idx];

        // Prefab'ý spawn et
        s.spawnEdilen = Instantiate(kalamarPrefab, nokta.position, nokta.rotation, nokta);

        // Spawn olan objenin child'larýnda "kalamar" isimli renderer'ý bul
        Renderer[] tumRenderlar = s.spawnEdilen.GetComponentsInChildren<Renderer>(true);
        s.renderer = null;
        string anahtar = rendererIsimAnahtari.ToLower();
        for (int rI = 0; rI < tumRenderlar.Length; rI++)
        {
            if (tumRenderlar[rI].gameObject.name.ToLower().Contains(anahtar))
            {
                s.renderer = tumRenderlar[rI];
                break;
            }
        }
        if (s.renderer == null && tumRenderlar.Length > 0)
            s.renderer = tumRenderlar[0];

        s.ilerleme = 0f;
        s.aktif = true;

        if (s.renderer != null)
        {
            s.renderer.GetPropertyBlock(propBlock);
            propBlock.SetFloat(shaderID, 0f);
            s.renderer.SetPropertyBlock(propBlock);
        }

        if (particlePrefab != null)
        {
            s.particle = Instantiate(particlePrefab, nokta.position, nokta.rotation, nokta);
            s.particle.Play();
        }

        if (konulmaSesi != null)
            AudioSource.PlayClipAtPoint(konulmaSesi, nokta.position);

        doluSayisi++;
        LoopSesGuncelle();

        return true;
    }

    public string[] KabulEdilenTagler => kabulEdilenTagler;
}