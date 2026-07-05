using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>게임 시작 시 배정 캐릭터·조작법 안내 팝업.</summary>
public static class CwslCharacterIntroPopup
{
    private static GameObject root;
    private static TextMeshProUGUI titleLabel;
    private static TextMeshProUGUI descriptionLabel;
    private static TextMeshProUGUI controlsLabel;

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
        boxRect.sizeDelta = new Vector2(620f, 420f);
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
        titleRect.anchoredPosition = new Vector2(0f, -24f);
        titleRect.sizeDelta = new Vector2(560f, 48f);
        titleLabel.color = new Color(1f, 0.92f, 0.45f);

        var roleHeader = CreateLabel(box.transform, "RoleHeader", "캐릭터 설명", 18f, FontStyles.Bold);
        var roleHeaderRect = roleHeader.rectTransform;
        roleHeaderRect.anchorMin = new Vector2(0f, 1f);
        roleHeaderRect.anchorMax = new Vector2(1f, 1f);
        roleHeaderRect.pivot = new Vector2(0f, 1f);
        roleHeaderRect.anchoredPosition = new Vector2(28f, -84f);
        roleHeaderRect.sizeDelta = new Vector2(-56f, 28f);
        roleHeader.alignment = TextAlignmentOptions.MidlineLeft;
        roleHeader.color = new Color(0.72f, 0.8f, 0.92f);

        descriptionLabel = CreateLabel(box.transform, "Description", string.Empty, 19f, FontStyles.Normal);
        var descriptionRect = descriptionLabel.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 1f);
        descriptionRect.anchorMax = new Vector2(1f, 1f);
        descriptionRect.pivot = new Vector2(0f, 1f);
        descriptionRect.anchoredPosition = new Vector2(28f, -112f);
        descriptionRect.sizeDelta = new Vector2(-56f, 88f);
        descriptionLabel.alignment = TextAlignmentOptions.TopLeft;
        descriptionLabel.enableWordWrapping = true;
        descriptionLabel.color = new Color(0.9f, 0.92f, 0.96f);

        var controlsHeader = CreateLabel(box.transform, "ControlsHeader", "조작법", 18f, FontStyles.Bold);
        var controlsHeaderRect = controlsHeader.rectTransform;
        controlsHeaderRect.anchorMin = new Vector2(0f, 1f);
        controlsHeaderRect.anchorMax = new Vector2(1f, 1f);
        controlsHeaderRect.pivot = new Vector2(0f, 1f);
        controlsHeaderRect.anchoredPosition = new Vector2(28f, -206f);
        controlsHeaderRect.sizeDelta = new Vector2(-56f, 28f);
        controlsHeader.alignment = TextAlignmentOptions.MidlineLeft;
        controlsHeader.color = new Color(0.72f, 0.8f, 0.92f);

        controlsLabel = CreateLabel(box.transform, "Controls", string.Empty, 18f, FontStyles.Normal);
        var controlsRect = controlsLabel.rectTransform;
        controlsRect.anchorMin = new Vector2(0f, 1f);
        controlsRect.anchorMax = new Vector2(1f, 1f);
        controlsRect.pivot = new Vector2(0f, 1f);
        controlsRect.anchoredPosition = new Vector2(28f, -234f);
        controlsRect.sizeDelta = new Vector2(-56f, 96f);
        controlsLabel.alignment = TextAlignmentOptions.TopLeft;
        controlsLabel.enableWordWrapping = true;
        controlsLabel.color = new Color(0.86f, 0.9f, 0.96f);

        var confirmButton = CreateButton(box.transform, "확인", new Vector2(0f, 28f));
        confirmButton.onClick.AddListener(Hide);

        root.SetActive(false);
    }

    public static void Show(CwslCharacterId characterId)
    {
        if (root == null)
            return;

        var entry = CwslCharacterCatalog.Get(characterId);
        titleLabel.text = $"당신의 캐릭터: {entry.DisplayName}";
        descriptionLabel.text = entry.Description;
        controlsLabel.text = entry.ControlHint;

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
        rect.sizeDelta = new Vector2(220f, 52f);

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
        labelText.fontSize = 24f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;

        return buttonObject.GetComponent<Button>();
    }
}
