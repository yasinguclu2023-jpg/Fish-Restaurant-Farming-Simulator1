using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Bal�k kasas� sistemi.
/// Slot pozisyonlar� Inspector'dan tan�mlan�r, runtime'da child olmas�na gerek yoktur.
/// Sebze kasas� (KasaSistemi) ile �ak��maz.
///
/// Kurulum:
/// 1. Bal�k kasas� objesine bu scripti ekle
/// 2. Inspector'da "Slot Pozisyonlari Olustur" butonuna bas veya elle slotSayisi ayarla
/// 3. slotLokalPozisyonlar ve slotLokalRotasyonlar dizilerini ayarla
///    (ya da mevcut bal�klarla sahneyi a�, "Mevcut Child'lardan Slot Al" context menu's�n� kullan)
/// 4. Art�k child bal�klar� silebilirsin - slotlar Inspector'da kay�tl� kal�r
/// </summary>
public class BalikKasaSistemi : MonoBehaviour, IIcerikliKap
{
    [Header("Container")]
    [Tooltip("�r�nlerin parent'� olacak transform. Bo� b�rak�rsan bu obje kullan�l�r.")]
    [SerializeField] private Transform urunContainer;

    [Header("Slot Ayarlar� (Inspector'dan Tan�mla)")]
    [Tooltip("Ka� slot olacak")]
    [SerializeField] private int slotSayisi = 9;

    [Tooltip("Her slotun lokal pozisyonu. Dizinin uzunlu�u slotSayisi ile e�le�meli.")]
    [SerializeField] private Vector3[] slotLokalPozisyonlar;

    [Tooltip("Her slotun lokal rotasyonu (Euler). Dizinin uzunlu�u slotSayisi ile e�le�meli.")]
    [SerializeField] private Vector3[] slotLokalRotasyonlar;

    [Header("Kabul Edilen �r�nler")]
    [Tooltip("Bu kasaya b�rak�labilecek tag'ler. Bo� b�rak�rsan her �eyi kabul eder.")]
    [SerializeField] private string[] kabulEdilenTagler;

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

    // Runtime slot takibi - sadece hangi slotta ne var onu tutar
    private GameObject[] slotIcerikleri;

    void Awake()
    {
        if (urunContainer == null)
            urunContainer = transform;

        SlotlariHazirla();
    }

    void SlotlariHazirla()
    {
        // Slot dizisini olu�tur
        slotIcerikleri = new GameObject[slotSayisi];

        // E�er pozisyon dizileri eksikse veya boyutlar� uyu�muyorsa d�zelt
        if (slotLokalPozisyonlar == null || slotLokalPozisyonlar.Length != slotSayisi)
        {
            Debug.LogWarning($"[BalikKasaSistemi] '{gameObject.name}': slotLokalPozisyonlar dizisi slotSayisi ({slotSayisi}) ile uyu�muyor! Inspector'dan ayarla.", this);
            // Fallback: e�it aral�kl� pozisyonlar olu�tur
            if (slotLokalPozisyonlar == null || slotLokalPozisyonlar.Length == 0)
            {
                slotLokalPozisyonlar = new Vector3[slotSayisi];
                for (int i = 0; i < slotSayisi; i++)
                {
                    // Basit grid d�zeni (3x3 �rnek)
                    int satir = i / 3;
                    int sutun = i % 3;
                    slotLokalPozisyonlar[i] = new Vector3(sutun * 0.3f - 0.3f, 0f, satir * 0.3f - 0.3f);
                }
            }
        }

        if (slotLokalRotasyonlar == null || slotLokalRotasyonlar.Length != slotSayisi)
        {
            slotLokalRotasyonlar = new Vector3[slotSayisi];
            // Hepsi s�f�r rotasyon
        }

        // Sahnede zaten child varsa onlar� slotlara ata
        MevcutChildlariSlotlaraAta();
    }

    void MevcutChildlariSlotlaraAta()
    {
        int childIndex = 0;
        for (int i = 0; i < slotSayisi && childIndex < urunContainer.childCount; i++)
        {
            Transform child = urunContainer.GetChild(childIndex);
            if (child.gameObject.activeSelf)
            {
                slotIcerikleri[i] = child.gameObject;

                // Pozisyonu slot pozisyonuna snap et
                child.localPosition = slotLokalPozisyonlar[i];
                child.localRotation = Quaternion.Euler(slotLokalRotasyonlar[i]);
            }
            childIndex++;
        }
    }

