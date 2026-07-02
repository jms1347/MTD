using UnityEngine;

/// <summary>
/// 타워·스킬 공격 시 적 조준점 계산.
/// </summary>
public static class DefenseCombatTargeting
{
    public const float DefaultBodyOffsetY = 0.45f;

    public static Vector3 ResolveEnemyAimPoint(Transform enemy)
    {
        if (enemy == null)
            return Vector3.zero;

        var collider = enemy.GetComponent<Collider>();
        if (collider != null)
            return collider.bounds.center;

        return enemy.position + Vector3.up * DefaultBodyOffsetY;
    }

    public static Vector3 ResolveGroundPoint(Transform enemy)
    {
        if (enemy == null)
            return Vector3.zero;

        var point = enemy.position;
        point.y = 0.05f;
        return point;
    }
}
