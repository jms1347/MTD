using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CwslPlayerStaminaHud : MonoBehaviour
{
    private CwslPlayerStamina playerStamina;
    private Image fillImage;
    private TextMeshProUGUI label;

    public static CwslPlayerStaminaHud Ensure(Transform canvasTransform, CwslPlayerStamina stamina)
    {
        var existing = canvasTransform.Find("CwslStaminaHud");
        CwslPlayerStaminaHud hud;
        if (existing != null)
        {
            hud = existing.GetComponent<CwslPlayerStaminaHud>();
            if (hud == null)
                hud = existing.gameObject.AddComponent<CwslPlayerStaminaHud>();
            hud.EnsureUiBuilt(existing.GetComponent<RectTransform>());
        }
        else
        {
            var root = new GameObject("CwslStaminaHud", typeof(RectTransform));
            root.transform.SetParent(canvasTransform, false);
            hud = root.AddComponent<CwslPlayerStaminaHud>();
            hud.BuildUi(root.GetComponent<RectTransform>());
        }

        hud.Bind(stamina);
        return hud;
    }

    private void EnsureUiBuilt(RectTransform rect)
    {
        if (fillImage != null || rect == null)
            return;

        BuildUi(rect);
    }

    private void BuildUi(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 24f);
        rect.sizeDelta = new Vector2(280f, 28f);

        var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.88f);

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(bg.transform, false);
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
        fillImage = fillGo.GetComponent<Image>();
        CwslUiSpriteUtil.ConfigureHorizontalFill(fillImage, new Color(0.35f, 0.82f, 1f, 0.95f));

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(transform, false);
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label = labelGo.GetComponent<TextMeshProUGUI>();
        CwslTmpFontUtil.ApplyFont(label);
        label.fontSize = 14f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.92f, 0.96f, 1f, 0.95f);
    }

    private void Update()
    {
        if (playerStamina == null)
            return;

        Refresh(playerStamina.Current, playerStamina.Max);
    }

    private void OnDestroy()
    {
        if (playerStamina != null)
            playerStamina.OnStaminaChanged -= Refresh;
    }

    public void Bind(CwslPlayerStamina stamina)
    {
        if (playerStamina == stamina)
            return;

        if (playerStamina != null)
            playerStamina.OnStaminaChanged -= Refresh;

        playerStamina = stamina;
        if (playerStamina != null)
        {
            playerStamina.OnStaminaChanged += Refresh;
            Refresh(playerStamina.Current, playerStamina.Max);
        }
    }

    private void Refresh(float current, float max)
    {
        if (fillImage != null)
        {
            if (fillImage.sprite == null)
                CwslUiSpriteUtil.ConfigureHorizontalFill(fillImage, new Color(0.35f, 0.82f, 1f, 0.95f));

            fillImage.fillAmount = max > 0f ? current / max : 0f;
        }

        if (label != null)
            label.text = $"스테미너 {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
}
