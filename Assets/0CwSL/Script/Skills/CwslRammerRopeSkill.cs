using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>질주자 E — 작은 원 범위의 적 전원을 밧줄 연결.</summary>
public class CwslRammerRopeSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotE;
    private const int MaxLinkedTargets = 12;

    private readonly NetworkVariable<bool> hasLink = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private readonly NetworkList<ulong> linkedObjectIds = new();

    private readonly Dictionary<ulong, LineRenderer> ropeLines = new();
    private readonly Dictionary<ulong, float> nextRopeDamageByTarget = new();
    private readonly Dictionary<ulong, float> nextSpinStunByTarget = new();

    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private Coroutine spinRoutine;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;
    public override bool IsActiveForCharacter(CwslCharacterId characterId) => characterId == CwslCharacterId.MomentumRammer;
    public override int SkillSlotIndex => BoundSlotIndex;
    public bool HasActiveLink => hasLink.Value && linkedObjectIds.Count > 0;

    public override void OnNetworkSpawn()
    {
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
        hasLink.OnValueChanged += HandleLinkChanged;
        linkedObjectIds.OnListChanged += HandleLinkedListChanged;
        RefreshRopeVisual();
    }

    public override void OnNetworkDespawn()
    {
        hasLink.OnValueChanged -= HandleLinkChanged;
        linkedObjectIds.OnListChanged -= HandleLinkedListChanged;
        ClearRopeVisual();
    }

    private void Update()
    {
        if (!HasActiveLink)
        {
            DisableAllRopeLines();
            return;
        }

        RefreshRopeVisual();
        if (!IsServer || spinRoutine != null)
            return;

        for (var i = linkedObjectIds.Count - 1; i >= 0; i--)
        {
            var targetId = linkedObjectIds[i];
            if (!TryGetLinkedTransform(targetId, out var target) || !IsValidLinkedTarget(target))
            {
                RemoveLinkedTargetAtServer(i);
                continue;
            }

            PullLinkedTargetServer(targetId, target);
        }
    }

    public override bool CanUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint) =>
        slotIndex == BoundSlotIndex && CanCastServer(senderClientId);

    public override bool TryUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint)
    {
        if (!IsServer || slotIndex != BoundSlotIndex)
            return false;

        return TryCastServer(senderClientId, worldPoint);
    }

    public bool CanCastServer(ulong senderClientId)
    {
        if (!IsServer || senderClientId != OwnerClientId)
            return false;
        if (skillCooldowns != null && !skillCooldowns.IsReady(BoundSlotIndex))
            return false;
        if (playerHealth != null && !playerHealth.IsAlive)
            return false;
        if (playerStun != null && playerStun.IsStunned)
            return false;
        return !HasActiveLink;
    }

    public bool TryCastServer(ulong senderClientId, Vector3 worldPoint)
    {
        if (!CanCastServer(senderClientId))
            return false;

        var targets = CollectTargetsInCircle(worldPoint);
        if (targets.Count == 0)
            return false;

        ClearLinksServer();
        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            if (target == null)
                continue;

            linkedObjectIds.Add(target.NetworkObjectId);
            PlayAttachFxClientRpc(target.transform.position);
        }

        hasLink.Value = linkedObjectIds.Count > 0;
        if (hasLink.Value)
            PlayAttachAreaFxClientRpc(worldPoint, CwslGameConstants.RammerRopeLinkRadius);
        return hasLink.Value;
    }

    public bool TryTriggerSpinServer()
    {
        if (!IsServer || !HasActiveLink || spinRoutine != null)
            return false;

        spinRoutine = StartCoroutine(SpinLinkedTargetsRoutine());
        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        return true;
    }

    private List<NetworkObject> CollectTargetsInCircle(Vector3 worldPoint)
    {
        var results = new List<NetworkObject>(MaxLinkedTargets);
        worldPoint.y = 0f;
        var radiusSq = CwslGameConstants.RammerRopeLinkRadius * CwslGameConstants.RammerRopeLinkRadius;
        var maxRopeRangeSq = CwslGameConstants.RammerRopeMaxDistance * CwslGameConstants.RammerRopeMaxDistance;

        foreach (var monster in CwslCombatRegistry.AliveMonsters)
        {
            if (monster == null || !monster.IsAlive || monster.NetworkObject == null)
                continue;

            var toCenter = monster.transform.position - worldPoint;
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude > radiusSq)
                continue;

            var toSelf = monster.transform.position - transform.position;
            toSelf.y = 0f;
            if (toSelf.sqrMagnitude > maxRopeRangeSq)
                continue;

            results.Add(monster.NetworkObject);
            if (results.Count >= MaxLinkedTargets)
                break;
        }

        return results;
    }

    private void PullLinkedTargetServer(ulong targetId, Transform target)
    {
        var toSelf = transform.position - target.position;
        toSelf.y = 0f;
        if (toSelf.sqrMagnitude < 1.2f * 1.2f)
            return;

        var before = target.position;
        var step = toSelf.normalized * (CwslGameConstants.RammerRopePullStrength * Time.deltaTime);
        var next = target.position + step;
        next.y = target.position.y;
        WarpOrMove(target, next);

        var movedSpeed = Vector3.Distance(before, target.position) / Mathf.Max(Time.deltaTime, 0.0001f);
        ApplyRopeDamageBySpeedServer(targetId, target, movedSpeed);
    }

    private IEnumerator SpinLinkedTargetsRoutine()
    {
        var elapsed = 0f;
        var angleBase = 0f;

        while (elapsed < CwslGameConstants.RammerRopeSpinDuration && HasActiveLink)
        {
            elapsed += Time.deltaTime;
            angleBase += CwslGameConstants.RammerRopeSpinAngularSpeed * Time.deltaTime;
            var count = linkedObjectIds.Count;

            for (var i = count - 1; i >= 0; i--)
            {
                var targetId = linkedObjectIds[i];
                if (!TryGetLinkedTransform(targetId, out var target))
                {
                    RemoveLinkedTargetAtServer(i);
                    continue;
                }

                if (!IsValidLinkedTarget(target))
                {
                    LaunchIfDeadEnemyServer(target, ResolveSpinDirection(angleBase));
                    RemoveLinkedTargetAtServer(i);
                    continue;
                }

                var angle = angleBase + (360f * i / Mathf.Max(1, count));
                var dir = ResolveSpinDirection(angle);
                var radius = Mathf.Clamp(
                    Vector3.Distance(transform.position, target.position),
                    CwslGameConstants.RammerRopeSpinRadiusMin,
                    CwslGameConstants.RammerRopeSpinRadiusMax);

                var before = target.position;
                var desired = transform.position + dir * radius;
                desired.y = target.position.y;
                WarpOrMove(target, desired);

                var movedSpeed = Vector3.Distance(before, target.position) / Mathf.Max(Time.deltaTime, 0.0001f);
                ApplyRopeDamageBySpeedServer(targetId, target, movedSpeed);
                ApplyStunToLinkedTargetServer(target);
                ApplySpinCollisionStunServer(target);
                PlayFlingFxClientRpc(target.position, dir);
            }

            yield return null;
        }

        spinRoutine = null;
        ClearLinksServer();
    }

    private void ApplyRopeDamageBySpeedServer(ulong targetId, Transform target, float movedSpeed)
    {
        if (!IsServer || target == null || movedSpeed <= 0.01f)
            return;

        if (nextRopeDamageByTarget.TryGetValue(targetId, out var nextTime) && Time.time < nextTime)
            return;

        nextRopeDamageByTarget[targetId] = Time.time + CwslGameConstants.RammerRopeDamageTickInterval;
        var damage = CwslCombatMath.ResolveSkillDamage(
            CwslCharacterId.MomentumRammer,
            CwslGameConstants.RammerRopeTickSkillCoeff) * movedSpeed;
        if (damage <= 0f)
            return;

        var hitPos = target.position + Vector3.up * 0.6f;
        var monster = target.GetComponent<CwslMonsterHealth>();
        if (monster != null && monster.IsAlive)
        {
            var direction = (target.position - transform.position).normalized;
            if (damage >= monster.CurrentHealth)
                LaunchDeadEnemyServer(monster, direction);
            monster.DamageFromPlayer(OwnerClientId, damage);
            return;
        }

        var ally = target.GetComponent<CwslPlayerHealth>();
        if (ally != null && ally.IsAlive)
            ally.TryReceiveMeleeHitServer(damage, hitPos);
    }

    private void ApplyStunToLinkedTargetServer(Transform target)
    {
        var networkObject = target.GetComponent<NetworkObject>();
        if (networkObject == null || !CanApplySpinStun(networkObject.NetworkObjectId))
            return;

        var stunPos = target.position + Vector3.up * 0.6f;
        target.GetComponent<CwslMonsterStun>()?.ApplyStunServer(CwslGameConstants.RammerRopeSpinStunDuration, stunPos);
        target.GetComponent<CwslPlayerStun>()?.ApplyStunServer(CwslGameConstants.RammerRopeSpinStunDuration, stunPos);
    }

    private void ApplySpinCollisionStunServer(Transform spinTarget)
    {
        var center = spinTarget.position;
        var hitRadiusSqr = CwslGameConstants.RammerRopeSpinHitRadius * CwslGameConstants.RammerRopeSpinHitRadius;

        foreach (var monster in CwslCombatRegistry.AliveMonsters)
        {
            if (monster == null || !monster.IsAlive || monster.transform == spinTarget)
                continue;

            var delta = monster.transform.position - center;
            delta.y = 0f;
            if (delta.sqrMagnitude > hitRadiusSqr)
                continue;

            if (CanApplySpinStun(monster.NetworkObjectId))
                monster.GetComponent<CwslMonsterStun>()?.ApplyStunServer(CwslGameConstants.RammerRopeSpinStunDuration, center + Vector3.up * 0.6f);
        }

        if (NetworkManager.Singleton == null)
            return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null || playerObject.NetworkObjectId == NetworkObjectId || playerObject.transform == spinTarget)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health == null || !health.IsAlive)
                continue;

            var delta = playerObject.transform.position - center;
            delta.y = 0f;
            if (delta.sqrMagnitude > hitRadiusSqr)
                continue;

            if (!CanApplySpinStun(playerObject.NetworkObjectId))
                continue;

            playerObject.GetComponent<CwslPlayerStun>()?.ApplyStunServer(CwslGameConstants.RammerRopeSpinStunDuration, center + Vector3.up * 0.6f);
            health.TryReceiveMeleeHitServer(
                CwslCombatMath.ResolveSkillDamage(CwslCharacterId.MomentumRammer, CwslGameConstants.RammerCollisionSkillCoeff),
                center + Vector3.up * 0.6f);
        }
    }

    private bool CanApplySpinStun(ulong targetId)
    {
        if (nextSpinStunByTarget.TryGetValue(targetId, out var next) && Time.time < next)
            return false;

        nextSpinStunByTarget[targetId] = Time.time + 0.45f;
        return true;
    }

    private bool IsValidLinkedTarget(Transform target)
    {
        if (target == null)
            return false;

        var monster = target.GetComponent<CwslMonsterHealth>();
        if (monster != null)
            return monster.IsAlive;

        var player = target.GetComponent<CwslPlayerHealth>();
        return player != null && player.IsAlive;
    }

    private bool TryGetLinkedTransform(ulong targetId, out Transform target)
    {
        target = null;
        if (NetworkManager.Singleton == null)
            return false;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var networkObject) ||
            networkObject == null)
            return false;

        target = networkObject.transform;
        return true;
    }

    private void RemoveLinkedTargetAtServer(int index)
    {
        if (!IsServer || index < 0 || index >= linkedObjectIds.Count)
            return;

        linkedObjectIds.RemoveAt(index);
        if (linkedObjectIds.Count == 0)
            hasLink.Value = false;
    }

    private void ClearLinksServer()
    {
        if (!IsServer)
            return;

        if (spinRoutine != null)
        {
            StopCoroutine(spinRoutine);
            spinRoutine = null;
        }

        linkedObjectIds.Clear();
        hasLink.Value = false;
    }

    private static void WarpOrMove(Transform target, Vector3 next)
    {
        next = CwslArenaUtility.ClampToPlayArea(
            next,
            target.GetComponent<CwslPlayerBodyCollider>()?.Radius ?? CwslGameConstants.PlayerBodyColliderRadiusDefault);

        var rammer = target.GetComponent<CwslMomentumRammerSkill>();
        if (rammer != null && rammer.IsMomentumActive)
        {
            target.position = next;
            return;
        }

        var agent = target.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.Warp(next);
        else
            target.position = next;
    }

    private void HandleLinkChanged(bool previous, bool current) => RefreshRopeVisual();
    private void HandleLinkedListChanged(NetworkListEvent<ulong> changeEvent) => RefreshRopeVisual();

    private void RefreshRopeVisual()
    {
        if (!HasActiveLink)
        {
            DisableAllRopeLines();
            return;
        }

        var activeIds = new HashSet<ulong>();
        for (var i = 0; i < linkedObjectIds.Count; i++)
        {
            var targetId = linkedObjectIds[i];
            if (!TryGetLinkedTransform(targetId, out var target))
                continue;

            activeIds.Add(targetId);
            var line = EnsureRopeLine(targetId);
            if (line == null)
                continue;

            line.enabled = true;
            line.SetPosition(0, transform.position + Vector3.up * 0.9f);
            line.SetPosition(1, target.position + Vector3.up * 0.9f);
        }

        foreach (var pair in ropeLines)
        {
            if (pair.Value != null)
                pair.Value.enabled = activeIds.Contains(pair.Key);
        }
    }

    private LineRenderer EnsureRopeLine(ulong targetId)
    {
        if (ropeLines.TryGetValue(targetId, out var existing) && existing != null)
            return existing;

        var go = new GameObject($"RammerRopeLine_{targetId}");
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.08f;
        line.endWidth = 0.05f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(0.95f, 0.78f, 0.35f, 0.95f);
        line.endColor = new Color(0.85f, 0.55f, 0.2f, 0.85f);
        line.enabled = false;
        ropeLines[targetId] = line;
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

    private static Vector3 ResolveSpinDirection(float angleDegrees)
    {
        var radians = angleDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
    }

    private void LaunchIfDeadEnemyServer(Transform target, Vector3 direction)
    {
        var monster = target != null ? target.GetComponent<CwslMonsterHealth>() : null;
        if (monster == null || monster.IsAlive)
            return;

        LaunchDeadEnemyServer(monster, direction);
    }

    private void LaunchDeadEnemyServer(CwslMonsterHealth monster, Vector3 direction)
    {
        if (monster == null || !monster.IsAlive)
            return;

        var knockback = monster.GetComponent<CwslMonsterKnockback>();
        if (knockback == null)
            knockback = monster.gameObject.AddComponent<CwslMonsterKnockback>();
        knockback.ApplyKnockbackServer(
            direction,
            CwslGameConstants.RammerRopeDeadLaunchDistance,
            CwslGameConstants.RammerRopeDeadLaunchDuration);
    }

    [ClientRpc]
    private void PlayAttachFxClientRpc(Vector3 targetPosition)
    {
        CwslVfxSpawner.SpawnRammerRopeAttach(targetPosition);
    }

    [ClientRpc]
    private void PlayAttachAreaFxClientRpc(Vector3 center, float radius)
    {
        var root = new GameObject("RammerRopeAreaFx");
        root.transform.position = center + Vector3.up * 0.05f;
        var ring = CwslVfxSpawner.AttachGatherChargeCircle(root.transform);
        if (ring != null)
        {
            var scale = Mathf.Max(0.3f, (radius * 2f) / 6f);
            ring.transform.localScale = Vector3.one * scale;
        }

        Destroy(root, 0.55f);
    }

    [ClientRpc]
    private void PlayFlingFxClientRpc(Vector3 targetPosition, Vector3 direction)
    {
        CwslVfxSpawner.SpawnRammerRopeFling(targetPosition, direction);
    }
}
