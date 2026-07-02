using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 몬스터 자식 HPBar — 앵커(HPBar)를 머리 위에 두고, 그 안 Canvas UI로 껍데기/값을 표시합니다.
/// </summary>
[RequireComponent(typeof(Health))]
[DefaultExecutionOrder(1500)]
public class HealthBarUI : MonoBehaviour
{
    private const string AnchorChildName = "HPBar";
    private const string CanvasChildName = "Canvas";
    private const string ShellChildName = "BarShell";
    private const string FillChildName = "BarFill";
    private const float FillInsetH = 0.06f;
    private const float FillInsetV = 0.18f;

    private enum RevealMode
    {
        Always = 0,
        OnFirstDamage = 1
    }

    [Header("배치")]
    [SerializeField] private float yOffset = 0.9f;
    [SerializeField] private float headPadding = 0.12f;

    [Header("크기 — HPBar 앵커 localScale × Canvas sizeDelta = 월드 크기")]
    [SerializeField] private Vector2 barPixelSize = new Vector2(160f, 22f);
    [SerializeField] private float barWorldScale = 0.005f;

    [Header("색상")]
    [SerializeField] private Color barBackgroundColor = new Color(0.08f, 0.04f, 0.04f, 0.88f);
    [SerializeField] private Color barFillHealthy = new Color(0.95f, 0.28f, 0.22f, 1f);
    [SerializeField] private Color barFillMid = new Color(1f, 0.55f, 0.12f, 1f);
    [SerializeField] private Color barFillLow = new Color(0.85f, 0.12f, 0.1f, 1f);
    [SerializeField] private float midHealthRatio = 0.5f;
    [SerializeField] private float lowHealthRatio = 0.25f;
    [SerializeField] private float fillSmoothSpeed = 12f;
    [SerializeField] private RevealMode revealMode = RevealMode.OnFirstDamage;

    private Health health;
    private Transform anchorRoot;
    private Transform canvasRoot;
    private Canvas barCanvas;
    private Image fillImage;
    private RectTransform fillRect;
    private float targetFill = 1f;
    private float displayedFill = 1f;
    private bool isBarVisible;

    private bool HideUntilDamaged => revealMode == RevealMode.OnFirstDamage;

    private void Awake()
    {
        health = GetComponent<Health>();
        BindHealthEvents();
    }

    private void OnDestroy()
    {
        UnbindHealthEvents();
    }

    private void OnDisable()
    {
        isBarVisible = false;
        SetBarActive(false);
    }

    private void BindHealthEvents()
    {
        if (health == null)
            return;

        health.OnHealthChanged -= OnHealthChanged;
        health.OnDamaged -= OnDamaged;
        health.OnDeath -= OnDeath;
        health.OnHealthChanged += OnHealthChanged;
        health.OnDamaged += OnDamaged;
        health.OnDeath += OnDeath;
    }

    private void UnbindHealthEvents()
    {
        if (health == null)
            return;

        health.OnHealthChanged -= OnHealthChanged;
        health.OnDamaged -= OnDamaged;
        health.OnDeath -= OnDeath;
    }

    public void ConfigureAsEnemy()
    {
        revealMode = RevealMode.OnFirstDamage;
        yOffset = 0.9f;
        barPixelSize = new Vector2(160f, 22f);
        barWorldScale = 0.005f;
        barBackgroundColor = new Color(0.08f, 0.04f, 0.04f, 0.88f);
        barFillHealthy = new Color(0.95f, 0.28f, 0.22f, 1f);
        barFillMid = new Color(1f, 0.55f, 0.12f, 1f);
        barFillLow = new Color(0.85f, 0.12f, 0.1f, 1f);
    }

    public void ConfigureAsAlly()
    {
        revealMode = RevealMode.Always;
        yOffset = 1.55f;
        barPixelSize = new Vector2(140f, 20f);
        barWorldScale = 0.005f;
    }

    public void ConfigureForNexus()
    {
        revealMode = RevealMode.Always;
        yOffset = 3.85f;
        barPixelSize = new Vector2(200f, 24f);
        barWorldScale = 0.005f;
        barBackgroundColor = new Color(0.08f, 0.04f, 0.04f, 0.88f);
        barFillHealthy = new Color(0.95f, 0.28f, 0.22f, 1f);
        barFillMid = new Color(1f, 0.55f, 0.12f, 1f);
        barFillLow = new Color(0.85f, 0.12f, 0.1f, 1f);
    }

