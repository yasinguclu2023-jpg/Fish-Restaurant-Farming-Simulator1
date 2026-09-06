using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Kesme tahtas� sistemi.
/// Eldeki nesne sadece Inspector'dan atanan slot pozisyonlar�na yerle�tirilebilir.
/// �zg�r hareket yoktur - nesne slota snap olur.
/// 
/// NOT: Herhangi bir slotun alt�nda child obje varsa, tahtaya yeni nesne eklenemez!
/// Bu sayede kesilmi� nesne tahtadayken yeni nesne koymak engellenir.
/// 
/// Kurulum:
/// 1. Kesme tahtas� objesine bu scripti ekle + Collider ekle
/// 2. Bo� GameObject'ler olu�tur (istedi�in yere koy)
/// 3. Inspector'da "Slot Pozisyonlar�" dizisine bu empty'leri s�r�kle
/// 4. Kabul edilen tag'leri ayarla
/// </summary>
public class KesmeTahtasiSistemi : MonoBehaviour
{
    [Header("Slot Pozisyonlar�")]
    [Tooltip("Nesne koyulacak pozisyonlar. Bo� GameObject'leri buraya s�r�kle.")]
    [SerializeField] private Transform[] slotPozisyonlari;

    [Header("Kabul Edilen �r�nler")]
    [Tooltip("Bu tahtaya konabilecek tag'ler. Bo� b�rak�rsan her �eyi kabul eder.")]
    [SerializeField] private string[] kabulEdilenTagler;

    [Header("Yerle�tirme Ayarlar�")]
    [SerializeField] private bool kinematicYap = true;
    [SerializeField] private bool colliderKapat = false;

    // Runtime slot verileri
    private GameObject[] slotNesneleri;

    void Awake()
    {
        if (slotPozisyonlari == null || slotPozisyonlari.Length == 0)
        {
            Debug.LogError($"[KesmeTahtasi] '{gameObject.name}' - Slot pozisyonlar� atanmam��! Inspector'dan empty objeleri s�r�kle.");
            return;
        }

        slotNesneleri = new GameObject[slotPozisyonlari.Length];
        Debug.Log($"[KesmeTahtasi] '{gameObject.name}' ba�lat�ld�. Slot say�s�: {slotPozisyonlari.Length}");
    }

