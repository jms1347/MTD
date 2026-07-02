using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 우상단 골드 표시. GameManager.OnMoneyChanged를 구독합니다.
/// </summary>
public class DefenseGoldUI : MonoBehaviour
{
    private const long CheatGoldPerClick = 1000L;

    private const float CollectAnimDuration = 0.38f;
    private const float CollectScalePunch = 0.08f;
    private static readonly Vector3 GoldTextNormalScale = Vector3.one;

    [SerializeField] private Text goldText;
    [SerializeField] private Image panelImage;

    private RectTransform flyTargetIcon;
    private long displayedAmount;
    private bool isAnimatingCollect;
    private Coroutine collectRoutine;
    private bool isBound;
    private bool isInitialized;

    public RectTransform FlyTargetIcon => flyTargetIcon;

    public void Initialize()
    {
        EnsureUiElements();
        DefenseGoldFlyReward.Bind(this);
        if (isInitialized)
            return;

        BindMoneyEvents();
        displayedAmount = GameManager.Instance != null ? GameManager.Instance.Money : 0;
        RefreshImmediate(displayedAmount);
        isInitialized = true;
    }

    /// <summary>패널·라벨·골드 아이콘만 보장. FlyReward 바인딩은 포함하지 않습니다.</summary>
    public void EnsureUiElements()
    {
        EnsureGoldPanelAndLabel();
        EnsureGoldIcon();
        ApplyGoldPanelLayout();
    }

    public void EnsureReady()
    {
        EnsureUiElements();
    }

    private void Start()
    {
        if (!isInitialized)
            Initialize();
        else
            EnsureUiElements();
    }

    private void OnEnable()
    {
        BindMoneyEvents();
    }

    private void OnDisable()
    {
        UnbindMoneyEvents();

        if (collectRoutine != null)
        {
            StopCoroutine(collectRoutine);
            collectRoutine = null;
        }

        isAnimatingCollect = false;
        ResetGoldTextScale();
    }

    public void ApplyCollectedGold(long amount)
    {
        if (amount <= 0 || GameManager.Instance == null)
            return;

        GameManager.Instance.AddMoney(amount);
        long targetAmount = GameManager.Instance.Money;

        if (collectRoutine != null)
        {
            StopCoroutine(collectRoutine);
            collectRoutine = null;
            ResetGoldTextScale();
        }

        collectRoutine = StartCoroutine(AnimateCollect(targetAmount));
    }

    private void ResetGoldTextScale()
    {
        if (goldText == null)
            return;

        goldText.rectTransform.localScale = GoldTextNormalScale;
    }

    private IEnumerator AnimateCollect(long targetAmount)
    {
        isAnimatingCollect = true;
        long startAmount = displayedAmount;
        float elapsed = 0f;
        ResetGoldTextScale();

        while (elapsed < CollectAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / CollectAnimDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            displayedAmount = (long)Mathf.Lerp(startAmount, targetAmount, eased);

            if (goldText != null)
            {
                goldText.text = displayedAmount.ToString("N0");
                float punch = 1f + Mathf.Sin(t * Mathf.PI) * CollectScalePunch;
                goldText.rectTransform.localScale = GoldTextNormalScale * punch;
            }

            yield return null;
        }

        displayedAmount = targetAmount;
        RefreshImmediate(displayedAmount);
        ResetGoldTextScale();

        isAnimatingCollect = false;
        collectRoutine = null;
    }

    private void BindMoneyEvents()
    {
        if (isBound || GameManager.Instance == null)
            return;

        GameManager.Instance.OnMoneyChanged -= Refresh;
        GameManager.Instance.OnMoneyChanged += Refresh;
        isBound = true;
    }

    private void UnbindMoneyEvents()
    {
        if (!isBound || GameManager.Instance == null)
            return;

        GameManager.Instance.OnMoneyChanged -= Refresh;
        isBound = false;
    }

