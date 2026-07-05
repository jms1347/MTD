using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>아레나 기믹 클라이언트 비주얼 — ETFX + 존 표시.</summary>
public static class CwslArenaGimmickVisuals
{
    private static Transform root;
    private static GameObject silhouette;
    private static GameObject silhouetteAura;
    private static Transform centerRingRoot;
    private static GameObject pressConferenceVfx;
    private static GameObject finalPhaseVfx;
    private static GameObject teamBall;
    private static GameObject teamBallVisual;
    private static GameObject teamBallTrail;
    private static LineRenderer teamBallPathLine;
    private static Transform watchMarkerRoot;
    private static float watchMarkerHideTime;
    private static ulong watchTargetClientId;
    private static AudioSource teamBallRollSource;
    private static AudioSource blackHoleLoopSource;
    private static GameObject fightZoneAura;
    private static GameObject bossFightShield;
    private static readonly GameObject[] lighthouseGlow = new GameObject[CwslGameConstants.LighthouseCount];
    private static readonly Light[] lighthouseTopLights = new Light[CwslGameConstants.LighthouseCount];
    private static readonly float[] lighthouseActiveUntilLocal = new float[CwslGameConstants.LighthouseCount];
    private static readonly float[] lighthouseChargeLocal = new float[CwslGameConstants.LighthouseCount];
    private static readonly List<GameObject> zoneMarkers = new();
    private const float SignLabelHeight = 5.2f;

    public static void EnsureLocal()
    {
        CwslLobbyGameSettings.EnsureLoaded();

        if (root != null)
            return;

        root = new GameObject("CwslArenaGimmickVisuals").transform;
        Object.DontDestroyOnLoad(root.gameObject);

        if (root.GetComponent<CwslArenaGimmickVisualRunner>() == null)
            root.gameObject.AddComponent<CwslArenaGimmickVisualRunner>();

        BuildZoneMarkers();
        BuildLighthouses();
        BuildSilhouette();
        BuildCenterRings();
        BuildTeamBall();
        BuildWatchMarker();
    }

    public static void PlayKarmaMilestone(int milestone)
    {
        EnsureLocal();
        CwslVfxSpawner.SpawnKarmaMilestone(Vector3.zero, milestone);
    }

    public static void SetSilhouetteActive(bool active)
    {
        EnsureLocal();
        if (silhouette != null)
            silhouette.SetActive(active);
        if (silhouetteAura != null)
            silhouetteAura.SetActive(active);
    }

    public static void SetPressConferenceActive(bool active, float duration)
    {
        EnsureLocal();
        if (pressConferenceVfx != null)
            pressConferenceVfx.SetActive(active);
        if (active && finalPhaseVfx != null)
            finalPhaseVfx.SetActive(false);
    }

    public static void SetFinalPhaseActive(bool active, float duration)
    {
        EnsureLocal();
        if (finalPhaseVfx != null)
            finalPhaseVfx.SetActive(active);
        if (active && pressConferenceVfx != null)
            pressConferenceVfx.SetActive(false);
    }

    public static void OnBossPhaseChanged(CwslBossPhase phase, Vector3 bossPosition)
    {
        EnsureLocal();
        CwslVfxSpawner.SpawnBossPhaseTransition(bossPosition, phase);
        CwslArenaAudioFeedback.PlayBossPhaseShift(bossPosition);
        SetFightZoneAuraActive(phase == CwslBossPhase.RedFight);
        RefreshBossFightShield(phase == CwslBossPhase.RedFight);
    }

    public static void StartTeamBall(Vector3 start, Vector3 direction, float pathLength)
    {
        EnsureLocal();
        if (teamBall == null)
            return;

        teamBall.SetActive(true);
        teamBall.transform.position = start;

        if (teamBallPathLine != null)
        {
            teamBallPathLine.positionCount = 2;
            teamBallPathLine.SetPosition(0, start + Vector3.up * 0.05f);
            teamBallPathLine.SetPosition(1, start + direction * pathLength + Vector3.up * 0.05f);
        }

        StopTeamBallRollSound();
        teamBallRollSource = CwslArenaAudioFeedback.StartLoop(
            teamBall.transform,
            CwslArenaAudioFeedback.ResolveTeamBallRoll());
    }

