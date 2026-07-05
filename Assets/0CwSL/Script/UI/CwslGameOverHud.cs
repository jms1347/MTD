using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>전원 사망 시 다시하기 UI.</summary>
public static class CwslGameOverHud
{
    private static GameObject root;
    private static Button restartButton;

    public static void Ensure(Transform canvasTransform)
    {
        if (root != null)
            return;

        root = new GameObject("CwslGameOverHud", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvasTransform, false);

        var panelRect = root.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var dim = root.GetComponent<Image>();
        dim.color = new Color(0.02f, 0.03f, 0.06f, 0.72f);
        dim.raycastTarget = true;

        var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(root.transform, false);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(460f, 240f);
        box.GetComponent<Image>().color = new Color(0.07f, 0.09f, 0.13f, 0.96f);

        var title = CreateLabel(box.transform, "Title", "전원 사망", 34f, FontStyles.Bold);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(400f, 48f);
        title.color = new Color(1f, 0.55f, 0.5f);

        var subtitle = CreateLabel(box.transform, "Subtitle", "파티가 모두 쓰러졌습니다.", 20f, FontStyles.Normal);
        var subtitleRect = subtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.anchoredPosition = new Vector2(0f, -84f);
        subtitleRect.sizeDelta = new Vector2(400f, 32f);
        subtitle.color = new Color(0.82f, 0.86f, 0.92f);

        restartButton = CreateButton(box.transform, "다시하기", new Vector2(0f, -48f));
        restartButton.onClick.AddListener(OnRestartClicked);

        root.SetActive(false);
    }

    public static void SetVisible(bool visible)
    {
        if (root == null)
            return;

        root.SetActive(visible);
        if (restartButton != null)
            restartButton.interactable = visible;
    }

    public static void SetDefenseResult(bool victory)
    {
        EnsureForDefense();
        if (root == null)
            return;

        root.SetActive(true);
        if (titleLabel != null)
        {
            titleLabel.text = victory ? "방어 성공!" : "넥서스 파괴";
            titleLabel.color = victory ? new Color(1f, 0.9f, 0.45f) : new Color(1f, 0.45f, 0.4f);
        }

        if (subtitleLabel != null)
            subtitleLabel.text = victory ? "5분을 버텼습니다." : "넥서스가 파괴되었습니다.";

        if (restartButton != null)
            restartButton.interactable = true;
    }

    private static TextMeshProUGUI titleLabel;
    private static TextMeshProUGUI subtitleLabel;

    private static void EnsureForDefense()
    {
        if (root != null)
            return;

        var canvas = GameObject.Find("CwslGameHudCanvas");
        if (canvas == null)
            return;

        Ensure(canvas.transform);
        titleLabel = root.transform.Find("Box/Title")?.GetComponent<TextMeshProUGUI>();
        subtitleLabel = root.transform.Find("Box/Subtitle")?.GetComponent<TextMeshProUGUI>();
    }

    private static void OnRestartClicked()
    {
        var flow = CwslGameFlow.Instance;
        if (flow == null)
        {
            CwslGameFlow.RestartFallback();
            return;
        }

        flow.RequestRestartServerRpc();
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
        var buttonObject = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
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
