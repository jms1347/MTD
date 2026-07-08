using UnityEngine;

public class MosquitoGameMosquitoController : MonoBehaviour
{
    private const float MoveSpeed = 2.8f;
    private const float VerticalSpeed = 2.2f;
    private const float CameraDistance = 0.55f;
    private const float CameraHeight = 0.08f;
    private const float LookSensitivity = 2.8f;

    [SerializeField] private Transform visual;
    [SerializeField] private Camera thirdPersonCamera;

    private float yaw;
    private float pitch = 12f;
    private bool activeRole;

    public bool IsActiveRole => activeRole;

    private void Awake()
    {
        if (thirdPersonCamera == null)
            thirdPersonCamera = GetComponentInChildren<Camera>(true);
    }

    public void SetActiveRole(bool active)
    {
        activeRole = active;
        enabled = active;
        if (visual != null)
            visual.gameObject.SetActive(active);
        if (thirdPersonCamera != null)
            thirdPersonCamera.gameObject.SetActive(active);

        Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !active;
    }

    private void LateUpdate()
    {
        if (!activeRole || thirdPersonCamera == null)
            return;

        UpdateCameraTransform();
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
        yaw += Input.GetAxisRaw("Mouse X") * LookSensitivity;
        pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * LookSensitivity, -25f, 70f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void HandleMove()
    {
        var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        input = Vector3.ClampMagnitude(input, 1f);
        var planar = transform.TransformDirection(input) * MoveSpeed;

        var vertical = 0f;
        if (Input.GetKey(KeyCode.Space))
            vertical += VerticalSpeed;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
            vertical -= VerticalSpeed;

        transform.position += (planar + Vector3.up * vertical) * Time.deltaTime;
    }

    private void UpdateCameraTransform()
    {
        var rotation = Quaternion.Euler(pitch, yaw, 0f);
        var offset = rotation * new Vector3(0f, CameraHeight, -CameraDistance);
        thirdPersonCamera.transform.position = transform.position + offset;
        thirdPersonCamera.transform.rotation = rotation;
    }

    public static MosquitoGameMosquitoController Create(Transform parent, Vector3 position)
    {
        var root = new GameObject("Mosquito");
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "MosquitoVisual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * 0.06f;
        Object.Destroy(visual.GetComponent<SphereCollider>());

        var bodyRenderer = visual.GetComponent<Renderer>();
        if (bodyRenderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.2f, 0.22f, 0.24f);
            bodyRenderer.sharedMaterial = material;
        }

        var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wing.name = "Wing";
        wing.transform.SetParent(visual.transform, false);
        wing.transform.localScale = new Vector3(1.6f, 0.08f, 0.45f);
        wing.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        Object.Destroy(wing.GetComponent<BoxCollider>());
        var wingRenderer = wing.GetComponent<Renderer>();
        if (wingRenderer != null)
        {
            var wingMaterial = new Material(Shader.Find("Standard"));
            wingMaterial.color = new Color(0.75f, 0.78f, 0.82f, 0.55f);
            wingMaterial.SetFloat("_Mode", 3f);
            wingMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            wingMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            wingMaterial.SetInt("_ZWrite", 0);
            wingMaterial.DisableKeyword("_ALPHATEST_ON");
            wingMaterial.EnableKeyword("_ALPHABLEND_ON");
            wingMaterial.renderQueue = 3000;
            wingRenderer.sharedMaterial = wingMaterial;
        }

        var cameraGo = new GameObject("ThirdPersonCamera");
        cameraGo.transform.SetParent(root.transform, false);
        var camera = cameraGo.AddComponent<Camera>();
        camera.nearClipPlane = 0.01f;
        camera.fieldOfView = 68f;

        var behaviour = root.AddComponent<MosquitoGameMosquitoController>();
        behaviour.visual = visual.transform;
        behaviour.thirdPersonCamera = camera;
        return behaviour;
    }
}
