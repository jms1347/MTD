using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ???: Q ????? ???????? ????? ????????? ?????????? ??) ??Q ??? ???? ????????????.
/// </summary>
public class CwslCrowdGatherSkill : CwslPlayerSkillBase
{
    private readonly NetworkVariable<bool> isCharging = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> syncedRadius = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<Vector3> syncedCenter = new(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> syncedAtMaxCharge = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerMovement movement;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private CwslPlayerStamina playerStamina;
    private CwslPlayerSkills playerSkills;
    private float chargeStartTime;
    private bool maxReadyNotified;
    private Coroutine pullRoutine;

    public bool IsCharging => isCharging.Value;
    public float ChargeRadius => syncedRadius.Value;
    public Vector3 ChargeCenter => syncedCenter.Value;
    public bool IsAtMaxCharge => syncedAtMaxCharge.Value;

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

    private void HandleDied()
    {
        CancelChargeServer();
    }

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
        var radius = syncedRadius.Value;
        isCharging.Value = false;
        syncedAtMaxCharge.Value = false;
        ClearZoneSlowServer(center, radius);
        EndChargeVisualClientRpc(center);
    }

    public override bool CanCastServer(ulong senderClientId)
    {
        return IsServer &&
               playerCharacter != null &&
               playerCharacter.CharacterId == CwslCharacterId.CrowdGatherer &&
               (playerHealth == null || playerHealth.IsAlive) &&
               !isCharging.Value &&
               (skillCooldowns == null || skillCooldowns.IsReady(0));
    }

    public bool BeginChargeServer(Vector3 worldPoint)
    {
        if (!IsServer)
            return false;

        if (!CanCastServer(OwnerClientId))
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
        syncedAtMaxCharge.Value = false;
        maxReadyNotified = false;
        chargeStartTime = Time.time;
        syncedCenter.Value = worldPoint;
        syncedRadius.Value = CwslGameConstants.GatherMinRadius;
        SyncChargeVisualClientRpc(worldPoint, syncedRadius.Value, false);
        PlayGatherCastClientRpc(worldPoint);
        return true;
    }

    public void UpdateChargeCenterServer(Vector3 worldPoint)
    {
        if (!IsServer || !isCharging.Value)
            return;

        worldPoint.y = 0f;
        syncedCenter.Value = worldPoint;
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
            CancelChargeServer();
            return;
        }

        var elapsed = Time.time - chargeStartTime;
        var chargeRatio = Mathf.Clamp01(elapsed / CwslGameConstants.GatherChargeSeconds);
        var radius = Mathf.Lerp(
            CwslGameConstants.GatherMinRadius,
            CwslGameConstants.GatherMaxRadius,
            chargeRatio);
        syncedRadius.Value = radius;

        var center = syncedCenter.Value;
        ApplySlowInZoneServer(center, radius);

        var atMax = chargeRatio >= 1f;
        syncedAtMaxCharge.Value = atMax;
        if (atMax && !maxReadyNotified)
            maxReadyNotified = true;

