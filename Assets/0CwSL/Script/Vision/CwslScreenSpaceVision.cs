using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스크린 스페이스 시야 마스크 — 원형 어둠.
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
    private bool isAbsoluteBlind;

    public void Activate(Transform target, float visionRadiusWorld, bool absoluteBlind = false)
    {
        followTarget = target;
        SetVisionRadius(visionRadiusWorld, absoluteBlind);
        EnsureOverlay();
        if (overlay != null)
            overlay.enabled = true;
    }

    public void SetVisionRadius(float visionRadiusWorld, bool absoluteBlind = false)
    {
        isAbsoluteBlind = absoluteBlind;
        isBlindVision = visionRadiusWorld <= 0.01f;
        visionRadius = isAbsoluteBlind ? 0f : isBlindVision ? 2.8f : Mathf.Max(6f, visionRadiusWorld);
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

        vignetteMaterial.SetFloat("_Aspect", CwslVisionShape.GetScreenAspect(camera));

        if (isAbsoluteBlind)
        {
            vignetteMaterial.SetVector("_Center", new Vector4(0.5f, 0.5f, 0f, 0f));
            vignetteMaterial.SetFloat("_InnerRadius", 0f);
            vignetteMaterial.SetFloat("_OuterRadius", 0.001f);
            vignetteMaterial.SetColor("_Color", DarkColor);
            ApplyScryMask(camera);
            return;
        }

        var worldAnchor = followTarget.position;
        var viewport = camera.WorldToViewportPoint(worldAnchor);
        if (viewport.z <= 0f)
            return;

        CwslVisionShape.ProjectViewportRadii(
            camera,
            worldAnchor,
            visionRadius,
            isBlindVision,
            out var innerRadius,
            out var outerRadius);

        vignetteMaterial.SetVector("_Center", new Vector4(viewport.x, viewport.y, 0f, 0f));
        vignetteMaterial.SetFloat("_InnerRadius", innerRadius);
        vignetteMaterial.SetFloat("_OuterRadius", outerRadius);
        vignetteMaterial.SetColor("_Color", DarkColor);
        ApplyScryMask(camera);
    }

    private void ApplyScryMask(Camera camera)
    {
        if (vignetteMaterial == null)
            return;

        var vision = CwslPlayerVision.Local;
        if (vision == null || !vision.TryGetActiveScry(out var scryCenter, out var scryRadius))
        {
            vignetteMaterial.SetFloat("_ScryActive", 0f);
            return;
        }

        var scryViewport = camera.WorldToViewportPoint(scryCenter);
        if (scryViewport.z <= 0f)
        {
            vignetteMaterial.SetFloat("_ScryActive", 0f);
            return;
        }

        CwslVisionShape.ProjectViewportRadii(
            camera,
            scryCenter,
            scryRadius,
            blindVision: false,
            out var innerRadius,
            out var outerRadius);

        vignetteMaterial.SetFloat("_ScryActive", 1f);
        vignetteMaterial.SetVector("_ScryCenter", new Vector4(scryViewport.x, scryViewport.y, 0f, 0f));
        vignetteMaterial.SetFloat("_ScryInnerRadius", innerRadius * 0.52f);
        vignetteMaterial.SetFloat("_ScryOuterRadius", outerRadius * 0.52f);
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
        canvas.sortingOrder = CwslGameConstants.VisionOverlaySortOrder;

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
