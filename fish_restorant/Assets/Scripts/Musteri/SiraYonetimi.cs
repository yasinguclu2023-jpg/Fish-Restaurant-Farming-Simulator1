using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Restoran sirasi yoneticisi (Singleton).
///
/// CALISMA MANTIGI (dinamik hedef):
/// - Slotlar onceden sabitlenmez. Sisteme katilan her musteri, o an EN ONDEKI BOS slotu
///   hedef alir. Bir slot kapildikca (o noktaya varan musteri claim ettikce) arkadakiler
///   otomatik olarak bir sonraki bos slota yonelir.
/// - Boylece musteriler varis sirasina gore ONDEN ARKAYA SIKISIK dizilir; uzun/kisa
///   yol farkindan dogan bosluklar olusmaz.
/// - Kapasite (orn. 5) dolunca yeni musteri sisteme katilamaz (uretici durur).
///
/// KURULUM:
/// 1. Sahnede bos bir GameObject olustur -> bu scripti ekle.
/// 2. Sira noktalarini (bos Transform'lar) sahneye diz, "siraNoktalari" listesine
///    EN ONDEN (index 0) EN ARKAYA dogru sirayla ata.
/// </summary>
public class SiraYonetimi : MonoBehaviour
{
    public static SiraYonetimi Instance { get; private set; }

    [Header("Sira Noktalari")]
    [Tooltip("EN ONDEN (index 0) EN ARKAYA dogru sirayla atanir. Musteriler buralarda durur.")]
    [SerializeField] private List<Transform> siraNoktalari = new List<Transform>();

    [Header("Ayarlar")]
    [Tooltip("Ayni anda sirada olabilecek (yuruyen + bekleyen) maksimum musteri. 0 = nokta sayisi kadar.")]
    [SerializeField] private int maksimumKapasite = 5;

    // Her slotu tutan musteri (null = bos). Boyut = siraNoktalari.Count
    private MusteriAI[] slotSahipleri;

    // Sistemdeki tum musteriler (siraya yuruyenler + bekleyenler) - kapasite icin.
    private readonly List<MusteriAI> kayitliMusteriler = new List<MusteriAI>();

    public int SistemdekiSayi => kayitliMusteriler.Count;

    public int Kapasite
    {
        get
        {
            int noktaSayisi = siraNoktalari != null ? siraNoktalari.Count : 0;
            if (maksimumKapasite <= 0) return noktaSayisi;
            return Mathf.Min(maksimumKapasite, noktaSayisi);
        }
    }

