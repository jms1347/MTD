using System.Collections.Generic;
using UnityEngine;

/// <summary>함정·광역 이벤트 클라이언트 연출.</summary>
public static class CwslArenaTrapVisuals
{
    private static Transform root;
    private static GameObject offsideLaserRoot;
    private static GameObject lightningZoneRoot;
    private static GameObject lightningZoneAura;
    private static readonly List<GameObject> zoneMarkers = new();
    private static readonly Dictionary<int, GameObject> hazardPadVisuals = new();
    private static GameObject meteorWarningRoot;
    private static GameObject lightningWarningRoot;

    public static void EnsureLocal()
    {
        CwslLobbyGameSettings.EnsureLoaded();

        if (root != null)
            return;

        root = new GameObject("CwslArenaTrapVisuals").transform;
        Object.DontDestroyOnLoad(root.gameObject);

        if (root.GetComponent<CwslArenaTrapVisualRunner>() == null)
            root.gameObject.AddComponent<CwslArenaTrapVisualRunner>();
    }

    public static void ShowOffsideLaser(Vector3 lineStart, Vector3 lineEnd)
    {
        EnsureLocal();
        HideOffsideLaser();
        offsideLaserRoot = new GameObject("OffsideLaser");
        offsideLaserRoot.transform.SetParent(root, false);
        CwslVfxSpawner.SpawnOffsideLaserLine(offsideLaserRoot.transform, lineStart, lineEnd);
        CwslArenaTrapAudio.PlayOffsideHorn((lineStart + lineEnd) * 0.5f);
    }

    public static void HideOffsideLaser()
    {
        if (offsideLaserRoot != null)
            Object.Destroy(offsideLaserRoot);
        offsideLaserRoot = null;
    }

    public static void PlayOffsideBlind(Vector3 position)
    {
        CwslVfxSpawner.SpawnFakeGoldExplosion(position + Vector3.up * 0.4f);
    }

    public static void PlayDonationScatter(Vector3 origin, int padIndex)
    {
        EnsureLocal();
        CwslGoldFeedback.PlaySpend(origin + Vector3.up * 0.5f, 1);
        CwslVfxSpawner.SpawnDonationBurst(origin);
    }

    public static void PlayMeteorShower(Vector3[] impactPoints)
    {
        if (impactPoints == null)
            return;

        foreach (var point in impactPoints)
        {
            var runner = new GameObject("CwslTrapMeteor");
            runner.transform.position = point;
            runner.AddComponent<CwslMeteorEffectRunner>().Play(
                point,
                14f,
                CwslGameConstants.MeteorShowerFallDelay,
                2.4f,
                3.2f);
        }
    }

    public static void ShowMeteorWarning(Vector3 impactPoint, float radius, float durationSeconds)
    {
        EnsureLocal();
        if (meteorWarningRoot == null)
        {
            meteorWarningRoot = new GameObject("MeteorWarnings");
            meteorWarningRoot.transform.SetParent(root, false);
        }

        var markerRoot = new GameObject("MeteorWarning");
        markerRoot.transform.SetParent(meteorWarningRoot.transform, false);
        markerRoot.transform.position = impactPoint;
        CwslVfxSpawner.AttachEventWarningZone(
            CwslGameSession.Instance?.Assets?.fightZoneAuraVfx,
            markerRoot.transform,
            radius * 2f);
        markerRoot
            .AddComponent<CwslEventZoneWarningMarker>()
            .Begin(durationSeconds, "☄", new Color(1f, 0.55f, 0.35f));
    }

    public static void ClearMeteorWarnings()
    {
        if (meteorWarningRoot == null)
            return;

        Object.Destroy(meteorWarningRoot);
        meteorWarningRoot = null;
    }

    public static void ShowLightningWarning(Vector3 center, float radius, float durationSeconds)
    {
        EnsureLocal();
        ClearLightningWarning();

        lightningWarningRoot = new GameObject("LightningWarning");
        lightningWarningRoot.transform.SetParent(root, false);
        lightningWarningRoot.transform.position = center;
        CwslVfxSpawner.AttachEventWarningZone(
            CwslGameSession.Instance?.Assets?.karmaHalfZoneAuraVfx,
            lightningWarningRoot.transform,
            radius * 2f);
        lightningWarningRoot
            .AddComponent<CwslEventZoneWarningMarker>()
            .Begin(durationSeconds, "⚡", new Color(0.55f, 0.82f, 1f));
    }

    public static void ClearLightningWarning()
    {
        if (lightningWarningRoot == null)
            return;

        Object.Destroy(lightningWarningRoot);
        lightningWarningRoot = null;
    }

    public static void StartLightningMode(Vector3 center, float radius)
    {
        EnsureLocal();
        EndLightningMode();

        lightningZoneRoot = new GameObject("LightningOrb");
        lightningZoneRoot.transform.SetParent(root, false);
        lightningZoneRoot.transform.position = center + Vector3.up * CwslGameConstants.LightningOrbHeight;
        lightningZoneAura = CwslVfxSpawner.AttachLightningOrb(
            lightningZoneRoot.transform,
            radius / 7f);
    }

    public static void PlayLightningMissile(Vector3 orbCenter, Vector3 target)
    {
        var origin = orbCenter + Vector3.up * CwslGameConstants.LightningOrbHeight;
        var runner = new GameObject("CwslLightningMissile");
        runner.transform.position = origin;
        runner.AddComponent<CwslLightningMissileRunner>().Play(
            origin,
            target,
            CwslGameConstants.LightningMissileSpeed);
    }

