using UnityEngine;

/// <summary>
/// Kýzartma makinesine eklenir.
/// Inspector'dan atanan Empty Transform'lara (slot'lara) kalamar spawn eder,
/// shader animasyonunu, sesleri ve particle'larý yönetir.
///
/// Her slot baðýmsýz piþer (kendi ilerlemesi, kendi shader deðeri).
///
/// Kurulum:
/// 1. Kýzartma makinesi objesine bu scripti ekle
/// 2. Collider olmalý (oyuncu raycast vurabilsin) ? RaycastSistemi.etkilesimLayer'ýnda olmalý
/// 3. Makinenin altýna Empty GameObject'ler oluþtur (spawn noktalarý)
/// 4. Bu Empty'leri 'slotNoktalari' dizisine sürükle
/// 5. Kalamar prefab'ýný ata (KalamarVerisi componenti olmalý)
/// 6. (Opsiyonel) Particle prefab'ý, sesler vs ata
/// </summary>
public class KizartmaMakinesi : MonoBehaviour
{
    [Header("Prefab'lar")]
    [Tooltip("Spawn edilecek kalamar prefab'ý (KalamarVerisi componenti olmalý)")]
    [SerializeField] private GameObject kalamarPrefab;

    [Tooltip("Her slot'a konulduðunda spawn edilecek particle (opsiyonel)")]
    [SerializeField] private ParticleSystem particlePrefab;

    [Header("Slot Noktalarý")]
    [Tooltip("Kalamarlarýn spawn olacaðý Empty Transform'lar. Sayý = ayný anda piþirilebilecek kalamar sayýsý.")]
    [SerializeField] private Transform[] slotNoktalari;

    [Header("Piþirme Ayarlarý")]
    [Tooltip("Piþme süresi (saniye) - shader slider 0'dan 1'e bu sürede çýkar")]
    [SerializeField] private float pismeSuresi = 5f;

    [Header("Ses Ayarlarý")]
    [Tooltip("Kalamar konulduðunda çalan tek seferlik ses (cýz efekti)")]
    [SerializeField] private AudioClip konulmaSesi;

    [Range(0f, 1f)]
    [SerializeField] private float konulmaSesSeviyesi = 1f;

    [Tooltip("Üzerinde kalamar varken çalan döngüsel ses (cýzýrtý)")]
    [SerializeField] private AudioClip pismeSesi;

    [Range(0f, 1f)]
    [SerializeField] private float pismeSesSeviyesi = 0.5f;

    // === Slot içsel veri ===
    private class SlotDurumu
    {
        public Transform nokta;
        public GameObject kalamar;
        public KalamarVerisi verisi;
        public ParticleSystem particle;
        public float ilerleme;
        public bool aktif;        // piþme aktif mi (false = bitmiþ)
    }

    private SlotDurumu[] slotlar;
    private AudioSource loopAudio;
    private int doluSlotSayisi = 0;

    void Awake()
    {
        if (slotNoktalari == null || slotNoktalari.Length == 0)
        {
            Debug.LogError($"[KýzartmaMakinesi] '{name}' için slot noktalarý atanmamýþ!");
            slotlar = new SlotDurumu[0];
            return;
        }

        slotlar = new SlotDurumu[slotNoktalari.Length];
        for (int i = 0; i < slotNoktalari.Length; i++)
            slotlar[i] = new SlotDurumu { nokta = slotNoktalari[i] };

        // Loop ses kaynaðý (PisirmeSistemi'ndeki ile ayný yaklaþým)
        loopAudio = gameObject.AddComponent<AudioSource>();
        loopAudio.clip = pismeSesi;
        loopAudio.loop = true;
        loopAudio.playOnAwake = false;
        loopAudio.volume = pismeSesSeviyesi;
        loopAudio.spatialBlend = 1f;
        loopAudio.minDistance = 1f;
        loopAudio.maxDistance = 15f;
        loopAudio.rolloffMode = AudioRolloffMode.Linear;
    }

