using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>탱커 W — 전방 돌진. Q 방패 강화 중이면 방패 반경만큼 광역 넉백.</summary>
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
    private NavMeshAgent agent;
    private Coroutine dashRoutine;
    private float nextDashTime;

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
        agent = GetComponent<NavMeshAgent>();
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

        if (dashRoutine != null || Time.time < nextDashTime)
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
        nextDashTime = Time.time + CwslGameConstants.TankShieldDashCooldown;
        dashRoutine = StartCoroutine(DashRoutine(direction));
        return true;
    }

    private Vector3 ResolveDashDirection(Vector3 worldPoint)
    {
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

        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        var empowered = fortifySkill != null && fortifySkill.IsShieldActive;
        PlayDashClientRpc(direction, empowered);

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
            transform.position = next;

            ApplyDashPushServer(direction, empowered);
            yield return null;
        }

        var finalPos = CwslArenaUtility.ClampToPlayArea(origin + direction * distance, bodyRadius);
        transform.position = finalPos;
        ApplyDashPushServer(direction, empowered);

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

    private void ApplyDashPushServer(Vector3 dashDirection, bool empowered)
    {
        PushMonstersInRadiusServer(
            transform.position,
            CwslGameConstants.TankShieldDashPushRadius,
            dashDirection,
            empowered);

        if (empowered)
            ApplyShieldAreaPushServer();
    }

    private void ApplyShieldAreaPushServer()
    {
        var radius = CwslGameConstants.FortifyShieldBlockRadius;
        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);
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
        }
    }

    private void PushMonstersInRadiusServer(
        Vector3 center,
        float radius,
        Vector3 dashDirection,
        bool empowered)
    {
        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);
        var radiusSq = radius * radius;
        var pushDistance = empowered
            ? CwslGameConstants.TankShieldDashEmpoweredPushDistance
            : CwslGameConstants.TankShieldDashPushDistance;
        var pushDuration = empowered
            ? CwslGameConstants.TankShieldDashEmpoweredPushDuration
            : CwslGameConstants.TankShieldDashPushDuration;

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
                : Vector3.Lerp(dashDirection, flat.normalized, empowered ? 0.35f : 0.15f).normalized;
            ApplyKnockbackToMonster(monster, direction, pushDistance, pushDuration);
        }
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
    }

    [ClientRpc]
    private void PlayDashClientRpc(Vector3 direction, bool empowered)
    {
        var visual = transform.Find("Visual");
        var bash = visual?.GetComponent<CwslPlayerShieldBashVisual>();
        if (bash == null)
            return;

        var target = transform.position + direction * CwslGameConstants.TankShieldDashDistance;
        bash.PlayWindup(target);
        if (empowered)
            CwslVfxSpawner.SpawnFortifyBlock(transform.position + Vector3.up * 0.9f);
    }
}
