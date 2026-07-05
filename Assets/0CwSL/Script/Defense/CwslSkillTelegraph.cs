using System.Collections.Generic;
using UnityEngine;

/// <summary>스킬 시전 전 바닥 경고 + 게이지 채움.</summary>
public static class CwslSkillTelegraph
{
    private static readonly Color DefaultColor = new(1f, 0.35f, 0.15f, 0.55f);
    private static readonly Color FillColor = new(1f, 0.15f, 0.08f, 0.78f);

    public static void ShowCircle(Vector3 center, float radius, float durationSeconds, string label)
    {
        var root = new GameObject("SkillTelegraph");
        root.transform.position = new Vector3(center.x, 0.05f, center.z);

        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.transform.SetParent(root.transform, false);
        ring.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
        Object.Destroy(ring.GetComponent<Collider>());
        var ringRenderer = ring.GetComponent<Renderer>();
        if (ringRenderer != null)
            ringRenderer.material = CwslMaterialUtil.CreateMatteColored(DefaultColor);

        var fill = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fill.name = "Fill";
        fill.transform.SetParent(root.transform, false);
        fill.transform.localScale = new Vector3(0.01f, 0.03f, 0.01f);
        Object.Destroy(fill.GetComponent<Collider>());
        var fillRenderer = fill.GetComponent<Renderer>();
        if (fillRenderer != null)
            fillRenderer.material = CwslMaterialUtil.CreateMatteColored(FillColor);

        var marker = root.AddComponent<CwslSkillTelegraphRunner>();
        marker.Begin(durationSeconds, label, fill.transform, radius * 2f);
    }

    public static void ShowMissileZones(IReadOnlyList<Vector3> centers, float radius, float durationSeconds, string label)
    {
        for (var i = 0; i < centers.Count; i++)
            ShowCircle(centers[i], radius, durationSeconds, $"{label} {i + 1}");
    }
}