    public void RefreshForNexus()
    {
        ConfigureForNexus();
        EnsureBarHierarchy();
        ApplyBarDimensions();
        RebuildBarContents();

        if (barCanvas != null)
            barCanvas.sortingOrder = 300;

        isBarVisible = true;
        SetBarActive(true);
        ApplyAnchorTransform();

        if (health != null)
            OnHealthChanged(health.CurrentHealth, health.MaxHealth);
        else
            ResetFillVisual();
    }

    public void RefreshForSpawn()
    {
        if (CompareTag("Enemy"))
            ConfigureAsEnemy();

        EnsureBarHierarchy();
        ApplyBarDimensions();
        RebuildBarContents();
        ResetFillVisual();

        isBarVisible = !HideUntilDamaged;
        SetBarActive(isBarVisible);

        if (isBarVisible)
            ApplyAnchorTransform();
    }

    public void NotifyDamaged()
    {
        if (!HideUntilDamaged || isBarVisible)
            return;

        ActivateBar();
    }

    private void LateUpdate()
    {
        if (!isBarVisible || anchorRoot == null)
            return;

        ApplyAnchorTransform();

        if (fillImage == null)
            return;

        if (Mathf.Abs(displayedFill - targetFill) <= 0.001f)
        {
            if (!Mathf.Approximately(displayedFill, targetFill))
            {
                displayedFill = targetFill;
                ApplyFillLayout(displayedFill);
                fillImage.color = ResolveFillColor(displayedFill);
            }

            return;
        }

        displayedFill = Mathf.Lerp(displayedFill, targetFill, Time.deltaTime * fillSmoothSpeed);
        ApplyFillLayout(displayedFill);
        fillImage.color = ResolveFillColor(displayedFill);
    }

    private void EnsureBarHierarchy()
    {
        anchorRoot = FindDirectChild(AnchorChildName);
        RemoveExtraAnchors(anchorRoot);

        if (anchorRoot == null)
        {
            var anchorObject = new GameObject(AnchorChildName);
            anchorObject.transform.SetParent(transform, false);
            anchorRoot = anchorObject.transform;
        }
        else if (StripLegacyCanvasFromAnchor())
        {
            canvasRoot = null;
            barCanvas = null;
            fillImage = null;
            fillRect = null;
        }

        EnsureCanvasChild();
    }

    private void Start()
    {
        if (!CompareTag("Nexus") || isBarVisible)
            return;

        RefreshForNexus();
    }

    private bool StripLegacyCanvasFromAnchor()
    {
        if (anchorRoot == null)
            return false;

        var legacyCanvas = anchorRoot.GetComponent<Canvas>();
        if (legacyCanvas == null)
            return false;

        DestroyHierarchyObject(legacyCanvas);

        for (int i = anchorRoot.childCount - 1; i >= 0; i--)
            DestroyHierarchyObject(anchorRoot.GetChild(i).gameObject);

        return true;
    }

    private void EnsureCanvasChild()
    {
        if (anchorRoot == null)
            return;

        canvasRoot = FindAliveChild(anchorRoot, CanvasChildName);
        if (canvasRoot == null)
        {
            var canvasObject = new GameObject(CanvasChildName, typeof(RectTransform));
            canvasObject.transform.SetParent(anchorRoot, false);
            canvasRoot = canvasObject.transform;

            barCanvas = canvasObject.AddComponent<Canvas>();
            barCanvas.renderMode = RenderMode.WorldSpace;
            barCanvas.sortingOrder = 200;
        }
        else
        {
            if (canvasRoot.GetComponent<RectTransform>() == null)
                canvasRoot.gameObject.AddComponent<RectTransform>();

            barCanvas = canvasRoot.GetComponent<Canvas>();
            if (barCanvas == null)
                barCanvas = canvasRoot.gameObject.AddComponent<Canvas>();

            barCanvas.renderMode = RenderMode.WorldSpace;
            barCanvas.sortingOrder = 200;
        }

        if (canvasRoot == null)
            return;

        var canvasRect = canvasRoot.GetComponent<RectTransform>();
        if (canvasRect == null)
            return;

        canvasRect.localPosition = Vector3.zero;
        canvasRect.localRotation = Quaternion.identity;
        canvasRect.localScale = Vector3.one;
        canvasRect.sizeDelta = barPixelSize;
    }

    private void RebuildBarContents()
    {
        if (canvasRoot == null)
            return;

        for (int i = canvasRoot.childCount - 1; i >= 0; i--)
            DestroyHierarchyObject(canvasRoot.GetChild(i).gameObject);

        CreateStretchImage(canvasRoot, ShellChildName, barBackgroundColor,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, out _);
        CreateFillBar(canvasRoot, FillChildName, out fillImage, out fillRect);
        fillImage.color = ResolveFillColor(displayedFill);
        ApplyFillLayout(displayedFill);
    }