    public static void PlayTeamBallHit(Vector3 hitPoint)
    {
        CwslVfxSpawner.SpawnTeamBallHit(hitPoint);
        CwslArenaAudioFeedback.PlayTeamBallHit(hitPoint);
    }

    public static void EndTeamBall()
    {
        StopTeamBallRollSound();
        if (teamBall != null)
            teamBall.SetActive(false);
    }

    private static void StopTeamBallRollSound()
    {
        CwslArenaAudioFeedback.StopLoop(teamBallRollSource);
        teamBallRollSource = null;
    }

    private static void StartBlackHoleLoopSound(Transform parent)
    {
        CwslArenaAudioFeedback.StopLoop(blackHoleLoopSource);
        blackHoleLoopSource = CwslArenaAudioFeedback.StartLoop(
            parent,
            CwslArenaAudioFeedback.ResolveBlackHoleLoop(),
            0.72f);
    }

    public static void ActivateLighthouse(int index, float duration)
    {
        EnsureLocal();
        if (index < 0 || index >= lighthouseGlow.Length || lighthouseGlow[index] == null)
            return;

        lighthouseActiveUntilLocal[index] = Time.time + duration;
        lighthouseChargeLocal[index] = 0f;
        lighthouseGlow[index].SetActive(true);

        if (lighthouseTopLights[index] != null)
        {
            lighthouseTopLights[index].enabled = true;
            lighthouseTopLights[index].intensity = 4.2f;
        }
    }

    public static void DeactivateLighthouse(int index)
    {
        if (index < 0 || index >= lighthouseGlow.Length)
            return;

        lighthouseActiveUntilLocal[index] = 0f;
        lighthouseChargeLocal[index] = 0f;

        if (lighthouseGlow[index] != null)
            lighthouseGlow[index].SetActive(false);

        if (lighthouseTopLights[index] != null)
        {
            lighthouseTopLights[index].enabled = false;
            lighthouseTopLights[index].intensity = 0f;
        }
    }

    public static void UpdateLighthouseCharge(int index, float normalizedCharge)
    {
        EnsureLocal();
        if (index < 0 || index >= lighthouseChargeLocal.Length)
            return;

        if (lighthouseActiveUntilLocal[index] > Time.time)
            return;

        lighthouseChargeLocal[index] = Mathf.Clamp01(normalizedCharge);

        if (lighthouseTopLights[index] == null)
            return;

        lighthouseTopLights[index].enabled = lighthouseChargeLocal[index] > 0.02f;
        lighthouseTopLights[index].intensity = Mathf.Lerp(0.35f, 1.8f, lighthouseChargeLocal[index]);
    }

    public static void PlayTrapPadTrigger(Vector3 padCenter)
    {
        EnsureLocal();
        CwslVfxSpawner.SpawnTrapPadTrigger(padCenter);
    }

    public static void SetWatchTarget(ulong clientId, float duration)
    {
        EnsureLocal();
        watchMarkerHideTime = Time.time + duration;
        watchTargetClientId = clientId;
        if (watchMarkerRoot == null)
            return;

        watchMarkerRoot.gameObject.SetActive(true);
        RefreshWatchVfx();
        UpdateWatchMarkerPosition();

        if (TryGetPlayerPosition(clientId, out var position))
            CwslArenaAudioFeedback.PlayBossWatchStart(position);
    }

    public static void ClearWatchTarget()
    {
        watchMarkerHideTime = 0f;
        watchTargetClientId = 0;
        ClearWatchVfx();
        if (watchMarkerRoot != null)
            watchMarkerRoot.gameObject.SetActive(false);
    }

    public static void SyncBallPosition(Vector3 position)
    {
        if (teamBall != null && teamBall.activeSelf)
            teamBall.transform.position = position;
    }

