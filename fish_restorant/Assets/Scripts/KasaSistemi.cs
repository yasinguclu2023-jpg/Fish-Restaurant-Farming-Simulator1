using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Kasadan �r�n alma sistemi.
/// 
/// �ki mod destekler:
/// 1. Child Modu (varsay�lan): Slot pozisyonlar� ba�lang��taki child'lardan okunur (sebze kasalar�)
/// 2. Manuel Mod: Slot pozisyonlar� Inspector'dan tan�mlan�r (bal�k kasalar� gibi child'lar silinebilir)
/// 
/// Manuel mod i�in:
///   - Inspector'da "manuelSlotKullan" = true yap
///   - Sahnede bal�klar� yerle�tir
///   - Component'a sa� t�kla ? "Mevcut Childlardan Slot Pozisyonlarini Kaydet"
///   - Art�k child'lar� silebilirsin, pozisyonlar kal�r
/// </summary>
public class KasaSistemi : MonoBehaviour, ICopEtkilesimi, IIcerikliKap
{
    [Header("�r�n Container")]
    [Tooltip("�r�nlerin bulundu�u parent. Bo� b�rak�rsan bu obje kullan�l�r.")]
    [SerializeField] private Transform urunContainer;

    [Header("Kapasite")]
    [Tooltip("Maksimum �r�n say�s�. 0 = s�n�rs�z")]
    [SerializeField] private int maksimumKapasite = 9;

    [Header("Kabul Edilen �r�nler")]
    [Tooltip("Bu kasaya b�rak�labilecek tag'ler. Bo� b�rak�rsan her �eyi kabul eder.")]
    [SerializeField] private string[] kabulEdilenTagler;

    [Header("Manuel Slot Tan�mlama")]
    [Tooltip("True yap�l�rsa slot pozisyonlar� Inspector'dan okunur, child'lara ba��ml� de�ildir.")]
    [SerializeField] private bool manuelSlotKullan = false;

    [Tooltip("Manuel slot pozisyonlar� (local space). manuelSlotKullan = true ise kullan�l�r.")]
    [SerializeField] private Vector3[] manuelSlotPozisyonlar;

    [Tooltip("Manuel slot rotasyonlar� (Euler, local space). manuelSlotKullan = true ise kullan�l�r.")]
    [SerializeField] private Vector3[] manuelSlotRotasyonlar;

    [Header("Cop Kovasi")]
    [Tooltip("True: cope atilinca sadece ICI bosalir, kasa elde kalir (balik kasasi). " +
             "False: kasa komple silinir (bitki vb.).")]
    [SerializeField] private bool copteIciniBosalt = false;

    [Header("Elde Doldurma")]
    [Tooltip("True: Bu kasa ELDE tutulurken, bakilan uygun urune (orn. balik) sol tik ile " +
             "urun kasanin icine doldurulur. False (varsayilan): mevcut davranis degismez.")]
    [SerializeField] private bool eldeykenDoldurulabilir = false;

    /// <summary>Kasa elde tutulurken bakilan urun icine doldurulabilir mi?</summary>
    public bool EldeDoldurmaAktif => eldeykenDoldurulabilir;

    [Header("Yerlestirme Modu")]
    [Tooltip("True: Bu kasa elde tutulurken preview surekli gozukmez; yerlestirme modu " +
             "X tusu ile acilip kapanir. False (varsayilan): mevcut davranis degismez.")]
    [SerializeField] private bool tusIleYerlestirme = false;

    /// <summary>Preview surekli gosterilmesin, X tusu ile acilip kapansin mi?</summary>
    public bool TusIleYerlestirme => tusIleYerlestirme;

    // Slot sistemi - pozisyonlar sabit, �r�nler de�i�ebilir
    private List<SlotVerisi> slotlar = new List<SlotVerisi>();

    private struct SlotVerisi
    {
        public Vector3 lokalPozisyon;
        public Quaternion lokalRotasyon;
        public GameObject mevcutUrun;
    }

    void Awake()
    {
        if (urunContainer == null)
            urunContainer = transform;

        if (manuelSlotKullan)
            ManuelSlotlariOlustur();
        else
            ChildSlotlariOlustur();
    }

    /// <summary>
    /// Orijinal davran��: child'lardan slot olu�tur (sebze kasalar� i�in)
    /// </summary>
    void ChildSlotlariOlustur()
    {
        for (int i = 0; i < urunContainer.childCount; i++)
        {
            Transform child = urunContainer.GetChild(i);

            SlotVerisi slot = new SlotVerisi
            {
                lokalPozisyon = child.localPosition,
                lokalRotasyon = child.localRotation,
                mevcutUrun = child.gameObject
            };

            slotlar.Add(slot);
        }

        if (maksimumKapasite == 0)
            maksimumKapasite = slotlar.Count;
    }

