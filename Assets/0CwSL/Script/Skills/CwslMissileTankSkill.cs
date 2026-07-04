using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 총잡이 전투.
/// 어택/추적/선택 적 — 1초 쿨 자동 사격 / Q — 골드 2 이상 시 양손 동시(쿨 무시)
/// </summary>
public class CwslMissileTankSkill : CwslPlayerSkillBase
{
    private const float GunCooldown = 1f;
    private const float MissileSpeed = 18f;
    private const float MissileLifetime = 7f;
    private const float MissileDamage = 1f;

    private enum GunSide
    {
        Right,
        Left
    }

    private float nextRightFireTime;
    private CwslMonsterHealth focusedTarget;
    private bool lastCombatPoseActive;
    private CwslGunCombatPoseMode lastCombatPoseMode;
    private Vector3 lastSyncedAimPoint;
    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerSelection selection;
    private CwslPlayerCannonAim cannonAim;
    private CwslPlayerGold playerGold;
    private CwslPlayerMovement movement;
    private CwslPlayerCombat combat;
    private NavMeshAgent navAgent;
    private bool manualCombatFacing;

    public float AttackRange => CwslGameConstants.MissileTankRange;

    public bool HasEnemyInRange() => TryGetNearestMonsterInRange(out _);

    public void ClearAttackFocus() => focusedTarget = null;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.MissileTank;

    public override bool CanCastServer(ulong senderClientId) => false;

    public override void OnNetworkSpawn()
    {
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        selection = GetComponent<CwslPlayerSelection>();
        cannonAim = GetComponent<CwslPlayerCannonAim>();
        playerGold = GetComponent<CwslPlayerGold>();
        movement = GetComponent<CwslPlayerMovement>();
        combat = GetComponent<CwslPlayerCombat>();
        navAgent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (!IsServer || playerCharacter == null || playerCharacter.CharacterId != CwslCharacterId.MissileTank)
            return;

        var hasTarget = TryResolveTarget(out var target);
        var aimPoint = hasTarget ? target.GetAimPoint() : transform.position + transform.forward;
        var poseMode = ResolveCombatPoseMode(hasTarget);
        var inCombat = hasTarget && (combat == null || !combat.IsPureMoveMode);

        UpdateCombatFacing(aimPoint, inCombat);

        if (inCombat)
            cannonAim?.SetAimServer(aimPoint);
        else
            cannonAim?.ResetAimServer();

        if (poseMode != CwslGunCombatPoseMode.Off)
        {
            if (!lastCombatPoseActive ||
                poseMode != lastCombatPoseMode ||
                Vector3.Distance(aimPoint, lastSyncedAimPoint) > 0.2f)
            {
                lastCombatPoseActive = true;
                lastCombatPoseMode = poseMode;
                lastSyncedAimPoint = aimPoint;
                SyncCombatPoseClientRpc(aimPoint, poseMode);
            }
        }
        else if (lastCombatPoseActive)
        {
            lastCombatPoseActive = false;
            lastCombatPoseMode = CwslGunCombatPoseMode.Off;
            SyncCombatPoseClientRpc(Vector3.zero, CwslGunCombatPoseMode.Off);
        }

        if (hasTarget && ShouldAutoFire())
            TryFireAttackServer(dualWieldMode: false);
    }

    private void UpdateCombatFacing(Vector3 aimPoint, bool inCombat)
    {
        if (!inCombat)
        {
            if (manualCombatFacing && navAgent != null)
                navAgent.updateRotation = true;
            manualCombatFacing = false;
            return;
        }

        if (navAgent != null && navAgent.enabled)
        {
            navAgent.updateRotation = false;
            manualCombatFacing = true;
        }

        var flat = aimPoint - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            return;

        var desired = Quaternion.LookRotation(flat.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            desired,
            900f * Time.deltaTime);
    }

    private CwslGunCombatPoseMode ResolveCombatPoseMode(bool hasTarget)
    {
        if (!hasTarget || combat == null || combat.IsPureMoveMode)
            return CwslGunCombatPoseMode.Off;

        if (movement != null && movement.IsMoving)
            return CwslGunCombatPoseMode.AttackMove;

        return CwslGunCombatPoseMode.Hold;
    }