    public bool SiraDoluMu => kayitliMusteriler.Count >= Kapasite;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            SlotDizisiHazirla();
        }
        else
        {
            Debug.LogWarning("[SiraYonetimi] Sahnede birden fazla SiraYonetimi var. Fazlasi yok sayildi.", this);
            Destroy(this);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>GUN YENIDEN BASLARKEN cagrilir: tum slotlari ve kayitli musterileri temizler.</summary>
    public void Sifirla()
    {
        if (slotSahipleri != null)
            for (int i = 0; i < slotSahipleri.Length; i++)
                slotSahipleri[i] = null;

        kayitliMusteriler.Clear();
    }

    private void SlotDizisiHazirla()
    {
        int n = siraNoktalari != null ? siraNoktalari.Count : 0;
        slotSahipleri = new MusteriAI[n];
    }

    /// <summary>
    /// Musteriyi sisteme katar (kapasiteden yer ayirir). Basarili ise true.
    /// Sira doluysa false (musteri beklemeli / sonra tekrar denemeli).
    /// </summary>
    public bool SistemeKatil(MusteriAI musteri)
    {
        if (musteri == null) return false;
        if (kayitliMusteriler.Contains(musteri)) return true;
        if (SiraDoluMu) return false;

        kayitliMusteriler.Add(musteri);
        return true;
    }

    /// <summary>
    /// Bu musterinin hedeflemesi gereken slot index'i:
    /// kendi kaptigi slot varsa o, yoksa o an en ondeki bos slot. (-1 = bos slot yok)
    /// </summary>
    public int HedefSlotIndex(MusteriAI musteri)
    {
        int sahip = SahipOlunanSlot(musteri);
        if (sahip >= 0) return sahip;
        return EnOndekiBosSlot();
    }

    /// <summary>HedefSlotIndex'in Transform karsiligini dondurur.</summary>
    public Transform HedefNokta(MusteriAI musteri) => GuvenliNokta(HedefSlotIndex(musteri));

    /// <summary>
    /// Belirtilen slotu bu musteri icin kapmayi dener.
    /// Slot bossa kapar ve true doner; zaten bu musterinindiyse true; baskasindaysa false.
    /// </summary>
    public bool SlotClaimDene(MusteriAI musteri, int index)
    {
        if (slotSahipleri == null || index < 0 || index >= slotSahipleri.Length) return false;
        if (slotSahipleri[index] == musteri) return true;
        if (slotSahipleri[index] == null)
        {
            slotSahipleri[index] = musteri;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Musteri sistemden tamamen ayrilir (slotunu birakir, kapasiteden duser).
    /// (Siparis/yeme adimlari eklenince kullanilacak.)
    /// </summary>
    public void SistemdenAyril(MusteriAI musteri)
    {
        if (musteri == null) return;

        int s = SahipOlunanSlot(musteri);
        if (s >= 0) slotSahipleri[s] = null;
        kayitliMusteriler.Remove(musteri);

        SirayiSikistir();
    }

    /// <summary>
    /// Dolu slotlari one dogru sikistirir (FIFO sirayi koruyarak): onden biri ayrilinca
    /// arkadakiler birer slot one kayar ve o slota dogru yurumeye baslar.
    /// </summary>
    private void SirayiSikistir()
    {
        if (slotSahipleri == null) return;

        int yeni = 0;
        for (int i = 0; i < slotSahipleri.Length; i++)
        {
            MusteriAI c = slotSahipleri[i];
            if (c == null) continue;

            if (i != yeni)
            {
                slotSahipleri[yeni] = c;
                slotSahipleri[i] = null;
                c.SiradaIlerlemeyeBasla(); // yeni (one) slotuna yuru
            }
            yeni++;
        }
    }

    /// <summary>Bu musteri en ondeki (slot 0) musteri mi?</summary>
    public bool BastaMi(MusteriAI musteri) => SahipOlunanSlot(musteri) == 0;

    private int SahipOlunanSlot(MusteriAI musteri)
    {
        if (slotSahipleri == null) return -1;
        for (int i = 0; i < slotSahipleri.Length; i++)
            if (slotSahipleri[i] == musteri) return i;
        return -1;
    }

    private int EnOndekiBosSlot()
    {
        if (slotSahipleri == null) return -1;
        for (int i = 0; i < slotSahipleri.Length; i++)
            if (slotSahipleri[i] == null) return i;
        return -1;
    }

    private Transform GuvenliNokta(int index)
    {
        if (siraNoktalari == null || index < 0 || index >= siraNoktalari.Count)
            return null;
        return siraNoktalari[index];
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (siraNoktalari == null) return;

        for (int i = 0; i < siraNoktalari.Count; i++)
        {
            Transform n = siraNoktalari[i];
            if (n == null) continue;

            bool dolu = Application.isPlaying && slotSahipleri != null
                        && i < slotSahipleri.Length && slotSahipleri[i] != null;

            Gizmos.color = dolu ? Color.red : (i == 0 ? Color.green : Color.yellow);
            Gizmos.DrawWireSphere(n.position, 0.25f);
            Gizmos.DrawLine(n.position, n.position + n.forward * 0.4f);

            UnityEditor.Handles.Label(n.position + Vector3.up * 0.3f, "Sira " + i);

            if (i > 0 && siraNoktalari[i - 1] != null)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(siraNoktalari[i - 1].position, n.position);
            }
        }
    }
#endif
}
