using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>??? W ????? ???. Q ?? ?? ?????? ???? ?? ???.</summary>
public class CwslTankShieldDashSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = 3;

    private CwslTankFortifySkill fortifySkill;
    private CwslPlayerMovement movement;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslTankShieldAttack shieldAttack;
    private CwslTankShieldSlamSkill slamSkill;
    private CwslTankShieldWhirlwindSkill whirlwindSkill;
    private CwslTankShieldSkillVisual shieldSkillVisual;
    private NavMeshAgent agent;
    private Coroutine dashRoutine;
    private CwslPlayerSkillCooldowns skillCooldowns;

    public bool IsDashing => dashRoutine != null;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.Tank;

    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        fortifySkill = GetComponent<CwslTankFortifySkill>();
        movement = GetComponent<CwslPlayerMovement>();
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        shieldAttack = GetComponent<CwslTankShieldAttack>();
        slamSkill = GetComponent<CwslTankShieldSlamSkill>();
        whirlwindSkill = GetComponent<CwslTankShieldWhirlwindSkill>();
        shieldSkillVisual = transform.Find("Visual")?.GetComponent<CwslTankShieldSkillVisual>();
        agent = GetComponent<NavMeshAgent>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
    }

    public override bool CanUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint)
    {
        if (slotIndex != BoundSlotIndex)
            return false;

        return CanCastServer(senderClientId, worldPoint);
    }

    public override bool TryUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint)
    {
        if (!IsServer || slotIndex != BoundSlotIndex)
            return false;

        return TryCastServer(senderClientId, worldPoint);
    }

    public bool CanCastServer(ulong senderClientId, Vector3 worldPoint)
    {
        if (!IsServer || senderClientId != OwnerClientId)
            return false;

        if (dashRoutine != null)
            return false;

        if (skillCooldowns != null && !skillCooldowns.IsReady(BoundSlotIndex))
            return false;

        if (playerHealth != null && !playerHealth.IsAlive)
            return false;

        if (playerStun != null && playerStun.IsStunned)
            return false;

        if (shieldAttack != null && shieldAttack.IsAttacking)
            return false;

        if (slamSkill != null && slamSkill.IsSlamming)
            return false;

        if (whirlwindSkill != null && whirlwindSkill.IsWhirlwinding)
            return false;

        return ResolveDashDirection(worldPoint).sqrMagnitude > 0.0001f;
    }

    public bool TryCastServer(ulong senderClientId, Vector3 worldPoint)
    {
        if (!CanCastServer(senderClientId, worldPoint))
            return false;

        var direction = ResolveDashDirection(worldPoint);
        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        dashRoutine = StartCoroutine(DashRoutine(direction));
        return true;
    }

    private Vector3 ResolveDashDirection(Vector3 worldPoint)
    {
        if (movement != null && movement.TryGetFlatMoveDirection(out var moveDirection))
            return moveDirection;

        var flat = worldPoint - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.15f)
            flat = transform.forward;

        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            flat = Vector3.forward;

        return flat.normalized;
    }

    private IEnumerator DashRoutine(Vector3 direction)
    {
        movement?.StopMovement();
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        shieldSkillVisual?.ResetShieldPoseImmediate();
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        var empowered = fortifySkill != null && fortifySkill.IsShieldActive;
        PlayDashClientRpc(direction, empowered);
        var notifiedMonsters = new HashSet<int>(16);

        var duration = CwslGameConstants.TankShieldDashDuration;
        var distance = CwslGameConstants.TankShieldDashDistance;
        var origin = transform.position;
        var bodyRadius = GetComponent<CwslPlayerBodyCollider>()?.Radius
            ?? CwslGameConstants.PlayerBodyColliderRadiusDefault;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var target = origin + direction * (distance * t);
            var next = CwslArenaUtility.ClampToPlayArea(target, bodyRadius);
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.position = next;

            ApplyDashPushServer(direction, empowered, notifiedMonsters);
            yield return null;
        }

        var finalPos = CwslArenaUtility.ClampToPlayArea(origin + direction * distance, bodyRadius);
        transform.position = finalPos;
        ApplyDashPushServer(direction, empowered, notifiedMonsters);

        if (agent != null && agent.enabled)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            if (agent.isOnNavMesh)
                agent.Warp(transform.position);
            agent.isStopped = false;
        }

        dashRoutine = null;
    }

    private void ApplyDashPushServer(Vector3 dashDirection, bool empowered, HashSet<int> notifiedMonsters)
    {
        PushMonstersInRadiusServer(
            transform.position,
            CwslGameConstants.TankShieldDashPushRadius,
            dashDirection,
            empowered,
            notifiedMonsters);

        if (empowered)
            ApplyShieldAreaPushServer(notifiedMonsters);
    }

    private void ApplyShieldAreaPushServer(HashSet<int> notifiedMonsters)
    {
        var radius = CwslGameConstants.FortifyShieldBlockRadius;
        var monsters = CwslCombatRegistry.AliveMonsters;
        var hitPositions = new List<Vector3>(8);
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var flat = monster.transform.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > radius * radius)
                continue;

            var direction = flat.sqrMagnitude < 0.0001f
                ? transform.forward
                : flat.normalized;
            ApplyKnockbackToMonster(
                monster,
                direction,
                CwslGameConstants.TankShieldDashShieldPushDistance,
                CwslGameConstants.TankShieldDashShieldPushDuration);

            var id = monster.GetInstanceID();
            if (notifiedMonsters.Add(id))
                hitPositions.Add(monster.GetFlatHitPoint());
        }

        if (hitPositions.Count > 0)
            PlayTankShieldDashImpactClientRpc(hitPositions.ToArray());
    }

    private void PushMonstersInRadiusServer(
        Vector3 center,
        float radius,
        Vector3 dashDirection,
        bool empowered,
        HashSet<int> notifiedMonsters)
    {
        var monsters = CwslCombatRegistry.AliveMonsters;
        var radiusSq = radius * radius;
        var pushDistance = empowered
            ? CwslGameConstants.TankShieldDashEmpoweredPushDistance
            : CwslGameConstants.TankShieldDashPushDistance;
        var pushDuration = empowered
            ? CwslGameConstants.TankShieldDashEmpoweredPushDuration
            : CwslGameConstants.TankShieldDashPushDuration;
        var hitPositions = new List<Vector3>(8);

        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var flat = monster.transform.position - center;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSq)
                continue;

            var direction = flat.sqrMagnitude < 0.0001f
                ? dashDirection
                : Vector3.Lerp(dashDirection, flat.normalized, empowered ? 0.25f : 0.1f).normalized;

            // 대시 도중 같은 몬스터에 대해 반복 사운드가 겹치지 않도록 1회만 재생.
            var id = monster.GetInstanceID();
            if (notifiedMonsters.Add(id))
            {
                hitPositions.Add(monster.GetFlatHitPoint());
                var dashDamage = CwslCombatMath.ResolveSkillDamage(
                    CwslCharacterId.Tank,
                    CwslGameConstants.TankDashContactSkillCoeff) * (empowered ? CwslTankSkillEmpower.GetPowerMultiplier(true) : 1f);
                monster.DamageFromPlayer(OwnerClientId, dashDamage);
            }

            ApplyKnockbackToMonster(monster, direction, pushDistance, pushDuration);
        }

        if (hitPositions.Count > 0)
            PlayTankShieldDashImpactClientRpc(hitPositions.ToArray());
    }

    private static void ApplyKnockbackToMonster(
        CwslMonsterHealth monster,
        Vector3 direction,
        float distance,
        float duration)
    {
        if (monster == null)
            return;

        var knockback = monster.GetComponent<CwslMonsterKnockback>();
        if (knockback == null)
            knockback = monster.gameObject.AddComponent<CwslMonsterKnockback>();

        knockback.ApplyKnockbackServer(direction, distance, duration);
        monster.NotifyHitFlinchServer(direction, distance * 0.42f);
    }

    [ClientRpc]
    private void PlayDashClientRpc(Vector3 direction, bool empowered)
    {
        var visual = transform.Find("Visual");
        var skillVisual = visual?.GetComponent<CwslTankShieldSkillVisual>();
        skillVisual?.PlayDash(direction, CwslGameConstants.TankShieldDashDuration);
        CwslSkillAudioFeedback.PlayTankShieldDashImpact(transform.position);

        var dashWave = visual?.GetComponent<CwslTankShieldDashWaveVisual>();
        if (dashWave == null && visual != null)
            dashWave = visual.gameObject.AddComponent<CwslTankShieldDashWaveVisual>();
        dashWave?.PlayDashWave(direction, empowered, CwslGameConstants.TankShieldDashDuration);

        if (empowered)
            CwslVfxSpawner.SpawnFortifyBlock(transform.position + Vector3.up * 0.9f);
    }

    [ClientRpc]
    private void PlayTankShieldDashImpactClientRpc(Vector3[] hitPositions)
    {
        if (hitPositions == null || hitPositions.Length == 0)
            return;

        foreach (var pos in hitPositions)
            CwslSkillAudioFeedback.PlayTankShieldDashImpact(pos);
    }
}