    private void ApplyBarDimensions()
    {
        if (canvasRoot == null)
            return;

        var canvasRect = canvasRoot.GetComponent<RectTransform>();
        if (canvasRect == null)
            return;

        canvasRect.sizeDelta = barPixelSize;
        canvasRect.localScale = Vector3.one;
    }

    private void ApplyAnchorTransform()
    {
        if (anchorRoot == null)
            return;

        float headOffset = ResolveBarHeight() + headPadding;
        anchorRoot.localPosition = new Vector3(0f, headOffset, 0f);
        anchorRoot.localScale = Vector3.one * barWorldScale;

        var camera = DefenseBillboardCamera.Resolve() ?? Camera.main;
        if (camera != null)
        {
            DefenseBillboardCamera.Face(anchorRoot, camera);
            if (barCanvas != null)
                barCanvas.worldCamera = camera;
        }
    }

    private float ResolveBarHeight()
    {
        if (CompareTag("Nexus"))
            return yOffset;

        return MonsterGroundPlacement.ResolveHeadOffset(transform, yOffset);
    }

    private Transform FindDirectChild(string childName)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private void RemoveExtraAnchors(Transform keep)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name != AnchorChildName || child == keep)
                continue;

            DestroyHierarchyObject(child.gameObject);
        }
    }

    private static Transform FindAliveChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child == null || child.name != childName)
                continue;

            return child;
        }

        return null;
    }

    private static void DestroyHierarchyObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(target);
        else
            UnityEngine.Object.DestroyImmediate(target);
    }

    private void OnDamaged(float amount)
    {
        if (amount > 0f)
            NotifyDamaged();
    }

    private void OnHealthChanged(float current, float max)
    {
        if (HideUntilDamaged && !isBarVisible && max > 0f && current < max - 0.001f)
            ActivateBar();

        if (!isBarVisible)
            return;

        targetFill = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        if (fillImage != null && Mathf.Approximately(displayedFill, targetFill))
        {
            ApplyFillLayout(targetFill);
            fillImage.color = ResolveFillColor(targetFill);
        }
    }

    private void OnDeath()
    {
        isBarVisible = false;
        SetBarActive(false);
    }

    private void ActivateBar()
    {
        if (anchorRoot == null || canvasRoot == null)
            EnsureBarHierarchy();

        if (fillImage == null)
            RebuildBarContents();

        if (anchorRoot == null)
            return;

        isBarVisible = true;
        SetBarActive(true);
        ApplyAnchorTransform();

        if (health != null)
            OnHealthChanged(health.CurrentHealth, health.MaxHealth);
    }

    private void SetBarActive(bool active)
    {
        if (anchorRoot != null)
            anchorRoot.gameObject.SetActive(active);
    }

    private void ResetFillVisual()
    {
        displayedFill = 1f;
        targetFill = 1f;
        if (fillImage == null)
            return;

        ApplyFillLayout(1f);
        fillImage.color = ResolveFillColor(1f);
    }

    private void ApplyFillLayout(float ratio)
    {
        if (fillRect == null)
            return;

        ratio = Mathf.Clamp01(ratio);
        float innerWidth = 1f - FillInsetH * 2f;
        fillRect.anchorMin = new Vector2(FillInsetH, FillInsetV);
        fillRect.anchorMax = new Vector2(FillInsetH + innerWidth * ratio, 1f - FillInsetV);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

    private static void CreateStretchImage(
        Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, out Image image)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        image = go.AddComponent<Image>();
        image.sprite = DefenseUISprites.White;
        image.color = color;
        image.raycastTarget = false;
    }

    private static void CreateFillBar(
        Transform parent, string name, out Image fill, out RectTransform rect)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        rect = go.AddComponent<RectTransform>();
        fill = go.AddComponent<Image>();
        fill.sprite = DefenseUISprites.White;
        fill.raycastTarget = false;
    }

    private Color ResolveFillColor(float ratio)
    {
        if (ratio <= lowHealthRatio) return barFillLow;
        if (ratio <= midHealthRatio)
            return Color.Lerp(barFillLow, barFillMid, (ratio - lowHealthRatio) / (midHealthRatio - lowHealthRatio));
        return Color.Lerp(barFillMid, barFillHealthy, (ratio - midHealthRatio) / (1f - midHealthRatio));
    }
}
