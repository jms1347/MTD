using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MosquitoGameHumanController : MonoBehaviour
{
    private const float EyeHeight = 1.62f;
    private const float MoveSpeed = 4.2f;
    private const float Gravity = -18f;
    private const float LookSensitivity = 2.4f;

    [SerializeField] private Camera firstPersonCamera;

    private CharacterController controller;
    private float pitch;
    private float verticalVelocity;
    private bool activeRole;

    public bool IsActiveRole => activeRole;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (firstPersonCamera == null)
            firstPersonCamera = GetComponentInChildren<Camera>(true);
    }

    public void SetActiveRole(bool active)
    {
        activeRole = active;
        enabled = active;
        if (firstPersonCamera != null)
            firstPersonCamera.gameObject.SetActive(active);

        Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !active;
    }

    private void Update()
    {
        if (!activeRole)
            return;

        HandleLook();
        HandleMove();
    }

    private void HandleLook()
    {
        var mouseX = Input.GetAxisRaw("Mouse X") * LookSensitivity;
        var mouseY = Input.GetAxisRaw("Mouse Y") * LookSensitivity;
        transform.Rotate(Vector3.up, mouseX, Space.World);
        pitch = Mathf.Clamp(pitch - mouseY, -85f, 85f);
        if (firstPersonCamera != null)
            firstPersonCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMove()
    {
        var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        input = Vector3.ClampMagnitude(input, 1f);
        var move = transform.TransformDirection(input) * MoveSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += Gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    public static MosquitoGameHumanController Create(Transform parent, Vector3 position, Quaternion rotation)
    {
        var root = new GameObject("Human");
        root.transform.SetParent(parent, false);
        root.transform.SetPositionAndRotation(position, rotation);

        var controller = root.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.32f;
        controller.center = new Vector3(0f, 0.92f, 0f);

        var cameraGo = new GameObject("FirstPersonCamera");
        cameraGo.transform.SetParent(root.transform, false);
        cameraGo.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
        var camera = cameraGo.AddComponent<Camera>();
        camera.nearClipPlane = 0.05f;
        camera.fieldOfView = 75f;
        cameraGo.AddComponent<AudioListener>();

        var behaviour = root.AddComponent<MosquitoGameHumanController>();
        behaviour.firstPersonCamera = camera;
        return behaviour;
    }
}
