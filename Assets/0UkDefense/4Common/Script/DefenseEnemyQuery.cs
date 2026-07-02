using UnityEngine;

/// <summary>
/// 살아 있는 적 탐색·피격 판정 공통 헬퍼.
/// </summary>
public static class DefenseEnemyQuery
{
    public static bool TryGetEnemyRoot(Collider collider, out GameObject enemyRoot)
    {
        enemyRoot = null;
        if (collider == null)
            return false;

        var monster = collider.GetComponentInParent<Monster>();
        if (monster != null)
            enemyRoot = monster.gameObject;
        else if (collider.CompareTag("Enemy"))
            enemyRoot = collider.gameObject;
        else
            return false;

        return enemyRoot != null && enemyRoot.CompareTag("Enemy");
    }

    public static bool IsLivingEnemy(
        GameObject enemy,
        bool requireLanded = false,
        string targetMobility = null)
    {
        if (enemy == null || !enemy.activeInHierarchy)
            return false;

        var health = enemy.GetComponent<Health>();
        if (health == null || !health.IsAlive)
            return false;

        var monster = enemy.GetComponent<Monster>();
        if (requireLanded && monster != null && !monster.IsLanded)
            return false;

        if (!string.IsNullOrWhiteSpace(targetMobility)
            && monster != null
            && !DefenseTargetMobilityUtility.CanTarget(targetMobility, monster))
            return false;

        return true;
    }

    public static bool IsAttackableCollider(
        Collider collider,
        out GameObject enemyRoot,
        string targetMobility = null,
        bool requireLanded = false)
    {
        enemyRoot = null;
        if (!TryGetEnemyRoot(collider, out enemyRoot))
            return false;

        return IsLivingEnemy(enemyRoot, requireLanded, targetMobility);
    }
}
