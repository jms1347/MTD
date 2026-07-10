using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>링거 Q — 홀드 중 슬로우+흡인, 뗄 때 이펙트 수축하며 중심으로 모음.</summary>
public class CwslCrowdGatherSkill : CwslPlayerSkillBase
{
    private readonly NetworkVariable<bool> isCharging = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<Vector3> syncedCenter = new(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerMovement movement;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private CwslPlayerStamina playerStamina;
    private CwslPlayerSkills playerSkills;
    private float zoneStartTime;
    private Coroutine pullRoutine;
    private readonly List<Transform> zoneTargets = new();

    public bool IsCharging => isCharging.Value;
    public Vector3 ChargeCenter => syncedCenter.Value;
    public float ChargeRadius => CwslGameConstants.GatherBlackHoleZoneRadius;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Charged;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.CrowdGatherer;

    public override void OnNetworkSpawn()
    {
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        playerHealth = GetComponent<CwslPlayerHealth>();
        movement = GetComponent<CwslPlayerMovement>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
        playerStamina = GetComponent<CwslPlayerStamina>();
        playerSkills = GetComponent<CwslPlayerSkills>();
        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged += HandleCharacterChanged;
        if (playerHealth != null)
            playerHealth.OnDied += HandleDied;
    }

    public override void OnNetworkDespawn()
    {
        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged -= HandleCharacterChanged;
        if (playerHealth != null)
            playerHealth.OnDied -= HandleDied;
    }

    private void HandleDied() => CancelChargeServer();

    private void HandleCharacterChanged(CwslCharacterId characterId)
    {
        if (!IsServer || characterId == CwslCharacterId.CrowdGatherer)
            return;

        CancelChargeServer();
    }

    private void CancelChargeServer()
    {
        if (!IsServer || !isCharging.Value)
            return;

        var center = syncedCenter.Value;
        var radius = CwslGameConstants.GatherBlackHoleZoneRadius;
        isCharging.Value = false;
        ClearZoneSlowServer(center, radius);
        EndChargeVisualClientRpc();
    }

    public override bool CanCastServer(ulong senderClientId)
    {
        return IsServer &&
               playerCharacter != null &&
               playerCharacter.CharacterId == CwslCharacterId.CrowdGatherer &&
               (playerHealth == null || playerHealth.IsAlive) &&
               !isCharging.Value &&
               pullRoutine == null &&
               (skillCooldowns == null || skillCooldowns.IsReady(0));
    }

    public bool BeginChargeServer(Vector3 worldPoint)
    {
        if (!IsServer || !CanCastServer(OwnerClientId))
            return false;

        var startCost = CwslCharacterSkillCatalog.GetStaminaCost(CwslCharacterId.CrowdGatherer, 0);
        if (CwslGameConstants.SkillsUseStamina &&
            playerStamina != null &&
            !playerStamina.TrySpendServer(startCost))
        {
            playerSkills?.NotifyStaminaInsufficientServer();
            return false;
        }

        worldPoint.y = 0f;
        movement?.StopMovement();
        isCharging.Value = true;
        zoneStartTime = Time.time;
        syncedCenter.Value = worldPoint;
        BeginBlackHoleVisualClientRpc(worldPoint, CwslGameConstants.GatherBlackHoleZoneRadius);
        PlayGatherCastClientRpc(worldPoint);
        return true;
    }

    public void UpdateChargeCenterServer(Vector3 worldPoint)
    {
        if (!IsServer || !isCharging.Value)
            return;

        worldPoint.y = 0f;
        syncedCenter.Value = worldPoint;
        SyncBlackHoleVisualClientRpc(worldPoint, CwslGameConstants.GatherBlackHoleZoneRadius);
    }

    public override void TickChargedServer()
    {
        if (!IsServer || !isCharging.Value)
            return;

        movement?.StopMovement();

        var drain = CwslGameConstants.GatherChargeStaminaDrainPerSecond * Time.deltaTime;
        if (drain > 0f &&
            CwslGameConstants.SkillsUseStamina &&
            playerStamina != null &&
            !playerStamina.TrySpendServer(drain))
        {
            FinishChargeServer();
            return;
        }

        var elapsed = Time.time - zoneStartTime;
        if (elapsed >= CwslGameConstants.GatherBlackHoleZoneDuration)
        {
            FinishChargeServer();
            return;
        }

        var center = syncedCenter.Value;
        var radius = CwslGameConstants.GatherBlackHoleZoneRadius;
        ApplySlowInZoneServer(center, radius);
        PullUnitsInZoneServer(center, radius);
    }

    public override void OnSkillReleasedServer(ulong senderClientId)
    {
        if (!IsServer || !isCharging.Value)
            return;

        FinishChargeServer();
    }

    private void FinishChargeServer()
    {
        if (!IsServer || !isCharging.Value)
            return;

        var center = syncedCenter.Value;
        var radius = CwslGameConstants.GatherBlackHoleZoneRadius;
        isCharging.Value = false;
        skillCooldowns?.BeginCooldown(0);
        ClearZoneSlowServer(center, radius);
        PlayReleasePullClientRpc(center, radius);

        if (pullRoutine != null)
            StopCoroutine(pullRoutine);
        pullRoutine = StartCoroutine(PullTargetsOnReleaseServer(center, radius));
    }

    private IEnumerator PullTargetsOnReleaseServer(Vector3 center, float radius)
    {
        var entries = CollectPullEntries(center, radius);
        if (entries.Count == 0)
        {
            pullRoutine = null;
            yield break;
        }

        var duration = CwslGameConstants.GatherPullSeconds;
        var elapsed = 0f;
        var targetPositions = new Vector3[entries.Count];
        for (var i = 0; i < entries.Count; i++)
            targetPositions[i] = ResolvePullDestination(center, entries[i].Transform);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Transform == null)
                    continue;

                var next = Vector3.Lerp(entry.StartPosition, targetPositions[i], t);
                ApplyPullPosition(entry, next);
            }

