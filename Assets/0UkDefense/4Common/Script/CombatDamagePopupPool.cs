using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 데미지 팝업 월드 UI 풀.
/// </summary>
public static class CombatDamagePopupPool
{
    private const string PoolRootName = "DamagePopupPOOL";
    private const int InitialSize = 24;
    private const int ExpandSize = 8;

    private static Transform poolRoot;
    private static Transform activeRoot;
    private static GameObject template;
    private static GameObjectPool pool;
    private static Font cachedFont;

    public static void Play(Vector3 worldAnchor, float damage, DamageElement element)
    {
        if (damage <= 0f)
            return;

        if (!EnsurePool())
            return;

        var instance = pool.Get();
        instance.transform.SetParent(activeRoot, false);

        var popup = instance.GetComponent<CombatDamagePopup>();
        popup.Play(damage, element, worldAnchor);
    }

    /// <summary>StageManager 준비 직후 호출 — 첫 피격 때 풀 생성 실패를 방지합니다.</summary>
    public static void EnsureReady()
    {
        EnsurePool();
    }

    internal static void Release(CombatDamagePopup popup)
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

        var existing = host.Find(PoolRootName);
        if (existing != null)
            poolRoot = existing;
        else
        {
            var rootObject = new GameObject(PoolRootName);
            rootObject.transform.SetParent(host, false);
            poolRoot = rootObject.transform;
        }

        activeRoot = EnsureChild(poolRoot, "Active");
        var templateRoot = EnsureChild(poolRoot, "Template");
        template = CreateTemplate(templateRoot);
        pool = new GameObjectPool(template, templateRoot, InitialSize, ExpandSize);
        return pool != null;
    }

    private static Transform ResolvePoolHost()
    {
        if (StageManager.Instance != null)
            return StageManager.Instance.transform;

        var stageManager = Object.FindFirstObjectByType<StageManager>();
        if (stageManager != null)
            return stageManager.transform;

        return null;
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
        cachedFont ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var root = new GameObject("DamagePopup_Template");
        root.transform.SetParent(parent, false);
        root.SetActive(false);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = DefenseBillboardCamera.Resolve();
        canvas.sortingOrder = 45;

        var group = root.AddComponent<CanvasGroup>();
        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(96f, 42f);

        var textObject = new GameObject("Text");
        textObject.transform.SetParent(root.transform, false);
        var textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObject.AddComponent<Text>();
        text.font = cachedFont;
        text.fontSize = 31;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        var outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);

        var popup = root.AddComponent<CombatDamagePopup>();
        popup.Bind(text, group);

        return root;
    }
}