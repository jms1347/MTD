using Unity.Netcode;
using UnityEngine;

public class CwslRangedMonster : CwslMonsterBase
{
    private const float PreferredRange = 11f;
    private const float MinRange = 8f;
    private const float DefaultFireCooldown = 2.1f;
    private const float AimHeight = 1.05f;

    private float fireTimer;
    private CwslRangedCannonAim cannonAim;

    public override void Initialize(CwslMonsterType type)
    {
        base.Initialize(type);
        EnsureCannonAim();
    }

    protected override void TickServerAI()
    {
        if (IsValidWallTarget(currentWallTarget))
        {
            var wallStand = GetCombatStandDistance();
            var wallDistance = GetFlatDistanceToCombatPosition();
            if (wallDistance > wallStand)
                MoveToward(GetTargetMovePosition());
            else
            {
                FaceTarget(GetTargetFacePosition());
                fireTimer -= Time.deltaTime;
                if (fireTimer <= 0f)
                {
                    fireTimer = GetFireCooldown();
                    currentWallTarget.DamageServer(GetScaledDamage(CwslMonsterStatCatalog.RangedProjectileDamage));
                }
            }

            return;
        }

        if (!IsValidTarget(currentTarget))
            return;

        var aimPoint = GetTargetAimPoint();
        cannonAim?.SetAimServer(aimPoint);

        var distance = GetFlatDistanceTo(currentTarget);

        if (distance > PreferredRange)
        {
            MoveToward(currentTarget.transform.position);
            return;
        }

        if (distance < MinRange)
        {
            var away = transform.position - currentTarget.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = -transform.forward;

            var fleeTarget = CwslArenaUtility.ClampToPlayArea(
                transform.position + away.normalized * 2f,
                GetMovementClampRadius());
            MoveToward(fleeTarget);
        }
        else
        {
            FaceTarget(aimPoint);
        }

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f)
            return;

        fireTimer = GetFireCooldown();
        FireProjectileServer(aimPoint);
    }

    protected virtual float GetFireCooldown() => DefaultFireCooldown;

    protected virtual CwslMonsterProjectileKind GetProjectileKind() => CwslMonsterProjectileKind.TankBullet;

    protected virtual void PlayFireFx(Vector3 muzzlePosition, Vector3 fireDirection)
    {
        var rotation = fireDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(fireDirection.normalized, Vector3.up)
            : transform.rotation;
        CwslVfxSpawner.SpawnRangedTankMuzzleFlash(muzzlePosition, rotation);
    }

    [ClientRpc]
    private void PlayFireFxClientRpc(Vector3 muzzlePosition, Vector3 fireDirection)
    {
        PlayFireFx(muzzlePosition, fireDirection);
    }

    private void EnsureCannonAim()
    {
        cannonAim = GetComponent<CwslRangedCannonAim>();
        if (cannonAim == null)
            cannonAim = gameObject.AddComponent<CwslRangedCannonAim>();
    }

    private void FaceTarget(Vector3 aimPoint)
    {
        var dir = CwslTargetQuery.GetFlatDirection(transform.position, aimPoint);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * 12f);
    }

    private static Vector3 GetAimPoint(NetworkObject target)
    {
        return target.transform.position + Vector3.up * AimHeight;
    }

    private Vector3 GetTargetAimPoint()
    {
        var nexus = currentTarget.GetComponent<CwslNexus>();
        if (nexus != null)
            return nexus.GetAimPoint();

        return GetAimPoint(currentTarget);
    }

    private void FireProjectileServer(Vector3 aimPoint)
    {
        var session = CwslGameSession.Instance;
        if (session == null || session.Assets.projectilePrefab == null)
            return;

        var muzzle = cannonAim != null ? cannonAim.GetMuzzlePosition() : transform.position + Vector3.up * 1.1f + transform.forward * 0.8f;
        var fireDirection = aimPoint - muzzle;
        if (fireDirection.sqrMagnitude < 0.0001f)
            fireDirection = transform.forward;
        else
            fireDirection.Normalize();

        var networkObject = CwslNetworkPoolService.Instance?.Get(
            session.Assets.projectilePrefab,
            muzzle,
            Quaternion.LookRotation(fireDirection, Vector3.up));
        if (networkObject == null)
            return;

        var projectile = networkObject.GetComponent<CwslMonsterProjectile>();
        var damage = GetScaledDamage(CwslMonsterStatCatalog.RangedProjectileDamage);
        projectile?.Configure(fireDirection, 14f, 8f, damage, GetProjectileKind());
        PlayFireFxClientRpc(muzzle, fireDirection);
    }
}
