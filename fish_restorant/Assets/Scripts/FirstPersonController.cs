using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 90f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraPitch = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Lock and hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // If no camera is assigned, try to find the main camera
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();

        // NOT: Escape tusu artik DuraklatmaMenusu tarafindan yonetiliyor.
        // (Imlec kilidi ve oyunun durdurulmasi orada yapiliyor.)
    }

    void HandleMovement()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");     // W/S

        // Determine speed (sprint with Shift)
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

        // Calculate movement direction relative to where player is facing
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        moveDirection.Normalize();

        // Apply movement
        Vector3 move = moveDirection * currentSpeed;

        // Apply gravity
        if (controller.isGrounded)
        {
            verticalVelocity = -2f; // Small downward force to keep grounded
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;

        // Move the character
        controller.Move(move * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate the player body left and right (Y-axis rotation)
        transform.Rotate(Vector3.up * mouseX);

        // Rotate the camera up and down (X-axis rotation)
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }
}