    private bool ShouldAutoFire()
    {
        if (playerGold == null || playerGold.Gold < CwslGameConstants.SkillGoldCost)
            return false;

        if (combat != null && combat.IsPureMoveMode)
            return false;

        if (combat != null && (combat.IsAttackMoveActive || combat.HasChaseTarget))
            return true;

        if (selection != null &&
            selection.TryGetSelectedTarget(out var selected) &&
            selected != null)
        {
            var health = selected.GetComponent<CwslMonsterHealth>();
            if (health != null && health.IsAlive && IsInRange(selected.transform.position))
                return true;
        }

        return false;
    }

    /// <param name="dualWieldMode">false=오른쪽 총, true=Q 양손 동시(골드2, 쿨 무시)</param>
    public bool TryFireAttackServer(bool dualWieldMode)
    {
        if (!IsServer || playerCharacter == null || playerCharacter.CharacterId != CwslCharacterId.MissileTank)
            return false;

        if (!TryResolveTarget(out _))
            return false;

        return dualWieldMode ? TryFireDualWieldServer() : TryFireSingleServer();
    }

    private bool TryFireSingleServer()
    {
        if (Time.time < nextRightFireTime)
            return false;

        if (playerGold == null || !playerGold.TrySpendGoldServer(CwslGameConstants.SkillGoldCost))
            return false;

        FireFromGun(GunSide.Right);
        nextRightFireTime = Time.time + GunCooldown;
        return true;
    }

    private bool TryFireDualWieldServer()
    {
        var dualCost = CwslGameConstants.SkillGoldCost * 2;
        if (playerGold == null || playerGold.Gold < dualCost)
            return false;

        if (!playerGold.TrySpendGoldServer(dualCost))
            return false;

        if (!TryPrepareShot(out var aimPoint, out var fireDirection, out var shotTarget))
            return false;

        FireProjectileServer(fireDirection, piercing: false, useLeftMuzzle: false, shotTarget);
        FireProjectileServer(fireDirection, piercing: false, useLeftMuzzle: true, shotTarget);
        PlayDualFireClientRpc(aimPoint);
        return true;
    }

    private void FireFromGun(GunSide gun)
    {
        if (!TryPrepareShot(out var aimPoint, out var fireDirection, out var shotTarget))
            return;

        var useLeftGun = gun == GunSide.Left;
        FireProjectileServer(fireDirection, piercing: false, useLeftGun, shotTarget);
        PlayFireClientRpc(aimPoint, useLeftGun);
    }

    private bool TryPrepareShot(out Vector3 aimPoint, out Vector3 fireDirection, out CwslMonsterHealth shotTarget)
    {
        aimPoint = transform.position + transform.forward * 10f;
        fireDirection = transform.forward;
        shotTarget = null;

        if (!TryResolveTarget(out shotTarget))
            return false;

        aimPoint = shotTarget.GetAimPoint();
        FaceFireDirectionServer(aimPoint - transform.position);
        cannonAim?.SnapAimServer(aimPoint);

        var muzzle = cannonAim != null
            ? cannonAim.GetMuzzlePosition()
            : transform.position + Vector3.up * 1.2f + transform.forward * 0.9f;

        var toAim = aimPoint - muzzle;
        if (toAim.sqrMagnitude > 0.0001f)
            fireDirection = toAim.normalized;
        else
        {
            var flat = aimPoint - transform.position;
            flat.y = 0f;
            fireDirection = flat.sqrMagnitude > 0.0001f ? flat.normalized : transform.forward;
        }

        return true;
    }

    private static void FaceFireDirectionServer(Transform actor, Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        actor.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void FaceFireDirectionServer(Vector3 direction) =>
        FaceFireDirectionServer(transform, direction);

    private bool TryResolveTarget(out CwslMonsterHealth target)
    {
        target = null;

        if (selection != null &&
            selection.TryGetSelectedTarget(out var selected) &&
            selected != null)
        {
            var selectedHealth = selected.GetComponent<CwslMonsterHealth>();
            if (selectedHealth != null &&
                selectedHealth.IsAlive &&
                IsInRange(selected.transform.position))
            {
                focusedTarget = selectedHealth;
                target = selectedHealth;
                return true;
            }
        }

        if (focusedTarget != null &&
            focusedTarget.IsAlive &&
            IsInRange(focusedTarget.transform.position))
        {
            target = focusedTarget;
            return true;
        }

        focusedTarget = null;
        if (!TryGetNearestMonsterInRange(out target))
            return false;

        focusedTarget = target;
        return true;
    }

    private bool TryGetNearestMonsterInRange(out CwslMonsterHealth target)
    {
        target = null;
        var bestDistance = float.MaxValue;
        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var flat = monster.transform.position - transform.position;
            flat.y = 0f;
            var distance = flat.magnitude;
            if (distance > CwslGameConstants.MissileTankRange || distance >= bestDistance)
                continue;

            bestDistance = distance;
            target = monster;
        }

        return target != null;
    }

