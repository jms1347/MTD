using UnityEngine;

public class CoopTankAttack : MonoBehaviour
{
    private CoopPlayerTowerUnit tankUnit;
    private Transform firePoint;
    private float nextFireTime;

    public void Initialize(CoopPlayerTowerUnit unit, Transform muzzle)
    {
        tankUnit = unit;
        firePoint = muzzle;
    }

    private void Update()
    {
        var session = CoopGameSession.Instance;
        if (session == null || !session.IsHostAuthority || tankUnit == null)
            return;

        if (!session.TryGetPlayer(tankUnit.PlayerId, out var player))
            return;

        if (player.towerHp <= 0f)
            return;

        var tankHealth = tankUnit.GetComponent<Health>();
        if (tankHealth != null && !tankHealth.IsAlive)
            return;

        if (!TryFindTarget(session, player, out var target))
            return;

        var aimPoint = DefenseCombatTargeting.ResolveEnemyAimPoint(target.transform);
        tankUnit.AimTurretAt(aimPoint);

        if (Time.time < nextFireTime)
            return;

        FireMissile(session, target, player, aimPoint);
        var fireInterval = Mathf.Max(0.35f, player.fireInterval * CoopSkillBuffs.GetFireIntervalMultiplier(player.playerId));
        nextFireTime = Time.time + fireInterval;
    }

    private bool TryFindTarget(CoopGameSession session, CoopPlayerState player, out GameObject target)
    {
        target = null;
        var origin = transform.position;
        var range = tankUnit.AttackRange;
        var best = range * range;

        if (player.orderType == CoopGameProtocol.OrderAttackTarget && player.attackTargetId >= 0)
        {
            if (TryFindEnemyById(player.attackTargetId, out target)
                && DefenseEnemyQuery.IsLivingEnemy(target)
                && IsWithinRange(origin, target.transform.position, range))
                return true;
        }

        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (!DefenseEnemyQuery.IsLivingEnemy(enemy))
                continue;

            var sqr = HorizontalDistanceSqr(origin, enemy.transform.position);
            if (sqr > best)
                continue;

            best = sqr;
            target = enemy;
        }

        return target != null;
    }

    private void FireMissile(CoopGameSession session, GameObject target, CoopPlayerState player, Vector3 aimPoint)
    {
        tankUnit.AimTurretAt(aimPoint, instant: true);

        var muzzle = firePoint != null ? firePoint : tankUnit.FirePoint;
        if (muzzle == null)
            muzzle = transform;

        if (!tankUnit.TryGetAimRotation(aimPoint, out var rotation))
            rotation = muzzle.rotation;

        muzzle.rotation = rotation;

        var damage = player.attack * CoopSkillBuffs.GetAttackMultiplier(player.playerId);
        CoopTankMissile.Fire(
            muzzle.position,
            rotation,
            target,
            damage,
            player.penetration,
            tankUnit.PlayerId,
            session);
    }

    public static void ApplyDamageToEnemy(
        CoopGameSession session,
        GameObject target,
        float damage,
        int penetration,
        string attackerPlayerId)
    {
        var synced = target.GetComponent<CoopSyncedMonster>();
        if (synced != null)
        {
            synced.TakeDamage(damage, penetration, attackerPlayerId);
            return;
        }

        var actor = target.GetComponent<CoopEnemyActor>();
        if (actor != null)
        {
            actor.TakeDamage(damage, penetration, attackerPlayerId);
            return;
        }

        var health = target.GetComponent<Health>();
        if (health != null && health.IsAlive)
        {
            health.SetFlatDefenseReduction(penetration);
            health.TakeDamage(Mathf.Max(1f, damage));
        }
    }

    private static bool TryFindEnemyById(int enemyId, out GameObject target)
    {
        target = null;
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            var synced = enemy.GetComponent<CoopSyncedMonster>();
            if (synced != null && synced.NetworkId == enemyId)
            {
                target = enemy;
                return true;
            }

            var actor = enemy.GetComponent<CoopEnemyActor>();
            if (actor != null && actor.NetworkId == enemyId)
            {
                target = enemy;
                return true;
            }
        }

        return false;
    }

    private static bool IsWithinRange(Vector3 origin, Vector3 targetPosition, float range)
        => HorizontalDistanceSqr(origin, targetPosition) <= range * range;

    private static float HorizontalDistanceSqr(Vector3 a, Vector3 b)
    {
        var flat = b - a;
        flat.y = 0f;
        return flat.sqrMagnitude;
    }
}
