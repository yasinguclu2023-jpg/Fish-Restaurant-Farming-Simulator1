using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Kağıt → Ekmek → Malzeme hiyerarşik slot sistemi + Sarma.
/// 
/// AKIŞ:
/// 1. Kağıt masaya yerleştirilir (normal NesneYerlestirmeSistemi ile)
/// 2. Ekmek kağıda konur → EkmekNoktasi'na otomatik snap olur
///    (Ekmek'e EkmekMalzemesi marker'ı eklenir → tek başına alınamaz)
/// 3. Malzemeler ekmeğe konur → slotlara otomatik snap olur
///    (Her malzemeye EkmekMalzemesi marker'ı eklenir → geri alınamaz)
/// 4. E tuşuyla sarma:
///    → Kağıt objesi yok edilir
///    → Ekmeğin TAG'ine göre doğru sarma görseli ekmek'in child'ı olarak spawn olur
///    → Ekmek'teki marker kaldırılır → artık alınabilir (child'larıyla birlikte)
///    → Geri dönüş yok
/// 
/// SARMA SEÇİMİ:
/// - "Sarma Eşleşmeleri" listesine her ekmek tag'i için ayrı sarma prefab eklenir
///   (ör: Ekmek tag'i → uzun sarma, AhtapotSandwich tag'i → sandwich sarması)
/// - Listede eşleşme bulunamazsa "Varsayılan Sarma" alanları fallback olarak kullanılır
/// </summary>
public class EkmekYerlestirmeSistemi : MonoBehaviour
{
    [System.Serializable]
    public class SarmaEslesmesi
    {
        [Header("Eşleşme")]
        [Tooltip("Bu sarmanın kullanılacağı ekmek tag'i (ör: Ekmek, AhtapotSandwich)")]
        public string ekmekTag;

        [Tooltip("Bu ekmek tag'i için spawn edilecek sarma görseli prefab")]
        public GameObject sarmaPrefab;

        [Header("Spawn Pozisyon / Rotasyon")]
        [Tooltip("Sarılmış kağıdın spawn olacağı pozisyon (ekmek'in child'ı olarak boş GO ata).\nBoş bırakılırsa ekmek'in pozisyonunda spawn olur.")]
        public Transform sarmaSpawnPozisyon;

        [Tooltip("Sarılmış kağıt için özel rotasyon kullan")]
        public bool ozelRotasyonKullan;

        [Tooltip("Sarılmış kağıt özel rotasyonu (local)")]
        public Vector3 ozelRotasyon;
    }

    [Header("Kağıt Ayarları")]
    [Tooltip("Ekmeğin yerleşeceği nokta (kağıdın child'ı)")]
    [SerializeField] private Transform ekmekNoktasi;

    [Tooltip("Kabul edilen ekmek tag'leri")]
    [SerializeField] private string[] kabulEdilenEkmekTagleri;

    [Header("Sarma Eşleşmeleri (Tag Bazlı)")]
    [Tooltip("Her ekmek tag'i için ayrı sarma prefab tanımla.\nÖr: Ekmek → uzun sarma, AhtapotSandwich → sandwich sarması")]
    [SerializeField] private List<SarmaEslesmesi> sarmaEslesmeleri = new List<SarmaEslesmesi>();

    [Header("Varsayılan Sarma (Fallback)")]
    [Tooltip("Yukarıdaki listede eşleşme bulunamazsa kullanılacak sarma prefab.\nTüm ekmekler aynı sarmayı kullanıyorsa sadece burayı doldurman yeterli.")]
    [SerializeField] private GameObject sarmaPrefab;

    [Tooltip("Varsayılan sarmanın spawn pozisyonu (ekmek'in child'ı).\nBoş bırakılırsa ekmek'in pozisyonunda spawn olur.")]
    [SerializeField] private Transform sarmaSpawnPozisyon;

    [Tooltip("Varsayılan sarma için özel rotasyon kullan")]
    [SerializeField] private bool sarmaOzelRotasyonKullan;

    [Tooltip("Varsayılan sarma özel rotasyonu (local)")]
    [SerializeField] private Vector3 sarmaOzelRotasyon;

    [Header("Sarma Sesi")]
    [Tooltip("Sarma sırasında çalacak ses")]
    [SerializeField] private AudioClip sarmaSesi;

    [Tooltip("Sarma ses seviyesi")]
    [Range(0f, 1f)]
    [SerializeField] private float sarmaSesSeviyesi = 1f;

    // Durum
    private GameObject yerlesenEkmek;
    private EkmekMalzemeAlici malzemeAlici;
    private bool ekmekYerlestirildi;
    private bool sarmaYapildi;

    // === MALZEME / EKMEK YERLEŞTİRME ===

    public bool NesneKabulEdilirMi(GameObject nesne)
    {
        if (nesne == null || sarmaYapildi) return false;

        if (!ekmekYerlestirildi)
            return EkmekTagiMi(nesne.tag);

        if (malzemeAlici != null)
            return malzemeAlici.MalzemeKabulEdilirMi(nesne);

        return false;
    }

