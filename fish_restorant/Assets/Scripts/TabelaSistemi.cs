using UnityEngine;

// Mevcut RaycastSistemi ile entegre �al���r.
// Tabelaya bakarken etkile�im tu�una bas�l�nca tabelay� 180� d�nd�r�r
// ve ZamanSistemi'ni a�ar/kapat�r (toggle). Kilitli imle�le uyumludur.
public class TabelaSistemi : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Sahnedeki ZamanSistemi objesini s�r�kle")]
    public ZamanSistemi zamanSistemi;

    [Tooltip("Bo� b�rak�l�rsa sahnede otomatik bulunur")]
    public RaycastSistemi raycastSistemi;

    [Tooltip("Tabela aktifken musteri uretir, pasifken durdurur. Uretici objesini surukle.")]
    public MusteriUretici musteriUretici;

    [Header("D�n�� Ayarlar�")]
    [Tooltip("T�klay�nca ka� derece d�ns�n")]
    public float donusAcisi = 180f;

    [Tooltip("Hangi eksende d�ns�n (genelde Y = yukar�)")]
    public Vector3 donusEkseni = Vector3.up;

    [Tooltip("D�n�� h�z� (derece/saniye). 0 = an�nda d�ner")]
    public float donusHizi = 360f;

    [Header("Etkile�im")]
    [Tooltip("Tabelaya bakarken bas�lacak tu� (varsay�lan: sol fare)")]
    public KeyCode etkilesimTusu = KeyCode.Mouse0;

    [Header("Ses (opsiyonel)")]
    [Tooltip("AudioSource otomatik olu�turulur, sadece klipleri ver")]
    public AudioClip acmaSesi;
    public AudioClip kapamaSesi;

    private AudioSource sesKaynagi;
    private bool acik = false;
    private Quaternion kapaliRotasyon;
    private Quaternion acikRotasyon;
    private Quaternion hedefRotasyon;

    void Awake()
    {
        // Ses kayna��n� kendisi olu�tursun
        sesKaynagi = GetComponent<AudioSource>();
        if (sesKaynagi == null)
            sesKaynagi = gameObject.AddComponent<AudioSource>();

        sesKaynagi.playOnAwake = false;
        sesKaynagi.spatialBlend = 1f; // 3D ses: tabelan�n oldu�u yerden gelsin
    }

    void Start()
    {
        if (raycastSistemi == null)
            raycastSistemi = FindObjectOfType<RaycastSistemi>();

        kapaliRotasyon = transform.rotation;
        acikRotasyon = transform.rotation * Quaternion.AngleAxis(donusAcisi, donusEkseni);
        hedefRotasyon = kapaliRotasyon;
    }

    void Update()
    {
        // D�n��� hedefe do�ru yumu�at
        if (donusHizi > 0f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, hedefRotasyon, donusHizi * Time.deltaTime);
        else
            transform.rotation = hedefRotasyon;

        if (raycastSistemi == null) return;

        // Bu tabelaya bakarken tu�a bas�ld� m�?
        if (Input.GetKeyDown(etkilesimTusu) && BuTabelayaBakiliyorMu())
        {
            Degistir();
        }
    }

    bool BuTabelayaBakiliyorMu()
    {
        GameObject bakilan = raycastSistemi.BakilanObje;
        if (bakilan == null) return false;

        // Bak�lan obje bu tabela ya da onun bir �ocu�u mu?
        return bakilan == gameObject || bakilan.transform.IsChildOf(transform);
    }

    void Degistir()
    {
        acik = !acik;

        if (acik)
        {
            hedefRotasyon = acikRotasyon;
            if (zamanSistemi != null) zamanSistemi.Ac();
            if (musteriUretici != null) musteriUretici.UretimiBaslat();
            SesCal(acmaSesi);
        }
        else
        {
            hedefRotasyon = kapaliRotasyon;
            if (zamanSistemi != null) zamanSistemi.Kapat();
            if (musteriUretici != null) musteriUretici.UretimiDurdur();
            SesCal(kapamaSesi);
        }
    }

    /// <summary>
    /// GUN YENIDEN BASLARKEN cagrilir (GunYoneticisi): tabelayi toggle etmeden
    /// dogrudan KAPALI konuma alir (gorsel + ic durum). Zaman/uretim GunYoneticisi
    /// tarafindan ayrica kapatilir.
    /// </summary>
    public void KapaliyaAl()
    {
        acik = false;
        hedefRotasyon = kapaliRotasyon;
    }

    void SesCal(AudioClip clip)
    {
        if (clip != null)
            sesKaynagi.PlayOneShot(clip);
    }
}