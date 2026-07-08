using UnityEngine;

public class MosquitoHumanTracker : MonoBehaviour
{
    [SerializeField] private Camera thirdPersonCamera;
    [SerializeField] private float followDistance = 1.1f;
    [SerializeField] private float followHeight = 0.35f;

    private Transform humanOutline;
    private HumanController human;

    private void Start()
    {
        if (thirdPersonCamera == null)
        {
            var cameraGo = new GameObject("MosquitoCamera");
            cameraGo.transform.SetParent(transform, false);
            thirdPersonCamera = cameraGo.AddComponent<Camera>();
            thirdPersonCamera.nearClipPlane = 0.02f;
            thirdPersonCamera.fieldOfView = 68f;
        }

        human = FindFirstObjectByType<HumanController>();
        if (human != null)
            humanOutline = human.EnsureTrackerOutline();
    }

    public void UpdateCamera(float pitch, float yaw)
    {
        if (thirdPersonCamera == null)
            return;

        var rotation = Quaternion.Euler(pitch, yaw, 0f);
        var offset = rotation * new Vector3(0f, followHeight, -followDistance);
        thirdPersonCamera.transform.position = transform.position + offset;
        thirdPersonCamera.transform.rotation = rotation;

        if (humanOutline != null)
            humanOutline.gameObject.SetActive(true);
    }

    private void OnGUI()
    {
        if (human == null || !human.IsAlive)
            return;

        DrawScreenEdgeIndicator(human.transform.position + Vector3.up * 1.5f);
    }

    private void DrawScreenEdgeIndicator(Vector3 worldPosition)
    {
        if (thirdPersonCamera == null)
            return;

        var screenPoint = thirdPersonCamera.WorldToScreenPoint(worldPosition);
        if (screenPoint.z < 0f)
            return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = Color.red;

        if (screenPoint.x >= 0f && screenPoint.x <= Screen.width && screenPoint.y >= 0f && screenPoint.y <= Screen.height)
        {
            GUI.Label(new Rect(screenPoint.x - 16f, Screen.height - screenPoint.y - 16f, 32f, 32f), "♥", style);
            return;
        }

        var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        var direction = new Vector2(screenPoint.x, Screen.height - screenPoint.y) - center;
        direction = direction.normalized * Mathf.Min(Screen.width, Screen.height) * 0.38f;
        var edge = center + direction;
        GUI.Label(new Rect(edge.x - 16f, edge.y - 16f, 32f, 32f), "→", style);
    }
}