    public bool NesneYerlestir(GameObject nesne, Vector3 orijinalScale, int orijinalLayer)
    {
        if (nesne == null || sarmaYapildi) return false;

        if (!ekmekYerlestirildi && EkmekTagiMi(nesne.tag))
            return EkmekYerlestir(nesne, orijinalScale, orijinalLayer);

        if (ekmekYerlestirildi && malzemeAlici != null)
            return malzemeAlici.MalzemeYerlestir(nesne, orijinalScale, orijinalLayer);

        return false;
    }

    bool EkmekYerlestir(GameObject ekmek, Vector3 orijinalScale, int orijinalLayer)
    {
        if (ekmekNoktasi == null)
        {
            Debug.LogError("[EkmekYerlestirme] EkmekNoktasi atanmamış!");
            return false;
        }

        if (ekmek.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        if (ekmek.TryGetComponent(out Collider col))
        {
            col.enabled = true;
            col.isTrigger = false;
        }

        ekmek.transform.SetParent(ekmekNoktasi);
        ekmek.transform.localPosition = Vector3.zero;
        ekmek.transform.localRotation = Quaternion.identity;
        ekmek.transform.localScale = DivideScale(orijinalScale, ekmekNoktasi.lossyScale);

        SetLayerRecursive(ekmek, orijinalLayer);

        yerlesenEkmek = ekmek;
        ekmekYerlestirildi = true;

        malzemeAlici = ekmek.GetComponent<EkmekMalzemeAlici>();
        if (malzemeAlici == null)
            malzemeAlici = ekmek.GetComponentInChildren<EkmekMalzemeAlici>();

        // Ekmek kağıda yerleşti → marker ekle (artık geri alınamaz)
        if (malzemeAlici != null)
            malzemeAlici.MarkerEkle();

        if (ekmek.TryGetComponent(out NesneSesVerisi ses))
            ses.BirakmaSesiCal();

        // Ekmek artık kağıdın child'ı → outline'ı kağıtla birleştir
        RaycastSistemi.HiyerarsiDegisti(ekmek);

        Debug.Log($"[EkmekYerlestirme] Ekmek yerleştirildi: {ekmek.name} (tag: {ekmek.tag})");
        return true;
    }

    // === SARMA ===

    /// <summary>
    /// Yerleşen ekmeğin tag'ine göre kullanılacak sarma eşleşmesini bulur.
    /// Listede eşleşme yoksa "Varsayılan Sarma" alanlarından geçici bir eşleşme üretir.
    /// Hiçbir prefab yoksa null döner.
    /// </summary>
    SarmaEslesmesi AktifSarmaAl()
    {
        if (yerlesenEkmek == null) return null;

        string tag = yerlesenEkmek.tag;

        // 1) Tag bazlı eşleşme listesinde ara
        for (int i = 0; i < sarmaEslesmeleri.Count; i++)
        {
            var es = sarmaEslesmeleri[i];
            if (es != null && es.ekmekTag == tag && es.sarmaPrefab != null)
                return es;
        }

        // 2) Bulunamazsa varsayılan (fallback) alanları kullan
        if (sarmaPrefab != null)
        {
            return new SarmaEslesmesi
            {
                ekmekTag = tag,
                sarmaPrefab = sarmaPrefab,
                sarmaSpawnPozisyon = sarmaSpawnPozisyon,
                ozelRotasyonKullan = sarmaOzelRotasyonKullan,
                ozelRotasyon = sarmaOzelRotasyon
            };
        }

        return null;
    }

    public bool SarmaYapilabilirMi()
    {
        return ekmekYerlestirildi && !sarmaYapildi && AktifSarmaAl() != null;
    }

    public void SarmaYap()
    {
        if (!SarmaYapilabilirMi())
        {
            Debug.Log("[EkmekSarma] Sarma yapılamaz!");
            return;
        }

        // Ekmeğin tag'ine göre doğru sarmayı seç
        SarmaEslesmesi sarma = AktifSarmaAl();
        if (sarma == null || sarma.sarmaPrefab == null)
        {
            Debug.LogWarning($"[EkmekSarma] '{yerlesenEkmek.tag}' tag'i için sarma prefab bulunamadı!");
            return;
        }

        sarmaYapildi = true;

        // === 1. TÜM REFERANSLARI ÖNCE AL (destroy öncesi) ===
        Vector3 ekmekWorldPos = yerlesenEkmek.transform.position;
        Quaternion ekmekWorldRot = yerlesenEkmek.transform.rotation;
        Vector3 ekmekWorldScale = yerlesenEkmek.transform.lossyScale;

        Vector3 sarmaWorldPos = ekmekWorldPos;
        Quaternion sarmaWorldRot = ekmekWorldRot;
        if (sarma.sarmaSpawnPozisyon != null)
        {
            sarmaWorldPos = sarma.sarmaSpawnPozisyon.position;
            sarmaWorldRot = sarma.sarmaSpawnPozisyon.rotation;
        }

        bool spawnPozVardi = sarma.sarmaSpawnPozisyon != null;
        GameObject kagitObj = gameObject;

        // === 2. EKMEĞİ KAĞITTAN KOPAR ===
        yerlesenEkmek.transform.SetParent(null);
        yerlesenEkmek.transform.position = ekmekWorldPos;
        yerlesenEkmek.transform.rotation = ekmekWorldRot;
        yerlesenEkmek.transform.localScale = ekmekWorldScale;

        // === 3. SARMA GÖRSELİ SPAWN ET (kağıt henüz yok edilmedi) ===
        GameObject sarmaGorsel = Instantiate(sarma.sarmaPrefab, yerlesenEkmek.transform);
        sarmaGorsel.name = "SarmaKagidi";

        if (spawnPozVardi)
        {
            sarmaGorsel.transform.position = sarmaWorldPos;
            if (sarma.ozelRotasyonKullan)
                sarmaGorsel.transform.localRotation = Quaternion.Euler(sarma.ozelRotasyon);
            else
                sarmaGorsel.transform.rotation = sarmaWorldRot;
        }
        else
        {
            sarmaGorsel.transform.localPosition = Vector3.zero;
            if (sarma.ozelRotasyonKullan)
                sarmaGorsel.transform.localRotation = Quaternion.Euler(sarma.ozelRotasyon);
            else
                sarmaGorsel.transform.localRotation = Quaternion.identity;
        }

        if (sarmaGorsel.TryGetComponent(out Rigidbody sarmaRb))
        {
            sarmaRb.isKinematic = true;
            sarmaRb.useGravity = false;
        }
        if (sarmaGorsel.TryGetComponent(out Collider sarmaCol))
            sarmaCol.enabled = false;

        if (sarmaGorsel.GetComponent<EkmekMalzemesi>() == null)
            sarmaGorsel.AddComponent<EkmekMalzemesi>();

        // === 4. EKMEK MARKER KALDIR + COLLİDER AÇ ===
        if (malzemeAlici != null)
            malzemeAlici.MarkerKaldir();

        if (yerlesenEkmek.TryGetComponent(out Collider ekmekCol))
            ekmekCol.enabled = true;

        // === 5. SES ÇAL ===
        if (sarmaSesi != null)
            AudioSource.PlayClipAtPoint(sarmaSesi, ekmekWorldPos, sarmaSesSeviyesi);

        Debug.Log($"[EkmekSarma] Sarma yapıldı! ({yerlesenEkmek.tag} → {sarma.sarmaPrefab.name}) " +
                  "Ekmek artık alınabilir (child'larıyla birlikte).");

        // === 6. EN SON KAĞIDI YOK ET ===
        Destroy(kagitObj);
    }

    // === PREVIEW DESTEĞİ ===

    public Transform PreviewNoktasiBul(GameObject nesne)
    {
        if (nesne == null || sarmaYapildi) return null;

        if (!ekmekYerlestirildi && EkmekTagiMi(nesne.tag))
            return ekmekNoktasi;

        if (ekmekYerlestirildi && malzemeAlici != null && malzemeAlici.MalzemeKabulEdilirMi(nesne))
            return malzemeAlici.UygunSlotBul(nesne.tag);

        return null;
    }

    public GameObject SpawnPrefabBul(GameObject nesne)
    {
        if (nesne == null || !ekmekYerlestirildi || malzemeAlici == null)
            return null;

        return malzemeAlici.SpawnPrefabBul(nesne.tag);
    }

    public bool SpawnRotasyonBilgisiAl(GameObject nesne, out Quaternion rotasyon)
    {
        rotasyon = Quaternion.identity;
        if (nesne == null || !ekmekYerlestirildi || malzemeAlici == null)
            return false;

        return malzemeAlici.SpawnRotasyonBilgisiAl(nesne.tag, out rotasyon);
    }

    // === YARDIMCI ===

    bool EkmekTagiMi(string tag)
    {
        if (kabulEdilenEkmekTagleri == null || kabulEdilenEkmekTagleri.Length == 0)
            return false;

        for (int i = 0; i < kabulEdilenEkmekTagleri.Length; i++)
        {
            if (kabulEdilenEkmekTagleri[i] == tag)
                return true;
        }
        return false;
    }

    Vector3 DivideScale(Vector3 hedef, Vector3 parent)
    {
        return new Vector3(
            parent.x != 0 ? hedef.x / parent.x : hedef.x,
            parent.y != 0 ? hedef.y / parent.y : hedef.y,
            parent.z != 0 ? hedef.z / parent.z : hedef.z
        );
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    // === PUBLIC PROPERTIES ===
    public bool EkmekVar => ekmekYerlestirildi;
    public bool SarmaYapildiMi => sarmaYapildi;
    public GameObject YerlesenEkmek => yerlesenEkmek;
    public EkmekMalzemeAlici MalzemeAlici => malzemeAlici;
    public bool Dolu => ekmekYerlestirildi && malzemeAlici != null && malzemeAlici.TumSlotlarDolu;
}