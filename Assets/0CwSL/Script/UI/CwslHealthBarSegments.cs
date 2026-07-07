using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>스타크래프트 스타일 — HP 50 단위 칸 구분선.</summary>
public static class CwslHealthBarSegments
{
    public const float HpPerSegment = 50f;

    public static int GetSegmentCount(float maxHealth) =>
        Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1f, maxHealth) / HpPerSegment));

    public static void ClearWorldDividers(List<Transform> dividers)
    {
        if (dividers == null)
            return;

        for (var i = 0; i < dividers.Count; i++)
        {
            if (dividers[i] != null)
                Object.Destroy(dividers[i].gameObject);
        }

        dividers.Clear();
    }

    public static void BuildWorldDividers(
        Transform parent,
        float barWidth,
        float barHeight,
        float maxHealth,
        List<Transform> dividers,
        float zOffset = -0.005f)
    {
        ClearWorldDividers(dividers);
        var count = GetSegmentCount(maxHealth);
        if (count <= 1)
            return;

        var segmentWidth = barWidth / count;
        var lineWidth = Mathf.Max(0.015f, barWidth * 0.008f);
        for (var i = 1; i < count; i++)
        {
            var divider = GameObject.CreatePrimitive(PrimitiveType.Cube);
            divider.name = $"HpSeg_{i}";
            divider.transform.SetParent(parent, false);
            divider.transform.localScale = new Vector3(lineWidth, barHeight * 1.15f, 0.11f);
            divider.transform.localPosition = new Vector3(-barWidth * 0.5f + segmentWidth * i, 0f, zOffset);
            Object.Destroy(divider.GetComponent<Collider>());
            CwslMaterialUtil.ApplyColor(
                divider.GetComponent<Renderer>(),
                new Color(0.02f, 0.02f, 0.03f, 0.88f));
            dividers.Add(divider.transform);
        }
    }

    public static void ClearUiDividers(List<GameObject> dividers)
    {
        if (dividers == null)
            return;

        for (var i = 0; i < dividers.Count; i++)
        {
            if (dividers[i] != null)
                Object.Destroy(dividers[i]);
        }

        dividers.Clear();
    }

    public static void BuildUiDividers(RectTransform barRect, float maxHealth, List<GameObject> dividers)
    {
        ClearUiDividers(dividers);
        if (barRect == null)
            return;

        var count = GetSegmentCount(maxHealth);
        if (count <= 1)
            return;

        for (var i = 1; i < count; i++)
        {
            var dividerObject = new GameObject($"HpSeg_{i}", typeof(RectTransform), typeof(Image));
            dividerObject.transform.SetParent(barRect, false);
            var rect = dividerObject.GetComponent<RectTransform>();
            var t = (float)i / count;
            rect.anchorMin = new Vector2(t, 0f);
            rect.anchorMax = new Vector2(t, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(2f, 0f);
            rect.anchoredPosition = Vector2.zero;
            var image = dividerObject.GetComponent<Image>();
            image.sprite = CwslUiSpriteUtil.WhiteSprite;
            image.color = new Color(0f, 0f, 0f, 0.55f);
            image.raycastTarget = false;
            dividers.Add(dividerObject);
        }
    }
}
