using UnityEngine;

/// <summary>
/// Fragman / tanýtým çekimleri için serbest uçan kamera.
/// F9 ile oyuncu kontrolünü kapatýp kamerayý devralýr, tekrar F9 ile geri verir.
///
/// KURULUM:
/// 1. Sahneye boþ bir GameObject oluþtur, adýný "SinematikKamera" yap.
/// 2. Üzerine Camera component'i ekle (Camera'yý Disabled býrakabilirsin, script kendi açýp kapatýyor).
/// 3. Bu script'i ekle.
/// 4. Inspector'da:
///    - Oyuncu Kamerasi  -> karakterin kamerasý
///    - Kapatilacak Scriptler -> karakter controller, mouse look, etkileþim scriptleri
///    - Gizlenecek UI -> HUD canvas'larý (fragmanda HUD görünmesin)
///
/// KONTROLLER:
/// F9    : sinematik moda gir / çýk
/// F10   : aðýr çekim aç / kapat
/// WASD  : yatay hareket   |   E/Q : yukarý / aþaðý
/// Mouse : bakýþ           |   Scroll : hýz ayarý
/// Shift : hýzlý           |   Ctrl : yavaþ (yakýn plan için)
/// </summary>
[RequireComponent(typeof(Camera))]
public class SinematikKamera : MonoBehaviour
{
    [Header("Kontrol")]
    [SerializeField] private KeyCode acKapaTusu = KeyCode.F9;
    [SerializeField] private KeyCode agirCekimTusu = KeyCode.F10;
    [SerializeField] private Camera oyuncuKamerasi;
    [SerializeField] private MonoBehaviour[] kapatilacakScriptler;
    [SerializeField] private GameObject[] gizlenecekUI;

    [Header("Hareket")]
    [SerializeField] private float hiz = 15f;
    [SerializeField] private float hizliCarpan = 4f;
    [SerializeField] private float yavasCarpan = 0.2f;
    [SerializeField] private float mouseHassasiyet = 2f;

    [Header("Yumusatma (fragman icin kritik)")]
    [Tooltip("Buyudukce hareket daha agir ve sinematik olur. 0.25-0.5 arasi iyi.")]
    [SerializeField, Range(0.02f, 1.5f)] private float konumYumusatma = 0.3f;
    [Tooltip("Bakis yumusatmasi. 0.08-0.2 arasi iyi.")]
    [SerializeField, Range(0.01f, 1f)] private float bakisYumusatma = 0.12f;

    [Header("Kamera Ayarlari")]
    [Tooltip("Tepeden cekimde uzak objelerin kaybolmamasi icin.")]
    [SerializeField] private float uzakKesmeMesafesi = 5000f;
    [SerializeField] private float agirCekimOrani = 0.35f;

    private Camera _kamera;
    private bool _aktif;
    private bool _agirCekim;

    private Vector3 _hedefKonum;
    private Vector3 _konumHizi;
    private float _yaw, _pitch;
    private float _hedefYaw, _hedefPitch;

    private void Awake()
    {
        _kamera = GetComponent<Camera>();
        _kamera.enabled = false;
        _kamera.farClipPlane = uzakKesmeMesafesi;

        // Sahnede ikinci AudioListener uyarisi cikmasin
        var dinleyici = GetComponent<AudioListener>();
        if (dinleyici != null) dinleyici.enabled = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(acKapaTusu)) ModDegistir(!_aktif);
        if (!_aktif) return;

        if (Input.GetKeyDown(agirCekimTusu)) AgirCekimDegistir();

        // unscaledDeltaTime: agir cekimde oyun yavaslar ama kamera normal hizda kalir
        float dt = Time.unscaledDeltaTime;

        Bakis(dt);
        Hareket(dt);
    }

    private void Bakis(float dt)
    {
        _hedefYaw += Input.GetAxisRaw("Mouse X") * mouseHassasiyet;
        _hedefPitch = Mathf.Clamp(_hedefPitch - Input.GetAxisRaw("Mouse Y") * mouseHassasiyet, -89f, 89f);

        // Framerate'ten bagimsiz yumusatma
        float t = 1f - Mathf.Exp(-dt / Mathf.Max(bakisYumusatma, 0.0001f));
        _yaw = Mathf.Lerp(_yaw, _hedefYaw, t);
        _pitch = Mathf.Lerp(_pitch, _hedefPitch, t);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private void Hareket(float dt)
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
            hiz = Mathf.Clamp(hiz * (1f + scroll * 0.15f), 0.5f, 500f);

        Vector3 girdi = Vector3.zero;
        girdi += transform.right * Input.GetAxisRaw("Horizontal");
        girdi += transform.forward * Input.GetAxisRaw("Vertical");
        if (Input.GetKey(KeyCode.E)) girdi += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) girdi += Vector3.down;

        float anlikHiz = hiz;
        if (Input.GetKey(KeyCode.LeftShift)) anlikHiz *= hizliCarpan;
        if (Input.GetKey(KeyCode.LeftControl)) anlikHiz *= yavasCarpan;

        _hedefKonum += girdi.normalized * (anlikHiz * dt);
        transform.position = Vector3.SmoothDamp(
            transform.position, _hedefKonum, ref _konumHizi, konumYumusatma, Mathf.Infinity, dt);
    }

    private void ModDegistir(bool ac)
    {
        _aktif = ac;
        _kamera.enabled = ac;

        if (oyuncuKamerasi != null) oyuncuKamerasi.enabled = !ac;

        if (kapatilacakScriptler != null)
            foreach (var s in kapatilacakScriptler)
                if (s != null) s.enabled = !ac;

        if (gizlenecekUI != null)
            foreach (var ui in gizlenecekUI)
                if (ui != null) ui.SetActive(!ac);

        Cursor.lockState = ac ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !ac;

        if (!ac)
        {
            if (_agirCekim) AgirCekimDegistir();
            return;
        }

        // Oyuncu kamerasinin bulundugu noktadan devral
        if (oyuncuKamerasi != null)
            transform.SetPositionAndRotation(oyuncuKamerasi.transform.position, oyuncuKamerasi.transform.rotation);

        _hedefKonum = transform.position;
        _konumHizi = Vector3.zero;

        Vector3 aci = transform.eulerAngles;
        _yaw = _hedefYaw = aci.y;
        _pitch = _hedefPitch = (aci.x > 180f) ? aci.x - 360f : aci.x;
    }

    private void AgirCekimDegistir()
    {
        _agirCekim = !_agirCekim;
        Time.timeScale = _agirCekim ? agirCekimOrani : 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private void OnDisable()
    {
        if (_agirCekim)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }
}