    /// <summary>
    /// Yeni davran��: Inspector'daki manuel pozisyonlardan slot olu�tur (bal�k kasas� i�in)
    /// Child varsa onlar� slotlara atar, yoksa bo� slotlar olu�turur.
    /// </summary>
    void ManuelSlotlariOlustur()
    {
        if (manuelSlotPozisyonlar == null || manuelSlotPozisyonlar.Length == 0)
        {
            Debug.LogWarning($"[KasaSistemi] '{gameObject.name}': Manuel slot kullan a��k ama pozisyon tan�mlanmam��! " +
                             "Inspector'da manuelSlotPozisyonlar dizisini doldur veya context menu'den kaydet.", this);
            // Fallback: child varsa onlardan olu�tur
            ChildSlotlariOlustur();
            return;
        }

        int slotCount = manuelSlotPozisyonlar.Length;

        // Rotasyon dizisi eksikse varsay�lan olu�tur
        if (manuelSlotRotasyonlar == null || manuelSlotRotasyonlar.Length != slotCount)
        {
            manuelSlotRotasyonlar = new Vector3[slotCount];
        }

        // Mevcut child'lar� listeye al (s�rayla e�le�tirmek i�in)
        List<GameObject> mevcutChildlar = new List<GameObject>();
        for (int i = 0; i < urunContainer.childCount; i++)
        {
            GameObject child = urunContainer.GetChild(i).gameObject;
            if (child.activeSelf)
                mevcutChildlar.Add(child);
        }

        // Slotlar� olu�tur
        for (int i = 0; i < slotCount; i++)
        {
            SlotVerisi slot = new SlotVerisi
            {
                lokalPozisyon = manuelSlotPozisyonlar[i],
                lokalRotasyon = Quaternion.Euler(manuelSlotRotasyonlar[i]),
                mevcutUrun = null
            };

            // E�er bu index'te child varsa slota ata
            if (i < mevcutChildlar.Count)
            {
                slot.mevcutUrun = mevcutChildlar[i];
                // Pozisyonunu slot pozisyonuna snap et
                mevcutChildlar[i].transform.localPosition = slot.lokalPozisyon;
                mevcutChildlar[i].transform.localRotation = slot.lokalRotasyon;
            }

            slotlar.Add(slot);
        }

        if (maksimumKapasite == 0)
            maksimumKapasite = slotCount;
    }

    /// <summary>
    /// Sondan ba�layarak ilk dolu slottaki �r�n� al
    /// </summary>
    public GameObject UrunAl()
    {
        for (int i = slotlar.Count - 1; i >= 0; i--)
        {
            SlotVerisi slot = slotlar[i];

            if (slot.mevcutUrun != null && slot.mevcutUrun.activeSelf)
            {
                GameObject urun = slot.mevcutUrun;

                // Slotu bo�alt
                slot.mevcutUrun = null;
                slotlar[i] = slot;

                urun.transform.SetParent(null);
                return urun;
            }
        }

        return null;
    }

