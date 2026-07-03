using Unity.Netcode;
using UnityEngine;

public class CwslRangedMonster : CwslMonsterBase
{
    private const float PreferredRange = 11f;
    private const float MinRange = 8f;
    private const float FireCooldown = 2.1f;
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
        if (!IsValidTarget(currentTarget))
            return;

        var aimPoint = GetAimPoint(currentTarget);
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
            MoveToward(transform.position + away.normalized * 2f);
        }
        else
        {
            FaceTarget(aimPoint);
        }

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f)
            return;

        fireTimer = FireCooldown;
        FireProjectileServer(aimPoint);
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
        projectile?.Configure(fireDirection, 14f, 8f);
    }
}