    private bool IsInRange(Vector3 worldPosition)
    {
        var flat = worldPosition - transform.position;
        flat.y = 0f;
        return flat.magnitude <= CwslGameConstants.MissileTankRange;
    }

    private void FireProjectileServer(
        Vector3 fireDirection,
        bool piercing,
        bool useLeftMuzzle,
        CwslMonsterHealth homingTarget)
    {
        var session = CwslGameSession.Instance;
        if (session == null || session.Assets.playerMissilePrefab == null)
            return;

        var muzzle = ResolveMuzzlePosition(useLeftMuzzle);

        if (fireDirection.sqrMagnitude < 0.0001f)
            fireDirection = transform.forward;

        var spawnOffset = ResolveSpawnOffset(muzzle, homingTarget);
        var spawnPosition = muzzle + fireDirection.normalized * spawnOffset;

        var networkObject = CwslNetworkPoolService.Instance?.Get(
            session.Assets.playerMissilePrefab,
            spawnPosition,
            Quaternion.LookRotation(fireDirection, Vector3.up));
        if (networkObject == null)
            return;

        var projectile = networkObject.GetComponent<CwslPlayerProjectile>();
        projectile?.Configure(
            fireDirection,
            MissileSpeed,
            MissileLifetime,
            OwnerClientId,
            MissileDamage,
            piercing,
            NetworkObject,
            homingTarget);
    }

    private Vector3 ResolveMuzzlePosition(bool useLeftMuzzle)
    {
        if (cannonAim == null)
            return transform.position + Vector3.up * 1.2f + transform.forward * 0.9f;

        return useLeftMuzzle
            ? cannonAim.GetLeftMuzzlePosition()
            : cannonAim.GetMuzzlePosition();
    }

    private float ResolveSpawnOffset(Vector3 muzzle, CwslMonsterHealth target)
    {
        var maxOffset = CwslGameConstants.PlayerArrowSpawnForwardOffset;
        if (target == null)
            return maxOffset;

        var toTarget = target.GetAimPoint() - muzzle;
        if (toTarget.sqrMagnitude < 0.0001f)
            return maxOffset;

        var distance = toTarget.magnitude;
        var leadRoom = 0.12f;
        return Mathf.Clamp(distance - leadRoom, CwslGameConstants.PlayerBulletSpawnMinOffset, maxOffset);
    }

    [ClientRpc]
    private void SyncCombatPoseClientRpc(Vector3 aimPoint, CwslGunCombatPoseMode mode)
    {
        var visual = transform.Find("Visual");
        visual?.GetComponent<CwslPlayerGunShootVisual>()?.SetCombatPose(aimPoint, mode);
    }

    [ClientRpc]
    private void PlayFireClientRpc(Vector3 aimPoint, bool useLeftGun)
    {
        cannonAim?.SnapAimClient(aimPoint);

        var visual = transform.Find("Visual");
        var gunVisual = visual?.GetComponent<CwslPlayerGunShootVisual>();
        if (gunVisual != null)
        {
            gunVisual.PlayShoot(aimPoint, useLeftGun);
            return;
        }

        visual?.GetComponent<CwslPlayerCannonRecoilVisual>()?.PlayFire();
    }

    [ClientRpc]
    private void PlayDualFireClientRpc(Vector3 aimPoint)
    {
        cannonAim?.SnapAimClient(aimPoint);
        var visual = transform.Find("Visual");
        visual?.GetComponent<CwslPlayerGunShootVisual>()?.PlayDualShoot(aimPoint);
    }
}
