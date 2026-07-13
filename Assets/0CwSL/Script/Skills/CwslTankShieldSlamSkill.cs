using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>??? E ???? ??????. ??? ????? + ?????????</summary>
public class CwslTankShieldSlamSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotE;

    private CwslTankFortifySkill fortifySkill;
    private CwslPlayerMovement movement;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslTankShieldDashSkill dashSkill;
    private CwslTankShieldWhirlwindSkill whirlwindSkill;
    private NavMeshAgent agent;
    private Coroutine slamRoutine;
    private Vector3 slamKnockbackDirection;
    private CwslPlayerSkillCooldowns skillCooldowns;

    public bool IsSlamming => slamRoutine != null;

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
        dashSkill = GetComponent<CwslTankShieldDashSkill>();
        whirlwindSkill = GetComponent<CwslTankShieldWhirlwindSkill>();
        agent = GetComponent<NavMeshAgent>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();

        if (playerHealth != null)
            playerHealth.OnDied += HandleOwnerDied;
    }

    public override void OnNetworkDespawn()
    {
        if (playerHealth != null)
            playerHealth.OnDied -= HandleOwnerDied;

        CancelSkillServer();
    }

    private void OnDisable()
    {
        CancelSkillServer();
    }

    private void HandleOwnerDied()
    {
        CancelSkillServer();
    }

    public void CancelSkillServer()
    {
        if (slamRoutine != null)
        {
            StopCoroutine(slamRoutine);
            slamRoutine = null;
        }

        ReleaseAgent();
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

        return TryCastServer(senderClientId, worldPoint);
    }

    public bool TryCastServer(ulong senderClientId, Vector3 worldPoint = default)
    {
        if (!CanCastServer(senderClientId))
            return false;

        slamKnockbackDirection = ResolveKnockbackDirection(worldPoint);
        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        slamRoutine = StartCoroutine(SlamRoutine());
        return true;
    }

    private Vector3 ResolveKnockbackDirection(Vector3 worldPoint)
    {
        var flat = worldPoint - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            flat = transform.forward;

        return flat.normalized;
    }

    private void FaceKnockbackDirection()
    {
        if (slamKnockbackDirection.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(slamKnockbackDirection, Vector3.up);
    }

    public bool CanCastServer(ulong senderClientId)
    {
        if (!IsServer || senderClientId != OwnerClientId)
            return false;

        if (slamRoutine != null)
            return false;

        if (skillCooldowns != null && !skillCooldowns.IsReady(BoundSlotIndex))
            return false;

        if (playerHealth != null && !playerHealth.IsAlive)
            return false;

        if (playerStun != null && playerStun.IsStunned)
            return false;

        if (dashSkill != null && dashSkill.IsDashing)
            return false;

        if (whirlwindSkill != null && whirlwindSkill.IsWhirlwinding)
            return false;

        return true;
    }

    private IEnumerator SlamRoutine()
    {
        try
        {
            movement?.StopMovement();
            HoldAgent();
            FaceKnockbackDirection();

            var empowered = CwslTankSkillEmpower.IsEmpowered(fortifySkill);
            PlaySlamClientRpc(empowered);

            yield return new WaitForSeconds(
                CwslGameConstants.TankShieldSlamWindup + CwslGameConstants.TankShieldSlamSlamDownTime);

            var radius = CwslGameConstants.TankShieldSlamRadius
                         * CwslTankSkillEmpower.GetRadiusMultiplier(empowered);
            var stunDuration = CwslGameConstants.TankShieldSlamStunDuration
                               * CwslTankSkillEmpower.GetPowerMultiplier(empowered);

            var impactPos = transform.position;
            impactPos.y = CwslTankShieldVfxUtil.VisualGroundY;
            CwslSkillAudioFeedback.PlayTankShieldSlamGroundImpact(impactPos);

            ApplySlamEffectsServer(radius, stunDuration, empowered);
        }
        finally
        {
            ReleaseAgent();
            slamRoutine = null;
        }
    }

    private void ApplySlamEffectsServer(float radius, float stunDuration, bool empowered)
    {
        var center = transform.position;
        var radiusSq = radius * radius;
        var monsters = CwslCombatRegistry.AliveMonsters;

        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var flat = monster.transform.position - center;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSq)
                continue;

            CwslMonsterStatusController.Ensure(monster)?.ApplyShockServer(stunDuration);

            var slamDamage = CwslCombatMath.ResolveSkillDamage(
                CwslCharacterId.Tank,
                CwslGameConstants.TankShieldSlamSkillCoeff) * CwslTankSkillEmpower.GetPowerMultiplier(empowered);
            monster.DamageFromPlayer(OwnerClientId, slamDamage);

            var knockback = monster.GetComponent<CwslMonsterKnockback>();
            if (knockback == null)
                knockback = monster.gameObject.AddComponent<CwslMonsterKnockback>();
            var pushDistance = empowered ? 2.4f : 1.1f;
            knockback.ApplyKnockbackServer(slamKnockbackDirection, pushDistance, 0.28f);
        }
    }

    private void HoldAgent()
    {
        if (agent == null || !agent.enabled)
            return;

        agent.isStopped = true;
        agent.ResetPath();
        agent.updateRotation = false;
    }

    private void ReleaseAgent()
    {
        if (agent == null || !agent.enabled)
            return;

        agent.updatePosition = true;
        agent.updateRotation = true;
        if (agent.isOnNavMesh)
            agent.Warp(transform.position);
        agent.isStopped = false;
    }

    [ClientRpc]
    private void PlaySlamClientRpc(bool empowered)
    {
        var visualRoot = transform.Find("Visual");
        var visual = visualRoot?.GetComponent<CwslTankShieldSkillVisual>();
        if (visual == null && visualRoot != null)
            visual = visualRoot.gameObject.AddComponent<CwslTankShieldSkillVisual>();
        visual?.PlaySlam(empowered);

        if (!IsOwner)
            return;

        var shake = CwslGameConstants.TankShieldSlamShakeMagnitude
                    * CwslTankSkillEmpower.GetPowerMultiplier(empowered);
        var duration = CwslGameConstants.TankShieldSlamShakeDuration
                       * (empowered ? 1.2f : 1f);
        CwslCameraShake.Play(duration, shake);
    }
}
