using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ekmek prefab'ına eklenir.
/// Ekmeğin üzerindeki malzeme slotlarını yönetir.
/// Her slot hangi tag'i kabul edeceğini bilir, bir kez dolunca tekrar kullanılamaz.
/// 
/// Yerleştirilen malzemelere otomatik olarak EkmekMalzemesi marker'ı eklenir,
/// böylece NesneAlmaSistemi bu malzemeleri geri almayı reddeder.
/// Ekmek'in kendisine de marker eklenir (sarma öncesi tek başına alınmasın).
/// </summary>
public class EkmekMalzemeAlici : MonoBehaviour
{
    [System.Serializable]
    public class MalzemeSlotu
    {
        [Header("Slot Ayarları")]
        [Tooltip("Bu slotun kabul ettiği tag (ör: Balik, Marul, Sogan, Domates)")]
        public string kabulEdilenTag;

        [Tooltip("Malzemenin snap olacağı pozisyon (ekmek child'ı)")]
        public Transform pozisyon;

        [Header("Rotasyon")]
        [Tooltip("Yerleştirmede özel rotasyon kullan")]
        public bool ozelRotasyonKullan;

        [Tooltip("Özel rotasyon (ozelRotasyonKullan açıksa)")]
        public Vector3 ozelRotasyon;

        [Header("Yön Sabitleme")]
        [Tooltip("Açıksa: malzeme ekmeğe yerleşirken, elde/ızgarada nasıl çevrilmiş olursa olsun " +
                 "(ör. balık ters-düz edilmiş) her zaman TEK sabit yöne bakar.\n" +
                 "Nesnenin alt (child) objelerine işlenmiş dönüşler de sıfırlanır, böylece " +
                 "180° çevirme yerleşmeye yansımaz.")]
        public bool tekYoneSabitle;

        [Header("Prefab Spawn")]
        [Tooltip("Malzeme konulunca elde tutulan nesne yok edilip bu prefab spawn edilir.\nBoş bırakılırsa elde tutulan nesne olduğu gibi yerleşir.")]
        public GameObject spawnPrefab;

        [Tooltip("Spawn edilen prefab'ın yerleşeceği pozisyon.\nBoş bırakılırsa slot pozisyonu kullanılır.")]
        public Transform spawnPozisyon;

        [Tooltip("Spawn edilen prefab için özel rotasyon kullan")]
        public bool spawnOzelRotasyonKullan;

        [Tooltip("Spawn prefab özel rotasyonu")]
        public Vector3 spawnOzelRotasyon;

        [Header("Ses")]
        [Tooltip("Bu malzeme konulduğunda çalacak ses klibi")]
        public AudioClip konulmaSesi;

        [Tooltip("Ses seviyesi")]
        [Range(0f, 1f)]
        public float sesSeviyesi = 1f;

        // Runtime
        [HideInInspector] public bool dolu;
        [HideInInspector] public GameObject yerlesenNesne;
    }

    [Header("Malzeme Slotları")]
    [Tooltip("Ekmeğe eklenebilecek malzeme slotları")]
    [SerializeField] private List<MalzemeSlotu> malzemeSlotlari = new List<MalzemeSlotu>();

    private AudioSource audioSource;

    /// <summary>
    /// Ekmek kağıda yerleştirildikten sonra çağrılır.
    /// Marker ekler - ekmek artık tek başına alınamaz (sarma yapılana kadar).
    /// </summary>
    public void MarkerEkle()
    {
        if (GetComponent<EkmekMalzemesi>() == null)
            gameObject.AddComponent<EkmekMalzemesi>();
    }

    public bool MalzemeKabulEdilirMi(GameObject nesne)
    {
        if (nesne == null) return false;
        return UygunSlotBul(nesne.tag) != null;
    }

    public Transform UygunSlotBul(string tag)
    {
        for (int i = 0; i < malzemeSlotlari.Count; i++)
        {
            var slot = malzemeSlotlari[i];
            if (!slot.dolu && slot.kabulEdilenTag == tag && slot.pozisyon != null)
                return slot.pozisyon;
        }
        return null;
    }

    public GameObject SpawnPrefabBul(string tag)
    {
        for (int i = 0; i < malzemeSlotlari.Count; i++)
        {
            var slot = malzemeSlotlari[i];
            if (!slot.dolu && slot.kabulEdilenTag == tag && slot.spawnPrefab != null)
                return slot.spawnPrefab;
        }
        return null;
    }

    public bool SpawnRotasyonBilgisiAl(string tag, out Quaternion rotasyon)
    {
        rotasyon = Quaternion.identity;
        for (int i = 0; i < malzemeSlotlari.Count; i++)
        {
            var slot = malzemeSlotlari[i];
            if (!slot.dolu && slot.kabulEdilenTag == tag && slot.spawnPrefab != null)
            {
                if (slot.spawnOzelRotasyonKullan)
                    rotasyon = Quaternion.Euler(slot.spawnOzelRotasyon);
                return true;
            }
        }
        return false;
    }

    public bool MalzemeYerlestir(GameObject nesne, Vector3 orijinalScale, int orijinalLayer)
    {
        if (nesne == null) return false;

        MalzemeSlotu slot = SlotBul(nesne.tag);
        if (slot == null)
        {
            Debug.Log($"[EkmekMalzeme] '{nesne.tag}' için uygun boş slot yok!");
            return false;
        }

        SesCalSlot(slot);

        if (slot.spawnPrefab != null)
            PrefabSpawnYerlestir(slot, nesne, orijinalLayer);
        else
            DirekYerlestir(slot, nesne, orijinalScale, orijinalLayer);

        slot.dolu = true;

        Debug.Log($"[EkmekMalzeme] '{nesne.tag}' malzeme yerleştirildi → {slot.pozisyon.name}" +
                  (slot.spawnPrefab != null ? $" (Prefab: {slot.spawnPrefab.name})" : ""));
        return true;
    }

