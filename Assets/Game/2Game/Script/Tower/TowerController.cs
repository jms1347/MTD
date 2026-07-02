using UnityEngine;

public class TowerController : MonoBehaviour
{
    [SerializeField] private DefenseMissileId missileId = DefenseMissileId.Physical;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackRange = 18f;
    [SerializeField] private float fireInterval = 1.2f;
    [SerializeField] private float missileSpeed = 35f;
    [SerializeField] private float missileDamage = 1f;
    [SerializeField] private int towerSheetId;
    [SerializeField] private int skillId;
    [SerializeField] private string targetMobility = DefenseTargetMobilityUtility.GroundLabel;

    private float nextFireTime;
    private bool useSheetCombat;

    public void Initialize(DefenseMissileId id, Transform muzzlePoint)
    {
        missileId = id;
        firePoint = muzzlePoint;
        useSheetCombat = false;

        if (TowerStatsManager.Instance != null)
            TowerStatsManager.Instance.ApplyTo(this);
    }

    public void Initialize(GameObject prefab, Transform muzzlePoint)
    {
        firePoint = muzzlePoint;
        useSheetCombat = false;
        DefenseMissileResolver.TryResolveId(prefab, out missileId);

        if (TowerStatsManager.Instance != null)
            TowerStatsManager.Instance.ApplyTo(this);
    }

    public void InitializeFromSheet(int sheetTowerId, Transform muzzlePoint)
    {
        firePoint = muzzlePoint;
        towerSheetId = sheetTowerId;
        useSheetCombat = false;

        if (DataManager.Instance != null &&
            DataManager.Instance.TryGetTower(sheetTowerId, out var towerData))
        {
            useSheetCombat = towerData.skillId > 0;
            skillId = towerData.skillId;
            if (useSheetCombat &&
                DataManager.Instance.TryGetSkill(towerData.skillId, out var skill))
            {
                attackRange = DefenseSkillPresentationCatalog.ResolveAttackRange(skill, towerData.attackRange);
            }
            else
            {
                attackRange = towerData.attackRange;
            }
            fireInterval = Mathf.Max(0.05f, towerData.fireInterval);
            missileDamage = towerData.baseDamage;
            targetMobility = string.IsNullOrWhiteSpace(towerData.targetMobility)
                ? DefenseTargetMobilityUtility.GroundLabel
                : towerData.targetMobility;
        }
        else if (TowerStatsManager.Instance != null)
        {
            TowerStatsManager.Instance.ApplyTo(this);
        }
    }

    public void ApplyStats(StandardTowerStats stats)
    {
        if (stats == null || useSheetCombat)
            return;

        missileDamage = stats.attackDamage;
        attackRange = stats.attackRange;
        fireInterval = stats.fireInterval;
        missileSpeed = stats.missileSpeed;
    }

    public float AttackRange => attackRange;
    public string TargetMobility => targetMobility;
    public float FireCooldownRemaining => Mathf.Max(0f, nextFireTime - Time.time);

    public bool TryGetActiveSkill(out DefenseSkillData skill)
    {
        skill = null;
        if (!useSheetCombat || skillId <= 0 || DataManager.Instance == null)
            return false;

        return DataManager.Instance.TryGetSkill(skillId, out skill);
    }

    public Transform MuzzlePoint => firePoint;

    public bool UsesParabolicAim
    {
        get
        {
            if (useSheetCombat && skillId > 0 &&
                DataManager.Instance != null &&
                DataManager.Instance.TryGetSkill(skillId, out var skill))
            {
                return skill.moveType == DefenseMoveType.Parabola
                    || skill.moveType == DefenseMoveType.Fixed;
            }

            return !useSheetCombat;
        }
    }

    private void Update()
    {
        if (Time.time < nextFireTime)
            return;

        if (useSheetCombat && skillId <= 0)
            return;

        if (!useSheetCombat && MissilePoolManager.Instance == null)
            return;

        if (UsesSelfVolcanoBurst())
        {
            FireVolcanoBurst();
            nextFireTime = Time.time + fireInterval;
            return;
        }

        Transform target = FindNearestEnemy();
        if (target == null)
            return;

        FireAt(target);
        nextFireTime = Time.time + fireInterval;
    }

    private bool UsesSelfVolcanoBurst()
    {
        return useSheetCombat
            && skillId > 0
            && DataManager.Instance != null
            && DataManager.Instance.TryGetSkill(skillId, out var skill)
            && DefenseSkillCombatTable.IsVolcanoEruptionSkill(skill);
    }

    private void FireVolcanoBurst()
    {
        if (!useSheetCombat || skillId <= 0 || DataManager.Instance == null)
            return;

        Vector3 spawnPos = GetMuzzleWorldPosition();
        DefenseSkillExecutor.ExecuteFromTower(
            skillId,
            spawnPos,
            null,
            new DefenseTowerCombatContext
            {
                towerSheetId = towerSheetId,
                baseDamage = missileDamage,
                fireInterval = fireInterval,
                attackRange = attackRange,
                missileSpeed = missileSpeed
            },
            targetMobility,
            0);
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

    private void FireAt(Transform target)
    {
        Vector3 spawnPos = GetMuzzleWorldPosition();

        if (useSheetCombat && skillId > 0 &&
            DataManager.Instance != null &&
            DataManager.Instance.TryGetSkill(skillId, out var skill))
        {
            DefenseSkillExecutor.ExecuteFromTower(
                skillId,
                spawnPos,
                target,
                new DefenseTowerCombatContext
                {
                    towerSheetId = towerSheetId,
                    baseDamage = missileDamage,
                    fireInterval = fireInterval,
                    attackRange = attackRange,
                    missileSpeed = missileSpeed
                },
                targetMobility,
                0);
            return;
        }

        if (MissilePoolManager.Instance == null)
            return;

        Vector3 aimPoint = DefenseCombatTargeting.ResolveEnemyAimPoint(target);

        Vector3 direction = aimPoint - spawnPos;
        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        var lookRotation = Quaternion.LookRotation(direction.normalized);
        Vector3 velocity = lookRotation * Vector3.forward * missileSpeed;

        MissilePoolManager.Instance.Spawn(missileId, spawnPos, lookRotation, missileDamage, velocity);
    }

    private Vector3 GetMuzzleWorldPosition()
    {
        if (firePoint != null)
            return firePoint.position;

        return transform.TransformPoint(new Vector3(0f, 0.75f, 0.55f));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

