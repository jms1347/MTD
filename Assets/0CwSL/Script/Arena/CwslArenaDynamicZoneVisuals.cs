using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>랜덤 등장 맵 기믹 클라이언트 비주얼.</summary>
public static class CwslArenaDynamicZoneVisuals
{
    private static Transform root;
    private static readonly Dictionary<int, GameObject> zoneVisuals = new();
    private static readonly Dictionary<int, GameObject> rallyAuras = new();

    public static void EnsureLocal()
    {
        if (root != null)
            return;

        root = new GameObject("CwslArenaDynamicZoneVisuals").transform;
        Object.DontDestroyOnLoad(root.gameObject);
    }

    public static void ShowSpawnWarning(Vector3 center, float radius, CwslDynamicZoneKind kind, float durationSeconds)
    {
        EnsureLocal();

        var markerRoot = new GameObject("GimmickSpawnWarning");
        markerRoot.transform.SetParent(root, false);
        markerRoot.transform.position = center;

        CwslVfxSpawner.AttachEventWarningZone(
            CwslGameSession.Instance?.Assets?.fightZoneAuraVfx,
            markerRoot.transform,
            radius * 2f);

        var (title, color) = ResolveWarningStyle(kind);
        markerRoot.AddComponent<CwslEventZoneWarningMarker>().Begin(durationSeconds, title, color);
    }

    public static void SpawnZoneVisual(int zoneId, CwslDynamicZoneKind kind, Vector3 center, float radius)
    {
        EnsureLocal();
        RemoveZoneVisual(zoneId);

        var (title, subtitle, signColor, attachAura) = ResolveZoneVisual(kind, radius);
        var zoneRoot = new GameObject($"DynamicZone{zoneId}");
        zoneRoot.transform.SetParent(root, false);
        zoneRoot.transform.position = center;
        zoneVisuals[zoneId] = zoneRoot;

        var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = "SignPost";
        post.transform.SetParent(zoneRoot.transform, false);
        post.transform.localPosition = Vector3.up * 1.1f;
        post.transform.localScale = new Vector3(0.35f, 2.2f, 0.12f);
        CwslMaterialUtil.ApplyColor(post.GetComponent<Renderer>(), signColor);
        Object.Destroy(post.GetComponent<Collider>());

        attachAura?.Invoke(zoneRoot.transform, radius * 2f);

        if (kind == CwslDynamicZoneKind.RallyZone)
        {
            var aura = rallyAuras.TryGetValue(zoneId, out var existing) ? existing : null;
            if (aura == null)
            {
                aura = CwslVfxSpawner.AttachRallyZoneAura(zoneRoot.transform, radius * 2f);
                rallyAuras[zoneId] = aura;
            }

            if (aura != null)
                aura.SetActive(false);
        }

        if (CwslLobbyGameSettings.ShowTrapGuideText)
            AddWorldLabel(zoneRoot.transform, title, subtitle);
    }

    public static void SetRallyZoneActive(int zoneId, bool active)
    {
        if (rallyAuras.TryGetValue(zoneId, out var aura) && aura != null)
            aura.SetActive(active);
    }

    public static void RemoveZoneVisual(int zoneId)
    {
        if (zoneVisuals.TryGetValue(zoneId, out var zoneRoot) && zoneRoot != null)
            Object.Destroy(zoneRoot);

        zoneVisuals.Remove(zoneId);
        rallyAuras.Remove(zoneId);
    }

