using TMPro;
using UnityEngine;

public static class CwslDamagePopupPool
{
    private const int InitialSize = 24;
    private const int ExpandSize = 8;

    private static Transform poolRoot;
    private static Transform activeRoot;
    private static GameObject template;
    private static CwslGameObjectPool pool;

    public static void EnsureReady()
    {
        EnsurePool();
    }

    public static void Play(Vector3 worldAnchor, float damage, CwslDamagePopupKind kind)
    {
        if (damage <= 0f && kind != CwslDamagePopupKind.Blocked)
            return;

        if (!EnsurePool())
            return;

        var instance = pool.Get();
        instance.transform.SetParent(activeRoot, false);

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

    private static bool EnsurePool()
    {
        if (pool != null)
            return true;

        var host = ResolvePoolHost();
        if (host == null)
            return false;

        poolRoot = EnsureChild(host, "DamagePopup");
        activeRoot = EnsureChild(poolRoot, "Active");
        var inactiveRoot = EnsureChild(poolRoot, "Inactive");
        template = CreateTemplate(inactiveRoot);
        pool = new CwslGameObjectPool(template, inactiveRoot, InitialSize, ExpandSize);
        return pool != null;
    }

    private static Transform ResolvePoolHost()
    {
        if (CwslNetworkPoolService.Instance != null && CwslNetworkPoolService.Instance.GamePoolRoot != null)
            return CwslNetworkPoolService.Instance.GamePoolRoot;

        var existing = GameObject.Find("GamePool");
        if (existing != null)
            return existing.transform;

        if (CwslGameSession.Instance != null)
        {
            var gamePool = new GameObject("GamePool");
            gamePool.transform.SetParent(CwslGameSession.Instance.transform, false);
            return gamePool.transform;
        }

        return Object.FindFirstObjectByType<CwslGameSession>()?.transform;
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        var existing = parent.Find(childName);
        if (existing != null)
            return existing;

        var childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private static GameObject CreateTemplate(Transform parent)
    {
        var root = new GameObject("CwslDamagePopup_Template");
        root.transform.SetParent(parent, false);
        root.SetActive(false);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 45;

        var group = root.AddComponent<CanvasGroup>();
        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160f, 72f);

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(root.transform, false);

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObject.GetComponent<TextMeshProUGUI>();
        CwslTmpFontUtil.ApplyFont(text);
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 52f;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;

        if (text.font != null)
        {
            text.outlineWidth = 0.22f;
            text.outlineColor = new Color32(0, 0, 0, 210);
        }

        var popup = root.AddComponent<CwslDamagePopup>();
        popup.Bind(text, group, canvas);
        return root;
    }
}
