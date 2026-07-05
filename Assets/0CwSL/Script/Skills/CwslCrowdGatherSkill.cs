using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 끌모: Q 홀드로 지면 원 확장 → 차지 중 적·투사체 슬로우(대상당 골드) → Q 해제 시 범위 안 적·총알 당김.
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
    private CwslPlayerGold playerGold;
    private CwslPlayerPillBuff pillBuff;
    private CwslPlayerMovement movement;
    private float chargeStartTime;
    private float nextGatherTime;
    private float nextSlowGoldTick;
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
        playerGold = GetComponent<CwslPlayerGold>();
        pillBuff = GetComponent<CwslPlayerPillBuff>();
        movement = GetComponent<CwslPlayerMovement>();
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
               Time.time >= nextGatherTime &&
               CanAffordSkillGold(CwslGameConstants.GatherStartGoldCost);
    }

    public bool BeginChargeServer(Vector3 worldPoint)
    {
        if (!IsServer)
            return false;

        if (!CanCastServer(OwnerClientId))
        {
            if (IsStartGoldInsufficient())
                RejectGatherStartClientRpc();
            return false;
        }

        if (!TrySpendSkillGold(CwslGameConstants.GatherStartGoldCost, playSpendEffect: false))
        {
            RejectGatherStartClientRpc();
            return false;
        }

        worldPoint.y = 0f;
        movement?.StopMovement();
        isCharging.Value = true;
        syncedAtMaxCharge.Value = false;
        maxReadyNotified = false;
        chargeStartTime = Time.time;
        nextSlowGoldTick = Time.time;
        syncedCenter.Value = worldPoint;
        syncedRadius.Value = CwslGameConstants.GatherMinRadius;
        SyncChargeVisualClientRpc(worldPoint, syncedRadius.Value, false);
        PlayGatherCenterSpendClientRpc(worldPoint);
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

        var elapsed = Time.time - chargeStartTime;
        var chargeRatio = Mathf.Clamp01(elapsed / CwslGameConstants.GatherChargeSeconds);
        var radius = Mathf.Lerp(
            CwslGameConstants.GatherMinRadius,
            CwslGameConstants.GatherMaxRadius,
            chargeRatio);
        syncedRadius.Value = radius;

        var center = syncedCenter.Value;
        ApplySlowInZoneServer(center, radius);

        if (!TickSlowGoldServer(center, radius))
            return;

        var atMax = chargeRatio >= 1f;
        syncedAtMaxCharge.Value = atMax;
        if (atMax && !maxReadyNotified)
        {
            maxReadyNotified = true;
            PlayMaxChargeReadyClientRpc(center);
        }

        SyncChargeVisualClientRpc(center, radius, atMax);
    }

    public override void OnSkillReleasedServer(ulong senderClientId)
    {
        if (!IsServer || !isCharging.Value)
            return;

        FinishChargeAndPullServer(syncedCenter.Value, syncedRadius.Value);
    }

    private bool IsStartGoldInsufficient()
    {
        return !CanAffordSkillGold(CwslGameConstants.GatherStartGoldCost);
    }

    private bool TickSlowGoldServer(Vector3 center, float radius)
    {
        if (Time.time < nextSlowGoldTick)
            return true;

        nextSlowGoldTick = Time.time + CwslGameConstants.GatherSlowGoldIntervalSeconds;

        var targetCount = CollectZoneTargets(center, radius).Count;
        if (targetCount <= 0)
            return true;

        var cost = targetCount * CwslGameConstants.GatherSlowGoldPerTarget;
        if (!CanAffordSkillGold(cost))
        {
            FinishChargeAndPullServer(center, radius);
            return false;
        }

        TrySpendSkillGold(cost, playSpendEffect: false);
        return true;
    }

    private void ApplySlowInZoneServer(Vector3 center, float radius)
    {
        foreach (var target in CollectZoneTargets(center, radius))
        {
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
            target.GetComponent<CwslSlowModifier>()?.ClearSlow();
        }
    }

    private void FinishChargeAndPullServer(Vector3 center, float radius)
    {
        if (!IsServer || !isCharging.Value)
            return;

        isCharging.Value = false;
        syncedAtMaxCharge.Value = false;
        nextGatherTime = Time.time + CwslGameConstants.GatherCooldown;
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

        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (!IsInsideFlatRadius(center, monster.transform.position, radiusSqr))
                continue;

            results.Add(monster.transform);
        }

        var monsterProjectiles = FindObjectsByType<CwslMonsterProjectile>(FindObjectsSortMode.None);
        foreach (var projectile in monsterProjectiles)
        {
            if (projectile == null || !projectile.IsActiveProjectile)
                continue;

            if (!IsInsideFlatRadius(center, projectile.transform.position, radiusSqr))
                continue;

            results.Add(projectile.transform);
        }

        var playerProjectiles = FindObjectsByType<CwslPlayerProjectile>(FindObjectsSortMode.None);
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
    private void RejectGatherStartClientRpc()
    {
        if (!IsOwner)
            return;

        CwslGatherAudioFeedback.StopChargeLoop();
        CwslGatherChargeVisual.Hide();
        CwslSkillGoldFeedback.ShowInsufficientGold("골드가 부족해 당김 스킬을 시전할 수 없습니다.");
    }

    [ClientRpc]
    private void PlayGatherCastClientRpc(Vector3 center)
    {
        CwslGatherAudioFeedback.PlayGatherCast(center);
    }

    [ClientRpc]
    private void PlayGatherCenterSpendClientRpc(Vector3 center)
    {
        CwslGatherChargeVisual.BeginLocalCharge(center);
        CwslGatherChargeVisual.PlayCenterSpend(center);
        CwslGatherAudioFeedback.StartChargeLoop(center);
    }

    [ClientRpc]
    private void SyncChargeVisualClientRpc(Vector3 center, float radius, bool atMax)
    {
        CwslGatherChargeVisual.Sync(center, radius, atMax);
        CwslGatherSlowVisual.Sync(center, radius);
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
    private void PlayMaxChargeReadyClientRpc(Vector3 center)
    {
        CwslVfxSpawner.SpawnGatherMaxReady(center);
    }

    [ClientRpc]
    private void PlayPullFxClientRpc(Vector3 center, float radius)
    {
        CwslGatherChargeVisual.PlayPull(center, radius);
    }

    private bool CanAffordSkillGold(int amount)
    {
        if (!CwslGameConstants.SkillsConsumeGold)
            return true;

        if (pillBuff != null && pillBuff.CanAffordSkillGold(playerGold, amount))
            return true;

        return playerGold != null && playerGold.Gold >= amount;
    }

    private bool TrySpendSkillGold(int amount, bool playSpendEffect = true)
    {
        if (!CwslGameConstants.SkillsConsumeGold)
            return true;

        if (pillBuff != null && pillBuff.TrySpendSkillGold(playerGold, amount, playSpendEffect))
            return true;

        return playerGold != null && playerGold.TrySpendGoldServer(amount, playSpendEffect);
    }
}
