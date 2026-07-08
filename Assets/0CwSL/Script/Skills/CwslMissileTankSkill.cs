using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ???????.
/// ???/??/??? ????1??????? ??? / Q ???? 2 ??? ????? ???(????)
/// </summary>
public class CwslMissileTankSkill : CwslPlayerSkillBase
{
    private const float GunCooldown = 1f;
    private const float MissileSpeed = 18f;
    private const float MissileLifetime = 7f;

    private enum GunSide
    {
        Right,
        Left
    }

    private float nextRightFireTime;
    private CwslMonsterHealth focusedTarget;
    private CwslEnemyBase focusedStructure;
    private bool lastCombatPoseActive;
    private CwslGunCombatPoseMode lastCombatPoseMode;
    private Vector3 lastSyncedAimPoint;
    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerSelection selection;
    private CwslPlayerCannonAim cannonAim;
    private CwslPlayerGold playerGold;
    private CwslPlayerPillBuff pillBuff;
    private CwslPlayerMovement movement;
    private CwslPlayerCombat combat;
    private NavMeshAgent navAgent;
    private CwslMissileTankAmmoController ammoController;
    private CwslMissileTankPowerBoostSkill powerBoostSkill;
    private bool manualCombatFacing;

    public float AttackRange => CwslGameConstants.MissileTankRange;

    public bool HasEnemyInRange() => TryGetNearestMonsterInRange(out _);

