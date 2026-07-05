using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>메테오·번개 등 이벤트 착탄/발동 전 바닥 경고 마커.</summary>
public class CwslEventZoneWarningMarker : MonoBehaviour
{
    public void Begin(float durationSeconds, string title, Color labelColor)
    {
        StartCoroutine(Run(durationSeconds, title, labelColor));
    }

    private IEnumerator Run(float durationSeconds, string title, Color labelColor)
    {
        var label = CreateLabel(title, labelColor);
        var baseScale = transform.localScale;
        var elapsed = 0f;

        while (elapsed < durationSeconds)
        {
            elapsed += Time.deltaTime;
            var remaining = Mathf.CeilToInt(Mathf.Max(0f, durationSeconds - elapsed));
            if (label != null)
                label.text = $"{title}\n<size=80%>{remaining}</size>";

            var pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.08f;
            transform.localScale = baseScale * pulse;
            yield return null;
        }

        if (label != null)
            Destroy(label.gameObject);
    }

    private TextMeshPro CreateLabel(string title, Color color)
    {
        var labelRoot = new GameObject("WarningLabel");
        labelRoot.transform.SetParent(transform, false);
        labelRoot.transform.localPosition = Vector3.up * 2.8f;
        labelRoot.AddComponent<CwslBillboardToCamera>();

        var text = labelRoot.AddComponent<TextMeshPro>();
        text.text = title;
        text.fontSize = 6.5f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.rectTransform.sizeDelta = new Vector2(5f, 2.5f);
        return text;
    }
}