    public static void TickClient()
    {
        if (watchMarkerRoot != null && watchMarkerRoot.gameObject.activeSelf)
        {
            if (Time.time >= watchMarkerHideTime)
                watchMarkerRoot.gameObject.SetActive(false);
            else
                UpdateWatchMarkerPosition();
        }

        if (bossFightShield != null && CwslBossHongmyeongbo.Active == null)
        {
            Object.Destroy(bossFightShield);
            bossFightShield = null;
        }

        if (CwslArenaGimmickSystem.Instance != null && CwslArenaGimmickSystem.Instance.IsBallActive)
            SyncBallPosition(CwslArenaGimmickSystem.Instance.BallPosition);

        for (var i = 0; i < lighthouseActiveUntilLocal.Length; i++)
        {
            if (lighthouseActiveUntilLocal[i] > 0f && Time.time >= lighthouseActiveUntilLocal[i])
                DeactivateLighthouse(i);
        }
    }

    private static void RefreshWatchVfx()
    {
        ClearWatchVfx();
        CwslVfxSpawner.AttachWatchGlare(watchMarkerRoot);
    }

    private static void ClearWatchVfx()
    {
        if (watchMarkerRoot == null)
            return;

        for (var i = watchMarkerRoot.childCount - 1; i >= 0; i--)
            Object.Destroy(watchMarkerRoot.GetChild(i).gameObject);
    }

    private static void SetFightZoneAuraActive(bool active)
    {
        if (fightZoneAura != null)
            fightZoneAura.SetActive(active);
    }

    private static void RefreshBossFightShield(bool active)
    {
        if (bossFightShield != null)
        {
            Object.Destroy(bossFightShield);
            bossFightShield = null;
        }

        if (!active || CwslBossHongmyeongbo.Active == null)
            return;

        bossFightShield = CwslVfxSpawner.AttachBossFightShield(CwslBossHongmyeongbo.Active.transform);
    }

    private static void UpdateWatchMarkerPosition()
    {
        if (watchMarkerRoot == null || !TryGetPlayerPosition(watchTargetClientId, out var position))
            return;

        watchMarkerRoot.position = position;
    }

    private static bool TryGetPlayerPosition(ulong clientId, out Vector3 position)
    {
        position = Vector3.zero;
        if (NetworkManager.Singleton == null)
            return false;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != clientId || client.PlayerObject == null)
                continue;