    public void ClearAttackFocus()
    {
        focusedTarget = null;
        focusedStructure = null;
    }

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
        pillBuff = GetComponent<CwslPlayerPillBuff>();
        movement = GetComponent<CwslPlayerMovement>();
        combat = GetComponent<CwslPlayerCombat>();
        navAgent = GetComponent<NavMeshAgent>();
        ammoController = GetComponent<CwslMissileTankAmmoController>();
        powerBoostSkill = GetComponent<CwslMissileTankPowerBoostSkill>();
    }

    private void Update()
    {
        if (!IsServer || playerCharacter == null || playerCharacter.CharacterId != CwslCharacterId.MissileTank)
            return;

        var hasStructure = TryResolveStructureTarget(out var structure);
        var hasMonster = TryResolveTarget(out var target);
        var hasTarget = hasStructure || hasMonster;
        var aimPoint = hasStructure
            ? structure.GetAimPoint()
            : hasMonster
                ? target.GetAimPoint()
                : transform.position + transform.forward;
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

            var enemyBase = selected.GetComponent<CwslEnemyBase>();
            if (enemyBase != null && enemyBase.IsAlive && IsInRange(selected.transform.position))
                return true;
        }

        return false;
    }

    /// <param name="dualWieldMode">false=??????? true=Q ??? ???(??2, ????)</param>
    public bool TryFireAttackServer(bool dualWieldMode)
    {
        if (!IsServer || playerCharacter == null || playerCharacter.CharacterId != CwslCharacterId.MissileTank)
            return false;

        if (!TryResolveTarget(out _) && !TryResolveStructureTarget(out _))
            return false;

        return dualWieldMode ? TryFireDualWieldServer() : TryFireSingleServer();
    }

    private bool TryFireSingleServer()
    {
        if (Time.time < nextRightFireTime)
            return false;

        FireFromGun(GunSide.Right);
        nextRightFireTime = Time.time + ResolveGunCooldown()
                            / Mathf.Max(0.25f, GetComponent<CwslAttackSpeedBuff>()?.AttackSpeedMultiplier ?? 1f);
        return true;
    }

    private bool TryFireDualWieldServer()
    {
        var playerSkills = GetComponent<CwslPlayerSkills>();
        var skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
        if (skillCooldowns != null && !skillCooldowns.IsReady(0))
            return false;

        if (playerSkills != null && !playerSkills.TrySpendStaminaForSlot(0))
            return false;

        var dualCost = CwslGameConstants.MissileDualWieldGoldCost;
        if (!CanAffordSkillGold(dualCost))
        {
            GetComponent<CwslPlayerSkills>()?.NotifyGoldInsufficientServer();
            return false;
        }

        if (!TrySpendSkillGold(dualCost))
        {
            GetComponent<CwslPlayerSkills>()?.NotifyGoldInsufficientServer();
            return false;
        }

        if (!TryPrepareShot(out var aimPoint, out var fireDirection, out var shotTarget))
            return false;

        FireProjectileServer(fireDirection, useLeftMuzzle: false, shotTarget);
        FireProjectileServer(fireDirection, useLeftMuzzle: true, shotTarget);
        PlayDualFireClientRpc(aimPoint);
        skillCooldowns?.BeginCooldown(0);
        return true;
    }

    private void FireFromGun(GunSide gun)
    {
        if (!TryPrepareShot(out var aimPoint, out var fireDirection, out var shotTarget))
            return;

        var useLeftGun = gun == GunSide.Left;
        FireProjectileServer(fireDirection, useLeftGun, shotTarget);
        PlayFireClientRpc(aimPoint, useLeftGun);
    }

    private bool TryPrepareShot(out Vector3 aimPoint, out Vector3 fireDirection, out CwslMonsterHealth shotTarget)
    {
        aimPoint = transform.position + transform.forward * 10f;
        fireDirection = transform.forward;
        shotTarget = null;

        if (TryResolveStructureTarget(out var structure))
        {
            aimPoint = structure.GetAimPoint();
            FaceFireDirectionServer(aimPoint - transform.position);
            cannonAim?.SnapAimServer(aimPoint);
            fireDirection = ResolveFireDirection(aimPoint, out _);
            return true;
        }

        if (!TryResolveTarget(out shotTarget))
            return false;

        aimPoint = shotTarget.GetAimPoint();
        FaceFireDirectionServer(aimPoint - transform.position);
        cannonAim?.SnapAimServer(aimPoint);
        fireDirection = ResolveFireDirection(aimPoint, out _);
        return true;
    }

    private Vector3 ResolveFireDirection(Vector3 aimPoint, out Vector3 muzzle)
    {
        muzzle = cannonAim != null
            ? cannonAim.GetMuzzlePosition()
            : transform.position + Vector3.up * 1.2f + transform.forward * 0.9f;

        var toAim = aimPoint - muzzle;
        if (toAim.sqrMagnitude > 0.0001f)
            return toAim.normalized;

        var flat = aimPoint - transform.position;
        flat.y = 0f;
        return flat.sqrMagnitude > 0.0001f ? flat.normalized : transform.forward;
    }

    private bool TryResolveStructureTarget(out CwslEnemyBase structure)
    {
        structure = null;

        if (selection != null &&
            selection.TryGetSelectedTarget(out var selected) &&
            selected != null)
        {
            var selectedBase = selected.GetComponent<CwslEnemyBase>();
            if (selectedBase != null &&
                selectedBase.IsAlive &&
                IsInRange(selected.transform.position))
            {
                focusedStructure = selectedBase;
                focusedTarget = null;
                structure = selectedBase;
                return true;
            }
        }

        if (focusedStructure != null &&
            focusedStructure.IsAlive &&
            IsInRange(focusedStructure.transform.position))
        {
            structure = focusedStructure;
            return true;
        }

        focusedStructure = null;
        return false;
    }

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
                focusedStructure = null;
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
        focusedStructure = null;
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

    private bool TryGetNearestMonsterInRange(out CwslMonsterHealth target)
    {
        target = null;
        var bestDistance = float.MaxValue;
        var monsters = CwslCombatRegistry.AliveMonsters;
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

    public void FireSmokeBombServer(Vector3 fireDirection)
    {
        if (!IsServer)
            return;

        FireProjectileServer(
            fireDirection,
            useLeftMuzzle: false,
            lockedTarget: null,
            forceSmokeBomb: true);
    }

    private float ResolveGunCooldown()
    {
        if (powerBoostSkill != null && powerBoostSkill.IsActive)
            return 0f;

        return GunCooldown;
    }

    private int ResolveMaxPierceHits()
    {
        if (powerBoostSkill == null || !powerBoostSkill.IsActive)
            return 0;

        return CwslGameConstants.MissileTankPowerBoostMaxPierce;
    }

    private CwslMissileTankAmmoKind ResolveCurrentAmmo() =>
        ammoController != null ? ammoController.CurrentAmmo : CwslMissileTankAmmoKind.Basic;

    private void FireProjectileServer(
        Vector3 fireDirection,
        bool useLeftMuzzle,
        CwslMonsterHealth lockedTarget,
        bool forceSmokeBomb = false)
    {
        var session = CwslGameSession.Instance;
        if (session == null || session.Assets.playerMissilePrefab == null)
            return;

        var muzzle = ResolveMuzzlePosition(useLeftMuzzle);

        if (fireDirection.sqrMagnitude < 0.0001f)
            fireDirection = transform.forward;

        var spawnOffset = ResolveSpawnOffset(muzzle, lockedTarget);
        var spawnPosition = muzzle + fireDirection.normalized * spawnOffset;

        var networkObject = CwslNetworkPoolService.Instance?.Get(
            session.Assets.playerMissilePrefab,
            spawnPosition,
            Quaternion.LookRotation(fireDirection, Vector3.up));
        if (networkObject == null)
            return;

        var projectile = networkObject.GetComponent<CwslPlayerProjectile>();
        var attackPower = playerCharacter != null
            ? CwslCharacterStatCatalog.GetAttackPower(playerCharacter.CharacterId)
            : CwslGameConstants.AttackDamage;

        var ammo = forceSmokeBomb ? CwslMissileTankAmmoKind.Basic : ResolveCurrentAmmo();
        projectile?.ConfigureAdvanced(
            fireDirection,
            MissileSpeed,
            MissileLifetime,
            OwnerClientId,
            attackPower,
            NetworkObject,
            lockedTarget,
            ammo,
            forceSmokeBomb ? 0 : ResolveMaxPierceHits(),
            forceSmokeBomb);
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
    public void PlaySmokeZoneVisualClientRpc(Vector3 center, float radius, float duration)
    {
        var prefab = CwslGameSession.Instance?.Assets?.missileTankSmokeZoneVfx;
        if (prefab == null)
            return;

        CwslVfxSpawner.Spawn(prefab, center, Quaternion.identity, duration, radius * 0.55f);
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

    private bool CanAffordSkillGold(int amount)
    {
        if (!CwslGameConstants.SkillsConsumeGold)
            return true;

        if (pillBuff != null && pillBuff.CanAffordSkillGold(playerGold, amount))
            return true;

        return playerGold != null && playerGold.Gold >= amount;
    }

    private bool TrySpendSkillGold(int amount)
    {
        if (!CwslGameConstants.SkillsConsumeGold)
            return true;

        if (pillBuff != null && pillBuff.TrySpendSkillGold(playerGold, amount))
            return true;

        return playerGold != null && playerGold.TrySpendGoldServer(amount);
    }
}
