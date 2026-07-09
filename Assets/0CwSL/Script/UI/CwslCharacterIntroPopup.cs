using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>게임 시작 시 랜덤 배정 캐릭터·스킬(QWER) 안내 팝업.</summary>
public static class CwslCharacterIntroPopup
{
    private static GameObject root;
    private static TextMeshProUGUI titleLabel;
    private static TextMeshProUGUI descriptionLabel;
    private static TextMeshProUGUI skillsLabel;
    private static TextMeshProUGUI movementLabel;

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
        boxRect.sizeDelta = new Vector2(680f, 500f);
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

        titleLabel = CreateLabel(box.transform, "Title", string.Empty, 34f, FontStyles.Bold);
        var titleRect = titleLabel.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -22f);
        titleRect.sizeDelta = new Vector2(620f, 48f);
        titleLabel.color = new Color(1f, 0.92f, 0.45f);

        var roleHeader = CreateLabel(box.transform, "RoleHeader", "캐릭터 설명", 18f, FontStyles.Bold);
        var roleHeaderRect = roleHeader.rectTransform;
        roleHeaderRect.anchorMin = new Vector2(0f, 1f);
        roleHeaderRect.anchorMax = new Vector2(1f, 1f);
        roleHeaderRect.pivot = new Vector2(0f, 1f);
        roleHeaderRect.anchoredPosition = new Vector2(28f, -78f);
        roleHeaderRect.sizeDelta = new Vector2(-56f, 28f);
        roleHeader.alignment = TextAlignmentOptions.MidlineLeft;
        roleHeader.color = new Color(0.72f, 0.8f, 0.92f);

        descriptionLabel = CreateLabel(box.transform, "Description", string.Empty, 19f, FontStyles.Normal);
        var descriptionRect = descriptionLabel.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 1f);
        descriptionRect.anchorMax = new Vector2(1f, 1f);
        descriptionRect.pivot = new Vector2(0f, 1f);
        descriptionRect.anchoredPosition = new Vector2(28f, -106f);
        descriptionRect.sizeDelta = new Vector2(-56f, 72f);
        descriptionLabel.alignment = TextAlignmentOptions.TopLeft;
        descriptionLabel.enableWordWrapping = true;
        descriptionLabel.color = new Color(0.9f, 0.92f, 0.96f);

        var skillsHeader = CreateLabel(box.transform, "SkillsHeader", "스킬 안내 (Q · W · E · R)", 18f, FontStyles.Bold);
        var skillsHeaderRect = skillsHeader.rectTransform;
        skillsHeaderRect.anchorMin = new Vector2(0f, 1f);
        skillsHeaderRect.anchorMax = new Vector2(1f, 1f);
        skillsHeaderRect.pivot = new Vector2(0f, 1f);
        skillsHeaderRect.anchoredPosition = new Vector2(28f, -186f);
        skillsHeaderRect.sizeDelta = new Vector2(-56f, 28f);
        skillsHeader.alignment = TextAlignmentOptions.MidlineLeft;
        skillsHeader.color = new Color(0.72f, 0.8f, 0.92f);

        skillsLabel = CreateLabel(box.transform, "Skills", string.Empty, 17f, FontStyles.Normal);
        var skillsRect = skillsLabel.rectTransform;
        skillsRect.anchorMin = new Vector2(0f, 1f);
        skillsRect.anchorMax = new Vector2(1f, 1f);
        skillsRect.pivot = new Vector2(0f, 1f);
        skillsRect.anchoredPosition = new Vector2(28f, -214f);
        skillsRect.sizeDelta = new Vector2(-56f, 168f);
        skillsLabel.alignment = TextAlignmentOptions.TopLeft;
        skillsLabel.enableWordWrapping = true;
        skillsLabel.color = new Color(0.86f, 0.9f, 0.96f);
        skillsLabel.lineSpacing = 2f;

        movementLabel = CreateLabel(box.transform, "Movement", string.Empty, 15f, FontStyles.Italic);
        var movementRect = movementLabel.rectTransform;
        movementRect.anchorMin = new Vector2(0f, 0f);
        movementRect.anchorMax = new Vector2(1f, 0f);
        movementRect.pivot = new Vector2(0f, 0f);
        movementRect.anchoredPosition = new Vector2(28f, 88f);
        movementRect.sizeDelta = new Vector2(-56f, 40f);
        movementLabel.alignment = TextAlignmentOptions.MidlineLeft;
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
        titleLabel.text = $"배정 캐릭터: {entry.DisplayName}";
        descriptionLabel.text = entry.Description;
        skillsLabel.text = CwslCharacterSkillCatalog.BuildGuideText(characterId);
        movementLabel.text = entry.ControlHint;

        root.transform.SetAsLastSibling();
        root.SetActive(true);
    }

    public static void Hide()
    {
        if (root != null)
            root.SetActive(false);
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