            position = client.PlayerObject.transform.position;
            return true;
        }

        return false;
    }

    private static void BuildZoneMarkers()
    {
        AddGimmickSign(
            "FightZone",
            new Vector3(CwslGameConstants.FightZoneCenterX, 0f, CwslGameConstants.FightZoneCenterZ),
            CwslGameConstants.FightZoneHalfSize * 2f,
            "FIGHT 존",
            "적 이동 +50% / 보스 3페이즈",
            new Color(0.85f, 0.12f, 0.1f),
            (parent, diameter) =>
            {
                fightZoneAura = CwslVfxSpawner.AttachFightZoneAura(parent, diameter);
                if (fightZoneAura != null)
                    fightZoneAura.SetActive(false);
            });

        AddGimmickSign(
            "BlackHoleZone",
            new Vector3(CwslGameConstants.BlackHoleZoneCenterX, 0f, CwslGameConstants.BlackHoleZoneCenterZ),
            CwslGameConstants.BlackHoleZoneHalfSize * 2f,
            "블랙홀",
            "중심으로 조금씩 당김",
            new Color(0.18f, 0.22f, 0.55f),
            (parent, diameter) =>
            {
                CwslVfxSpawner.AttachBlackHoleVortex(parent, diameter);
                StartBlackHoleLoopSound(parent);
            });

        AddGimmickSign(
            "KarmaHalfZone",
            new Vector3(CwslGameConstants.KarmaHalfZoneCenterX, 0f, CwslGameConstants.KarmaHalfZoneCenterZ),
            CwslGameConstants.KarmaHalfZoneHalfSize * 2f,
            "업보 면역",
            "받는 업보 50%",
            new Color(0.35f, 0.55f, 0.95f),
            (parent, diameter) => CwslVfxSpawner.AttachZoneAura(
                CwslGameSession.Instance?.Assets?.karmaHalfZoneAuraVfx,
                parent,
                diameter));

        AddGimmickSign(
            "Tianyuan",
            Vector3.zero,
            CwslGameConstants.TianyuanRadius * 2f,
            "천원",
            "중앙=보스+30%딜 / 밖=스킬+1골드",
            new Color(0.9f, 0.12f, 0.12f),
            (parent, diameter) => CwslVfxSpawner.AttachZoneAura(
                CwslGameSession.Instance?.Assets?.tianyuanAuraVfx,
                parent,
                diameter));

        AddGimmickSign(
            "FogVortex1",
            new Vector3(CwslGameConstants.FogVortexCenterX, 0f, CwslGameConstants.FogVortexCenterZ),
            CwslGameConstants.FogVortexRadius * 2f,
            "안개 지대",
            "시야 가림",
            new Color(0.2f, 0.22f, 0.28f),
            (parent, diameter) => CwslVfxSpawner.AttachFogZone(parent, diameter));

        AddGimmickSign(
            "FogVortex2",
            new Vector3(CwslGameConstants.FogVortexCenterX2, 0f, CwslGameConstants.FogVortexCenterZ2),
            CwslGameConstants.FogVortexRadius * 2f,
            "안개 지대",
            "시야 가림",
            new Color(0.2f, 0.22f, 0.28f),
            (parent, diameter) => CwslVfxSpawner.AttachFogZone(parent, diameter));
    }

    private static void BuildTrapPads()
    {
        for (var i = 0; i < CwslGameConstants.TrapPadCount; i++)
        {
            var center = CwslArenaZones.GetTrapPadCenter(i);
            var monsterType = CwslArenaZones.GetTrapPadMonsterType(i);
            var signColor = monsterType switch
            {
                CwslMonsterType.Suicide => new Color(0.95f, 0.35f, 0.08f),
                CwslMonsterType.Ranged => new Color(0.55f, 0.25f, 0.95f),
                _ => new Color(0.9f, 0.18f, 0.1f)
            };
            AddGimmickSign(
                $"TrapPad{i}",
                center,
                CwslGameConstants.TrapPadRadius * 2f,
                CwslArenaZones.GetTrapPadLabel(i),
                CwslArenaZones.GetTrapPadSubtitle(i),
                signColor,
                (parent, diameter) => CwslVfxSpawner.AttachZoneAura(
                    CwslGameSession.Instance?.Assets?.trapPadAuraVfx,
                    parent,
                    diameter * 0.55f),
                showLabel: CwslLobbyGameSettings.ShowTrapGuideText);
        }
    }

    private static void BuildLighthouses()
    {
        const float pillarHeight = 2.45f;

        for (var i = 0; i < CwslGameConstants.LighthouseCount; i++)
        {
            var center = CwslArenaZones.GetLighthouseCenter(i);
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = $"Lighthouse{i}";
            pillar.transform.SetParent(root, false);
            pillar.transform.position = center + Vector3.up * 1.2f;
            pillar.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);
            CwslMaterialUtil.ApplyColor(pillar.GetComponent<Renderer>(), new Color(0.75f, 0.78f, 0.85f));
            Object.Destroy(pillar.GetComponent<Collider>());

            var topPosition = center + Vector3.up * pillarHeight;
            var glowRoot = new GameObject($"LighthouseGlow{i}");
            glowRoot.transform.SetParent(root, false);
            glowRoot.transform.position = topPosition;
            glowRoot.SetActive(false);
            CwslVfxSpawner.AttachLighthouseGlow(glowRoot.transform);
            lighthouseGlow[i] = glowRoot;

            var lightObject = new GameObject("LighthouseTopLight");
            lightObject.transform.SetParent(glowRoot.transform, false);
            lightObject.transform.localPosition = Vector3.zero;
            var topLight = lightObject.AddComponent<Light>();
            topLight.type = LightType.Point;
            topLight.color = new Color(1f, 0.92f, 0.55f);
            topLight.range = 22f;
            topLight.intensity = 0f;
            topLight.shadows = LightShadows.None;
            topLight.enabled = false;
            lighthouseTopLights[i] = topLight;

            AddGimmickSign(
                $"LighthouseSign{i}",
                center,
                CwslGameConstants.LighthouseRadius * 2f,
                "등대",
                "5초 서있으면 30초 시야+7",
                new Color(0.75f, 0.78f, 0.85f),
                null);
        }
    }

    private static void BuildSilhouette()
    {
        silhouette = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        silhouette.name = "HongSilhouette";
        silhouette.transform.SetParent(root, false);
        silhouette.transform.position = new Vector3(0f, 4f, 0f);
        silhouette.transform.localScale = new Vector3(2.4f, 4f, 2.4f);
        CwslMaterialUtil.ApplyColor(silhouette.GetComponent<Renderer>(), new Color(0.35f, 0.02f, 0.02f, 0.55f));
        Object.Destroy(silhouette.GetComponent<Collider>());
        silhouette.SetActive(false);

        silhouetteAura = CwslVfxSpawner.AttachSilhouetteAura(silhouette.transform);
        if (silhouetteAura != null)
            silhouetteAura.SetActive(false);
    }

    private static void BuildCenterRings()
    {
        centerRingRoot = new GameObject("CenterRings").transform;
        centerRingRoot.SetParent(root, false);
        centerRingRoot.position = new Vector3(0f, 0.04f, 0f);

        pressConferenceVfx = CwslVfxSpawner.AttachPressConferenceRing(
            centerRingRoot,
            CwslGameConstants.PressConferenceRadius * 2f);
        if (pressConferenceVfx != null)
            pressConferenceVfx.SetActive(false);

        finalPhaseVfx = CwslVfxSpawner.AttachFinalPhaseRing(
            centerRingRoot,
            CwslGameConstants.PressConferenceRadius * 2.4f);
        if (finalPhaseVfx != null)
            finalPhaseVfx.SetActive(false);
    }

    private static void BuildTeamBall()
    {
        teamBall = new GameObject("TeamBall");
        teamBall.transform.SetParent(root, false);
        teamBall.SetActive(false);

        teamBallVisual = CwslVfxSpawner.AttachTeamBallVisual(teamBall.transform);
        teamBallTrail = CwslVfxSpawner.AttachTeamBallTrail(teamBall.transform);

        if (teamBallVisual == null)
        {
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallback.name = "TeamBallFallback";
            fallback.transform.SetParent(teamBall.transform, false);
            fallback.transform.localScale = Vector3.one * CwslGameConstants.TeamBallRadius * 2f;
            CwslMaterialUtil.ApplyColor(fallback.GetComponent<Renderer>(), new Color(1f, 0.92f, 0.15f));
            Object.Destroy(fallback.GetComponent<Collider>());
        }

        var trailObject = new GameObject("TeamBallPath");
        trailObject.transform.SetParent(root, false);
        teamBallPathLine = trailObject.AddComponent<LineRenderer>();
        teamBallPathLine.material = CwslMaterialUtil.CreateMatteColored(new Color(0.95f, 0.15f, 0.1f, 0.55f));
        teamBallPathLine.startWidth = CwslGameConstants.TeamBallTrailWidth;
        teamBallPathLine.endWidth = CwslGameConstants.TeamBallTrailWidth;
        teamBallPathLine.positionCount = 2;
    }

    private static void BuildWatchMarker()
    {
        watchMarkerRoot = new GameObject("WatchMarker").transform;
        watchMarkerRoot.SetParent(root, false);
        watchMarkerRoot.gameObject.SetActive(false);
    }

    private static GameObject AddGimmickSign(
        string name,
        Vector3 center,
        float diameter,
        string title,
        string subtitle,
        Color signColor,
        System.Action<Transform, float> attachAura,
        bool showLabel = true)
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
        if (showLabel)
            AddWorldLabel(zoneRoot.transform, title, subtitle);
        return zoneRoot;
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
        text.color = new Color(1f, 0.96f, 0.88f);
        text.rectTransform.sizeDelta = new Vector2(9f, 3.5f);
        labelRoot.AddComponent<CwslBillboardToCamera>();
    }
}

/// <summary>기믹 비주얼 틱 (로컬 플레이어 HUD에서 구동).</summary>
public class CwslArenaGimmickVisualRunner : MonoBehaviour
{
    private void Update()
    {
        CwslArenaGimmickVisuals.TickClient();
    }
}