    /// <summary>
    /// Herhangi bir slotun alt�nda child obje var m� kontrol et.
    /// E�er varsa tahta kilitlidir, yeni nesne eklenemez.
    /// </summary>
    bool TahtadaChildVarMi()
    {
        if (slotPozisyonlari == null) return false;

        for (int i = 0; i < slotPozisyonlari.Length; i++)
        {
            if (slotPozisyonlari[i] != null && slotPozisyonlari[i].childCount > 0)
            {
                // Child'�n aktif oldu�undan emin ol
                foreach (Transform child in slotPozisyonlari[i])
                {
                    if (child.gameObject.activeSelf)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Nesneyi tahtaya yerle�tir - ilk bo� slota koyar.
    /// </summary>
    public bool NesneYerlestir(GameObject nesne, Vector3 orijinalScale, int orijinalLayer)
    {
        if (nesne == null || slotNesneleri == null) return false;

        // ===== YEN� KONTROL: Tahtada herhangi bir child varsa ekleme yapma =====
        if (TahtadaChildVarMi())
        {
            Debug.Log($"[KesmeTahtasi] Tahtada zaten nesne var! Yeni nesne eklenemez.");
            return false;
        }
        // =======================================================================

        if (!NesneKabulEdilirMi(nesne)) return false;

        // Ayn� nesne zaten tahtada m� kontrol et
        for (int i = 0; i < slotNesneleri.Length; i++)
        {
            if (slotNesneleri[i] == nesne)
                return false;
        }

        for (int i = 0; i < slotNesneleri.Length; i++)
        {
            if (!SlotDoluMu(i))
            {
                slotNesneleri[i] = nesne;

                Transform slot = slotPozisyonlari[i];
                nesne.transform.SetParent(slot, false);
                nesne.transform.localPosition = Vector3.zero;
                nesne.transform.localRotation = Quaternion.identity;
                nesne.transform.localScale = LokalScaleHesapla(orijinalScale, slot);

                SetLayerRecursive(nesne, orijinalLayer);

                // Nesne artık tahtanın child'ı → outline'ı tahtayla birleştir
                RaycastSistemi.HiyerarsiDegisti(nesne);

                if (kinematicYap && nesne.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                Collider[] colliders = nesne.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < colliders.Length; c++)
                {
                    colliders[c].enabled = !colliderKapat;
                    if (!colliderKapat)
                        colliders[c].isTrigger = false;
                }

                if (nesne.TryGetComponent(out NesneSesVerisi sesVerisi))
                    sesVerisi.BirakmaSesiCal(slot.position);

                Debug.Log($"[KesmeTahtasi] '{nesne.name}' slot {i}'e yerle�tirildi.");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Slot dolu mu kontrol et (Destroy edilmi� objeleri de temizler)
    /// </summary>
    bool SlotDoluMu(int index)
    {
        if (slotNesneleri[index] == null)
        {
            slotNesneleri[index] = null; // Unity destroyed objeyi ger�ek null yap
            return false;
        }
        return true;
    }

    /// <summary>
    /// Sondan ba�layarak ilk dolu slottaki nesneyi al
    /// </summary>
    public GameObject NesneAl()
    {
        if (slotNesneleri == null) return null;

        for (int i = slotNesneleri.Length - 1; i >= 0; i--)
        {
            if (SlotDoluMu(i) && slotNesneleri[i].activeSelf)
            {
                GameObject nesne = slotNesneleri[i];
                slotNesneleri[i] = null;
                nesne.transform.SetParent(null);

                // Nesne hiyerarşiden çıktı → outline'ı tahtadan ayır
                RaycastSistemi.HiyerarsiDegisti(nesne);
                return nesne;
            }
        }

        return null;
    }

    /// <summary>
    /// Bu nesne tahtaya konabilir mi?
    /// </summary>
    public bool NesneKabulEdilirMi(GameObject nesne)
    {
        if (nesne == null || slotNesneleri == null) return false;

        // ===== YEN� KONTROL: Tahtada child varsa kabul etme =====
        if (TahtadaChildVarMi())
        {
            return false;
        }
        // ========================================================

        bool bosSlotVar = false;
        for (int i = 0; i < slotNesneleri.Length; i++)
        {
            if (!SlotDoluMu(i))
            {
                bosSlotVar = true;
                break;
            }
        }
        if (!bosSlotVar) return false;

        if (kabulEdilenTagler == null || kabulEdilenTagler.Length == 0)
            return true;

        string nesneTag = nesne.tag;
        for (int i = 0; i < kabulEdilenTagler.Length; i++)
        {
            if (kabulEdilenTagler[i] == nesneTag)
                return true;
        }

        return false;
    }

    Vector3 LokalScaleHesapla(Vector3 orijinalScale, Transform parent)
    {
        Vector3 parentScale = parent.lossyScale;
        return new Vector3(
            orijinalScale.x / parentScale.x,
            orijinalScale.y / parentScale.y,
            orijinalScale.z / parentScale.z
        );
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    /// <summary>
    /// Tahtada nesne var m�? (Child kontrol� ile)
    /// </summary>
    public bool NesneVarMi
    {
        get
        {
            // �nce child kontrol� yap
            if (TahtadaChildVarMi()) return true;

            // Sonra dizi kontrol�
            if (slotNesneleri == null) return false;
            for (int i = 0; i < slotNesneleri.Length; i++)
            {
                if (SlotDoluMu(i) && slotNesneleri[i].activeSelf)
                    return true;
            }
            return false;
        }
    }

    public int MevcutNesneSayisi
    {
        get
        {
            if (slotNesneleri == null) return 0;
            int sayac = 0;
            for (int i = 0; i < slotNesneleri.Length; i++)
            {
                if (SlotDoluMu(i) && slotNesneleri[i].activeSelf)
                    sayac++;
            }
            return sayac;
        }
    }

    /// <summary>
    /// Tahta dolu mu? Child varsa da dolu say�l�r.
    /// </summary>
    public bool DoluMu => TahtadaChildVarMi() || (slotNesneleri != null && MevcutNesneSayisi >= slotNesneleri.Length);

    public int BosSlotSayisi => TahtadaChildVarMi() ? 0 : (slotNesneleri != null ? slotNesneleri.Length - MevcutNesneSayisi : 0);
    public int MaksimumKapasite => slotNesneleri != null ? slotNesneleri.Length : 0;

    /// <summary>
    /// Tahtaya nesne eklenebilir mi? (Child kontrol� dahil)
    /// </summary>
    public bool EklenebilirMi => !TahtadaChildVarMi() && BosSlotSayisi > 0;

    /// <summary>
    /// �lk bo� slotun Transform'u. Preview g�stermek i�in kullan�l�r.
    /// Child varsa null d�ner (ekleme yap�lamaz).
    /// </summary>
    public Transform IlkBosSlot
    {
        get
        {
            // Child varsa bo� slot yok demektir
            if (TahtadaChildVarMi()) return null;

            if (slotNesneleri == null || slotPozisyonlari == null) return null;
            for (int i = 0; i < slotNesneleri.Length; i++)
            {
                if (slotNesneleri[i] == null)
                    return slotPozisyonlari[i];
            }
            return null;
        }
    }

    /// <summary>
    /// Son dolu slottaki nesneyi d�nd�r�r (almadan, sadece referans).
    /// KesmeSistemi taraf�ndan kullan�l�r.
    /// </summary>
    public GameObject SonDoluSlotNesnesi
    {
        get
        {
            if (slotNesneleri == null) return null;
            for (int i = slotNesneleri.Length - 1; i >= 0; i--)
            {
                if (SlotDoluMu(i) && slotNesneleri[i].activeSelf)
                    return slotNesneleri[i];
            }
            return null;
        }
    }

    /// <summary>
    /// Slot dizisindeki eski nesneyi yeni nesneyle de�i�tirir.
    /// KesmeSistemi nesneyi Destroy edip yenisini olu�turdu�unda �a�r�l�r.
    /// </summary>
    public void SlottakiNesneyiDegistir(GameObject eskiNesne, GameObject yeniNesne)
    {
        if (slotNesneleri == null) return;
        for (int i = 0; i < slotNesneleri.Length; i++)
        {
            if (slotNesneleri[i] == eskiNesne)
            {
                slotNesneleri[i] = yeniNesne;
                return;
            }
        }
    }
}