    private static (string title, Color color) ResolveWarningStyle(CwslDynamicZoneKind kind)
    {
        return kind switch
        {
            CwslDynamicZoneKind.TrapSuicide => ("자폭 함정", new Color(0.95f, 0.35f, 0.12f, 1f)),
            CwslDynamicZoneKind.TrapRanged => ("원거리 함정", new Color(0.55f, 0.35f, 0.95f, 1f)),
            CwslDynamicZoneKind.DonationPad => ("기부 천사", new Color(1f, 0.92f, 0.35f, 1f)),
            CwslDynamicZoneKind.BadGrass => ("불량 잔디", new Color(0.35f, 0.65f, 0.25f, 1f)),
            CwslDynamicZoneKind.HealingSpring => ("회복 샘", new Color(0.25f, 0.82f, 0.95f, 1f)),
            CwslDynamicZoneKind.TailwindGrass => ("순풍 잔디", new Color(0.35f, 0.9f, 0.55f, 1f)),
            CwslDynamicZoneKind.RallyZone => ("연합 거점", new Color(0.45f, 0.65f, 1f, 1f)),
            _ => ("기믹", new Color(1f, 0.55f, 0.35f, 1f))
        };
    }

    private static (
        string title,
        string subtitle,
        Color signColor,
        System.Action<Transform, float> attachAura) ResolveZoneVisual(CwslDynamicZoneKind kind, float radius)
    {
        return kind switch
        {
            CwslDynamicZoneKind.TrapSuicide => (
                "자폭 함정",
                "밟으면 자폭 몬스터 출몰",
                new Color(0.95f, 0.35f, 0.08f),
                (parent, diameter) => CwslVfxSpawner.AttachZoneAura(
                    CwslGameSession.Instance?.Assets?.trapPadAuraVfx,
                    parent,
                    diameter * 0.55f)),
            CwslDynamicZoneKind.TrapRanged => (
                "원거리 함정",
                "밟으면 원거리 몬스터 출몰",
                new Color(0.55f, 0.25f, 0.95f),
                (parent, diameter) => CwslVfxSpawner.AttachZoneAura(
                    CwslGameSession.Instance?.Assets?.trapPadAuraVfx,
                    parent,
                    diameter * 0.55f)),
            CwslDynamicZoneKind.DonationPad => (
                "기부 천사",
                "밟으면 골드 절반 분산",
                new Color(1f, 0.92f, 0.35f),
                (parent, diameter) => CwslVfxSpawner.AttachDonationPadGlow(parent, diameter * 0.35f)),
            CwslDynamicZoneKind.BadGrass => (
                "불량 잔디",
                "이동속도 -70%",
                new Color(0.25f, 0.45f, 0.18f),
                (parent, diameter) => CwslVfxSpawner.AttachBadGrassAura(parent, diameter * 0.55f)),
            CwslDynamicZoneKind.HealingSpring => (
                "회복 샘",
                "서있으면 체력 회복",
                new Color(0.25f, 0.82f, 0.95f),
                (parent, diameter) => CwslVfxSpawner.AttachHealingSpringAura(parent, diameter)),
            CwslDynamicZoneKind.TailwindGrass => (
                "순풍 잔디",
                "이동속도 +35%",
                new Color(0.35f, 0.9f, 0.55f),
                (parent, diameter) => CwslVfxSpawner.AttachTailwindGrassAura(parent, diameter)),
            CwslDynamicZoneKind.RallyZone => (
                "연합 거점",
                "아군 2명 이상 시 공격+25% · 시야+3",
                new Color(0.45f, 0.65f, 1f),
                (parent, diameter) => CwslVfxSpawner.AttachRallyZoneAura(parent, diameter)),
            _ => (
                "골드 샘",
                "서있으면 골드 획득",
                new Color(1f, 0.88f, 0.2f),
                (parent, diameter) => CwslVfxSpawner.AttachGoldSpringAura(parent, diameter))
        };
    }

    private static void AddWorldLabel(Transform parent, string title, string subtitle)
    {
        var labelRoot = new GameObject("Label");
        labelRoot.transform.SetParent(parent, false);
        labelRoot.transform.localPosition = Vector3.up * 5.2f;
        labelRoot.AddComponent<CwslBillboardToCamera>();

        var text = labelRoot.AddComponent<TextMeshPro>();
        text.text = $"{title}\n<size=70%>{subtitle}</size>";
        text.fontSize = 5.2f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.96f, 0.88f);
        text.rectTransform.sizeDelta = new Vector2(5f, 2.5f);
    }
}
