using TMPro;
using UnityEngine;

/// <summary>
/// 드릴·건설 중 플레이어 위에 남은 시간을 표시합니다.
/// </summary>
public class FarmDrillCountdownUI : MonoBehaviour
{
    private TextMeshProUGUI countdownText;
    private Transform followTarget;
    private string labelPrefix = string.Empty;

    [SerializeField] private float heightOffset = 1.85f;
    [SerializeField] private float worldScale = 0.014f;

    public static FarmDrillCountdownUI Create(Transform target)
    {
        var root = new GameObject("FarmDrillCountdown");
        root.transform.SetParent(target, false);
        var ui = root.AddComponent<FarmDrillCountdownUI>();
        ui.followTarget = target;
        ui.BuildUi();
        root.SetActive(false);
        return ui;
    }

    public void SetPrefix(string prefix)
    {
        labelPrefix = prefix ?? string.Empty;
    }

    private void BuildUi()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var canvasRect = GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(260f, 96f);
        canvasRect.localScale = Vector3.one * worldScale;

        var labelObject = new GameObject("CountdownLabel", typeof(RectTransform));
        labelObject.transform.SetParent(transform, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        countdownText = labelObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            countdownText.font = TMP_Settings.defaultFontAsset;

        countdownText.fontSize = 44;
        countdownText.fontStyle = FontStyles.Bold;
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.color = new Color(1f, 0.95f, 0.55f);
        countdownText.outlineWidth = 0.28f;
        countdownText.outlineColor = new Color32(0, 0, 0, 210);
        countdownText.raycastTarget = false;
        countdownText.enableWordWrapping = false;
    }

    public void SetRemaining(float seconds)
    {
        if (countdownText == null)
            return;

        seconds = Mathf.Max(0f, seconds);
        countdownText.text = string.IsNullOrEmpty(labelPrefix)
            ? seconds.ToString("0.0")
            : $"{labelPrefix} {seconds:0.0}s";
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        labelPrefix = string.Empty;
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (followTarget == null || !gameObject.activeSelf)
            return;

        transform.position = followTarget.position + Vector3.up * heightOffset;

        var cam = Camera.main;
        if (cam != null)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
