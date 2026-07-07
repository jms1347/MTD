using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>미사일 탱커 E — 뒤로 대시 + 연막탄 1발.</summary>
public class CwslMissileTankSmokeDashSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = 1;

    private CwslMissileTankSkill missileSkill;
    private CwslPlayerMovement movement;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslMissileTankPowerBoostSkill powerBoostSkill;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private NavMeshAgent agent;
    private Coroutine dashRoutine;

    public bool IsDashing => dashRoutine != null;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.MissileTank;

    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        missileSkill = GetComponent<CwslMissileTankSkill>();
        movement = GetComponent<CwslPlayerMovement>();
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        powerBoostSkill = GetComponent<CwslMissileTankPowerBoostSkill>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
        agent = GetComponent<NavMeshAgent>();
    }

    public override bool CanUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint) =>
        slotIndex == BoundSlotIndex && CanCastServer(senderClientId, worldPoint);

    public override bool TryUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint)
    {
        if (!IsServer || slotIndex != BoundSlotIndex)
            return false;

        return TryCastServer(senderClientId, worldPoint);
    }

    public bool TryCastServer(ulong senderClientId, Vector3 worldPoint)
    {
        if (!CanCastServer(senderClientId, worldPoint))
            return false;

        var dashDirection = ResolveBackwardDirection(worldPoint);
        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        dashRoutine = StartCoroutine(DashRoutine(dashDirection));
        return true;
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

        return ResolveBackwardDirection(worldPoint).sqrMagnitude > 0.0001f;
    }

    private Vector3 ResolveBackwardDirection(Vector3 worldPoint)
    {
        var towardMouse = worldPoint - transform.position;
        towardMouse.y = 0f;
        if (towardMouse.sqrMagnitude < 0.15f)
            towardMouse = transform.forward;

        return (-towardMouse).normalized;
    }

    private IEnumerator DashRoutine(Vector3 dashDirection)
    {
        movement?.StopMovement();
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        var faceDirection = -dashDirection;
        if (faceDirection.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(faceDirection, Vector3.up);

        PlayDashClientRpc(dashDirection);
        missileSkill?.FireSmokeBombServer(faceDirection);

        var duration = CwslGameConstants.MissileTankSmokeDashDuration;
        var distance = CwslGameConstants.MissileTankSmokeDashDistance;
        var origin = transform.position;
        var bodyRadius = GetComponent<CwslPlayerBodyCollider>()?.Radius
            ?? CwslGameConstants.PlayerBodyColliderRadiusDefault;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var target = origin + dashDirection * (distance * t);
            transform.position = CwslArenaUtility.ClampToPlayArea(target, bodyRadius);
            yield return null;
        }

        transform.position = CwslArenaUtility.ClampToPlayArea(origin + dashDirection * distance, bodyRadius);

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

    [ClientRpc]
    private void PlayDashClientRpc(Vector3 dashDirection)
    {
        var visual = transform.Find("Visual");
        if (visual == null)
            return;

        var trailPrefab = CwslGameSession.Instance?.Assets?.missileTankSmokeDashTrailVfx;
        if (trailPrefab == null)
            return;

        var trail = CwslVfxSpawner.TryInstantiate(trailPrefab, visual.position, Quaternion.identity);
        if (trail == null)
            return;

        trail.transform.SetParent(visual, false);
        trail.transform.localPosition = Vector3.zero;
        var flatDash = dashDirection;
        flatDash.y = 0f;
        if (flatDash.sqrMagnitude > 0.0001f)
            trail.transform.rotation = Quaternion.LookRotation(-flatDash.normalized, Vector3.up);

        Destroy(trail, CwslGameConstants.MissileTankSmokeDashDuration + 0.35f);
    }
}