    public static void PlayLightningStrike(Vector3 strikePoint)
    {
        CwslVfxSpawner.SpawnLightningStrike(strikePoint);
    }

    public static void EndLightningMode()
    {
        if (lightningZoneRoot != null)
            Object.Destroy(lightningZoneRoot);
        lightningZoneRoot = null;
        lightningZoneAura = null;
    }

    public static void ShowHazardPadWarning(Vector3 center, float radius, CwslHazardPadKind kind, float durationSeconds)
    {
        EnsureLocal();

        var markerRoot = new GameObject("HazardPadWarning");
        markerRoot.transform.SetParent(root, false);
        markerRoot.transform.position = center;
        CwslVfxSpawner.AttachEventWarningZone(
            CwslGameSession.Instance?.Assets?.karmaHalfZoneAuraVfx,
            markerRoot.transform,
            radius * 2f);

        var (title, color) = kind switch
        {
            CwslHazardPadKind.Acid => ("산성", new Color(0.45f, 0.95f, 0.35f, 1f)),
            CwslHazardPadKind.Lava => ("용암", new Color(1f, 0.45f, 0.15f, 1f)),
            _ => ("물웅덩이", new Color(0.35f, 0.75f, 1f, 1f))
        };
        markerRoot.AddComponent<CwslEventZoneWarningMarker>().Begin(durationSeconds, title, color);
    }

    public static void SpawnHazardPad(int padId, CwslHazardPadKind kind, Vector3 center, float radius)
    {
        EnsureLocal();
        RemoveHazardPad(padId);

        var padRoot = new GameObject($"HazardPad_{padId}_{kind}");
        padRoot.transform.SetParent(root, false);
        padRoot.transform.position = center;
        CwslVfxSpawner.AttachHazardPad(kind, padRoot.transform, radius * 2f);
        hazardPadVisuals[padId] = padRoot;
    }

    public static void RemoveHazardPad(int padId)
    {
        if (!hazardPadVisuals.TryGetValue(padId, out var padRoot) || padRoot == null)
            return;

        Object.Destroy(padRoot);
        hazardPadVisuals.Remove(padId);
    }

    public static void PlayLavaGoldLeak(Vector3 position)
    {
        CwslGoldFeedback.PlaySpend(position + Vector3.up * 0.35f, 1);
    }

    private static void BuildStaticTraps()
    {
        for (var i = 0; i < CwslGameConstants.DonationPadCount; i++)
        {
            var center = CwslArenaZones.GetDonationPadCenter(i);
            AddTrapSign(
                $"DonationPad{i}",
                center,
                CwslGameConstants.DonationPadRadius * 2f,
                "기부 천사",
                "밟으면 골드 절반 분산",
                new Color(1f, 0.92f, 0.35f),
                (parent, diameter) => CwslVfxSpawner.AttachDonationPadGlow(parent, diameter * 0.35f));
        }

        for (var i = 0; i < CwslGameConstants.BadGrassPatchCount; i++)
        {
            var center = CwslArenaZones.GetBadGrassCenter(i);
            AddTrapSign(
                $"BadGrass{i}",
                center,
                CwslGameConstants.BadGrassPatchRadius * 2f,
                "불량 잔디",
                "이동속도 -70%",
                new Color(0.25f, 0.45f, 0.18f),
                (parent, diameter) => CwslVfxSpawner.AttachBadGrassAura(parent, diameter * 0.55f));
        }
    }

    private static void AddTrapSign(
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
        if (CwslLobbyGameSettings.ShowTrapGuideText)
            AddWorldLabel(zoneRoot.transform, title, subtitle);
    }

    private static void AddWorldLabel(Transform parent, string title, string subtitle)
    {
        var labelRoot = new GameObject("Label");
        labelRoot.transform.SetParent(parent, false);
        labelRoot.transform.localPosition = Vector3.up * 5.2f;
        labelRoot.transform.localRotation = Quaternion.identity;

        var text = labelRoot.AddComponent<TMPro.TextMeshPro>();
        text.text = string.IsNullOrEmpty(subtitle) ? title : $"{title}\n<size=72%>{subtitle}</size>";
        text.fontSize = 7f;
        text.fontStyle = TMPro.FontStyles.Bold;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.96f, 0.88f);
        text.rectTransform.sizeDelta = new Vector2(9f, 3.5f);
        labelRoot.AddComponent<CwslBillboardToCamera>();
    }
}

public static class CwslArenaTrapAudio
{
    public static void PlayOffsideHorn(Vector3 position)
    {
        var clip = CwslGameSession.Instance?.Assets?.offsideHornSound;
#if UNITY_EDITOR
        if (clip == null)
            clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.OffsideHornSound);
#endif
        if (clip == null)
            return;

        var soundObject = new GameObject("CwslOffsideHorn");
        soundObject.transform.position = position + Vector3.up * 0.5f;
        var source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = 1f;
        source.spatialBlend = 0f;
        source.Play();
        Object.Destroy(soundObject, clip.length + 0.05f);
    }
}

public class CwslArenaTrapVisualRunner : MonoBehaviour
{
    private void Start()
    {
        CwslArenaTrapVisuals.EnsureLocal();
    }
}
