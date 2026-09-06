using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bir mutfak monitoru. Gelen siparisleri GRID icinde fis olarak gosterir.
///
/// Her siparis icin fis prefab'indan (SiparisGosterici) bir KOPYA olusturulur ve
/// container'a (Grid Layout Group) eklenir. Boylece elle Image kopyalamak gerekmez,
/// paylasim sorunu olmaz; her fisin kendi ikonlari olur.
///
/// KURULUM:
/// 1. Monitorde bir "container" objesi olustur -> Grid Layout Group ekle
///    (Constraint = Fixed Column Count = 3 -> 3 sutun, 2 satir = 6 fis).
/// 2. Bir "SiparisFisi" prefab'i yap: SiparisGosterici scripti (Tiklanabilir KAPALI) +
///    balik/sebze/icecek/yan ikon slotlari + masa no text.
/// 3. Bu script'i monitor objesine ekle; fisContainer ve fisPrefab'i ata.
/// </summary>
public class MutfakEkrani : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Fislerin ekleneceği yer (Grid Layout Group olan obje).")]
    [SerializeField] private Transform fisContainer;

    [Tooltip("Bir siparis fisi prefab'i (SiparisGosterici, Tiklanabilir kapali).")]
    [SerializeField] private SiparisGosterici fisPrefab;

    [Header("Kapasite")]
    [Tooltip("Bu monitorde ayni anda gosterilebilecek maksimum fis.")]
    [SerializeField] private int maxFis = 6;

    // En eski en basta (index 0). fisler ve fisSiparisleri paralel (ayni index).
    private readonly List<SiparisGosterici> fisler = new List<SiparisGosterici>();
    private readonly List<Siparis> fisSiparisleri = new List<Siparis>();

    public int FisSayisi => fisler.Count;
    public bool DoluMu => fisler.Count >= maxFis;

    /// <summary>Yeni bir siparis fisi ekler (en sona).</summary>
    public void FisEkle(Siparis siparis)
    {
        if (fisPrefab == null) { Debug.LogWarning("[MutfakEkrani] fisPrefab ATANMAMIS!", this); return; }
        if (fisContainer == null) { Debug.LogWarning("[MutfakEkrani] fisContainer ATANMAMIS!", this); return; }
        if (siparis == null) { Debug.LogWarning("[MutfakEkrani] siparis null!", this); return; }
        if (DoluMu) return; // kapasite (6) dolu, ekleme

        SiparisGosterici fis = Instantiate(fisPrefab, fisContainer);
        fis.Goster(siparis);
        fisler.Add(fis);
        fisSiparisleri.Add(siparis);
    }

    /// <summary>En eski (ilk) fisi kaldirir.</summary>
    public void EnEskiFisiKaldir()
    {
        if (fisler.Count == 0) return;
        FisiSil(0);
    }

    /// <summary>Belirli bir siparise ait fisi kaldirir. Bulunup silindiyse true.</summary>
    public bool FisKaldir(Siparis siparis)
    {
        if (siparis == null) return false;
        int idx = fisSiparisleri.IndexOf(siparis);
        if (idx < 0) return false;
        FisiSil(idx);
        return true;
    }

    private void FisiSil(int index)
    {
        SiparisGosterici fis = fisler[index];
        fisler.RemoveAt(index);
        fisSiparisleri.RemoveAt(index);
        if (fis != null) Destroy(fis.gameObject);
    }

    /// <summary>Tum fisleri temizler.</summary>
    public void TumFisleriKaldir()
    {
        for (int i = 0; i < fisler.Count; i++)
            if (fisler[i] != null) Destroy(fisler[i].gameObject);
        fisler.Clear();
        fisSiparisleri.Clear();
    }
}
