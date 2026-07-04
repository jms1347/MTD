using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스크린 스페이스 시야 마스크.
/// 플레이어의 화면 좌표를 중심으로 부드러운 원형 어둠을 그려,
/// 쿼터뷰 카메라에서도 위쪽이 잘리지 않는다.
/// </summary>
public class CwslScreenSpaceVision : MonoBehaviour
{
    private static readonly Color DarkColor = new(0.01f, 0.012f, 0.018f, 0.96f);

    private Canvas canvas;
    private RawImage overlay;
    private Material vignetteMaterial;
    private Transform followTarget;
    private float visionRadius = 14f;
    private bool isBlindVision;

    public void Activate(Transform target, float visionRadiusWorld)
    {
        followTarget = target;
        SetVisionRadius(visionRadiusWorld);
        EnsureOverlay();
        if (overlay != null)
            overlay.enabled = true;
    }

    public void SetVisionRadius(float visionRadiusWorld)
    {
        isBlindVision = visionRadiusWorld <= 0.01f;
        visionRadius = isBlindVision ? 2.8f : Mathf.Max(6f, visionRadiusWorld);
    }

    public void Deactivate()
    {
        if (overlay != null)
            overlay.enabled = false;
    }

    private void LateUpdate()
    {
        if (overlay == null || !overlay.enabled || followTarget == null)
            return;

        var camera = Camera.main;
        if (camera == null || vignetteMaterial == null)
            return;

        // 플레이어 발밑(지면)을 스크린 좌표로
        var worldAnchor = followTarget.position;
        var viewport = camera.WorldToViewportPoint(worldAnchor);
        if (viewport.z <= 0f)
            return;

        // 시야 반경을 뷰포트 단위로 변환 (지면 위 한 점 투영)
        var edgeWorld = worldAnchor + GetCameraRightOnGround(camera) * visionRadius;
        var edgeViewport = camera.WorldToViewportPoint(edgeWorld);
        var center = new Vector2(viewport.x, viewport.y);
        var edge = new Vector2(edgeViewport.x, edgeViewport.y);

        // aspect 보정 전 거리
        var aspect = (float)Screen.width / Mathf.Max(1f, Screen.height);
        var delta = edge - center;
        delta.x *= aspect;
        var radiusViewport = delta.magnitude;
        radiusViewport = Mathf.Clamp(radiusViewport, 0.04f, 0.98f);

        // 안쪽은 밝고, 바깥으로 smoothstep 페이드
        // 시야 없는 캐릭터는 더 타이트하게
        var inner = isBlindVision ? radiusViewport * 0.25f : radiusViewport * 0.58f;
        var outer = isBlindVision ? radiusViewport * 1.15f : radiusViewport * 1.28f;

        vignetteMaterial.SetVector("_Center", new Vector4(center.x, center.y, 0f, 0f));
        vignetteMaterial.SetFloat("_InnerRadius", inner);
        vignetteMaterial.SetFloat("_OuterRadius", outer);
        vignetteMaterial.SetFloat("_Aspect", aspect);
        vignetteMaterial.SetColor("_Color", DarkColor);
    }

    private static Vector3 GetCameraRightOnGround(Camera camera)
    {
        var right = camera.transform.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.0001f)
            return Vector3.right;
        return right.normalized;
    }

    private void EnsureOverlay()
    {
        if (overlay != null)
            return;

        var shader = Shader.Find("CwSL/VisionVignette");
        if (shader == null)
        {
            Debug.LogWarning("[CwSL] VisionVignette 쉐이더를 찾을 수 없습니다.");
            return;
        }

        vignetteMaterial = new Material(shader)
        {
            name = "CwslVisionVignetteMaterial",
            hideFlags = HideFlags.HideAndDontSave
        };

        var canvasObject = new GameObject("CwslScreenSpaceVisionCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var imageObject = new GameObject("VisionVignette", typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(canvasObject.transform, false);
        var rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlay = imageObject.GetComponent<RawImage>();
        overlay.raycastTarget = false;
        overlay.texture = Texture2D.whiteTexture;
        overlay.material = vignetteMaterial;
        overlay.color = Color.white;
    }

    private void OnDestroy()
    {
        if (vignetteMaterial != null)
            Destroy(vignetteMaterial);
    }
}
