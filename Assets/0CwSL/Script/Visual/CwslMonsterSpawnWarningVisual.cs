using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>몬스터 리스폰 위치 사전 경고(약 2초).</summary>
public static class CwslMonsterSpawnWarningVisual
{
    private static Transform root;

    public static void Show(Vector3 position, CwslMonsterType monsterType, float durationSeconds)
    {
        EnsureRoot();

        var markerObject = new GameObject("MonsterSpawnWarning");
        markerObject.transform.SetParent(root, false);
        markerObject.transform.position = new Vector3(position.x, 0f, position.z);

        var labelColor = ResolveLabelColor(monsterType);
        var ringDiameter = CwslGameConstants.MonsterSpawnWarningRadius * 2f;
        var ring = CwslGroundRingVisual.Create(
            markerObject.transform.position,
            ringDiameter,
            new Color(labelColor.r, labelColor.g, labelColor.b, 0.55f));
        ring.transform.SetParent(markerObject.transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.04f, 0f);

        var core = CwslGroundRingVisual.Create(
            markerObject.transform.position,
            ringDiameter * 0.42f,
            new Color(labelColor.r, labelColor.g, labelColor.b, 0.82f),
            0.05f);
        core.transform.SetParent(markerObject.transform, false);
        core.transform.localPosition = new Vector3(0f, 0.05f, 0f);

        markerObject.AddComponent<CwslMonsterSpawnWarningRunner>()
            .Begin(durationSeconds, ResolveLabel(monsterType), labelColor, ring.transform, core.transform);
    }

    private static void EnsureRoot()
    {
        if (root != null)
            return;

        root = new GameObject("CwslMonsterSpawnWarnings").transform;
        Object.DontDestroyOnLoad(root.gameObject);
    }

    private static string ResolveLabel(CwslMonsterType monsterType)
    {
        return monsterType switch
        {
            CwslMonsterType.Ranged => "궁수",
            CwslMonsterType.Suicide => "돌격",
            _ => "적"
        };
    }

    private static Color ResolveLabelColor(CwslMonsterType monsterType)
    {
        return monsterType switch
        {
            CwslMonsterType.Ranged => new Color(0.55f, 0.35f, 0.95f, 1f),
            CwslMonsterType.Suicide => new Color(0.95f, 0.42f, 0.12f, 1f),
            _ => new Color(0.95f, 0.22f, 0.16f, 1f)
        };
    }
}

public class CwslMonsterSpawnWarningRunner : MonoBehaviour
{
    private TextMeshPro label;
    private Transform ring;
    private Transform core;
    private string title;
    private Color labelColor;
    private float durationSeconds;
    private Vector3 ringBaseScale;
    private Vector3 coreBaseScale;

    public void Begin(
        float duration,
        string warningTitle,
        Color color,
        Transform warningRing,
        Transform warningCore)
    {
        durationSeconds = duration;
        title = warningTitle;
        labelColor = color;
        ring = warningRing;
        core = warningCore;
        ringBaseScale = ring != null ? ring.localScale : Vector3.one;
        coreBaseScale = core != null ? core.localScale : Vector3.one;
        label = CreateLabel(warningTitle, color);
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        var elapsed = 0f;
        while (elapsed < durationSeconds)
        {
            elapsed += Time.deltaTime;
            var remaining = Mathf.CeilToInt(Mathf.Max(0f, durationSeconds - elapsed));
            if (label != null)
                label.text = $"{title}\n<size=72%>{remaining}</size>";

            var pulse = 1f + Mathf.Sin(Time.time * 9f) * 0.1f;
            if (ring != null)
                ring.localScale = ringBaseScale * pulse;
            if (core != null)
                core.localScale = coreBaseScale * (1f + Mathf.Sin(Time.time * 11f) * 0.08f);

            yield return null;
        }

        Destroy(gameObject);
    }

    private TextMeshPro CreateLabel(string warningTitle, Color color)
    {
        var labelRoot = new GameObject("SpawnWarningLabel");
        labelRoot.transform.SetParent(transform, false);
        labelRoot.transform.localPosition = Vector3.up * 1.8f;
        labelRoot.AddComponent<CwslBillboardToCamera>();

        var text = labelRoot.AddComponent<TextMeshPro>();
        text.text = warningTitle;
        text.fontSize = 5.2f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.rectTransform.sizeDelta = new Vector2(4f, 2f);
        return text;
    }
}
