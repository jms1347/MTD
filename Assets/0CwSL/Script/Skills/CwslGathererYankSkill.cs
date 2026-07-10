using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>링거 W — 순간 블랙홀 영역, 밧줄 연결+슬로우 흡인 3초 후 중심 수렴. 미사일/자폭 유발 시 광역 폭발.</summary>
public class CwslGathererYankSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotW;

    private readonly NetworkVariable<bool> isActive = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<Vector3> syncedAreaCenter = new(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkList<ulong> linkedObjectIds = new();

    private readonly Dictionary<ulong, LineRenderer> ropeLines = new();
    private readonly List<Transform> scratchTargets = new();

    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private Coroutine sequenceRoutine;

    public bool HasActiveSequence => isActive.Value;
    public Vector3 AreaCenter => syncedAreaCenter.Value;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.CrowdGatherer;

    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
        isActive.OnValueChanged += HandleActiveChanged;
        linkedObjectIds.OnListChanged += HandleLinkedListChanged;
        if (playerHealth != null)
            playerHealth.OnDied += HandleDied;
    }

    public override void OnNetworkDespawn()
    {
        isActive.OnValueChanged -= HandleActiveChanged;
        linkedObjectIds.OnListChanged -= HandleLinkedListChanged;
        if (playerHealth != null)
            playerHealth.OnDied -= HandleDied;
        ClearRopeVisual();
    }

    private void Update()
    {
        if (isActive.Value)
            RefreshRopeVisual();
    }

    public override bool CanUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint) =>
        slotIndex == BoundSlotIndex && CanCastServer(senderClientId);

    public override bool TryUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint)
    {
        if (!IsServer || slotIndex != BoundSlotIndex)
            return false;

        return TryCastServer(senderClientId, worldPoint);
    }

    public bool TryCastServer(ulong senderClientId, Vector3 worldPoint)
    {
        if (!IsServer || senderClientId != OwnerClientId || !CanCastServer(senderClientId))
            return false;

        worldPoint.y = 0f;
        worldPoint = CwslArenaUtility.ClampToPlayArea(worldPoint, 0.5f);
        syncedAreaCenter.Value = worldPoint;
        LinkRopesInAreaServer();
        isActive.Value = true;

        if (sequenceRoutine != null)
            StopCoroutine(sequenceRoutine);
        sequenceRoutine = StartCoroutine(YankSequenceRoutine());
        return true;
    }

    public bool CanCastServer(ulong senderClientId)
    {
        if (!IsServer || senderClientId != OwnerClientId)
            return false;

        if (isActive.Value || sequenceRoutine != null)
            return false;

        if (playerHealth != null && !playerHealth.IsAlive)
            return false;

        if (playerStun != null && playerStun.IsStunned)
            return false;

        return skillCooldowns == null || skillCooldowns.IsReady(BoundSlotIndex);
    }

    private IEnumerator YankSequenceRoutine()
    {
        var center = syncedAreaCenter.Value;
        var radius = CwslGameConstants.GathererRopeAreaRadius;
        var pullDuration = CwslGameConstants.GathererYankDuration;
        var convergeDuration = CwslGameConstants.GathererRopeConvergeSeconds;

        BeginYankVisualClientRpc(center, radius, pullDuration, convergeDuration);
        PlayYankCastClientRpc(center);

        var elapsed = 0f;
        while (elapsed < pullDuration)
        {
            elapsed += Time.deltaTime;
            ApplySlowOnLinkedServer(center, radius);
            PullLinkedTowardCenterServer(center, radius);
            yield return null;
        }

        yield return ConvergeLinkedToCenterServer(center, convergeDuration);

        var shouldExplode = HasExplosiveTriggerInLinked();
        if (shouldExplode)
        {
            ApplyExplosionDamageServer(center, radius);
            PlayYankExplosionClientRpc(center, radius);
        }

        ClearSlowOnLinkedServer(center, radius);
        FinishSequenceServer();
        sequenceRoutine = null;
    }

    private void LinkRopesInAreaServer()
    {
        linkedObjectIds.Clear();
        var center = syncedAreaCenter.Value;
        var radius = CwslGameConstants.GathererRopeAreaRadius;
        CwslGathererSkillUtil.CollectInCircle(center, radius, scratchTargets, swappableOnly: true);

        foreach (var target in scratchTargets)
        {
            if (target == null)
                continue;

            var networkObject = target.GetComponent<NetworkObject>()
                                ?? target.GetComponentInParent<NetworkObject>();
            if (networkObject == null || !networkObject.IsSpawned)
                continue;

            linkedObjectIds.Add(networkObject.NetworkObjectId);
        }
    }

    private void ApplySlowOnLinkedServer(Vector3 center, float radius)
    {
        var attackerId = OwnerClientId;
        CollectLinkedTransforms(scratchTargets);
        foreach (var target in scratchTargets)
        {
            if (target == null)
                continue;

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

    private void PullLinkedTowardCenterServer(Vector3 center, float radius)
    {
        CollectLinkedTransforms(scratchTargets);
        foreach (var target in scratchTargets)
        {
            CwslGathererSkillUtil.PullTowardCenter(
                target,
                center,
                radius,
                CwslGameConstants.GatherBlackHolePullSpeed);
        }
    }

    private IEnumerator ConvergeLinkedToCenterServer(Vector3 center, float duration)
    {
        var entries = new List<(ulong id, Transform transform, Vector3 start)>();
        for (var i = 0; i < linkedObjectIds.Count; i++)
        {
            var id = linkedObjectIds[i];
            if (!TryGetLinkedTransform(id, out var target) || target == null)
                continue;

            entries.Add((id, target, target.position));
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            foreach (var entry in entries)
            {
                if (entry.transform == null)
                    continue;

                var next = Vector3.Lerp(entry.start, center, t);
                next.y = entry.transform.position.y;
                CwslGathererSkillUtil.WarpTransform(entry.transform, next);
            }

            yield return null;
        }

        foreach (var entry in entries)
        {
            if (entry.transform == null)
                continue;

            var destination = center;
            destination.y = entry.transform.position.y;
            CwslGathererSkillUtil.WarpTransform(entry.transform, destination);
        }
    }

    private void ClearSlowOnLinkedServer(Vector3 center, float radius)
    {
        CollectLinkedTransforms(scratchTargets);
        foreach (var target in scratchTargets)
        {
            target.GetComponent<CwslMonsterHealth>()
                ?.GetComponent<CwslMonsterStatusController>()
                ?.ClearFrostServer();
            target.GetComponent<CwslSlowModifier>()?.ClearSlow();
        }
    }

    private void CollectLinkedTransforms(List<Transform> results)
    {
        results.Clear();
        for (var i = 0; i < linkedObjectIds.Count; i++)
        {
            if (TryGetLinkedTransform(linkedObjectIds[i], out var target) && target != null)
                results.Add(target);
        }
    }

    private bool HasExplosiveTriggerInLinked()
    {
        CollectLinkedTransforms(scratchTargets);
        foreach (var target in scratchTargets)
        {
            if (target == null)
                continue;

            if (target.GetComponent<CwslMonsterProjectile>() != null
                || target.GetComponent<CwslPlayerProjectile>() != null
                || target.GetComponent<CwslFrozenOrbProjectile>() != null)
                return true;

            var monster = target.GetComponent<CwslMonsterHealth>();
            if (CwslGathererSkillUtil.IsMissileOrBombMonster(monster))
                return true;

            var networkObject = target.GetComponent<NetworkObject>();
            if (CwslGathererSkillUtil.IsMissileOrBombPlayer(networkObject))
                return true;
        }

        return false;
    }

    private void ApplyExplosionDamageServer(Vector3 center, float radius)
    {
        var attackerId = OwnerClientId;
        var damage = CwslCombatMath.ResolveSkillDamage(
            CwslCharacterId.CrowdGatherer,
            CwslGameConstants.GathererRopeExplosionSkillCoeff);

        CwslGathererSkillUtil.CollectInCircle(center, radius, scratchTargets, swappableOnly: false);
        foreach (var target in scratchTargets)
        {
            if (target == null)
                continue;

            var monster = target.GetComponent<CwslMonsterHealth>();
            if (monster != null && monster.IsAlive)
            {
                monster.DamageFromPlayer(attackerId, damage);
                continue;
            }

            var player = target.GetComponent<CwslPlayerHealth>();
            if (player != null && player.IsAlive)
                player.TryReceiveExplosionHitServer(damage, target.position);
        }
    }

    private void FinishSequenceServer()
    {
        linkedObjectIds.Clear();
        isActive.Value = false;
        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        EndYankVisualClientRpc();
        ClearRopeVisualClientRpc();
    }

    private bool TryGetLinkedTransform(ulong networkObjectId, out Transform target)
    {
        target = null;
        if (NetworkManager.Singleton == null)
            return false;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject)
            || networkObject == null)
            return false;

        target = networkObject.transform;
        return target != null;
    }

    private void HandleDied()
    {
        if (!IsServer)
            return;

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        if (!isActive.Value)
            return;

        linkedObjectIds.Clear();
        isActive.Value = false;
        EndYankVisualClientRpc();
        ClearRopeVisualClientRpc();
    }

    private void HandleActiveChanged(bool previous, bool current) => RefreshRopeVisual();

    private void HandleLinkedListChanged(NetworkListEvent<ulong> changeEvent) => RefreshRopeVisual();

    private void RefreshRopeVisual()
    {
        if (!isActive.Value || linkedObjectIds.Count < 1)
        {
            DisableAllRopeLines();
            return;
        }

        var center = syncedAreaCenter.Value + Vector3.up * 0.65f;
        var activeKeys = new HashSet<ulong>();

        for (var i = 0; i < linkedObjectIds.Count; i++)
        {
            var targetId = linkedObjectIds[i];
            if (!TryGetLinkedTransform(targetId, out var target) || target == null)
                continue;

            activeKeys.Add(targetId);
            var line = GetOrCreateRopeLine(targetId);
            line.enabled = true;
            line.positionCount = 2;
            line.SetPosition(0, center);
            line.SetPosition(1, target.position + Vector3.up * 0.65f);
        }

        foreach (var pair in ropeLines)
        {
            if (pair.Value != null && !activeKeys.Contains(pair.Key))
                pair.Value.enabled = false;
        }
    }

    private LineRenderer GetOrCreateRopeLine(ulong key)
    {
        if (ropeLines.TryGetValue(key, out var existing) && existing != null)
            return existing;

        var go = new GameObject($"GathererRope_{key}");
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = line.endColor = new Color(0.82f, 0.72f, 0.35f, 0.95f);
        line.startWidth = line.endWidth = 0.1f;
        line.positionCount = 2;
        line.useWorldSpace = true;
        ropeLines[key] = line;
        return line;
    }

    private void DisableAllRopeLines()
    {
        foreach (var pair in ropeLines)
        {
            if (pair.Value != null)
                pair.Value.enabled = false;
        }
    }

    private void ClearRopeVisual()
    {
        foreach (var pair in ropeLines)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }

        ropeLines.Clear();
    }

    [ClientRpc]
    private void PlayYankCastClientRpc(Vector3 center) =>
        CwslGatherAudioFeedback.PlayGatherCast(center);

    [ClientRpc]
    private void BeginYankVisualClientRpc(Vector3 center, float radius, float pullDuration, float convergeSeconds) =>
        CwslGathererYankVisual.Begin(center, radius, pullDuration, convergeSeconds);

    [ClientRpc]
    private void PlayYankExplosionClientRpc(Vector3 center, float radius) =>
        CwslGathererYankVisual.PlayExplosion(center, radius);

    [ClientRpc]
    private void EndYankVisualClientRpc() =>
        CwslGathererYankVisual.Hide();

    [ClientRpc]
    private void ClearRopeVisualClientRpc() =>
        ClearRopeVisual();
}
