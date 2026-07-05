using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>아군 버프 존 클라이언트 비주얼.</summary>
public static class CwslArenaBuffVisuals
{
    private static Transform root;
    private static readonly GameObject[] rallyZoneAuras = new GameObject[CwslGameConstants.RallyZoneCount];
    private static readonly List<GameObject> zoneMarkers = new();
    private const float SignLabelHeight = 5.2f;

    public static void EnsureLocal()
    {
        if (root != null)
            return;

        root = new GameObject("CwslArenaBuffVisuals").transform;
        Object.DontDestroyOnLoad(root.gameObject);

        if (root.GetComponent<CwslArenaBuffVisualRunner>() == null)
            root.gameObject.AddComponent<CwslArenaBuffVisualRunner>();
    }

    public static void SetRallyZoneActive(int zoneIndex, bool active)
    {
        CwslArenaDynamicZoneVisuals.SetRallyZoneActive(zoneIndex, active);
    }

    public static void PlayGoldSpringBurst(Vector3 position)
    {
        CwslVfxSpawner.SpawnGoldSpringBurst(position + Vector3.up * 0.35f);
    }

    private static void BuildBuffZones()
    {
        for (var i = 0; i < CwslGameConstants.HealingSpringCount; i++)
        {
            var center = CwslArenaZones.GetHealingSpringCenter(i);
            AddBuffSign(
                $"HealingSpring{i}",
                center,
                CwslGameConstants.HealingSpringRadius * 2f,
                "회복 샘",
                "서있으면 체력 회복",
                new Color(0.25f, 0.82f, 0.95f),
                (parent, diameter) => CwslVfxSpawner.AttachHealingSpringAura(parent, diameter));
        }

        for (var i = 0; i < CwslGameConstants.TailwindGrassCount; i++)
        {
            var center = CwslArenaZones.GetTailwindGrassCenter(i);
            AddBuffSign(
                $"TailwindGrass{i}",
                center,
                CwslGameConstants.TailwindGrassRadius * 2f,
                "순풍 잔디",
                "이동속도 +35%",
                new Color(0.35f, 0.9f, 0.55f),
                (parent, diameter) => CwslVfxSpawner.AttachTailwindGrassAura(parent, diameter));
        }

        for (var i = 0; i < CwslGameConstants.RallyZoneCount; i++)
        {
            var center = CwslArenaZones.GetRallyZoneCenter(i);
            AddBuffSign(
                $"RallyZone{i}",
                center,
                CwslGameConstants.RallyZoneRadius * 2f,
                "연합 거점",
                "아군 2명 이상 시 공격+25% · 시야+3",
                new Color(0.45f, 0.65f, 1f),
                (parent, diameter) =>
                {
                    rallyZoneAuras[i] = CwslVfxSpawner.AttachRallyZoneAura(parent, diameter);
                    if (rallyZoneAuras[i] != null)
                        rallyZoneAuras[i].SetActive(false);
                });
        }

        for (var i = 0; i < CwslGameConstants.GoldSpringCount; i++)
        {
            var center = CwslArenaZones.GetGoldSpringCenter(i);
            AddBuffSign(
                $"GoldSpring{i}",
                center,
                CwslGameConstants.GoldSpringRadius * 2f,
                "골드 샘",
                "서있으면 골드 획득",
                new Color(1f, 0.88f, 0.2f),
                (parent, diameter) => CwslVfxSpawner.AttachGoldSpringAura(parent, diameter));
        }
    }

    private static void AddBuffSign(
        string name,
        Vector3 center,
        float diameter,
        string title,
        string subtitle,
        Color signColor,
        System.Action<Transform, float> attachAura)
    {
        var zoneRoot = new GameObject(name);
        zoneRoot.transform.SetParent(root, false);
        zoneRoot.transform.position = center;
        zoneMarkers.Add(zoneRoot);

        var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = "SignPost";
        post.transform.SetParent(zoneRoot.transform, false);
        post.transform.localPosition = Vector3.up * 1.1f;
        post.transform.localScale = new Vector3(0.35f, 2.2f, 0.12f);
        CwslMaterialUtil.ApplyColor(post.GetComponent<Renderer>(), signColor);
        Object.Destroy(post.GetComponent<Collider>());

        attachAura?.Invoke(zoneRoot.transform, diameter);
        AddWorldLabel(zoneRoot.transform, title, subtitle);
    }

    private static void AddWorldLabel(Transform parent, string title, string subtitle)
    {
        var labelRoot = new GameObject("Label");
        labelRoot.transform.SetParent(parent, false);
        labelRoot.transform.localPosition = Vector3.up * SignLabelHeight;
        labelRoot.transform.localRotation = Quaternion.identity;

        var text = labelRoot.AddComponent<TextMeshPro>();
        text.text = string.IsNullOrEmpty(subtitle) ? title : $"{title}\n<size=72%>{subtitle}</size>";
        text.fontSize = 7f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.85f, 1f, 0.92f);
        text.rectTransform.sizeDelta = new Vector2(9f, 3.5f);
        labelRoot.AddComponent<CwslBillboardToCamera>();
    }
}

public class CwslArenaBuffVisualRunner : MonoBehaviour
{
    private void Update()
    {
        // 버프 비주얼은 이벤트 기반으로 갱신됩니다.
    }
}
