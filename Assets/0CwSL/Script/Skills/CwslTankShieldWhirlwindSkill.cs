using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>탱커 R — 방패 회전. 4초간 광역 공격.</summary>
public class CwslTankShieldWhirlwindSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = 2;

    private CwslTankFortifySkill fortifySkill;
    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerMovement movement;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslTankShieldDashSkill dashSkill;
    private CwslTankShieldSlamSkill slamSkill;
    private NavMeshAgent agent;
    private Coroutine whirlRoutine;
    private float nextWhirlTime;

    public bool IsWhirlwinding => whirlRoutine != null;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.Tank;

    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        fortifySkill = GetComponent<CwslTankFortifySkill>();
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        movement = GetComponent<CwslPlayerMovement>();
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        dashSkill = GetComponent<CwslTankShieldDashSkill>();
        slamSkill = GetComponent<CwslTankShieldSlamSkill>();
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkDespawn()
    {
        if (movement != null)
            movement.WhirlwindSpeedMultiplier = 1f;
    }

    public override bool CanUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint)
    {
        if (slotIndex != BoundSlotIndex)
            return false;

        return CanCastServer(senderClientId);
    }

    public override bool TryUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint)
    {
        if (!IsServer || slotIndex != BoundSlotIndex)
            return false;

        return TryCastServer(senderClientId);
    }

    public bool CanCastServer(ulong senderClientId)
    {
        if (!IsServer || senderClientId != OwnerClientId)
            return false;

        if (whirlRoutine != null || Time.time < nextWhirlTime)
            return false;

        if (playerHealth != null && !playerHealth.IsAlive)
            return false;

        if (playerStun != null && playerStun.IsStunned)
            return false;

        if (dashSkill != null && dashSkill.IsDashing)
            return false;

        if (slamSkill != null && slamSkill.IsSlamming)
            return false;

        return true;
    }

    public bool TryCastServer(ulong senderClientId)
    {
        if (!CanCastServer(senderClientId))
            return false;

        nextWhirlTime = Time.time + CwslGameConstants.TankShieldWhirlwindCooldown;
        whirlRoutine = StartCoroutine(WhirlwindRoutine());
        return true;
    }

    private IEnumerator WhirlwindRoutine()
    {
        BeginWhirlwindMovement();

        var empowered = CwslTankSkillEmpower.IsEmpowered(fortifySkill);
        var duration = CwslGameConstants.TankShieldWhirlwindDuration;
        PlayWhirlwindClientRpc(duration, empowered);

        var elapsed = 0f;
        var tickTimer = 0f;
        var spinSpeed = CwslGameConstants.TankShieldWhirlwindSpinSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tickTimer += Time.deltaTime;
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

            if (tickTimer >= CwslGameConstants.TankShieldWhirlwindTickInterval)
            {
                tickTimer = 0f;
                ApplyWhirlwindTickServer(empowered);
            }

            yield return null;
        }

        ApplyWhirlwindTickServer(empowered);
        EndWhirlwindMovement();
        whirlRoutine = null;
    }

    private void BeginWhirlwindMovement()
    {
        if (movement != null)
            movement.WhirlwindSpeedMultiplier = CwslGameConstants.TankShieldWhirlwindMoveSpeedMultiplier;

        HoldAgentRotationOnly();
    }

    private void EndWhirlwindMovement()
    {
        if (movement != null)
            movement.WhirlwindSpeedMultiplier = 1f;

        ReleaseAgentRotation();
    }

    private void HoldAgentRotationOnly()
    {
        if (agent == null || !agent.enabled)
            return;

        agent.updateRotation = false;
        agent.isStopped = false;
    }

    private void ReleaseAgentRotation()
    {
        if (agent == null || !agent.enabled)
            return;

        agent.updateRotation = true;
        if (agent.isOnNavMesh)
            agent.Warp(transform.position);
        agent.isStopped = false;
    }

    private void ApplyWhirlwindTickServer(bool empowered)
    {
        var attackPower = playerCharacter != null
            ? CwslCharacterStatCatalog.GetAttackPower(playerCharacter.CharacterId)
            : CwslGameConstants.AttackDamage;
        var damage = attackPower
                     * CwslGameConstants.TankShieldWhirlwindDamagePerTick
                     * CwslTankSkillEmpower.GetPowerMultiplier(empowered);

        var radius = CwslGameConstants.TankShieldWhirlwindRadius
                     * CwslTankSkillEmpower.GetRadiusMultiplier(empowered);
        var radiusSq = radius * radius;
        var center = transform.position;
        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);

        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var flat = monster.transform.position - center;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSq)
                continue;

            monster.DamageFromPlayer(OwnerClientId, damage);

            if (flat.sqrMagnitude > 0.0001f)
            {
                var knockback = monster.GetComponent<CwslMonsterKnockback>();
                if (knockback == null)
                    knockback = monster.gameObject.AddComponent<CwslMonsterKnockback>();
                var push = empowered ? 1.35f : 0.55f;
                knockback.ApplyKnockbackServer(flat.normalized, push, 0.22f);
            }
        }
    }

    [ClientRpc]
    private void PlayWhirlwindClientRpc(float duration, bool empowered)
    {
        var visualRoot = transform.Find("Visual");
        var visual = visualRoot?.GetComponent<CwslTankShieldSkillVisual>();
        if (visual == null && visualRoot != null)
            visual = visualRoot.gameObject.AddComponent<CwslTankShieldSkillVisual>();
        visual?.PlayWhirlwind(duration, empowered);
    }
}