        SyncChargeVisualClientRpc(center, radius, atMax);
    }

    public override void OnSkillReleasedServer(ulong senderClientId)
    {
        if (!IsServer || !isCharging.Value)
            return;

        FinishChargeAndPullServer(syncedCenter.Value, syncedRadius.Value);
    }

    private void ApplySlowInZoneServer(Vector3 center, float radius)
    {
        var attackerId = NetworkObject != null ? NetworkObject.OwnerClientId : 0ul;
        foreach (var target in CollectZoneTargets(center, radius))
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

            var modifier = CwslSlowModifier.Ensure(target);
            modifier?.ApplySlow(
                CwslGameConstants.GatherSlowMultiplier,
                CwslGameConstants.GatherSlowRefreshSeconds);
        }
    }

    private void ClearZoneSlowServer(Vector3 center, float radius)
    {
        foreach (var target in CollectZoneTargets(center, radius))
        {
            target.GetComponent<CwslMonsterHealth>()
                ?.GetComponent<CwslMonsterStatusController>()
                ?.ClearFrostServer();
            target.GetComponent<CwslSlowModifier>()?.ClearSlow();
        }
    }

    private void FinishChargeAndPullServer(Vector3 center, float radius)
    {
        if (!IsServer || !isCharging.Value)
            return;

        isCharging.Value = false;
        syncedAtMaxCharge.Value = false;
        skillCooldowns?.BeginCooldown(0);
        ClearZoneSlowServer(center, radius);
        EndChargeVisualClientRpc(center);
        PlayPullFxClientRpc(center, radius);

        if (pullRoutine != null)
            StopCoroutine(pullRoutine);
        pullRoutine = StartCoroutine(PullTargetsServer(center, radius));
    }

    private IEnumerator PullTargetsServer(Vector3 center, float radius)
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

    private struct PullEntry
    {
        public Transform Transform;
        public NavMeshAgent Agent;
        public Vector3 StartPosition;
        public bool IsProjectile;
    }

    private List<Transform> CollectZoneTargets(Vector3 center, float radius)
    {
        var results = new List<Transform>();
        var radiusSqr = radius * radius;

        var monsters = CwslCombatRegistry.AliveMonsters;
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (!IsInsideFlatRadius(center, monster.transform.position, radiusSqr))
                continue;

            results.Add(monster.transform);
        }

        var monsterProjectiles = CwslCombatRegistry.ActiveMonsterProjectiles;
        foreach (var projectile in monsterProjectiles)
        {
            if (projectile == null || !projectile.IsActiveProjectile)
                continue;

            if (!IsInsideFlatRadius(center, projectile.transform.position, radiusSqr))
                continue;

            results.Add(projectile.transform);
        }

        var playerProjectiles = CwslCombatRegistry.ActivePlayerProjectiles;
        foreach (var projectile in playerProjectiles)
        {
            if (projectile == null || !projectile.IsActiveProjectile)
                continue;

            if (!IsInsideFlatRadius(center, projectile.transform.position, radiusSqr))
                continue;

            results.Add(projectile.transform);
        }

        return results;
    }

    private List<PullEntry> CollectPullEntries(Vector3 center, float radius)
    {
        var results = new List<PullEntry>();
        foreach (var target in CollectZoneTargets(center, radius))
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

    private static bool IsInsideFlatRadius(Vector3 center, Vector3 target, float radiusSqr)
    {
        var flat = target - center;
        flat.y = 0f;
        return flat.sqrMagnitude <= radiusSqr;
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

        if (entry.Agent != null && entry.Agent.enabled)
        {
            if (entry.Agent.isOnNavMesh)
                entry.Agent.Warp(position);
            else
                entry.Transform.position = position;
        }
        else
        {
            entry.Transform.position = position;
        }
    }

    [ClientRpc]
    private void PlayGatherCastClientRpc(Vector3 center)
    {
        CwslGatherAudioFeedback.PlayGatherCast(center);
    }

    [ClientRpc]
    private void SyncChargeVisualClientRpc(Vector3 center, float radius, bool atMax)
    {
        CwslGatherChargeVisual.BeginLocalCharge(center);
        CwslGatherChargeVisual.Sync(center, radius, atMax);
        CwslGatherSlowVisual.Sync(center, radius);
        CwslGatherAudioFeedback.StartChargeLoop(center);
        CwslGatherAudioFeedback.UpdateChargeLoopPosition(center);
    }

    [ClientRpc]
    private void EndChargeVisualClientRpc(Vector3 center)
    {
        CwslGatherAudioFeedback.PlayChargeEnd(center);
        CwslGatherChargeVisual.Hide();
        CwslGatherSlowVisual.Clear();
    }

    [ClientRpc]
    private void PlayPullFxClientRpc(Vector3 center, float radius)
    {
        CwslGatherChargeVisual.PlayPull(center, radius);
    }

}