    /// <summary>
    /// �r�n� kasaya ekle - ilk bo� slota yerle�ir
    /// </summary>
    public void UrunEkle(GameObject urun)
    {
        if (urun == null) return;

        // �lk bo� slotu bul
        for (int i = 0; i < slotlar.Count; i++)
        {
            SlotVerisi slot = slotlar[i];

            if (slot.mevcutUrun == null)
            {
                // Slota yerle�tir
                slot.mevcutUrun = urun;
                slotlar[i] = slot;

                urun.transform.SetParent(urunContainer);
                urun.transform.localPosition = slot.lokalPozisyon;
                urun.transform.localRotation = slot.lokalRotasyon;
                return;
            }
        }

        // Slot yoksa veya hepsi doluysa (kapasite a��ld�ysa) merkeze koy
        urun.transform.SetParent(urunContainer);
        urun.transform.localPosition = Vector3.zero;
        urun.transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Bu �r�n kasaya b�rak�labilir mi?
    /// </summary>
    public bool UrunKabulEdilirMi(GameObject urun)
    {
        if (urun == null) return false;

        // Kapasite kontrol�
        if (MevcutUrunSayisi >= maksimumKapasite)
            return false;

        // Tag listesi bo�sa her �eyi kabul et
        if (kabulEdilenTagler == null || kabulEdilenTagler.Length == 0)
            return true;

        string urunTag = urun.tag;
        for (int i = 0; i < kabulEdilenTagler.Length; i++)
        {
            if (kabulEdilenTagler[i] == urunTag)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Kasada �r�n var m�?
    /// </summary>
    public bool UrunVarMi
    {
        get
        {
            for (int i = 0; i < slotlar.Count; i++)
            {
                if (slotlar[i].mevcutUrun != null && slotlar[i].mevcutUrun.activeSelf)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Mevcut �r�n say�s�
    /// </summary>
    public int MevcutUrunSayisi
    {
        get
        {
            int sayac = 0;
            for (int i = 0; i < slotlar.Count; i++)
            {
                if (slotlar[i].mevcutUrun != null && slotlar[i].mevcutUrun.activeSelf)
                    sayac++;
            }
            return sayac;
        }
    }

    /// <summary>
    /// Kasa dolu mu?
    /// </summary>
    public bool DoluMu => MevcutUrunSayisi >= maksimumKapasite;

    /// <summary>
    /// Maksimum kapasite
    /// </summary>
    public int MaksimumKapasite => maksimumKapasite;

    // ===== COP KOVASI =====

    /// <summary>
    /// Cop kovasina atilinca: bayrak aciksa (balik kasasi) icini bosaltir ve true doner (kasa kalir);
    /// kapaliysa (bitki vb.) false doner -> cop kasayi komple siler.
    /// </summary>
    public bool CopeAtildi()
    {
        if (!copteIciniBosalt) return false; // kasa komple silinsin
        IceriBosalt();
        return true;                         // sadece ici bosaldi, kasa elde kalir
    }

    // ===== GUN SONU (IIcerikliKap) =====

    void OnEnable()
    {
        IcerikliKapDefteri.Kaydet(this);
    }

    void OnDisable()
    {
        IcerikliKapDefteri.Sil(this);
    }

    /// <summary>Depo icinde mi kontrolu icin kabin dunya konumu.</summary>
    public Vector3 KapKonumu => transform.position;

    /// <summary>
    /// Gun sonu temizligi: kasa kalir, icerigi bosalir.
    /// ONEMLI: Icerideki parca da bir KAP ise (orn. rafta duran kuvetler) O DA SILINMEZ;
    /// slotunda kalir, sadece kendi icerigi bosaltilir.
    /// (Cop kovasi davranisi degismez: IceriBosalt her seyi siler.)
    /// </summary>
    public void IciniBosalt()
    {
        for (int i = 0; i < slotlar.Count; i++)
        {
            SlotVerisi slot = slotlar[i];
            if (slot.mevcutUrun == null) continue;

            // Kap ise: yerinde kalir (ici bosalir) -> slot dolu kalmaya devam eder.
            if (KapYardimcisi.ParcayiIsle(slot.mevcutUrun)) continue;

            // Sıradan urun yok edildi -> slotu bosalt.
            slot.mevcutUrun = null;
            slotlar[i] = slot;
        }
    }

    /// <summary>Kasadaki tum urunleri yok eder, slotlari bosaltir. Kasa objesi kalir.</summary>
    public void IceriBosalt()
    {
        for (int i = 0; i < slotlar.Count; i++)
        {
            SlotVerisi slot = slotlar[i];
            if (slot.mevcutUrun != null)
            {
                Destroy(slot.mevcutUrun);
                slot.mevcutUrun = null;
                slotlar[i] = slot;
            }
        }
    }

    // ============================================================
    // EDITOR YARDIMCI METODLARI
    // ============================================================

#if UNITY_EDITOR
    /// <summary>
    /// Context menu: Mevcut child'lar�n pozisyonlar�n� manuel slot dizisine kaydet.
    /// Sahneye �r�nleri yerle�tir, bu fonksiyonu �al��t�r, sonra �r�nleri silebilirsin.
    /// </summary>
    [ContextMenu("Mevcut Childlardan Slot Pozisyonlarini Kaydet")]
    void ChildlardanSlotPozisyonuKaydet()
    {
        Transform container = urunContainer != null ? urunContainer : transform;

        int count = container.childCount;
        manuelSlotPozisyonlar = new Vector3[count];
        manuelSlotRotasyonlar = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Transform child = container.GetChild(i);
            manuelSlotPozisyonlar[i] = child.localPosition;
            manuelSlotRotasyonlar[i] = child.localRotation.eulerAngles;
        }

        manuelSlotKullan = true;
        maksimumKapasite = count;

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[KasaSistemi] '{gameObject.name}': {count} slot pozisyonu kaydedildi! " +
                  "manuelSlotKullan otomatik olarak true yap�ld�.", this);
    }

    /// <summary>
    /// Slotlar� sahnede gizmo olarak g�ster
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!manuelSlotKullan || manuelSlotPozisyonlar == null) return;

        Transform container = urunContainer != null ? urunContainer : transform;

        for (int i = 0; i < manuelSlotPozisyonlar.Length; i++)
        {
            Vector3 worldPos = container.TransformPoint(manuelSlotPozisyonlar[i]);

            // Runtime'da dolu/bo� kontrol�
            bool dolu = Application.isPlaying && slotlar != null && i < slotlar.Count
                        && slotlar[i].mevcutUrun != null;

            Gizmos.color = dolu ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(worldPos, 0.05f);
            Gizmos.DrawLine(worldPos, worldPos + container.up * 0.1f);

            UnityEditor.Handles.Label(worldPos + Vector3.up * 0.12f, $"Slot {i}");
        }
    }
#endif
}