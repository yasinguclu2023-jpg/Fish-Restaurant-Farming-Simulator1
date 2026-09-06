using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Hareket Ayarlarý")]
    [SerializeField] private bool hareketAktif = true;
    [SerializeField] private float yuruyusHizi = 5f;
    [SerializeField] private float kosmaHizi = 8f;

    [Header("Zýplama Ayarlarý")]
    [SerializeField] private bool ziplamaAktif = true;
    [SerializeField] private float ziplamaGucu = 8f;
    [SerializeField] private float yercekimi = -20f;

    [Header("Fare Ayarlarý")]
    [SerializeField] private float fareDuyarliligi = 2f;
    [SerializeField] private Transform kameraTransform;
    [SerializeField] private float yukariLimit = -80f;
    [SerializeField] private float asagiLimit = 80f;

    private CharacterController controller;
    private Vector3 hiz;
    private float rotasyonX = 0f;
    private bool yerdeMi;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (kameraTransform == null)
        {
            kameraTransform = Camera.main.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        YerKontrol();
        HareketIsle();
        FareIsle();
    }

    void YerKontrol()
    {
        yerdeMi = controller.isGrounded;

        if (yerdeMi && hiz.y < 0)
        {
            hiz.y = -2f;
        }
    }

    void HareketIsle()
    {
        if (!hareketAktif) return;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        bool kosuyor = Input.GetKey(KeyCode.LeftShift);
        float mevcutHiz = kosuyor ? kosmaHizi : yuruyusHizi;

        Vector3 hareket = transform.right * x + transform.forward * z;
        controller.Move(hareket * mevcutHiz * Time.deltaTime);

        if (ziplamaAktif && Input.GetButtonDown("Jump") && yerdeMi)
        {
            hiz.y = Mathf.Sqrt(ziplamaGucu * -2f * yercekimi);
        }

        hiz.y += yercekimi * Time.deltaTime;
        controller.Move(hiz * Time.deltaTime);
    }

    void FareIsle()
    {
        float fareX = Input.GetAxis("Mouse X") * fareDuyarliligi;
        float fareY = Input.GetAxis("Mouse Y") * fareDuyarliligi;

        rotasyonX -= fareY;
        rotasyonX = Mathf.Clamp(rotasyonX, yukariLimit, asagiLimit);

        kameraTransform.localRotation = Quaternion.Euler(rotasyonX, 0f, 0f);
        transform.Rotate(Vector3.up * fareX);
    }

    void OnValidate()
    {
        if (yuruyusHizi < 0) yuruyusHizi = 0;
        if (kosmaHizi < yuruyusHizi) kosmaHizi = yuruyusHizi;
        if (ziplamaGucu < 0) ziplamaGucu = 0;
    }
}