    void Update()
    {
        if (doluSlotSayisi == 0) return;

        float artis = Time.deltaTime / pismeSuresi;

        for (int i = 0; i < slotlar.Length; i++)
        {
            var s = slotlar[i];
            if (!s.aktif) continue;
            if (s.kalamar == null) { s.aktif = false; continue; }

            s.ilerleme = Mathf.Clamp01(s.ilerleme + artis);

            if (s.verisi != null)
                s.verisi.IlerlemeyiAyarla(s.ilerleme);

            if (s.ilerleme >= 1f)
            {
                s.aktif = false;
                Debug.Log($"[KýzartmaMakinesi] Slot {i} piþti! ({s.kalamar.name})");
            }
        }
    }

    /// <summary>
    /// Ýlk boþ slot'a yeni kalamar spawn eder.
    /// </summary>
    /// <returns>Yerleþtirme baþarýlýysa true</returns>
    public bool KalamarYerlestir()
    {
        if (kalamarPrefab == null)
        {
            Debug.LogError("[KýzartmaMakinesi] Kalamar prefab'ý atanmamýþ!");
            return false;
        }

        int idx = BosSlotBul();
        if (idx < 0) return false; // tüm slotlar dolu

        var slot = slotlar[idx];

        // Kalamarý spawn et (slot'a child olarak)
        slot.kalamar = Instantiate(kalamarPrefab, slot.nokta.position, slot.nokta.rotation, slot.nokta);
        slot.verisi = slot.kalamar.GetComponent<KalamarVerisi>();
        if (slot.verisi == null)
            slot.verisi = slot.kalamar.GetComponentInChildren<KalamarVerisi>();

        slot.ilerleme = 0f;
        slot.aktif = true;
        if (slot.verisi != null) slot.verisi.IlerlemeyiAyarla(0f);

        // Particle spawn (slot'a child)
        if (particlePrefab != null)
        {
            slot.particle = Instantiate(particlePrefab, slot.nokta.position, slot.nokta.rotation, slot.nokta);
            slot.particle.Play();
        }

        // Tek seferlik "cýz" sesi
        if (konulmaSesi != null)
            AudioSource.PlayClipAtPoint(konulmaSesi, slot.nokta.position, konulmaSesSeviyesi);

        doluSlotSayisi++;
        LoopSesGuncelle();

        Debug.Log($"[KýzartmaMakinesi] Slot {idx}'e kalamar yerleþtirildi.");
        return true;
    }

    /// <summary>
    /// Belirtilen slot'tan kalamarý al (envantere/ele). Particle ve referanslar temizlenir.
    /// </summary>
    public GameObject SlotKalamariAl(int slotIndex)
    {
        if (slotlar == null || slotIndex < 0 || slotIndex >= slotlar.Length) return null;
        var slot = slotlar[slotIndex];
        if (slot.kalamar == null) return null;

        GameObject alinan = slot.kalamar;
        alinan.transform.SetParent(null);

        if (slot.particle != null)
        {
            slot.particle.Stop();
            Destroy(slot.particle.gameObject, 1f);
        }

        slot.kalamar = null;
        slot.verisi = null;
        slot.particle = null;
        slot.ilerleme = 0f;
        slot.aktif = false;

        doluSlotSayisi = Mathf.Max(0, doluSlotSayisi - 1);
        LoopSesGuncelle();

        return alinan;
    }

    int BosSlotBul()
    {
        for (int i = 0; i < slotlar.Length; i++)
            if (slotlar[i].kalamar == null) return i;
        return -1;
    }

    void LoopSesGuncelle()
    {
        if (loopAudio == null || pismeSesi == null) return;

        if (doluSlotSayisi > 0 && !loopAudio.isPlaying)
            loopAudio.Play();
        else if (doluSlotSayisi == 0 && loopAudio.isPlaying)
            loopAudio.Stop();
    }

    // === Public Properties ===
    public bool BosSlotVar => BosSlotBul() >= 0;
    public int ToplamSlot => slotlar != null ? slotlar.Length : 0;
    public int DoluSlot => doluSlotSayisi;
    public float PismeSuresi => pismeSuresi;
}