    void PrefabSpawnYerlestir(MalzemeSlotu slot, GameObject eldeNesne, int orijinalLayer)
    {
        Object.Destroy(eldeNesne);

        Transform hedefPoz = slot.spawnPozisyon != null ? slot.spawnPozisyon : slot.pozisyon;

        GameObject spawned = Object.Instantiate(slot.spawnPrefab, hedefPoz);
        spawned.transform.localPosition = Vector3.zero;

        if (slot.spawnOzelRotasyonKullan)
            spawned.transform.localRotation = Quaternion.Euler(slot.spawnOzelRotasyon);
        else
            spawned.transform.localRotation = Quaternion.identity;

        if (spawned.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        if (spawned.TryGetComponent(out Collider col))
            col.enabled = false;

        SetLayerRecursive(spawned, orijinalLayer);

        if (spawned.GetComponent<EkmekMalzemesi>() == null)
            spawned.AddComponent<EkmekMalzemesi>();

        slot.yerlesenNesne = spawned;

        // Malzeme artık ekmeğin child'ı → outline'ı ekmekle birleştir
        RaycastSistemi.HiyerarsiDegisti(spawned);
    }

    void DirekYerlestir(MalzemeSlotu slot, GameObject nesne, Vector3 orijinalScale, int orijinalLayer)
    {
        if (nesne.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        if (nesne.TryGetComponent(out Collider col))
            col.enabled = false;

        nesne.transform.SetParent(slot.pozisyon);
        nesne.transform.localPosition = Vector3.zero;

        // Tik açıksa: elde/ızgarada oluşmuş tüm dönüşleri (alt objeler dahil) yok say,
        // böylece nesne her zaman tek sabit yöne bakar. Kökün rotasyonu hemen aşağıda ayarlanır.
        if (slot.tekYoneSabitle)
            AltRotasyonlariSifirla(nesne.transform);

        if (slot.ozelRotasyonKullan)
            nesne.transform.localRotation = Quaternion.Euler(slot.ozelRotasyon);
        else
            nesne.transform.localRotation = Quaternion.identity;

        Vector3 parentScale = slot.pozisyon.lossyScale;
        nesne.transform.localScale = new Vector3(
            parentScale.x != 0 ? orijinalScale.x / parentScale.x : orijinalScale.x,
            parentScale.y != 0 ? orijinalScale.y / parentScale.y : orijinalScale.y,
            parentScale.z != 0 ? orijinalScale.z / parentScale.z : orijinalScale.z
        );

        SetLayerRecursive(nesne, orijinalLayer);

        if (nesne.GetComponent<EkmekMalzemesi>() == null)
            nesne.AddComponent<EkmekMalzemesi>();

        slot.yerlesenNesne = nesne;

        // Malzeme artık ekmeğin child'ı → outline'ı ekmekle birleştir
        RaycastSistemi.HiyerarsiDegisti(nesne);
    }

    void SesCalSlot(MalzemeSlotu slot)
    {
        if (slot.konulmaSesi == null) return;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 10f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }

        audioSource.PlayOneShot(slot.konulmaSesi, slot.sesSeviyesi);
    }

    MalzemeSlotu SlotBul(string tag)
    {
        for (int i = 0; i < malzemeSlotlari.Count; i++)
        {
            var slot = malzemeSlotlari[i];
            if (!slot.dolu && slot.kabulEdilenTag == tag && slot.pozisyon != null)
                return slot;
        }
        return null;
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    // Nesnenin alt (child) objelerindeki dönüşleri identity'ye sıfırlar.
    // Izgarada 180° ters-düz edilen balık gibi, dönüşü child mesh'e işlenmiş
    // nesnelerde o dönüşü temizleyip tek sabit yöne getirmek için kullanılır.
    // (Kökün kendi rotasyonu çağıran tarafından ayrıca ayarlanır.)
    void AltRotasyonlariSifirla(Transform kok)
    {
        foreach (Transform child in kok)
        {
            child.localRotation = Quaternion.identity;
            AltRotasyonlariSifirla(child);
        }
    }

    public void MarkerKaldir()
    {
        EkmekMalzemesi marker = GetComponent<EkmekMalzemesi>();
        if (marker != null)
            Destroy(marker);
    }

    // === Public Properties ===

    public bool TumSlotlarDolu
    {
        get
        {
            for (int i = 0; i < malzemeSlotlari.Count; i++)
            {
                if (!malzemeSlotlari[i].dolu) return false;
            }
            return malzemeSlotlari.Count > 0;
        }
    }

    public int ToplamSlotSayisi => malzemeSlotlari.Count;

    public int DoluSlotSayisi
    {
        get
        {
            int sayi = 0;
            for (int i = 0; i < malzemeSlotlari.Count; i++)
            {
                if (malzemeSlotlari[i].dolu) sayi++;
            }
            return sayi;
        }
    }

    public int BosSlotSayisi => ToplamSlotSayisi - DoluSlotSayisi;

    public bool SlotDoluMu(string tag)
    {
        for (int i = 0; i < malzemeSlotlari.Count; i++)
        {
            if (malzemeSlotlari[i].kabulEdilenTag == tag)
                return malzemeSlotlari[i].dolu;
        }
        return true;
    }

    public List<GameObject> YerlesenMalzemeler
    {
        get
        {
            var liste = new List<GameObject>();
            for (int i = 0; i < malzemeSlotlari.Count; i++)
            {
                if (malzemeSlotlari[i].dolu && malzemeSlotlari[i].yerlesenNesne != null)
                    liste.Add(malzemeSlotlari[i].yerlesenNesne);
            }
            return liste;
        }
    }
}