    /// <summary>
    /// Sondan ba�layarak ilk dolu slottaki �r�n� al
    /// </summary>
    public GameObject UrunAl()
    {
        for (int i = slotSayisi - 1; i >= 0; i--)
        {
            if (slotIcerikleri[i] != null && slotIcerikleri[i].activeSelf)
            {
                GameObject urun = slotIcerikleri[i];
                slotIcerikleri[i] = null;

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

        for (int i = 0; i < slotSayisi; i++)
        {
            if (slotIcerikleri[i] == null)
            {
                slotIcerikleri[i] = urun;

                urun.transform.SetParent(urunContainer);
                urun.transform.localPosition = slotLokalPozisyonlar[i];
                urun.transform.localRotation = Quaternion.Euler(slotLokalRotasyonlar[i]);

                // Scale d�zeltme (parent'a ge�ince bozulabilir)
                // Gerekirse buraya scale ayar� eklenebilir

                return;
            }
        }

        // T�m slotlar doluysa - normalde buraya d��memeli (kapasite kontrol� var)
        Debug.LogWarning($"[BalikKasaSistemi] '{gameObject.name}': T�m slotlar dolu, �r�n eklenemedi!", this);
    }

    /// <summary>
    /// Bu �r�n kasaya b�rak�labilir mi?
    /// </summary>
    public bool UrunKabulEdilirMi(GameObject urun)
    {
        if (urun == null) return false;

        // Kapasite kontrol�
        if (MevcutUrunSayisi >= slotSayisi)
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
            for (int i = 0; i < slotSayisi; i++)
            {
                if (slotIcerikleri != null && slotIcerikleri[i] != null && slotIcerikleri[i].activeSelf)
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
            if (slotIcerikleri == null) return 0;
            for (int i = 0; i < slotSayisi; i++)
            {
                if (slotIcerikleri[i] != null && slotIcerikleri[i].activeSelf)
                    sayac++;
            }
            return sayac;
        }
    }

    /// <summary>
    /// Kasa dolu mu?
    /// </summary>
    public bool DoluMu => MevcutUrunSayisi >= slotSayisi;

    /// <summary>
    /// Maksimum kapasite
    /// </summary>
    public int MaksimumKapasite => slotSayisi;

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
    /// Gun sonu temizligi: slotlardaki tum urunleri (baliklari) yok eder, slotlari bosaltir.
    /// Kasa objesi ve slot pozisyonlari korunur.
    /// </summary>
    public void IciniBosalt()
    {
        if (slotIcerikleri == null) return;

        for (int i = 0; i < slotIcerikleri.Length; i++)
        {
            if (slotIcerikleri[i] == null) continue;

            // Icerideki parca bir KAP ise silinmez; slotunda kalir, sadece ici bosalir.
            if (KapYardimcisi.ParcayiIsle(slotIcerikleri[i])) continue;

            slotIcerikleri[i] = null;
        }
    }

    // ============================================================
    // EDITOR YARDIMCI METODLARI
    // ============================================================

#if UNITY_EDITOR
    /// <summary>
    /// Context menu: Mevcut child'lar�n pozisyonlar�n� slot dizisine kaydet.
    /// Sahneye bal�klar� yerle�tir, bu fonksiyonu �al��t�r, sonra bal�klar� silebilirsin.
    /// </summary>
    [ContextMenu("Mevcut Childlardan Slot Pozisyonlarini Al")]
    void ChildlardanSlotPozisyonuAl()
    {
        Transform container = urunContainer != null ? urunContainer : transform;

        slotSayisi = container.childCount;
        slotLokalPozisyonlar = new Vector3[slotSayisi];
        slotLokalRotasyonlar = new Vector3[slotSayisi];

        for (int i = 0; i < slotSayisi; i++)
        {
            Transform child = container.GetChild(i);
            slotLokalPozisyonlar[i] = child.localPosition;
            slotLokalRotasyonlar[i] = child.localRotation.eulerAngles;
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[BalikKasaSistemi] {slotSayisi} slot pozisyonu kaydedildi!", this);
    }

    /// <summary>
    /// Slotlar� sahnede gizmo olarak g�ster (bo� slotlar sar�, dolu slotlar ye�il)
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (slotLokalPozisyonlar == null) return;

        Transform container = urunContainer != null ? urunContainer : transform;

        for (int i = 0; i < slotLokalPozisyonlar.Length; i++)
        {
            Vector3 worldPos = container.TransformPoint(slotLokalPozisyonlar[i]);

            bool dolu = slotIcerikleri != null && i < slotIcerikleri.Length
                        && slotIcerikleri[i] != null;

            Gizmos.color = dolu ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(worldPos, 0.05f);
            Gizmos.DrawLine(worldPos, worldPos + container.up * 0.1f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(worldPos + Vector3.up * 0.12f, $"Slot {i}");
#endif
        }
    }
#endif
}