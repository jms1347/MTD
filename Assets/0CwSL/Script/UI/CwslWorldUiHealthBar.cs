using UnityEngine;
using UnityEngine.UI;

/// <summary>월드 오브젝트를 화면 UI 체력 바로 표시.</summary>
public class CwslWorldUiHealthBar : MonoBehaviour
{
    private const string OverlayCanvasName = "CwslWorldUiOverlayCanvas";
    private static Canvas overlayCanvas;
    private static RectTransform overlayCanvasRect;

    private RectTransform rootRect;
    private Image backImage;
    private Image fillImage;

    private float barWidth = 84f;
    private float barHeight = 10f;
    private float worldHeightOffset = 2f;
    private bool visible = true;

    public void Configure(float widthPixels, float heightPixels, float offset, Color fill, Color? back = null)
    {
        barWidth = Mathf.Max(20f, widthPixels);
        barHeight = Mathf.Max(4f, heightPixels);
        worldHeightOffset = offset;
        EnsureUi();
        if (fillImage != null)
            fillImage.color = fill;
        if (backImage != null)
            backImage.color = back ?? new Color(0.1f, 0.1f, 0.12f, 0.85f);
    }

    public void Refresh(float ratio, float maxHealth = -1f)
    {
        EnsureUi();
        if (fillImage == null)
            return;

        fillImage.fillAmount = Mathf.Clamp01(ratio);
    }

    public void SetVisible(bool isVisible)
    {
        visible = isVisible;
        if (rootRect != null)
            rootRect.gameObject.SetActive(isVisible);
    }

    private void LateUpdate()
    {
        if (!visible || rootRect == null)
            return;

        var camera = Camera.main;
        if (camera == null || overlayCanvasRect == null)
        {
            rootRect.gameObject.SetActive(false);
            return;
        }

        var world = transform.position + Vector3.up * worldHeightOffset;
        var screenPoint = camera.WorldToScreenPoint(world);
        var onFront = screenPoint.z > 0f;
        if (!onFront)
        {
            rootRect.gameObject.SetActive(false);
            return;
        }

        if (!rootRect.gameObject.activeSelf)
            rootRect.gameObject.SetActive(true);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayCanvasRect,
                screenPoint,
                overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera,
                out var anchored))
        {
            rootRect.anchoredPosition = anchored;
        }
    }

    private void OnDestroy()
    {
        if (rootRect != null)
            Destroy(rootRect.gameObject);
    }

    private void EnsureUi()
    {
        if (!EnsureOverlayCanvas())
            return;

        if (rootRect != null)
            return;

        var rootGo = new GameObject("WorldUiHealthBar", typeof(RectTransform));
        rootRect = rootGo.GetComponent<RectTransform>();
        rootRect.SetParent(overlayCanvasRect, false);
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(barWidth, barHeight);

        var backGo = new GameObject("Back", typeof(RectTransform), typeof(Image));
        var backRect = backGo.GetComponent<RectTransform>();
        backRect.SetParent(rootRect, false);
        backRect.anchorMin = Vector2.zero;
        backRect.anchorMax = Vector2.one;
        backRect.offsetMin = Vector2.zero;
        backRect.offsetMax = Vector2.zero;
        backImage = backGo.GetComponent<Image>();
        backImage.raycastTarget = false;
        backImage.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.SetParent(rootRect, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillImage = fillGo.GetComponent<Image>();
        fillImage.raycastTarget = false;
        CwslUiSpriteUtil.ConfigureHorizontalFill(fillImage, new Color(0.85f, 0.55f, 0.25f, 0.98f));
    }

    private static bool EnsureOverlayCanvas()
    {
        if (overlayCanvasRect != null && overlayCanvas != null)
            return true;

        var existing = GameObject.Find(OverlayCanvasName);
        if (existing != null)
        {
            overlayCanvas = existing.GetComponent<Canvas>();
            overlayCanvasRect = existing.GetComponent<RectTransform>();
            if (overlayCanvas != null && overlayCanvasRect != null)
                return true;
        }

        var canvasGo = new GameObject(
            OverlayCanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        overlayCanvas = canvasGo.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = CwslGameConstants.HudCanvasSortOrder + 20;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        overlayCanvasRect = canvasGo.GetComponent<RectTransform>();
        overlayCanvasRect.anchorMin = Vector2.zero;
        overlayCanvasRect.anchorMax = Vector2.one;
        overlayCanvasRect.offsetMin = Vector2.zero;
        overlayCanvasRect.offsetMax = Vector2.zero;
        return true;
    }
}
