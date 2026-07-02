using UnityEngine;

/// <summary>
/// TurretPivot(포신)을 사거리 내 적을 향해 회전합니다.
/// 포물선 스킬은 탄도 각도만큼 포신을 들어 올립니다.
/// </summary>
public class DefenseTowerAimController : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 480f;

    private Transform turretPivot;
    private Transform muzzlePoint;
    private float attackRange;
    private string targetMobility;
    private bool useParabolicAim;
    private bool aimEnabled = true;

    public void Configure(Transform pivot, float range, string mobility = null)
    {
        turretPivot = pivot;
        attackRange = range;
        targetMobility = mobility;
        muzzlePoint = DefenseTowerVisualBuilder.FindFirePoint(transform);
        useParabolicAim = false;
        aimEnabled = true;
    }

    public void ConfigureFromTower(TowerController tower)
    {
        if (tower == null)
            return;

        turretPivot = DefenseTowerVisualBuilder.FindTurretPivot(tower.transform);
        muzzlePoint = tower.MuzzlePoint;
        attackRange = tower.AttackRange;
        targetMobility = tower.TargetMobility;
        useParabolicAim = tower.UsesParabolicAim;
        aimEnabled = !tower.TryGetActiveSkill(out var skill)
            || !DefenseSkillCombatTable.IsVolcanoEruptionSkill(skill);
    }

    private void LateUpdate()
    {
        if (!aimEnabled || turretPivot == null)
            return;

        Transform target = FindNearestEnemy();
        if (target == null)
            return;

        RotateToward(DefenseCombatTargeting.ResolveEnemyAimPoint(target));
    }

    private void RotateToward(Vector3 worldPoint)
    {
        Quaternion? targetRotation = useParabolicAim
            ? ResolveParabolicRotation(worldPoint)
            : ResolveFlatRotation(worldPoint);

        if (!targetRotation.HasValue)
            return;

        turretPivot.rotation = Quaternion.RotateTowards(
            turretPivot.rotation,
            targetRotation.Value,
            rotateSpeed * Time.deltaTime);
    }

    private Quaternion? ResolveFlatRotation(Vector3 worldPoint)
    {
        Vector3 flat = worldPoint - turretPivot.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            return null;

        return Quaternion.LookRotation(flat.normalized, Vector3.up);
    }

    private Quaternion? ResolveParabolicRotation(Vector3 worldPoint)
    {
        Vector3 origin = muzzlePoint != null ? muzzlePoint.position : turretPivot.position;
        Vector3 landPoint = DefenseBallisticUtility.ProjectToGround(worldPoint);

        float horizontal = Vector3.Distance(
            new Vector3(origin.x, 0f, origin.z),
            new Vector3(landPoint.x, 0f, landPoint.z));
        float arcHeight = Mathf.Clamp(horizontal * 0.42f + 2f, 3f, 16f);
        Vector3 velocity = DefenseBallisticUtility.ComputeArcVelocity(origin, landPoint, arcHeight);
        if (velocity.sqrMagnitude < 0.0001f)
            return ResolveFlatRotation(worldPoint);

        return Quaternion.LookRotation(velocity.normalized, Vector3.up);
    }

    private Transform FindNearestEnemy()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;
        float nearestSqr = attackRange * attackRange;
        Vector3 origin = transform.position;

        foreach (var enemy in enemies)
        {
            if (!DefenseEnemyQuery.IsLivingEnemy(enemy, targetMobility: targetMobility))
                continue;

            float sqr = (enemy.transform.position - origin).sqrMagnitude;
            if (sqr > nearestSqr)
                continue;

            nearestSqr = sqr;
            nearest = enemy.transform;
        }

        return nearest;
    }
}
