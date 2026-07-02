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

        if (Time.time < nextFireTime)
            return;

        if (!session.TryGetPlayer(tankUnit.PlayerId, out var player))
            return;

        if (!ShouldAttack(player))
            return;

        if (!TryFindTarget(session, player, out var target))
            return;

        ApplyDamage(session, target, player);
        var fireInterval = Mathf.Max(0.5f, player.fireInterval * CoopSkillBuffs.GetFireIntervalMultiplier(player.playerId));
        nextFireTime = Time.time + fireInterval;
    }

    private static bool ShouldAttack(CoopPlayerState player)
        => player.orderType == CoopGameProtocol.OrderAttackMove
            || player.orderType == CoopGameProtocol.OrderAttackTarget;

    private bool TryFindTarget(CoopGameSession session, CoopPlayerState player, out GameObject target)
    {
        target = null;
        var origin = transform.position;
        var range = tankUnit.AttackRange;
        var best = range * range;

        if (player.orderType == CoopGameProtocol.OrderAttackTarget && player.attackTargetId >= 0)
        {
            if (TryFindEnemyById(player.attackTargetId, out target)
                && DefenseEnemyQuery.IsLivingEnemy(target))
                return true;

            return false;
        }

        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (!DefenseEnemyQuery.IsLivingEnemy(enemy))
                continue;

            var sqr = (enemy.transform.position - origin).sqrMagnitude;
            if (sqr > best)
                continue;

            best = sqr;
            target = enemy;
        }

        return target != null;
    }

    private void ApplyDamage(CoopGameSession session, GameObject target, CoopPlayerState player)
    {
        if (firePoint != null)
        {
            var look = target.transform.position - firePoint.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                firePoint.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        var damage = player.attack * CoopSkillBuffs.GetAttackMultiplier(player.playerId);
        var synced = target.GetComponent<CoopSyncedMonster>();
        if (synced != null)
        {
            synced.TakeDamage(damage, player.penetration, tankUnit.PlayerId);
            return;
        }

        var actor = target.GetComponent<CoopEnemyActor>();
        if (actor != null)
        {
            actor.TakeDamage(damage, player.penetration, tankUnit.PlayerId);
            return;
        }

        var health = target.GetComponent<Health>();
        if (health != null && health.IsAlive)
        {
            health.SetFlatDefenseReduction(player.penetration);
            health.TakeDamage(damage);
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
}
