using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UkDefense CombatDamagePopupPool — 독립 오버레이 캔버스(항상 표시).
/// </summary>
public static class CwslDamagePopupPool
{
    private const string PoolRootName = "DamagePopupPOOL";
    private const string CanvasName = "CwslDamagePopupCanvas";
    private const int InitialSize = CwslGameConstants.PoolHighChurnInitialSize;
    private const int ExpandSize = CwslGameConstants.PoolHighChurnExpandSize;
    private const int CanvasSortOrder = 220;

    private static Transform poolRoot;
    private static Transform activeRoot;
    private static GameObject template;
    private static CwslGameObjectPool pool;
    private static RectTransform overlayCanvasRect;
    private static Canvas overlayCanvas;

    public static void EnsureReady()
    {
        EnsurePool();
    }

    public static void Play(Vector3 worldAnchor, float damage, CwslDamagePopupKind kind)
    {
        var displayAmount = kind == CwslDamagePopupKind.Blocked
            ? damage
            : Mathf.CeilToInt(damage);
        if (displayAmount <= 0 && kind != CwslDamagePopupKind.Blocked)
            return;

        if (!EnsurePool())
            return;

        var instance = pool.Get();
        instance.transform.SetParent(activeRoot, false);
        instance.transform.SetAsLastSibling();

        if (!instance.activeSelf)
            instance.SetActive(true);

        var popup = instance.GetComponent<CwslDamagePopup>();
        popup.Play(damage, kind, worldAnchor);
    }

    internal static void Release(CwslDamagePopup popup)
    {
        if (popup == null)
            return;

        popup.StopAndHide();
        if (pool != null)
            pool.Release(popup.gameObject);
    }

    internal static bool TryWorldToScreenLocal(Vector3 worldPosition, out Vector2 localPoint)
    {
        localPoint = default;
        if (overlayCanvasRect == null)
            return false;

        var camera = CwslBillboardToCamera.ResolveCamera();
        if (camera == null)
            return false;

        var viewport = camera.WorldToViewportPoint(worldPosition);
        var viewportPoint = viewport.z > 0f
            ? new Vector2(viewport.x, viewport.y)
            : new Vector2(Mathf.Clamp01(viewport.x), Mathf.Clamp01(viewport.y));

        viewportPoint.x = Mathf.Clamp(viewportPoint.x, 0.04f, 0.96f);
        viewportPoint.y = Mathf.Clamp(viewportPoint.y, 0.08f, 0.92f);

        var screenPoint = new Vector2(
            viewportPoint.x * Screen.width,
            viewportPoint.y * Screen.height);

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayCanvasRect,
            screenPoint,
            null,
            out localPoint);
    }

    private static bool EnsurePool()
    {
        if (pool != null && activeRoot != null && template != null)
            return true;

        if (!EnsureOverlayCanvas())
            return false;

        var existing = overlayCanvasRect.Find(PoolRootName);
        poolRoot = existing != null ? existing : CreatePoolRoot(overlayCanvasRect);

        activeRoot = EnsureChild(poolRoot, "Active");
        var templateRoot = EnsureChild(poolRoot, "Template");
        templateRoot.gameObject.SetActive(true);
        activeRoot.gameObject.SetActive(true);
        poolRoot.gameObject.SetActive(true);

        template = CreateTemplate(templateRoot);
        pool = new CwslGameObjectPool(template, templateRoot, InitialSize, ExpandSize);
        return pool != null;
    }

    private static Transform CreatePoolRoot(Transform parent)
    {
        var rootObject = new GameObject(PoolRootName, typeof(RectTransform));
        var rect = rootObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rootObject.SetActive(true);
        return rect;
    }

    private static bool EnsureOverlayCanvas()
    {
        if (overlayCanvasRect != null && overlayCanvas != null)
        {
            overlayCanvasRect.gameObject.SetActive(true);
            overlayCanvas.enabled = true;
            return true;
        }

        var existing = GameObject.Find(CanvasName);
        if (existing != null)
        {
            overlayCanvasRect = existing.GetComponent<RectTransform>();
            overlayCanvas = existing.GetComponent<Canvas>();
            if (overlayCanvasRect != null)
            {
                existing.SetActive(true);
                if (overlayCanvas != null)
                    overlayCanvas.enabled = true;
                return true;
            }
        }

        var canvasObject = new GameObject(
            CanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));

        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = CanvasSortOrder;
        overlayCanvas.enabled = true;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        overlayCanvasRect = canvasObject.GetComponent<RectTransform>();
        overlayCanvasRect.anchorMin = Vector2.zero;
        overlayCanvasRect.anchorMax = Vector2.one;
        overlayCanvasRect.offsetMin = Vector2.zero;
        overlayCanvasRect.offsetMax = Vector2.zero;
        overlayCanvasRect.pivot = new Vector2(0.5f, 0.5f);
        overlayCanvasRect.localScale = Vector3.one;
        canvasObject.SetActive(true);
        Object.DontDestroyOnLoad(canvasObject);
        return overlayCanvasRect != null;
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        var existing = parent.Find(childName);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return existing;
        }

        var childObject = new GameObject(childName, typeof(RectTransform));
        var rect = childObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        childObject.SetActive(true);
        return rect;
    }

    private static GameObject CreateTemplate(Transform parent)
    {
        var root = new GameObject("CwslDamagePopup_Template", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        root.SetActive(false);

        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(178f, 78f);
        rect.anchoredPosition = Vector2.zero;

        var group = root.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        var textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(root.transform, false);

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObject.AddComponent<TextMeshProUGUI>();
        CwslTmpFontUtil.ApplyFont(text);
        text.fontSize = 56f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.overflowMode = TextOverflowModes.Overflow;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        if (text.font == null)
            Debug.LogWarning("[CwSL] 데미지 팝업 폰트를 찾지 못했습니다. TMP 기본 폰트를 확인하세요.");

        var popup = root.AddComponent<CwslDamagePopup>();
        popup.Bind(text, group, rect);
        return root;
    }

    internal static void TryApplyOutline(TMP_Text text)
    {
        if (text == null || text.font == null)
            return;

        text.ForceMeshUpdate(true);
        if (text.fontSharedMaterial == null)
            return;

        text.outlineWidth = 0.18f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.85f);
    }
}
