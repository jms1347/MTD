using UnityEngine;

public class FreeLookCamera : MonoBehaviour
{
    private Transform _transform;
    public float sensitivity = 2f;  // Mouse sensitivity
    public float moveSpeed = 2f;    // Player movement speed
    public float smoothTime = 0.3f;  // Smooth time for camera movement

    float mouseX, mouseY;
    Vector3 currentVelocity;

    void Awake()
    {
        _transform = transform;
        Cursor.lockState = CursorLockMode.Locked;  // Lock cursor to the center of the screen
    }

    void Update()
    {
        HandleMouseLook();
        HandlePlayerMovement();
    }

    void HandleMouseLook()
    {
        mouseX += Input.GetAxis("Mouse X") * sensitivity;
        mouseY -= Input.GetAxis("Mouse Y") * sensitivity;  // Invert Y-axis for more intuitive control

        mouseY = Mathf.Clamp(mouseY, -90f, 90f);  // Limit vertical rotation to prevent camera flipping

        _transform.localRotation = Quaternion.Euler(mouseY, mouseX, 0f);  // Rotate camera vertically
        // playerBody.Rotate(Vector3.up * mouseX);  // Rotate player's body horizontally
    }

    void HandlePlayerMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        float worldVertical = 0f;

        if (Input.GetKey(KeyCode.E))
        {
            worldVertical = 1;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            worldVertical = -1;
        }

        Vector3 moveDirection = (_transform.forward * vertical + _transform.right * horizontal + Vector3.up * worldVertical).normalized;
        Vector3 moveVelocity = moveDirection * moveSpeed;


        _transform.position = Vector3.SmoothDamp(_transform.position, _transform.position + moveVelocity, ref currentVelocity, smoothTime);
    }
}