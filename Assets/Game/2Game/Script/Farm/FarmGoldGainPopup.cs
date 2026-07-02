using TMPro;
using UnityEngine;

/// <summary>
/// 드릴·채굴 보상을 일꾼 머리 위에 +골드 텍스트로 표시합니다.
/// </summary>
public class FarmGoldGainPopup : MonoBehaviour
{
    private const float Lifetime = 1.15f;
    private const float RiseSpeed = 0.95f;
    private const float HeightOffset = 2.05f;

    private Transform anchor;
    private TextMeshProUGUI label;
    private float elapsed;
    private Color baseColor;

    public static void Show(Transform anchor, long amount)
    {
        if (anchor == null || amount <= 0)
            return;

        var root = new GameObject("FarmGoldGainPopup");
        var popup = root.AddComponent<FarmGoldGainPopup>();
        popup.Initialize(anchor, amount);
    }

    private void Initialize(Transform followTarget, long amount)
    {
        anchor = followTarget;
        BuildUi(amount);
        baseColor = label.color;
        SnapToAnchor();
    }

    private void BuildUi(long amount)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var canvasRect = GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(280f, 88f);
        canvasRect.localScale = Vector3.one * 0.014f;

        var labelObject = new GameObject("GainLabel", typeof(RectTransform));
        labelObject.transform.SetParent(transform, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        label = labelObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            label.font = TMP_Settings.defaultFontAsset;

        label.text = $"+{amount:N0}";
        label.fontSize = 48;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.92f, 0.2f, 1f);
        label.outlineWidth = 0.3f;
        label.outlineColor = new Color32(0, 0, 0, 220);
        label.raycastTarget = false;
        label.enableWordWrapping = false;
    }

    private void LateUpdate()
    {
        if (anchor == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;
        float rise = elapsed * RiseSpeed;
        transform.position = anchor.position + Vector3.up * (HeightOffset + rise);

        var cam = Camera.main;
        if (cam != null)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);

        float fadeStart = Lifetime * 0.45f;
        float alpha = elapsed < fadeStart
            ? 1f
            : 1f - Mathf.Clamp01((elapsed - fadeStart) / (Lifetime - fadeStart));

        label.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha);

        if (elapsed >= Lifetime)
            Destroy(gameObject);
    }

    private void SnapToAnchor()
    {
        if (anchor == null)
            return;

        transform.position = anchor.position + Vector3.up * HeightOffset;

        var cam = Camera.main;
        if (cam != null)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
