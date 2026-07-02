using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 독침 벌집 — 독침 3발이 적을 추적하다가 잠시 후 폭발합니다.
/// </summary>
public static class DefensePoisonStingerSwarm
{
    public const int StingerCount = 3;
    private const float ChaseFuseSeconds = 3f;
    private const float LaunchSpreadRadius = 0.25f;
    private const float LaunchUpBoost = 1.2f;

    public static void Fire(
        Vector3 origin,
        Transform primaryTarget,
        DefenseSkillData skill,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int linkDepth)
    {
        if (skill == null || MissilePoolManager.Instance == null)
            return;

        var prefab = DefenseSkillCombatTable.GetMissilePrefabForSkill(skill);
        if (prefab == null)
            return;

        var targets = CollectTargets(origin, tower.attackRange, targetMobility, primaryTarget, StingerCount);
        if (targets.Count == 0)
            return;

        float speed = DefenseSkillCombatTable.ResolveMissileSpeed(skill, tower.missileSpeed);
        float damage = tower.baseDamage * skill.damageMultiplier;

        for (int i = 0; i < StingerCount; i++)
        {
            var target = targets[i % targets.Count];
            if (target == null || !target.gameObject.activeInHierarchy)
                continue;

            Vector2 spread = Random.insideUnitCircle * LaunchSpreadRadius;
            Vector3 spawnPos = origin + new Vector3(spread.x, LaunchUpBoost, spread.y);
            Vector3 aimPoint = DefenseCombatTargeting.ResolveEnemyAimPoint(target);
            Vector3 direction = aimPoint - spawnPos;
            if (direction.sqrMagnitude < 0.01f)
                direction = Vector3.forward;
            else
                direction.Normalize();

            var context = DefenseSkillProjectileContext.Create(
                skill,
                tower,
                linkDepth,
                target,
                targetMobility);
            context.homingTarget = target;
            context.expDuration = ChaseFuseSeconds;

            MissilePoolManager.Instance.SpawnWithSkill(
                prefab,
                spawnPos,
                Quaternion.LookRotation(direction),
                damage,
                direction * speed,
                skill.DamageElement,
                context);
        }
    }

    private static List<Transform> CollectTargets(
        Vector3 origin,
        float range,
        string targetMobility,
        Transform primaryTarget,
        int maxCount)
    {
        var result = new List<Transform>(maxCount);
        if (primaryTarget != null && primaryTarget.gameObject.activeInHierarchy)
            result.Add(primaryTarget);

        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float rangeSqr = range * range;

        foreach (var enemy in enemies)
        {
            if (!DefenseEnemyQuery.IsLivingEnemy(enemy, targetMobility: targetMobility))
                continue;

            if (primaryTarget != null && enemy.transform == primaryTarget)
                continue;

            float sqr = (enemy.transform.position - origin).sqrMagnitude;
            if (sqr > rangeSqr)
                continue;

            result.Add(enemy.transform);
            if (result.Count >= maxCount)
                break;
        }

        return result;
    }
}
