using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
public class CwslArenaGimmickSystem : NetworkBehaviour
{
    public static CwslArenaGimmickSystem Instance { get; private set; }

    private readonly NetworkVariable<bool> silhouetteActive = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> pressConferenceActive = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> pressConferenceEndTime = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<Vector3> syncedBallPosition = new(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> ballActive = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> bossFightPhaseActive = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> bossFinalPhaseActive = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> bossFinalPhaseEndTime = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> teamBallStealsGold = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private CwslBossWatchState bossWatchState;
    private bool milestone1Triggered;
    private bool milestone2Triggered;
    private bool milestone3Triggered;
    private bool pressConferenceTriggered;
    private float nextPeriodicBallTime;
    private float nextWatchTime;

    private Vector3 ballDirection = Vector3.forward;
    private float ballTraveled;
    private readonly Dictionary<ulong, float> ballHitCooldownByClient = new();
    private readonly float[] lighthouseActiveUntil = new float[CwslGameConstants.LighthouseCount];
    private readonly float[] lighthouseStandTime = new float[CwslGameConstants.LighthouseCount];
    private readonly float[] lighthouseChargeSyncTime = new float[CwslGameConstants.LighthouseCount];
    private static readonly float[] lighthouseActiveUntilSynced = new float[CwslGameConstants.LighthouseCount];
    private readonly float[] trapPadCooldownUntil = new float[CwslGameConstants.TrapPadCount];

    public bool SilhouetteActive => silhouetteActive.Value;
    public bool PressConferenceActive => pressConferenceActive.Value;
    public bool IsBallActive => ballActive.Value;
    public bool BossFightPhaseActive => bossFightPhaseActive.Value;
    public bool BossFinalPhaseActive => bossFinalPhaseActive.Value;
    public Vector3 BallPosition => syncedBallPosition.Value;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void OnNetworkSpawn()
    {
        CwslArenaGimmickVisuals.EnsureLocal();
        syncedBallPosition.OnValueChanged += HandleBallPositionChanged;
        ballActive.OnValueChanged += HandleBallActiveChanged;

        bossWatchState = GetComponent<CwslBossWatchState>();
        if (!IsServer)
            return;

        nextPeriodicBallTime = Time.time + CwslGameConstants.TeamBallPeriodicInterval;
        nextWatchTime = Time.time + CwslGameConstants.BossWatchCooldown * 0.5f;

        if (CwslKarmaSystem.Instance != null)
            CwslKarmaSystem.Instance.OnKarmaChanged += HandleKarmaChanged;
    }

    public override void OnNetworkDespawn()
    {
        syncedBallPosition.OnValueChanged -= HandleBallPositionChanged;
        ballActive.OnValueChanged -= HandleBallActiveChanged;

        if (CwslKarmaSystem.Instance != null)
            CwslKarmaSystem.Instance.OnKarmaChanged -= HandleKarmaChanged;
    }

    private void HandleBallPositionChanged(Vector3 previous, Vector3 current)
    {
        if (ballActive.Value)
            CwslArenaGimmickVisuals.SyncBallPosition(current);
    }

    private void HandleBallActiveChanged(bool previous, bool active)
    {
        if (!active)
            CwslArenaGimmickVisuals.EndTeamBall();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        TickTeamBallServer();
        TickPressConferenceServer();
        TickWatchServer();
        TickBlackHolePullServer();
        TickLighthouseServer();
        TickTrapPadsServer();
        TickPeriodicTeamBallServer();
        TickBossFightDrainServer();
        TickBossFinalPhaseServer();
    }

    public static bool AreSkillsFreeInFightPhase(Vector3 casterPosition)
    {
        return Instance != null
               && Instance.BossFightPhaseActive
               && CwslArenaZones.IsInFightZone(casterPosition);
    }

    public static bool IsBossFinalPhaseDarkness => Instance != null && Instance.BossFinalPhaseActive;

    public static bool IsInsideFinalPhaseVision(Vector3 worldPosition) =>
        CwslArenaZones.IsInPressConference(worldPosition);

    public static int GetExtraSkillGoldCost(Vector3 casterPosition)
    {
        if (AreSkillsFreeInFightPhase(casterPosition))
            return 0;

        var extra = 0;
        if (Instance != null && Instance.PressConferenceActive && CwslArenaZones.IsInPressConference(casterPosition))
            extra += CwslGameConstants.PressConferenceSkillGoldPenalty;

        if (!CwslArenaZones.IsInTianyuan(casterPosition))
            extra += CwslGameConstants.TianyuanOutsideSkillCostPenalty;

        return extra;
    }

    public static float GetLighthouseVisionBonus(Vector3 playerPosition)
    {
        for (var i = 0; i < CwslGameConstants.LighthouseCount; i++)
        {
            if (Time.time >= lighthouseActiveUntilSynced[i])
                continue;

            if (CwslArenaZones.IsNearLighthouse(playerPosition, i, out _))
                return CwslGameConstants.LighthouseVisionBonus;
        }

        return 0f;
    }

    public static bool IsFogVortexAt(Vector3 position) => CwslArenaZones.IsInFogVortex(position);

    public void NotifyBossSpawnedServer()
    {
        if (!IsServer)
            return;

        pressConferenceActive.Value = false;
        pressConferenceEndTime.Value = 0f;
        SyncPressConferenceClientRpc(false, 0f);
    }

    public void EnterBossPhaseServer(CwslBossPhase phase)
    {
        if (!IsServer)
            return;

        teamBallStealsGold.Value = phase == CwslBossPhase.WhiteTeamBall;
        bossFightPhaseActive.Value = phase == CwslBossPhase.RedFight;

        if (phase == CwslBossPhase.GoldFinal)
        {
            bossFinalPhaseActive.Value = true;
            bossFinalPhaseEndTime.Value = Time.time + CwslGameConstants.BossFinalPhaseDuration;
            SyncFinalPhaseClientRpc(true, CwslGameConstants.BossFinalPhaseDuration);
        }
    }

    public void CastPhase2TeamBallsServer()
    {
        if (!IsServer)
            return;

        StartCoroutine(CastPhase2TeamBallsRoutine());
    }

    private System.Collections.IEnumerator CastPhase2TeamBallsRoutine()
    {
        var count = Random.Range(
            CwslGameConstants.BossPhase2BallCountMin,
            CwslGameConstants.BossPhase2BallCountMax + 1);

        for (var i = 0; i < count; i++)
        {
            while (ballActive.Value)
                yield return null;

            var angle = Mathf.PI * 0.35f * i + Random.Range(-0.15f, 0.15f);
            var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            CastTeamBallServer(direction);
            yield return new WaitForSeconds(0.3f);
        }
    }

    public void NotifyBossHpPhaseServer(int phaseIndex)
    {
        if (!IsServer)
            return;
    }

    public void TryCastTeamBallFromBossServer(Vector3 originHint)
    {
        if (!IsServer)
            return;

        CastTeamBallServer(originHint);
    }

    public bool TryCastTeamBallPeriodicServer(bool bossAlive)
    {
        if (!IsServer || ballActive.Value || Time.time < nextPeriodicBallTime)
            return false;

        nextPeriodicBallTime = Time.time + CwslGameConstants.TeamBallPeriodicInterval;
        CastTeamBallServer(Vector3.zero);
        return true;
    }

    private void HandleKarmaChanged(long karma)
    {
        if (!IsServer)
            return;

        if (!milestone1Triggered && karma >= CwslGameConstants.KarmaMilestoneShake1)
        {
            milestone1Triggered = true;
            PlayKarmaMilestoneClientRpc(1);
        }

        if (!milestone2Triggered && karma >= CwslGameConstants.KarmaMilestoneShake2)
        {
            milestone2Triggered = true;
            PlayKarmaMilestoneClientRpc(2);
        }

        if (!milestone3Triggered && karma >= CwslGameConstants.KarmaMilestoneShake3)
        {
            milestone3Triggered = true;
            PlayKarmaMilestoneClientRpc(3);
        }

        if (!silhouetteActive.Value && karma >= CwslGameConstants.KarmaSilhouetteThreshold)
        {
            silhouetteActive.Value = true;
            SyncSilhouetteClientRpc(true);
        }

        if (!pressConferenceTriggered && karma >= CwslGameConstants.KarmaPressConferenceThreshold)
            StartPressConferenceServer();
    }

    private void StartPressConferenceServer()
    {
        pressConferenceTriggered = true;
        pressConferenceActive.Value = true;
        pressConferenceEndTime.Value = Time.time + CwslGameConstants.PressConferenceDuration;
        SyncPressConferenceClientRpc(true, CwslGameConstants.PressConferenceDuration);
    }

    private void TickPressConferenceServer()
    {
        if (!pressConferenceActive.Value)
            return;

        if (Time.time >= pressConferenceEndTime.Value)
        {
            pressConferenceActive.Value = false;
            SyncPressConferenceClientRpc(false, 0f);
        }
    }

    private void TickWatchServer()
    {
        if (CwslKarmaSystem.Instance == null
            || CwslKarmaSystem.Instance.Karma < CwslGameConstants.KarmaSilhouetteThreshold)
            return;

        if (Time.time < nextWatchTime)
            return;

        nextWatchTime = Time.time + CwslGameConstants.BossWatchCooldown;
        bossWatchState?.TryStartWatchServer();
    }

    private void TickPeriodicTeamBallServer()
    {
        TryCastTeamBallPeriodicServer(bossAlive: false);
    }

    private void TickBlackHolePullServer()
    {
        if (NetworkManager.Singleton == null)
            return;

        var center = CwslArenaZones.GetBlackHoleCenter();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health != null && !health.IsAlive)
                continue;

            var position = playerObject.transform.position;
            if (!CwslArenaZones.IsInBlackHoleZone(position))
                continue;

            ApplyBlackHolePullServer(playerObject, center, position);
        }
    }

    private static void ApplyBlackHolePullServer(NetworkObject playerObject, Vector3 center, Vector3 position)
    {
        var flat = center - position;
        flat.y = 0f;
        var distance = flat.magnitude;
        if (distance < 0.35f)
            return;

        var strength = Mathf.Lerp(1.35f, 0.4f, distance / CwslGameConstants.BlackHoleZoneHalfSize);
        var pull = flat.normalized * (CwslGameConstants.BlackHolePullSpeed * strength * Time.deltaTime);
        var next = position + pull;
        next.y = position.y;

        var rammer = playerObject.GetComponent<CwslMomentumRammerSkill>();
        if (rammer != null && rammer.IsMomentumActive)
        {
            playerObject.transform.position = next;
            return;
        }

        var agent = playerObject.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.Warp(next);
        else
            playerObject.transform.position = next;
    }

    private void TickTrapPadsServer()
    {
        if (NetworkManager.Singleton == null)
            return;

        for (var padIndex = 0; padIndex < CwslGameConstants.TrapPadCount; padIndex++)
        {
            if (Time.time < trapPadCooldownUntil[padIndex])
                continue;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerObject = client.PlayerObject;
                if (playerObject == null)
                    continue;

                var health = playerObject.GetComponent<CwslPlayerHealth>();
                if (health == null || !health.IsAlive)
                    continue;

                if (!CwslArenaZones.IsOnTrapPad(playerObject.transform.position, padIndex, out var padCenter))
                    continue;

                TriggerTrapPadServer(padIndex, padCenter, playerObject.transform.position);
                break;
            }
        }
    }

    private void TriggerTrapPadServer(int padIndex, Vector3 padCenter, Vector3 triggerPosition)
    {
        trapPadCooldownUntil[padIndex] = Time.time + CwslGameConstants.TrapPadCooldownSeconds;
        var spawnCount = Random.Range(
            CwslGameConstants.TrapPadSpawnMin,
            CwslGameConstants.TrapPadSpawnMax + 1);

        var spawner = CwslGameSession.Instance?.MonsterSpawner
                      ?? FindFirstObjectByType<CwslMonsterSpawner>();
        spawner?.SpawnMonstersNearServer(
            triggerPosition,
            spawnCount,
            CwslGameConstants.TrapPadSpawnSpread,
            CwslArenaZones.GetTrapPadMonsterType(padIndex));

        PlayTrapPadTriggerClientRpc(padCenter);
    }

    private void TickLighthouseServer()
    {
        if (NetworkManager.Singleton == null)
            return;

        for (var i = 0; i < CwslGameConstants.LighthouseCount; i++)
        {
            if (lighthouseActiveUntil[i] > 0f && Time.time >= lighthouseActiveUntil[i])
            {
                lighthouseActiveUntil[i] = 0f;
                lighthouseActiveUntilSynced[i] = 0f;
                DeactivateLighthouseClientRpc(i);
            }

            if (Time.time < lighthouseActiveUntil[i])
                continue;

            var anyoneStanding = false;
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerObject = client.PlayerObject;
                if (playerObject == null)
                    continue;

                var health = playerObject.GetComponent<CwslPlayerHealth>();
                if (health != null && !health.IsAlive)
                    continue;

                if (!CwslArenaZones.IsNearLighthouse(playerObject.transform.position, i, out _))
                    continue;

                if (!IsStandingForLighthouse(playerObject))
                    continue;

                anyoneStanding = true;
                break;
            }

            if (anyoneStanding)
                lighthouseStandTime[i] += Time.deltaTime;
            else
                lighthouseStandTime[i] = 0f;

            var normalizedCharge = lighthouseStandTime[i] / CwslGameConstants.LighthouseActivateSeconds;
            if (Time.time - lighthouseChargeSyncTime[i] >= 0.2f)
            {
                lighthouseChargeSyncTime[i] = Time.time;
                UpdateLighthouseChargeClientRpc(i, normalizedCharge);
            }

            if (lighthouseStandTime[i] < CwslGameConstants.LighthouseActivateSeconds)
                continue;

            lighthouseStandTime[i] = 0f;
            var duration = CwslGameConstants.LighthouseDuration;
            lighthouseActiveUntil[i] = Time.time + duration;
            lighthouseActiveUntilSynced[i] = lighthouseActiveUntil[i];
            ActivateLighthouseClientRpc(i, duration);
        }
    }

    private static bool IsStandingForLighthouse(NetworkObject playerObject)
    {
        var movement = playerObject.GetComponent<CwslPlayerMovement>();
        if (movement == null)
            return true;

        if (movement.CurrentMoveSpeed > 0.45f)
            return false;

        var agent = playerObject.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh && agent.hasPath)
            return agent.remainingDistance <= 0.4f;

        return true;
    }

    private void CastTeamBallServer(Vector3 originHint)
    {
        var direction = ResolveTeamBallDirection(originHint);
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector3.forward;
        direction.Normalize();

        var start = ResolveTeamBallStart(direction);
        ballDirection = direction;
        ballTraveled = 0f;
        ballHitCooldownByClient.Clear();
        ballActive.Value = true;
        syncedBallPosition.Value = start;
        StartTeamBallClientRpc(start, direction, CwslGameConstants.TeamBallPathLength);
    }

    private static Vector3 ResolveTeamBallDirection(Vector3 originHint)
    {
        if (originHint.sqrMagnitude > 0.01f)
        {
            var flat = originHint;
            flat.y = 0f;
            return flat.normalized;
        }

        var angle = Random.Range(0f, Mathf.PI * 2f);
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }

    private static Vector3 ResolveTeamBallStart(Vector3 direction)
    {
        var extent = CwslGameConstants.ArenaHalfExtent - 1f;
        var start = -direction * extent;
        start.y = 0.35f;
        return CwslArenaUtility.ClampToArena(start);
    }

    private void TickTeamBallServer()
    {
        if (!ballActive.Value)
            return;

        var delta = CwslGameConstants.TeamBallSpeed * Time.deltaTime;
        ballTraveled += delta;
        syncedBallPosition.Value += ballDirection * delta;

        TickTeamBallHitsServer(syncedBallPosition.Value);

        if (ballTraveled >= CwslGameConstants.TeamBallPathLength)
            EndTeamBallServer();
    }

    private void TickTeamBallHitsServer(Vector3 ballPosition)
    {
        if (NetworkManager.Singleton == null)
            return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health == null || !health.IsAlive)
                continue;

            var flat = playerObject.transform.position - ballPosition;
            flat.y = 0f;
            if (flat.sqrMagnitude > CwslGameConstants.TeamBallRadius * CwslGameConstants.TeamBallRadius)
                continue;

            if (ballHitCooldownByClient.TryGetValue(client.ClientId, out var nextHitTime)
                && Time.time < nextHitTime)
                continue;

            ballHitCooldownByClient[client.ClientId] = Time.time + CwslGameConstants.TeamBallHitCooldown;
            var hitPoint = playerObject.transform.position + Vector3.up * 0.9f;
            health.TryReceiveExplosionHitServer(CwslGameConstants.TeamBallDamage, hitPoint);
            if (teamBallStealsGold.Value)
                StealTeamBallGoldServer(playerObject);

            PlayTeamBallHitClientRpc(hitPoint);
        }
    }

    private static void StealTeamBallGoldServer(NetworkObject playerObject)
    {
        var gold = playerObject.GetComponent<CwslPlayerGold>();
        if (gold == null)
            return;

        var steal = Mathf.Min(gold.Gold, CwslGameConstants.TeamBallGoldSteal);
        if (steal > 0)
            gold.TrySpendGoldServer(steal);
    }

    private void TickBossFightDrainServer()
    {
        if (!bossFightPhaseActive.Value || NetworkManager.Singleton == null)
            return;

        var damage = CwslGameConstants.BossFightZoneHpDrainPerSecond * Time.deltaTime;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            if (!CwslArenaZones.IsInFightZone(playerObject.transform.position))
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            health?.TryReceiveEnvironmentHitServer(damage, playerObject.transform.position);
        }
    }

    private void TickBossFinalPhaseServer()
    {
        if (!bossFinalPhaseActive.Value)
            return;

        var bossAlive = CwslBossHongmyeongbo.Active != null;
        if (bossAlive)
        {
            var bossHealth = CwslBossHongmyeongbo.Active.GetComponent<CwslMonsterHealth>();
            bossAlive = bossHealth != null && bossHealth.IsAlive;
        }

        if (!bossAlive)
        {
            bossFinalPhaseActive.Value = false;
            SyncFinalPhaseClientRpc(false, 0f);
            return;
        }

        if (Time.time < bossFinalPhaseEndTime.Value)
            return;

        InstakillAllPlayersServer();
        bossFinalPhaseActive.Value = false;
        SyncFinalPhaseClientRpc(false, 0f);
    }

    private static void InstakillAllPlayersServer()
    {
        if (NetworkManager.Singleton == null)
            return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            health?.TryReceiveExplosionHitServer(
                CwslGameConstants.PlayerMaxHealth * 2f,
                playerObject.transform.position + Vector3.up * 0.9f);
        }
    }

    private void EndTeamBallServer()
    {
        ballActive.Value = false;
        EndTeamBallClientRpc();
    }

    [ClientRpc]
    private void PlayTrapPadTriggerClientRpc(Vector3 padCenter)
    {
        CwslArenaGimmickVisuals.PlayTrapPadTrigger(padCenter);
    }

    [ClientRpc]
    private void PlayKarmaMilestoneClientRpc(int milestone)
    {
        CwslArenaGimmickVisuals.PlayKarmaMilestone(milestone);
    }

    [ClientRpc]
    private void SyncSilhouetteClientRpc(bool active)
    {
        CwslArenaGimmickVisuals.SetSilhouetteActive(active);
    }

    [ClientRpc]
    private void SyncPressConferenceClientRpc(bool active, float duration)
    {
        CwslArenaGimmickVisuals.SetPressConferenceActive(active, duration);
    }

    [ClientRpc]
    private void StartTeamBallClientRpc(Vector3 start, Vector3 direction, float pathLength)
    {
        CwslArenaGimmickVisuals.StartTeamBall(start, direction, pathLength);
    }

    [ClientRpc]
    private void PlayTeamBallHitClientRpc(Vector3 hitPoint)
    {
        CwslArenaGimmickVisuals.PlayTeamBallHit(hitPoint);
    }

    [ClientRpc]
    private void EndTeamBallClientRpc()
    {
        CwslArenaGimmickVisuals.EndTeamBall();
    }

    [ClientRpc]
    private void ActivateLighthouseClientRpc(int index, float duration)
    {
        lighthouseActiveUntilSynced[index] = Time.time + duration;
        CwslArenaGimmickVisuals.ActivateLighthouse(index, duration);
    }

    [ClientRpc]
    private void DeactivateLighthouseClientRpc(int index)
    {
        lighthouseActiveUntilSynced[index] = 0f;
        CwslArenaGimmickVisuals.DeactivateLighthouse(index);
    }

    [ClientRpc]
    private void UpdateLighthouseChargeClientRpc(int index, float normalizedCharge)
    {
        CwslArenaGimmickVisuals.UpdateLighthouseCharge(index, normalizedCharge);
    }

    [ClientRpc]
    private void SyncFinalPhaseClientRpc(bool active, float duration)
    {
        CwslArenaGimmickVisuals.SetFinalPhaseActive(active, duration);
    }
}