    private void EnsureGoldPanelAndLabel()
    {
        if (goldText != null)
        {
            if (panelImage == null && goldText.transform.parent != null)
                panelImage = goldText.transform.parent.GetComponent<Image>();

            ConfigureGoldLabel(goldText);
            return;
        }

        var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        var panel = new GameObject("GoldPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.sizeDelta = new Vector2(280f, 52f);
        panelRect.anchoredPosition = new Vector2(-16f, -16f);

        panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.1f, 0.08f, 0.82f);
        panelImage.raycastTarget = false;

        var labelGo = new GameObject("GoldLabel", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(panel.transform, false);
        goldText = labelGo.GetComponent<Text>();
        goldText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        goldText.fontSize = 28;
        goldText.fontStyle = FontStyle.Bold;
        goldText.alignment = TextAnchor.MiddleRight;
        goldText.color = new Color(1f, 0.92f, 0.55f);
        goldText.raycastTarget = false;
        goldText.text = "0";
        ConfigureGoldLabel(goldText);
    }

    private void EnsureGoldIcon()
    {
        var panelTransform = ResolveGoldPanelTransform();
        if (panelTransform == null)
            return;

        var existingIcon = panelTransform.Find("GoldIcon") as RectTransform;
        if (existingIcon != null)
        {
            flyTargetIcon = existingIcon;
            ApplyCoinIcon(existingIcon.GetComponent<Image>());
            ConfigureGoldIcon(existingIcon);
            EnsureGoldIconClickHandler(existingIcon.gameObject);
            return;
        }

        var icon = new GameObject("GoldIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Button));
        icon.transform.SetParent(panelTransform, false);
        icon.transform.SetAsFirstSibling();

        flyTargetIcon = icon.GetComponent<RectTransform>();
        ConfigureGoldIcon(flyTargetIcon);

        var iconImage = icon.GetComponent<Image>();
        ApplyCoinIcon(iconImage);
        iconImage.raycastTarget = true;
        EnsureGoldIconClickHandler(icon);
    }

    private void EnsureGoldIconClickHandler(GameObject iconGo)
    {
        if (iconGo == null)
            return;

        var image = iconGo.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;

        var button = iconGo.GetComponent<Button>();
        if (button == null)
            button = iconGo.AddComponent<Button>();

        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.RemoveListener(OnGoldIconClicked);
        button.onClick.AddListener(OnGoldIconClicked);
    }

    private void OnGoldIconClicked()
    {
        ApplyCollectedGold(CheatGoldPerClick);
    }

    private void ApplyGoldPanelLayout()
    {
        var panelTransform = ResolveGoldPanelTransform() as RectTransform;
        if (panelTransform == null)
            return;

        panelTransform.sizeDelta = new Vector2(280f, 52f);

        if (panelTransform.GetComponent<RectMask2D>() == null)
            panelTransform.gameObject.AddComponent<RectMask2D>();

        var layout = panelTransform.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = panelTransform.gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.padding = new RectOffset(10, 12, 4, 4);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (flyTargetIcon != null)
            ConfigureGoldIcon(flyTargetIcon);

        if (goldText != null)
            ConfigureGoldLabel(goldText);

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelTransform);
    }

    private static void ConfigureGoldIcon(RectTransform iconRect)
    {
        if (iconRect == null)
            return;

        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(32f, 32f);
        iconRect.anchoredPosition = Vector2.zero;

        var layoutElement = iconRect.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = iconRect.gameObject.AddComponent<LayoutElement>();

        layoutElement.minWidth = 32f;
        layoutElement.preferredWidth = 32f;
        layoutElement.minHeight = 32f;
        layoutElement.preferredHeight = 32f;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }

    private static void ConfigureGoldLabel(Text label)
    {
        if (label == null)
            return;

        var labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = new Vector2(210f, 40f);
        labelRect.anchoredPosition = Vector2.zero;

        var layoutElement = labelRect.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = labelRect.gameObject.AddComponent<LayoutElement>();

        layoutElement.minWidth = 120f;
        layoutElement.preferredWidth = 210f;
        layoutElement.flexibleWidth = 1f;
        layoutElement.minHeight = 36f;
        layoutElement.preferredHeight = 40f;
        layoutElement.flexibleHeight = 0f;

        label.alignment = TextAnchor.MiddleRight;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.resizeTextForBestFit = false;
        label.fontSize = 28;
        labelRect.localScale = GoldTextNormalScale;
    }

    private Transform ResolveGoldPanelTransform()
    {
        if (panelImage != null)
            return panelImage.transform;

        if (goldText != null && goldText.transform.parent != null)
            return goldText.transform.parent;

        var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
        return canvas != null ? canvas.transform.Find("GoldPanel") : null;
    }

    private static void ApplyCoinIcon(Image image)
    {
        if (image == null)
            return;

        DefenseGoldCoinVisual.ApplyHudIcon(image);
        if (image.sprite != null)
            return;

        image.sprite = DefenseUISprites.White;
        image.color = new Color(1f, 0.84f, 0.2f);
    }

    private void Refresh(long amount)
    {
        if (isAnimatingCollect)
            return;

        displayedAmount = amount;
        RefreshImmediate(amount);
    }

    private void RefreshImmediate(long amount)
    {
        if (goldText == null)
            return;

        goldText.text = amount.ToString("N0");
    }
}
