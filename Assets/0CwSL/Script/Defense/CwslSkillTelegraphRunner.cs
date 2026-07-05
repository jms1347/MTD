using System.Collections;
using TMPro;
using UnityEngine;

public class CwslSkillTelegraphRunner : MonoBehaviour
{
    public void Begin(float durationSeconds, string title, Transform fillTransform, float targetDiameter)
    {
        StartCoroutine(Run(durationSeconds, title, fillTransform, targetDiameter, transform));
    }

    private IEnumerator Run(float durationSeconds, string title, Transform fillTransform, float targetDiameter, Transform labelAnchor)
    {
        var label = CreateLabel(title, labelAnchor);
        var elapsed = 0f;

        while (elapsed < durationSeconds)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, durationSeconds));
            if (fillTransform != null)
                fillTransform.localScale = new Vector3(Mathf.Lerp(0.01f, targetDiameter, t), 0.03f, Mathf.Lerp(0.01f, targetDiameter, t));

            if (label != null)
            {
                var remaining = Mathf.CeilToInt(Mathf.Max(0f, durationSeconds - elapsed));
                label.text = $"{title}\n<size=80%>{remaining}</size>";
            }

            yield return null;
        }

        if (label != null)
            Destroy(label.gameObject);
        Destroy(gameObject);
    }

    private static TextMeshPro CreateLabel(string title, Transform parent)
    {
        var labelRoot = new GameObject("TelegraphLabel");
        labelRoot.transform.SetParent(parent, false);
        labelRoot.transform.localPosition = Vector3.up * 2.8f;
        labelRoot.AddComponent<CwslBillboardToCamera>();

        var text = labelRoot.AddComponent<TextMeshPro>();
        text.text = title;
        text.fontSize = 5.5f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.85f, 0.45f);
        text.rectTransform.sizeDelta = new Vector2(4f, 2f);
        return text;
    }
}
