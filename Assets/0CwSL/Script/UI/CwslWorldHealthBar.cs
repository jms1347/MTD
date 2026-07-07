using System.Collections.Generic;
using UnityEngine;

/// <summary>월드 스페이스 HP 바 — 넥서스·적 기지 등 구조물용.</summary>
public class CwslWorldHealthBar : MonoBehaviour
{
    private Transform barRoot;
    private Transform fillTransform;
    private Renderer fillRenderer;
    private Renderer backRenderer;
    private readonly List<Transform> segmentDividers = new();
    private float lastSegmentMaxHealth = -1f;

    private float barWidth = 2.4f;
    private float barHeight = 0.14f;
    private float heightOffset = 3f;
    private Color fillColor = new(0.25f, 0.9f, 0.35f);
    private Color backColor = new(0.12f, 0.12f, 0.14f, 0.9f);

    public void Configure(float width, float height, float offset, Color fill, Color? back = null)
    {
        barWidth = width;
        barHeight = height;
        heightOffset = offset;
        fillColor = fill;
        if (back.HasValue)
            backColor = back.Value;

        EnsureVisual();
        Refresh(1f);
    }

    public void Refresh(float ratio, float maxHealth = -1f)
    {
        EnsureVisual();
        if (fillTransform == null)
            return;

        if (maxHealth > 0f)
            EnsureSegments(maxHealth);

        ratio = Mathf.Clamp01(ratio);
        fillTransform.localScale = new Vector3(barWidth * ratio, barHeight, 0.1f);
        fillTransform.localPosition = new Vector3(-barWidth * 0.5f + (barWidth * ratio * 0.5f), 0f, -0.01f);

        if (fillRenderer != null)
        {
            var color = Color.Lerp(new Color(0.95f, 0.2f, 0.2f), fillColor, ratio);
            CwslMaterialUtil.ApplyColor(fillRenderer, color);
        }
    }

    public void SetVisible(bool visible)
    {
        EnsureVisual();
        if (barRoot != null)
            barRoot.gameObject.SetActive(visible);
    }

    private void LateUpdate()
    {
        if (barRoot == null)
            return;

        barRoot.position = transform.position + Vector3.up * heightOffset;
    }

    private void EnsureVisual()
    {
        if (barRoot != null)
            return;

        var rootObject = new GameObject("StructureHealthBar");
        rootObject.transform.SetParent(transform, false);
        barRoot = rootObject.transform;
        barRoot.gameObject.AddComponent<CwslBillboardToCamera>();

        var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.name = "HealthBar_Back";
        back.transform.SetParent(barRoot, false);
        back.transform.localScale = new Vector3(barWidth, barHeight, 0.08f);
        Destroy(back.GetComponent<Collider>());
        backRenderer = back.GetComponent<Renderer>();
        CwslMaterialUtil.ApplyColor(backRenderer, backColor);

        var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fill.name = "HealthBar_Fill";
        fill.transform.SetParent(barRoot, false);
        fill.transform.localScale = new Vector3(barWidth, barHeight, 0.1f);
        Destroy(fill.GetComponent<Collider>());
        fillRenderer = fill.GetComponent<Renderer>();
        CwslMaterialUtil.ApplyColor(fillRenderer, fillColor);
        fillTransform = fill.transform;
    }

    private void EnsureSegments(float maxHealth)
    {
        if (barRoot == null || Mathf.Approximately(lastSegmentMaxHealth, maxHealth))
            return;

        CwslHealthBarSegments.BuildWorldDividers(
            barRoot,
            barWidth,
            barHeight,
            maxHealth,
            segmentDividers);
        lastSegmentMaxHealth = maxHealth;
    }
}
