using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 끌모: Q 홀드로 지면 원 확장 → 뗄 때 범위 안 적·아군을 중심으로 당김 (데미지 없음).
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
    private CwslPlayerMovement movement;
    private float chargeStartTime;
    private float nextGoldSpendTime;
    private float nextGatherTime;
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

        isCharging.Value = false;
        syncedAtMaxCharge.Value = false;
        EndChargeVisualClientRpc();
    }

    public override bool CanCastServer(ulong senderClientId)
    {
        return IsServer &&
               playerCharacter != null &&
               playerCharacter.CharacterId == CwslCharacterId.CrowdGatherer &&
               (playerHealth == null || playerHealth.IsAlive) &&
               !isCharging.Value &&
               Time.time >= nextGatherTime &&
               (playerGold == null || playerGold.Gold >= CwslGameConstants.SkillGoldCost);
    }

    public void BeginChargeServer(Vector3 worldPoint)
    {
        if (!IsServer || !CanCastServer(OwnerClientId))
            return;

        if (playerGold == null || !playerGold.TrySpendGoldServer(CwslGameConstants.SkillGoldCost))
            return;

        worldPoint.y = 0f;
        movement?.StopMovement();
        isCharging.Value = true;
        syncedAtMaxCharge.Value = false;
        maxReadyNotified = false;
        chargeStartTime = Time.time;
        nextGoldSpendTime = Time.time + CwslGameConstants.GatherGoldIntervalSeconds;
        syncedCenter.Value = worldPoint;
        syncedRadius.Value = CwslGameConstants.GatherMinRadius;
        SyncChargeVisualClientRpc(worldPoint, syncedRadius.Value, false);
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

        if (Time.time >= nextGoldSpendTime)
        {
            nextGoldSpendTime = Time.time + CwslGameConstants.GatherGoldIntervalSeconds;
            if (playerGold == null || !playerGold.TrySpendGoldServer(CwslGameConstants.SkillGoldCost))
            {
                CancelChargeServer();
                return;
            }
        }

        var elapsed = Time.time - chargeStartTime;
        var chargeRatio = Mathf.Clamp01(elapsed / CwslGameConstants.GatherChargeSeconds);
        var radius = Mathf.Lerp(
            CwslGameConstants.GatherMinRadius,
            CwslGameConstants.GatherMaxRadius,
            chargeRatio);
        syncedRadius.Value = radius;

        var atMax = chargeRatio >= 1f;
        syncedAtMaxCharge.Value = atMax;
        if (atMax && !maxReadyNotified)
        {
            maxReadyNotified = true;
            PlayMaxChargeReadyClientRpc(syncedCenter.Value);
        }

        SyncChargeVisualClientRpc(syncedCenter.Value, radius, atMax);
    }

    public override void OnSkillReleasedServer(ulong senderClientId)
    {
        if (!IsServer || !isCharging.Value)
            return;

        isCharging.Value = false;
        syncedAtMaxCharge.Value = false;
        EndChargeVisualClientRpc();

        var radius = syncedRadius.Value;
        var center = syncedCenter.Value;
        if (radius < CwslGameConstants.GatherMinReleaseRadius)
            return;

        nextGatherTime = Time.time + CwslGameConstants.GatherCooldown;
        PlayPullFxClientRpc(center, radius);
        if (pullRoutine != null)
            StopCoroutine(pullRoutine);
        pullRoutine = StartCoroutine(PullTargetsServer(center, radius));
    }

    private IEnumerator PullTargetsServer(Vector3 center, float radius)
    {
        var entries = CollectPullEntries(center, radius);
        if (entries.Count == 0)
            yield break;

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
        public CwslPlayerMovement Movement;
        public CwslMomentumRammerSkill Rammer;
        public Vector3 StartPosition;
    }

    private List<PullEntry> CollectPullEntries(Vector3 center, float radius)
    {
        var results = new List<PullEntry>();
        var radiusSqr = radius * radius;

        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (!IsInsideFlatRadius(center, monster.transform.position, radiusSqr))
                continue;

            results.Add(new PullEntry
            {
                Transform = monster.transform,
                Agent = monster.GetComponent<NavMeshAgent>(),
                StartPosition = monster.transform.position
            });
        }

        if (NetworkManager.Singleton != null)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerObject = client.PlayerObject;
                if (playerObject == null)
                    continue;

                var health = playerObject.GetComponent<CwslPlayerHealth>();
                if (health != null && !health.IsAlive)
                    continue;

                if (!IsInsideFlatRadius(center, playerObject.transform.position, radiusSqr))
                    continue;

                results.Add(new PullEntry
                {
                    Transform = playerObject.transform,
                    Agent = playerObject.GetComponent<NavMeshAgent>(),
                    Movement = playerObject.GetComponent<CwslPlayerMovement>(),
                    Rammer = playerObject.GetComponent<CwslMomentumRammerSkill>(),
                    StartPosition = playerObject.transform.position
                });
            }
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
        var destination = center;
        destination.y = target.position.y;
        if (NavMesh.SamplePosition(center, out var hit, 2.5f, NavMesh.AllAreas))
            destination = hit.position;
        return destination;
    }

    private static void ApplyPullPosition(PullEntry entry, Vector3 position)
    {
        entry.Rammer?.StopMomentumForStunServer();
        entry.Movement?.StopMovement();

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
    private void SyncChargeVisualClientRpc(Vector3 center, float radius, bool atMax)
    {
        CwslGatherChargeVisual.Sync(center, radius, atMax);
    }

    [ClientRpc]
    private void EndChargeVisualClientRpc()
    {
        CwslGatherChargeVisual.Hide();
    }

    [ClientRpc]
    private void PlayMaxChargeReadyClientRpc(Vector3 center)
    {
        CwslVfxSpawner.SpawnGatherMaxReady(center);
    }

    [ClientRpc]
    private void PlayPullFxClientRpc(Vector3 center, float radius)
    {
        CwslGatherChargeVisual.Hide();
        CwslVfxSpawner.SpawnGatherPull(center, radius);
    }
}
