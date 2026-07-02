using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoguelikeCardSlotView : MonoBehaviour
{
    [SerializeField] private Image cardImage;
    [SerializeField] private Image typeBadgeImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private Button button;
    [SerializeField] private RoguelikeCardGrowEffect growEffect;

    private Action onClick;

    public void Bind(
        RoguelikeCardData card,
        RoguelikeCardVisualCatalog catalog,
        Action clickHandler)
    {
        onClick = clickHandler;

        if (cardImage != null && catalog != null)
            cardImage.sprite = catalog.Resolve(card.cardColor);

        if (cardImage != null && cardImage.sprite == null)
            cardImage.color = ResolveColorTint(card.cardColor);

        if (nameText != null)
            nameText.text = card.cardName;

        if (descriptionText != null)
            descriptionText.text = BuildDescription(card);

        if (typeText != null)
            typeText.text = ResolveTypeLabel(card);

        if (typeBadgeImage != null)
            typeBadgeImage.color = ResolveTypeBadgeColor(card);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        growEffect?.SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        growEffect?.SetSelected(selected);
    }

    private void HandleClick()
    {
        onClick?.Invoke();
    }

    private static string BuildDescription(RoguelikeCardData card)
    {
        if (!string.IsNullOrWhiteSpace(card.description))
            return card.description;

        if (card.cardType == RoguelikeCardType.Conditional)
            return $"{ResolveConditionLabel(card.conditionType)} {card.conditionValue} 달성 시 보상";

        return string.Empty;
    }

    private static string ResolveTypeLabel(RoguelikeCardData card)
    {
        if (card == null)
            return string.Empty;

        return card.cardType switch
        {
            RoguelikeCardType.Magic when card.IsGroundTargetMagic => "마법·조준",
            RoguelikeCardType.Magic => "마법·즉시",
            RoguelikeCardType.Conditional => "조건",
            _ => "패시브"
        };
    }

    private static string ResolveConditionLabel(RoguelikeConditionType type)
    {
        return type switch
        {
            RoguelikeConditionType.KillEnemies => "적 처치",
            RoguelikeConditionType.ClearStages => "스테이지 클리어",
            RoguelikeConditionType.SpendGold => "골드 소비",
            RoguelikeConditionType.BuildTowers => "타워 건설",
            _ => "조건"
        };
    }

    private static Color ResolveTypeBadgeColor(RoguelikeCardData card)
    {
        if (card == null)
            return new Color(0.25f, 0.75f, 0.95f, 0.9f);

        return card.cardType switch
        {
            RoguelikeCardType.Magic when card.IsGroundTargetMagic => new Color(0.35f, 0.55f, 1f, 0.92f),
            RoguelikeCardType.Magic => new Color(0.35f, 0.9f, 0.45f, 0.92f),
            RoguelikeCardType.Conditional => new Color(0.95f, 0.82f, 0.2f, 0.92f),
            _ => new Color(0.95f, 0.35f, 0.35f, 0.92f)
        };
    }

    public static RoguelikeCardSlotView Create(
        Transform parent,
        RoguelikeCardVisualCatalog catalog,
        RoguelikeCardData card,
        Action clickHandler)
    {
        var root = new GameObject($"Card_{card.cardCode}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(RoguelikeCardGrowEffect));
        root.transform.SetParent(parent, false);

        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(220f, 320f);

        var background = root.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.35f);

        var button = root.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;

        var grow = root.GetComponent<RoguelikeCardGrowEffect>();

        var artObject = new GameObject("Art", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        artObject.transform.SetParent(root.transform, false);
        var artRect = artObject.GetComponent<RectTransform>();
        artRect.anchorMin = Vector2.zero;
        artRect.anchorMax = Vector2.one;
        artRect.offsetMin = new Vector2(10f, 56f);
        artRect.offsetMax = new Vector2(-10f, -74f);
        var artImage = artObject.GetComponent<Image>();
        artImage.preserveAspect = true;
        artImage.raycastTarget = false;
        if (catalog != null)
            artImage.sprite = catalog.Resolve(card.cardColor);
        if (artImage.sprite == null)
            artImage.color = ResolveColorTint(card.cardColor);

        var badgeObject = new GameObject("TypeBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badgeObject.transform.SetParent(root.transform, false);
        var badgeRect = badgeObject.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(0f, 1f);
        badgeRect.pivot = new Vector2(0f, 1f);
        badgeRect.anchoredPosition = new Vector2(14f, -12f);
        badgeRect.sizeDelta = new Vector2(72f, 24f);
        var badgeImage = badgeObject.GetComponent<Image>();
        badgeImage.color = ResolveTypeBadgeColor(card);
        badgeImage.raycastTarget = false;

        var badgeTextObject = CreateText(badgeObject.transform, "TypeLabel", 13f, TextAlignmentOptions.Center);
        var badgeTextRect = badgeTextObject.GetComponent<RectTransform>();
        badgeTextRect.anchorMin = Vector2.zero;
        badgeTextRect.anchorMax = Vector2.one;
        badgeTextRect.offsetMin = Vector2.zero;
        badgeTextRect.offsetMax = Vector2.zero;

        var nameObject = CreateText(root.transform, "Name", 22f, TextAlignmentOptions.Top);
        var nameRect = nameObject.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -40f);
        nameRect.sizeDelta = new Vector2(-24f, 34f);

        var descObject = CreateText(root.transform, "Description", 15f, TextAlignmentOptions.Top);
        var descRect = descObject.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 0f);
        descRect.anchorMax = new Vector2(1f, 0f);
        descRect.pivot = new Vector2(0.5f, 0f);
        descRect.anchoredPosition = new Vector2(0f, 14f);
        descRect.sizeDelta = new Vector2(-24f, 88f);

        var slot = root.AddComponent<RoguelikeCardSlotView>();
        slot.cardImage = artImage;
        slot.typeBadgeImage = badgeImage;
        slot.nameText = nameObject.GetComponent<TextMeshProUGUI>();
        slot.descriptionText = descObject.GetComponent<TextMeshProUGUI>();
        slot.typeText = badgeTextObject.GetComponent<TextMeshProUGUI>();
        slot.button = button;
        slot.growEffect = grow;
        slot.Bind(card, catalog, clickHandler);
        return slot;
    }

    private static GameObject CreateText(Transform parent, string name, float fontSize, TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return textObject;
    }

    private static Color ResolveColorTint(RoguelikeCardColor color)
    {
        return color switch
        {
            RoguelikeCardColor.Blue => new Color(0.45f, 0.65f, 1f, 1f),
            RoguelikeCardColor.Green => new Color(0.45f, 0.9f, 0.55f, 1f),
            RoguelikeCardColor.Yellow => new Color(1f, 0.85f, 0.35f, 1f),
            _ => new Color(1f, 0.45f, 0.45f, 1f)
        };
    }
}
