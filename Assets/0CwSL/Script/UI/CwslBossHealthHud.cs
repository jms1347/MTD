using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>보스 HP — 화면 상단 고정.</summary>
public class CwslBossHealthHud : MonoBehaviour
{
    private static CwslBossHealthHud instance;

    private RectTransform panelRect;
    private Image fillImage;
    private TextMeshProUGUI label;

    public static void Ensure(Transform canvasTransform)
    {
        if (instance != null || canvasTransform == null)
            return;

        var panelObject = new GameObject("CwslBossHealthHud", typeof(RectTransform), typeof(CwslBossHealthHud));
        panelObject.transform.SetParent(canvasTransform, false);
        instance = panelObject.GetComponent<CwslBossHealthHud>();
        instance.Build(panelObject.GetComponent<RectTransform>());
        panelObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        Refresh();
    }

    private void Build(RectTransform panel)
    {
        panelRect = panel;
        panel.anchorMin = new Vector2(0.5f, 1f);
        panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -94f);
        panel.sizeDelta = new Vector2(520f, 52f);

        var background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.05f, 0.06f, 0.08f, 0.88f);
        background.raycastTarget = false;

        var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(panel, false);
        var accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.sizeDelta = new Vector2(0f, 3f);
        accent.GetComponent<Image>().color = new Color(0.95f, 0.15f, 0.1f, 0.95f);
        accent.GetComponent<Image>().raycastTarget = false;

        label = CreateLabel(panel, "Label", new Vector2(0f, -6f), new Vector2(-16f, -8f), 22f, FontStyles.Bold);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = new Color(1f, 0.88f, 0.55f);

        var barBackground = new GameObject("BarBackground", typeof(RectTransform), typeof(Image));
        barBackground.transform.SetParent(panel, false);
        var barBackgroundRect = barBackground.GetComponent<RectTransform>();
        barBackgroundRect.anchorMin = new Vector2(0f, 0f);
        barBackgroundRect.anchorMax = new Vector2(1f, 0f);
        barBackgroundRect.pivot = new Vector2(0.5f, 0f);
        barBackgroundRect.anchoredPosition = new Vector2(0f, 8f);
        barBackgroundRect.sizeDelta = new Vector2(-24f, 10f);
        barBackground.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        barBackground.GetComponent<Image>().raycastTarget = false;

        var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(barBackground.transform, false);
        var fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillImage = fillObject.GetComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.color = new Color(0.95f, 0.2f, 0.12f, 1f);
        fillImage.raycastTarget = false;
    }

    private void Refresh()
    {
        if (panelRect == null)
            return;

        var boss = CwslBossHongmyeongbo.Active;
        if (boss == null)
        {
            panelRect.gameObject.SetActive(false);
            return;
        }

        var health = boss.GetComponent<CwslMonsterHealth>();
        if (health == null || !health.IsAlive)
        {
            panelRect.gameObject.SetActive(false);
            return;
        }

        panelRect.gameObject.SetActive(true);

        var maxHealth = Mathf.Max(1f, health.MaxHealth);
        var ratio = Mathf.Clamp01(health.CurrentHealth / maxHealth);
        fillImage.fillAmount = ratio;

        label.text = $"홍명보  HP {health.CurrentHealth:0} / {maxHealth:0}";
    }

    private static TextMeshProUGUI CreateLabel(
        RectTransform parent,
        string name,
        Vector2 offsetMin,
        Vector2 offsetMax,
        float fontSize,
        FontStyles style)
    {
        var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        var rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var label = labelObject.GetComponent<TextMeshProUGUI>();
        CwslTmpFontUtil.ApplyFont(label);
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        return label;
    }
}