            yield return null;
        }

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry.Transform == null)
                continue;

            ApplyPullPosition(entry, targetPositions[i]);
        }

        pullRoutine = null;
    }

    private void PullUnitsInZoneServer(Vector3 center, float radius)
    {
        CwslGathererSkillUtil.CollectInCircle(center, radius, zoneTargets);
        foreach (var target in zoneTargets)
        {
            CwslGathererSkillUtil.PullTowardCenter(
                target,
                center,
                radius,
                CwslGameConstants.GatherBlackHolePullSpeed);
        }
    }

    private void ApplySlowInZoneServer(Vector3 center, float radius)
    {
        var attackerId = NetworkObject != null ? NetworkObject.OwnerClientId : 0ul;
        CwslGathererSkillUtil.CollectInCircle(center, radius, zoneTargets);
        foreach (var target in zoneTargets)
        {
            var monsterHealth = target.GetComponent<CwslMonsterHealth>();
            if (monsterHealth != null)
            {
                CwslMonsterStatusController.Ensure(monsterHealth)?.ApplyFrostServer(
                    attackerId,
                    CwslGameConstants.GatherSlowRefreshSeconds,
                    1);
                continue;
            }

            CwslSlowModifier.Ensure(target)?.ApplySlow(
                CwslGameConstants.GatherSlowMultiplier,
                CwslGameConstants.GatherSlowRefreshSeconds);
        }
    }

    private void ClearZoneSlowServer(Vector3 center, float radius)
    {
        CwslGathererSkillUtil.CollectInCircle(center, radius, zoneTargets);
        foreach (var target in zoneTargets)
        {
            target.GetComponent<CwslMonsterHealth>()
                ?.GetComponent<CwslMonsterStatusController>()
                ?.ClearFrostServer();
            target.GetComponent<CwslSlowModifier>()?.ClearSlow();
        }
    }

    private struct PullEntry
    {
        public Transform Transform;
        public NavMeshAgent Agent;
        public Vector3 StartPosition;
        public bool IsProjectile;
    }

    private static List<PullEntry> CollectPullEntries(Vector3 center, float radius)
    {
        var results = new List<PullEntry>();
        var scratch = new List<Transform>();
        CwslGathererSkillUtil.CollectInCircle(center, radius, scratch);
        foreach (var target in scratch)
        {
            if (target == null)
                continue;

            var isProjectile = target.GetComponent<CwslMonsterProjectile>() != null
                               || target.GetComponent<CwslPlayerProjectile>() != null;
            results.Add(new PullEntry
            {
                Transform = target,
                Agent = isProjectile ? null : target.GetComponent<NavMeshAgent>(),
                StartPosition = target.position,
                IsProjectile = isProjectile
            });
        }

        return results;
    }

    private static Vector3 ResolvePullDestination(Vector3 center, Transform target)
    {
        if (target.GetComponent<CwslMonsterProjectile>() != null
            || target.GetComponent<CwslPlayerProjectile>() != null)
        {
            var destination = center + Vector3.up * 0.85f;
            destination.y = target.position.y;
            return destination;
        }

        var groundDestination = center;
        groundDestination.y = target.position.y;
        if (NavMesh.SamplePosition(center, out var hit, 2.5f, NavMesh.AllAreas))
            groundDestination = hit.position;
        return groundDestination;
    }

    private static void ApplyPullPosition(PullEntry entry, Vector3 position)
    {
        if (entry.Transform == null)
            return;

        if (entry.IsProjectile)
        {
            entry.Transform.position = position;
            return;
        }

        if (entry.Agent != null && entry.Agent.enabled && entry.Agent.isOnNavMesh)
            entry.Agent.Warp(position);
        else
            entry.Transform.position = position;
    }

    [ClientRpc]
    private void PlayGatherCastClientRpc(Vector3 center) =>
        CwslGatherAudioFeedback.PlayGatherCast(center);

    [ClientRpc]
    private void BeginBlackHoleVisualClientRpc(Vector3 center, float radius)
    {
        CwslGatherChargeVisual.BeginBlackHoleZone(center, radius);
        CwslGatherSlowVisual.Sync(center, radius);
        CwslGatherAudioFeedback.StartChargeLoop(center);
    }

    [ClientRpc]
    private void SyncBlackHoleVisualClientRpc(Vector3 center, float radius)
    {
        CwslGatherChargeVisual.SyncBlackHoleZone(center, radius);
        CwslGatherSlowVisual.Sync(center, radius);
        CwslGatherAudioFeedback.UpdateChargeLoopPosition(center);
    }

    [ClientRpc]
    private void PlayReleasePullClientRpc(Vector3 center, float radius)
    {
        CwslGatherAudioFeedback.PlayChargeEnd(center);
        CwslGatherSlowVisual.Clear();
        CwslGatherChargeVisual.PlayReleasePull(center, radius);
    }

    [ClientRpc]
    private void EndChargeVisualClientRpc()
    {
        CwslGatherAudioFeedback.PlayChargeEnd(syncedCenter.Value);
        CwslGatherChargeVisual.Hide();
        CwslGatherSlowVisual.Clear();
    }
}
