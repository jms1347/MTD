using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>게임 시작 시 랜덤 배정 캐릭터·스킬(QWER) 안내 팝업.</summary>
public static class CwslCharacterIntroPopup
{
    private sealed class SkillCardUi
    {
        public TextMeshProUGUI keyLabel;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI tierLabel;
        public TextMeshProUGUI costLabel;
        public TextMeshProUGUI descriptionLabel;
    }

    private static GameObject root;
    private static TextMeshProUGUI titleLabel;
    private static TextMeshProUGUI objectiveLabel;
    private static TextMeshProUGUI descriptionLabel;
    private static TextMeshProUGUI staminaRulesLabel;
    private static TextMeshProUGUI movementLabel;
    private static readonly SkillCardUi[] skillCards = new SkillCardUi[CwslCharacterSkillCatalog.SkillCount];

    private const float CardWidth = 196f;
    private const float CardHeight = 248f;
    private const float CardSpacing = 12f;

    public static void Ensure(Transform canvasTransform)
    {
        if (root != null || canvasTransform == null)
            return;

        root = new GameObject("CwslCharacterIntroPopup", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvasTransform, false);

        var panelRect = root.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var dim = root.GetComponent<Image>();
        dim.color = new Color(0.02f, 0.03f, 0.06f, 0.78f);
        dim.raycastTarget = true;

        var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(root.transform, false);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(900f, 700f);
        box.GetComponent<Image>().color = new Color(0.07f, 0.09f, 0.13f, 0.98f);

        var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(box.transform, false);
        var accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 4f);
        accent.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f, 0.95f);

        titleLabel = CreateLabel(box.transform, "Title", string.Empty, 38f, FontStyles.Bold);
        var titleRect = titleLabel.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);
        titleRect.sizeDelta = new Vector2(820f, 52f);
        titleLabel.color = new Color(1f, 0.92f, 0.45f);

        objectiveLabel = CreateLabel(box.transform, "Objective", string.Empty, 22f, FontStyles.Bold);
        var objectiveRect = objectiveLabel.rectTransform;
        objectiveRect.anchorMin = new Vector2(0.5f, 1f);
        objectiveRect.anchorMax = new Vector2(0.5f, 1f);
        objectiveRect.pivot = new Vector2(0.5f, 1f);
        objectiveRect.anchoredPosition = new Vector2(0f, -62f);
        objectiveRect.sizeDelta = new Vector2(820f, 34f);
        objectiveLabel.color = new Color(1f, 0.58f, 0.35f);
        objectiveLabel.gameObject.SetActive(false);

        descriptionLabel = CreateLabel(box.transform, "Description", string.Empty, 17f, FontStyles.Normal);
        var descriptionRect = descriptionLabel.rectTransform;
        descriptionRect.anchorMin = new Vector2(0.5f, 1f);
        descriptionRect.anchorMax = new Vector2(0.5f, 1f);
        descriptionRect.pivot = new Vector2(0.5f, 1f);
        descriptionRect.anchoredPosition = new Vector2(0f, -98f);
        descriptionRect.sizeDelta = new Vector2(820f, 52f);
        descriptionLabel.alignment = TextAlignmentOptions.Center;
        descriptionLabel.enableWordWrapping = true;
        descriptionLabel.color = new Color(0.82f, 0.86f, 0.92f);

        var cardsRow = new GameObject("SkillCards", typeof(RectTransform));
        cardsRow.transform.SetParent(box.transform, false);
        var cardsRect = cardsRow.GetComponent<RectTransform>();
        cardsRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardsRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardsRect.pivot = new Vector2(0.5f, 0.5f);
        cardsRect.anchoredPosition = new Vector2(0f, 34f);
        cardsRect.sizeDelta = new Vector2(
            CardWidth * CwslCharacterSkillCatalog.SkillCount
            + CardSpacing * (CwslCharacterSkillCatalog.SkillCount - 1),
            CardHeight);

        for (var i = 0; i < CwslCharacterSkillCatalog.SkillCount; i++)
            skillCards[i] = CreateSkillCard(cardsRow.transform, i);

        var staminaHeader = CreateLabel(box.transform, "StaminaHeader", "SP 규칙", 16f, FontStyles.Bold);
        var staminaHeaderRect = staminaHeader.rectTransform;
        staminaHeaderRect.anchorMin = new Vector2(0.5f, 0f);
        staminaHeaderRect.anchorMax = new Vector2(0.5f, 0f);
        staminaHeaderRect.pivot = new Vector2(0.5f, 0f);
        staminaHeaderRect.anchoredPosition = new Vector2(0f, 228f);
        staminaHeaderRect.sizeDelta = new Vector2(820f, 24f);
        staminaHeader.alignment = TextAlignmentOptions.MidlineLeft;
        staminaHeader.color = new Color(0.72f, 0.8f, 0.92f);

        staminaRulesLabel = CreateLabel(box.transform, "StaminaRules", string.Empty, 14f, FontStyles.Normal);
        var staminaRulesRect = staminaRulesLabel.rectTransform;
        staminaRulesRect.anchorMin = new Vector2(0.5f, 0f);
        staminaRulesRect.anchorMax = new Vector2(0.5f, 0f);
        staminaRulesRect.pivot = new Vector2(0.5f, 0f);
        staminaRulesRect.anchoredPosition = new Vector2(0f, 132f);
        staminaRulesRect.sizeDelta = new Vector2(820f, 96f);
        staminaRulesLabel.alignment = TextAlignmentOptions.TopLeft;
        staminaRulesLabel.enableWordWrapping = true;
        staminaRulesLabel.lineSpacing = -2f;
        staminaRulesLabel.color = new Color(0.74f, 0.8f, 0.9f);
        staminaRulesLabel.text = CwslSkillStaminaTable.BuildRulesGuideText();

        movementLabel = CreateLabel(box.transform, "Movement", string.Empty, 15f, FontStyles.Italic);
        var movementRect = movementLabel.rectTransform;
        movementRect.anchorMin = new Vector2(0.5f, 0f);
        movementRect.anchorMax = new Vector2(0.5f, 0f);
        movementRect.pivot = new Vector2(0.5f, 0f);
        movementRect.anchoredPosition = new Vector2(0f, 88f);
        movementRect.sizeDelta = new Vector2(820f, 36f);
        movementLabel.alignment = TextAlignmentOptions.Center;
        movementLabel.enableWordWrapping = true;
        movementLabel.color = new Color(0.62f, 0.68f, 0.76f);

        var confirmButton = CreateButton(box.transform, "확인하고 시작", new Vector2(0f, 24f));
        confirmButton.onClick.AddListener(Hide);

        root.SetActive(false);
    }

    public static bool IsVisible => root != null && root.activeSelf;

    public static void Show(CwslCharacterId characterId)
    {
        if (root == null)
            return;

        var entry = CwslCharacterCatalog.Get(characterId);
        titleLabel.text = entry.DisplayName;
        if (CwslGameConstants.UseDefenseMode)
        {
            objectiveLabel.gameObject.SetActive(true);
            objectiveLabel.text = CwslMonsterManager.GetDefenseGoalLabel();
        }
        else if (objectiveLabel != null)
        {
            objectiveLabel.gameObject.SetActive(false);
        }

        descriptionLabel.text = entry.Description;
        movementLabel.text = entry.ControlHint;

        var skills = CwslCharacterSkillCatalog.GetSkills(characterId);
        for (var i = 0; i < skillCards.Length; i++)
        {
            var skill = skills[i];
            var card = skillCards[i];
            if (card == null)
                continue;

            card.keyLabel.text = skill.KeyHint;
            card.nameLabel.text = skill.DisplayName;
            card.tierLabel.text = $"등급  {CwslCharacterSkillCatalog.BuildSkillTierLabel(skill)}";
            card.costLabel.text = $"코스트  {CwslCharacterSkillCatalog.BuildSkillCostLabel(characterId, skill)}";
            card.descriptionLabel.text = skill.Description;
        }

        root.transform.SetAsLastSibling();
        root.SetActive(true);
    }

    public static void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    private static SkillCardUi CreateSkillCard(Transform parent, int index)
    {
        var keyHint = CwslCharacterSkillCatalog.HudKeyOrder[index];
        var cardRoot = new GameObject($"SkillCard_{keyHint}", typeof(RectTransform), typeof(Image));
        cardRoot.transform.SetParent(parent, false);

        var cardRect = cardRoot.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0f, 0.5f);
        cardRect.anchorMax = new Vector2(0f, 0.5f);
        cardRect.pivot = new Vector2(0f, 0.5f);
        cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);
        cardRect.anchoredPosition = new Vector2(index * (CardWidth + CardSpacing), 0f);

        var cardBg = cardRoot.GetComponent<Image>();
        cardBg.color = new Color(0.1f, 0.12f, 0.17f, 0.98f);

        var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(cardRoot.transform, false);
        var borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        border.GetComponent<Image>().color = new Color(0.28f, 0.36f, 0.48f, 0.55f);

        var inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(cardRoot.transform, false);
        var innerRect = inner.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2f, 2f);
        innerRect.offsetMax = new Vector2(-2f, -2f);
        inner.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

        var keyBadge = new GameObject("KeyBadge", typeof(RectTransform), typeof(Image));
        keyBadge.transform.SetParent(inner.transform, false);
        var keyBadgeRect = keyBadge.GetComponent<RectTransform>();
        keyBadgeRect.anchorMin = new Vector2(0.5f, 1f);
        keyBadgeRect.anchorMax = new Vector2(0.5f, 1f);
        keyBadgeRect.pivot = new Vector2(0.5f, 1f);
        keyBadgeRect.anchoredPosition = new Vector2(0f, -10f);
        keyBadgeRect.sizeDelta = new Vector2(42f, 42f);
        keyBadge.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.95f, 0.95f);

        var keyLabel = CreateLabel(keyBadge.transform, "Key", keyHint, 24f, FontStyles.Bold);
        var keyLabelRect = keyLabel.rectTransform;
        keyLabelRect.anchorMin = Vector2.zero;
        keyLabelRect.anchorMax = Vector2.one;
        keyLabelRect.offsetMin = Vector2.zero;
        keyLabelRect.offsetMax = Vector2.zero;
        keyLabel.color = Color.white;

        var nameLabel = CreateLabel(inner.transform, "Name", string.Empty, 19f, FontStyles.Bold);
        var nameRect = nameLabel.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -58f);
        nameRect.sizeDelta = new Vector2(-16f, 30f);
        nameLabel.color = new Color(0.95f, 0.97f, 1f);

        var tierLabel = CreateLabel(inner.transform, "Tier", string.Empty, 14f, FontStyles.Bold);
        var tierRect = tierLabel.rectTransform;
        tierRect.anchorMin = new Vector2(0f, 1f);
        tierRect.anchorMax = new Vector2(1f, 1f);
        tierRect.pivot = new Vector2(0.5f, 1f);
        tierRect.anchoredPosition = new Vector2(0f, -90f);
        tierRect.sizeDelta = new Vector2(-16f, 22f);
        tierLabel.alignment = TextAlignmentOptions.MidlineLeft;
        tierLabel.color = new Color(1f, 0.82f, 0.38f);

        var costLabel = CreateLabel(inner.transform, "Cost", string.Empty, 14f, FontStyles.Normal);
        var costRect = costLabel.rectTransform;
        costRect.anchorMin = new Vector2(0f, 1f);
        costRect.anchorMax = new Vector2(1f, 1f);
        costRect.pivot = new Vector2(0.5f, 1f);
        costRect.anchoredPosition = new Vector2(0f, -112f);
        costRect.sizeDelta = new Vector2(-16f, 22f);
        costLabel.alignment = TextAlignmentOptions.MidlineLeft;
        costLabel.color = new Color(0.72f, 0.86f, 1f);

        var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(inner.transform, false);
        var dividerRect = divider.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0f, 1f);
        dividerRect.anchorMax = new Vector2(1f, 1f);
        dividerRect.pivot = new Vector2(0.5f, 1f);
        dividerRect.anchoredPosition = new Vector2(0f, -136f);
        dividerRect.sizeDelta = new Vector2(-20f, 1f);
        divider.GetComponent<Image>().color = new Color(0.35f, 0.42f, 0.52f, 0.65f);

        var descriptionLabel = CreateLabel(inner.transform, "Description", string.Empty, 14f, FontStyles.Normal);
        var descriptionRect = descriptionLabel.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 0f);
        descriptionRect.anchorMax = new Vector2(1f, 1f);
        descriptionRect.offsetMin = new Vector2(10f, 10f);
        descriptionRect.offsetMax = new Vector2(-10f, -142f);
        descriptionLabel.alignment = TextAlignmentOptions.TopLeft;
        descriptionLabel.enableWordWrapping = true;
        descriptionLabel.lineSpacing = -2f;
        descriptionLabel.color = new Color(0.84f, 0.88f, 0.94f);

        return new SkillCardUi
        {
            keyLabel = keyLabel,
            nameLabel = nameLabel,
            tierLabel = tierLabel,
            costLabel = costLabel,
            descriptionLabel = descriptionLabel,
        };
    }

    private static TextMeshProUGUI CreateLabel(
        Transform parent,
        string name,
        string text,
        float fontSize,
        FontStyles style)
    {
        var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        CwslTmpFontUtil.ApplyFont(label);
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        return label;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 anchoredPosition)
    {
        var buttonObject = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(260f, 52f);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.2f, 0.55f, 0.95f, 1f);

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var labelText = labelObject.GetComponent<TextMeshProUGUI>();
        CwslTmpFontUtil.ApplyFont(labelText);
        labelText.text = label;
        labelText.fontSize = 22f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;

        return buttonObject.GetComponent<Button>();
    }
}
