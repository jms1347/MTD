using UnityEngine;

/// <summary>
/// 타워 GameObject에서 사정거리를 조회합니다.
/// </summary>
public static class DefenseTowerCombatRange
{
    private const float MinRange = 0.5f;

    public static bool TryGetRange(Transform tower, out float range)
    {
        range = 0f;
        if (tower == null)
            return false;

        if (tower.TryGetComponent<TowerController>(out var standard))
        {
            range = standard.AttackRange;
            return range >= MinRange;
        }

        if (tower.TryGetComponent<ChainLightningTowerController>(out var chain))
        {
            range = chain.AttackRange;
            return range >= MinRange;
        }

        if (tower.TryGetComponent<MeteorTowerController>(out var meteor))
        {
            range = meteor.TargetingRange;
            return range >= MinRange;
        }

        return false;
    }

    public static void EnsurePickCollider(GameObject tower)
    {
        if (tower == null)
            return;

        var collider = tower.GetComponent<BoxCollider>();
        if (collider == null)
            collider = tower.AddComponent<BoxCollider>();

        collider.isTrigger = true;
        collider.center = new Vector3(0f, 0.55f, 0f);
        collider.size = new Vector3(1.05f, 1.1f, 1.05f);
    }

    public static Transform ResolveTowerRoot(Collider collider)
    {
        if (collider == null)
            return null;

        var current = collider.transform;
        while (current != null)
        {
            if (current.CompareTag("Tower") || HasTowerCombatComponent(current))
                return current;

            current = current.parent;
        }

        return null;
    }

    private static bool HasTowerCombatComponent(Transform transform)
    {
        return transform.GetComponent<TowerController>() != null
            || transform.GetComponent<ChainLightningTowerController>() != null
            || transform.GetComponent<MeteorTowerController>() != null;
    }
}
