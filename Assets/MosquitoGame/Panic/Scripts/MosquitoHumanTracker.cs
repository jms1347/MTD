using UnityEngine;

/// <summary>
/// 모기 Owner 전용: 네트워크로 등록된 인간을 추적하고,
/// HumanTarget 레이어 아웃라인 + 화면 UI로 항상 위치를 표시한다.
/// </summary>
public class MosquitoHumanTracker : MonoBehaviour
{
    [SerializeField] private Camera thirdPersonCamera;
    [SerializeField] private float followDistance = 1.1f;
    [SerializeField] private float followHeight = 0.35f;
    [SerializeField] private bool ownerOnlyUi = true;

    private MosquitoController mosquito;
    private readonly System.Collections.Generic.List<HumanController> trackedHumans = new();

    public Camera ThirdPersonCamera => thirdPersonCamera;

    private void Awake()
    {
        mosquito = GetComponent<MosquitoController>();
    }

    public void EnsureCamera()
    {
        if (thirdPersonCamera != null)
        {
            PanicVisionLayers.ApplyMosquitoCameraCulling(thirdPersonCamera);
            return;
        }

        var cameraGo = new GameObject("MosquitoCamera");
        cameraGo.transform.SetParent(transform, false);
        thirdPersonCamera = cameraGo.AddComponent<Camera>();
        thirdPersonCamera.nearClipPlane = 0.02f;
        thirdPersonCamera.fieldOfView = 68f;
        thirdPersonCamera.enabled = false;
        PanicVisionLayers.ApplyMosquitoCameraCulling(thirdPersonCamera);
    }

    private void OnEnable()
    {
        EnsureCamera();
        BindRegistry();
        RefreshAllHumans();
    }

    private void OnDisable()
    {
        UnbindRegistry();
    }

    private void BindRegistry()
    {
        var registry = HumanTargetRegistry.Instance;
        if (registry == null)
            return;

        registry.OnHumanRegistered -= HandleHumanRegistered;
        registry.OnHumanUnregistered -= HandleHumanUnregistered;
        registry.OnHumanRegistered += HandleHumanRegistered;
        registry.OnHumanUnregistered += HandleHumanUnregistered;
    }

    private void UnbindRegistry()
    {
        var registry = HumanTargetRegistry.Instance;
        if (registry == null)
            return;

        registry.OnHumanRegistered -= HandleHumanRegistered;
        registry.OnHumanUnregistered -= HandleHumanUnregistered;
    }

    private void RefreshAllHumans()
    {
        trackedHumans.Clear();
        var registry = HumanTargetRegistry.Instance;
        if (registry == null)
        {
            // Host 시작 직전 폴백
            var fallback = FindObjectsByType<HumanController>(FindObjectsSortMode.None);
            foreach (var human in fallback)
                HandleHumanRegistered(human);
            return;
        }

        registry.GetAliveHumans(trackedHumans);
        for (var i = 0; i < trackedHumans.Count; i++)
            EnsureOutline(trackedHumans[i]);
    }

    private void HandleHumanRegistered(HumanController human)
    {
        if (human == null || trackedHumans.Contains(human))
            return;

        trackedHumans.Add(human);
        EnsureOutline(human);
    }

    private void HandleHumanUnregistered(HumanController human)
    {
        trackedHumans.Remove(human);
    }

    private static void EnsureOutline(HumanController human)
    {
        if (human == null)
            return;

        human.EnsureMosquitoVisibleOutline();
    }

    public void SetOwnerCameraActive(bool active)
    {
        EnsureCamera();
        if (thirdPersonCamera == null)
            return;

        thirdPersonCamera.enabled = active;
        if (active)
        {
            PanicVisionLayers.ApplyMosquitoCameraCulling(thirdPersonCamera);
            if (thirdPersonCamera.GetComponent<AudioListener>() == null)
                thirdPersonCamera.gameObject.AddComponent<AudioListener>();
        }
        else
        {
            var listener = thirdPersonCamera.GetComponent<AudioListener>();
            if (listener != null)
                Destroy(listener);
        }
    }

    public void UpdateCamera(float pitch, float yaw)
    {
        if (thirdPersonCamera == null || !thirdPersonCamera.enabled)
            return;

        var rotation = Quaternion.Euler(pitch, yaw, 0f);
        var offset = rotation * new Vector3(0f, followHeight, -followDistance);
        thirdPersonCamera.transform.position = transform.position + offset;
        thirdPersonCamera.transform.rotation = rotation;
    }

    private bool CanDrawUi()
    {
        if (!ownerOnlyUi)
            return true;

        if (mosquito == null)
            return false;

        return mosquito.IsOwner && mosquito.IsAlive;
    }

    private void OnGUI()
    {
        if (!CanDrawUi() || thirdPersonCamera == null || !thirdPersonCamera.enabled)
            return;

        for (var i = trackedHumans.Count - 1; i >= 0; i--)
        {
            var human = trackedHumans[i];
            if (human == null)
            {
                trackedHumans.RemoveAt(i);
                continue;
            }

            if (!human.IsAlive)
                continue;

            DrawScreenEdgeIndicator(human.GetAimPoint());
        }
    }

    private void DrawScreenEdgeIndicator(Vector3 worldPosition)
    {
        var screenPoint = thirdPersonCamera.WorldToScreenPoint(worldPosition);
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = Color.red;

        // 카메라 뒤 → 화면 가장자리 방향만 표시
        if (screenPoint.z < 0f)
        {
            screenPoint.x = Screen.width - screenPoint.x;
            screenPoint.y = Screen.height - screenPoint.y;
        }

        var onScreen =
            screenPoint.z > 0f &&
            screenPoint.x >= 0f && screenPoint.x <= Screen.width &&
            screenPoint.y >= 0f && screenPoint.y <= Screen.height;

        if (onScreen)
        {
            GUI.Label(
                new Rect(screenPoint.x - 16f, Screen.height - screenPoint.y - 16f, 32f, 32f),
                "♥",
                style);
            return;
        }

        var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        var direction = new Vector2(screenPoint.x, Screen.height - screenPoint.y) - center;
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.up;

        direction = direction.normalized * Mathf.Min(Screen.width, Screen.height) * 0.38f;
        var edge = center + direction;
        GUI.Label(new Rect(edge.x - 16f, edge.y - 16f, 32f, 32f), "→", style);
    }
}
