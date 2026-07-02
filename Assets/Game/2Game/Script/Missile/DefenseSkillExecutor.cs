using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미사일(스킬) DB 기준 공격 실행.
/// </summary>
public static class DefenseSkillExecutor
{
    public const int MaxLinkedSkillDepth = 5;

    public static void ExecuteFromTower(
        int skillId,
        Vector3 origin,
        Transform target,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int linkDepth = 0)
    {
        if (skillId <= 0 || DataManager.Instance == null)
            return;

        if (!DataManager.Instance.TryGetSkill(skillId, out var skill))
            return;

        Execute(skill, origin, target, tower, targetMobility, linkDepth);
    }

    public static void ExecuteFromTower(
        int skillId,
        Vector3 origin,
        Transform target,
        float towerBaseDamage,
        float fallbackMissileSpeed,
        string targetMobility,
        int linkDepth = 0)
    {
        ExecuteFromTower(
            skillId,
            origin,
            target,
            DefenseTowerCombatContext.FromLegacy(towerBaseDamage, fallbackMissileSpeed),
            targetMobility,
            linkDepth);
    }

    public static void ExecuteFromTowerAtPoint(
        int skillId,
        Vector3 origin,
        Vector3 groundPoint,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int linkDepth = 0)
    {
        if (skillId <= 0 || DataManager.Instance == null)
            return;

        if (!DataManager.Instance.TryGetSkill(skillId, out var skill))
            return;

        ExecuteAtPoint(skill, origin, groundPoint, tower, targetMobility, linkDepth);
    }

    public static void ExecuteFromTowerAtPoint(
        int skillId,
        Vector3 origin,
        Vector3 groundPoint,
        float towerBaseDamage,
        float fallbackMissileSpeed,
        string targetMobility,
        int linkDepth = 0)
    {
        ExecuteFromTowerAtPoint(
            skillId,
            origin,
            groundPoint,
            DefenseTowerCombatContext.FromLegacy(towerBaseDamage, fallbackMissileSpeed),
            targetMobility,
            linkDepth);
    }

    public static void Execute(
        DefenseSkillData skill,
        Vector3 origin,
        Transform target,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int linkDepth = 0)
    {
        if (skill == null)
            return;

        switch (skill.skillType)
        {
            case DefenseSkillType.Missile:
                ExecuteMissile(skill, origin, target, tower, targetMobility, linkDepth);
                break;
        }
    }

    private static void ExecuteAtPoint(
        DefenseSkillData skill,
        Vector3 origin,
        Vector3 groundPoint,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int linkDepth)
    {
        if (skill == null)
            return;

        groundPoint = DefenseBallisticUtility.ProjectToGround(groundPoint);

        if (DefenseSkillCombatTable.IsVolcanoEruptionSkill(skill))
        {
            var context = DefenseSkillProjectileContext.Create(
                skill,
                tower,
                linkDepth,
                null,
                targetMobility);
            DefenseVolcanoEruption.FireFountainAroundSelf(origin, skill, context, targetMobility);
            return;
        }

        switch (skill.skillType)
        {
            case DefenseSkillType.Missile:
                ExecuteMissileAtPoint(skill, origin, groundPoint, tower, targetMobility, linkDepth);
                break;
        }
    }

    private static void ExecuteMissileAtPoint(
        DefenseSkillData skill,
        Vector3 origin,
        Vector3 groundPoint,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int linkDepth)
    {
        if (UsesDirectLightningStrike(skill))
        {
            var target = FindNearestEnemy(groundPoint, Mathf.Max(1.5f, skill.splashRadius), targetMobility);
            if (target != null)
                DefenseLightningStrike.Execute(skill, origin, target, tower.baseDamage, targetMobility, linkDepth);
            return;
        }

        switch (skill.moveType)
        {
            case DefenseMoveType.InstantHit:
                ApplyInstantHitAtPoint(skill, origin, groundPoint, tower, targetMobility, linkDepth);
                break;
            default:
                FireProjectileAtPoint(skill, origin, groundPoint, tower, targetMobility, linkDepth);
                break;
        }
    }

    private static void FireProjectileAtPoint(
        DefenseSkillData skill,
        Vector3 origin,
        Vector3 groundPoint,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int linkDepth)
    {
        if (MissilePoolManager.Instance == null)
            return;

        var prefab = DefenseSkillCombatTable.GetMissilePrefabForSkill(skill);
        if (prefab == null)
            return;

        float speed = DefenseSkillCombatTable.ResolveMissileSpeed(skill, tower.missileSpeed);
        groundPoint = DefenseBallisticUtility.ProjectToGround(groundPoint);
        Vector3 direction = groundPoint - origin;
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector3.forward;
        else
            direction.Normalize();

        bool useBallistic = skill.moveType == DefenseMoveType.Parabola || skill.moveType == DefenseMoveType.Fixed;
        float horizontal = Vector3.Distance(
            new Vector3(origin.x, 0f, origin.z),
            new Vector3(groundPoint.x, 0f, groundPoint.z));
        float arcHeight = Mathf.Clamp(horizontal * 0.42f + 2f, 3f, 16f);
        bool useSkyBurst = DefenseSkillCombatTable.UsesSkyBurstTargeting(skill);
        Vector3 arcTarget = useSkyBurst
            ? DefenseSkillCombatTable.ResolveSkyBurstPoint(origin, groundPoint, skill)
            : groundPoint;
        Vector3 velocity = useBallistic
            ? DefenseBallisticUtility.ComputeArcVelocity(
                origin,
                arcTarget,
                useSkyBurst ? 0.65f : arcHeight)
            : direction * speed;

        var lookRotation = velocity.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(velocity.normalized)
            : Quaternion.LookRotation(direction);

        var context = DefenseSkillProjectileContext.Create(
            skill,
            tower,
            linkDepth,
            null,
            targetMobility);
        context.ballisticLandPoint = groundPoint;
        context.hasBallisticLandPoint = useBallistic;
        if (useSkyBurst)
        {
            context.skyBurstPoint = arcTarget;
            context.hasSkyBurstPoint = true;
        }
        context.moveType = skill.moveType;
        float damage = context.ResolveDamage(skill);

        MissilePoolManager.Instance.SpawnWithSkill(
            prefab,
            origin,
            lookRotation,
            damage,
            velocity,
            skill.DamageElement,
            context);
    }

    private static void ApplyInstantHitAtPoint(
        DefenseSkillData skill,
        Vector3 origin,
        Vector3 groundPoint,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int linkDepth)
    {
        float damage = tower.baseDamage * skill.damageMultiplier;
        if (skill.element == DefenseSkillElement.Lightning)
            DefenseLightningStrike.PlayStrikeVfx(origin, groundPoint + Vector3.up * 0.45f);

        DefenseInstantHitVfx.PlayStrike(skill, origin, groundPoint);

        ApplySplash(skill, groundPoint, damage, null, targetMobility);
        TryExecuteLinkedSkill(skill, groundPoint, null, tower, targetMobility, linkDepth);
    }

    private static void ExecuteMissile(
        DefenseSkillData skill,
        Vector3 origin,
        Transform target,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int linkDepth)
    {
        if (DefenseSkillCombatTable.IsArcRepeaterChainSkill(skill))
        {
            DefenseLightningChainCast.ExecuteFromTower(
                origin,
                target,
                skill,
                tower,
                targetMobility,
                maxTargets: 3,
                playFirstHitSound: true);
            return;
        }

        if (DefenseSkillCombatTable.IsPoisonStingerSwarmSkill(skill))
        {
            DefensePoisonStingerSwarm.Fire(
                origin,
                target,
                skill,
                tower,
                targetMobility,
                linkDepth);
            return;
        }

        if (UsesDirectLightningStrike(skill))
        {
            DefenseLightningStrike.Execute(
                skill, origin, target, tower.baseDamage, targetMobility, linkDepth);
            return;
        }

        if (DefenseSkillPresentationCatalog.UsesSustainedBeamWithoutMissile(skill))
        {
            ApplySustainedBeamHit(skill, origin, target, tower, targetMobility, linkDepth);
            return;
        }

        if (DefenseSkillCombatTable.IsVolcanoEruptionSkill(skill))
        {
            DefenseVolcanoEruption.FireFountainAroundSelf(origin, skill, tower, targetMobility, linkDepth);
            return;
        }

        switch (skill.moveType)
        {
            case DefenseMoveType.InstantHit:
                ApplyInstantHit(skill, origin, target, tower, targetMobility, linkDepth);
                break;
            default:
                FireProjectile(skill, origin, target, tower, targetMobility, linkDepth);
                break;
        }
    }

    private static void FireProjectile(
        DefenseSkillData skill,
        Vector3 origin,
        Transform target,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int linkDepth)
    {
        if (MissilePoolManager.Instance == null)
            return;

        var prefab = DefenseSkillCombatTable.GetMissilePrefabForSkill(skill);
        if (prefab == null)
            return;

        float speed = DefenseSkillCombatTable.ResolveMissileSpeed(skill, tower.missileSpeed);
        Vector3 groundPoint = ResolveProjectileLandPoint(origin, target, speed, skill);
        Vector3 direction = groundPoint - origin;
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector3.forward;
        else
            direction.Normalize();

        bool useBallistic = skill.moveType == DefenseMoveType.Parabola || skill.moveType == DefenseMoveType.Fixed;
        float horizontal = Vector3.Distance(
            new Vector3(origin.x, 0f, origin.z),
            new Vector3(groundPoint.x, 0f, groundPoint.z));
        float arcHeight = Mathf.Clamp(horizontal * 0.42f + 2f, 3f, 16f);
        bool useSkyBurst = DefenseSkillCombatTable.UsesSkyBurstTargeting(skill);
        Vector3 arcTarget = useSkyBurst
            ? DefenseSkillCombatTable.ResolveSkyBurstPoint(origin, groundPoint, skill)
            : groundPoint;
        Vector3 velocity;
        Transform homingTarget = skill.isHoming && !useBallistic ? target : null;

        if (useBallistic)
            velocity = DefenseBallisticUtility.ComputeArcVelocity(
                origin,
                arcTarget,
                useSkyBurst ? 0.65f : arcHeight);
        else
            velocity = direction * speed;

        var lookRotation = velocity.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(velocity.normalized)
            : Quaternion.LookRotation(direction);

        var context = DefenseSkillProjectileContext.Create(
            skill,
            tower,
            linkDepth,
            homingTarget,
            targetMobility);
        context.ballisticLandPoint = groundPoint;
        context.hasBallisticLandPoint = useBallistic;
        if (useSkyBurst)
        {
            context.skyBurstPoint = arcTarget;
            context.hasSkyBurstPoint = true;
        }
        context.moveType = skill.moveType;
        float damage = context.ResolveDamage(skill);

        MissilePoolManager.Instance.SpawnWithSkill(
            prefab,
            origin,
            lookRotation,
            damage,
            velocity,
            skill.DamageElement,
            context);
    }

    private static Vector3 ResolveProjectileLandPoint(
        Vector3 origin,
        Transform target,
        float speed,
        DefenseSkillData skill)
    {
        var moveType = skill.moveType;
        if (target == null)
            return DefenseBallisticUtility.ProjectToGround(origin + Vector3.forward * Mathf.Max(6f, speed * 0.35f));

        if (moveType == DefenseMoveType.Parabola || moveType == DefenseMoveType.Fixed)
            return DefenseBallisticUtility.ProjectToGround(target.position);

        return DefenseCombatTargeting.ResolveEnemyAimPoint(target);
    }

    public static bool HasLinkedSkillSpawn(DefenseSkillData skill)
    {
        return LinkedSkillSpawner.CanSpawn(skill);
    }

    private static void ApplySustainedBeamHit(
        DefenseSkillData skill,
        Vector3 origin,
        Transform target,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int linkDepth)
    {
        if (target == null)
            return;

        float damage = tower.baseDamage * skill.damageMultiplier;
        int maxHits = Mathf.Max(1, skill.maxHit);
        float range = tower.attackRange > 0.05f ? tower.attackRange : 18f;

        Vector3 aimPoint = DefenseCombatTargeting.ResolveEnemyAimPoint(target);
        Vector3 direction = aimPoint - origin;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector3.forward;
        else
            direction.Normalize();

        var hits = CollectSustainedBeamTargets(origin, direction, range, maxHits, targetMobility);
        for (int i = 0; i < hits.Count; i++)
        {
            var enemy = hits[i];
            if (enemy == null)
                continue;

            Vector3 hitPoint = DefenseCombatTargeting.ResolveEnemyAimPoint(enemy);
            MonsterStatusCombatResolver.ApplyDamageToEnemy(
                enemy.gameObject,
                damage,
                skill.element,
                hitPoint);

            DefenseEffectApplicator.ApplySkillEffects(enemy.gameObject, skill, hitPoint);

            if (skill.splashRadius > 0.05f)
                ApplySplash(skill, hitPoint, damage, enemy, targetMobility);
        }

        if (hits.Count > 0)
        {
            var first = hits[0];
            Vector3 anchor = DefenseCombatTargeting.ResolveEnemyAimPoint(first);
            TryExecuteLinkedSkill(skill, anchor, first, tower, targetMobility, linkDepth);
        }
    }

    private static List<Transform> CollectSustainedBeamTargets(
        Vector3 origin,
        Vector3 direction,
        float range,
        int maxHits,
        string targetMobility)
    {
        var result = new List<Transform>(maxHits);
        var candidates = new List<(Transform transform, float distance)>();

        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float rangeSqr = range * range;
        const float beamHalfWidth = 1.35f;
        float beamHalfWidthSqr = beamHalfWidth * beamHalfWidth;

        foreach (var enemy in enemies)
        {
            if (!DefenseEnemyQuery.IsLivingEnemy(enemy, targetMobility: targetMobility))
                continue;

            Vector3 toEnemy = enemy.transform.position - origin;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > rangeSqr)
                continue;

            float distanceAlongBeam = Vector3.Dot(toEnemy, direction);
            if (distanceAlongBeam < 0f)
                continue;

            Vector3 closestOnBeam = origin + direction * distanceAlongBeam;
            var enemyFlat = new Vector3(enemy.transform.position.x, 0f, enemy.transform.position.z);
            var beamFlat = new Vector3(closestOnBeam.x, 0f, closestOnBeam.z);
            if ((enemyFlat - beamFlat).sqrMagnitude > beamHalfWidthSqr)
                continue;

            candidates.Add((enemy.transform, distanceAlongBeam));
        }

        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < candidates.Count && result.Count < maxHits; i++)
            result.Add(candidates[i].transform);

        return result;
    }

    private static void ApplyInstantHit(
        DefenseSkillData skill,
        Vector3 origin,
        Transform target,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int linkDepth)
    {
        if (target == null)
            return;

        var collider = target.GetComponent<Collider>();
        if (collider == null || !IsEnemyAttackable(collider, targetMobility))
            return;

        Vector3 hitPoint = DefenseCombatTargeting.ResolveEnemyAimPoint(target);
        float damage = tower.baseDamage * skill.damageMultiplier;

        if (skill.element == DefenseSkillElement.Lightning)
            DefenseLightningStrike.PlayStrikeVfx(origin, hitPoint);

        DefenseInstantHitVfx.PlayStrike(skill, origin, hitPoint);

        MonsterStatusCombatResolver.ApplyDamageToEnemy(
            target.gameObject,
            damage,
            skill.element,
            hitPoint);

        ApplySplash(skill, hitPoint, damage, target, targetMobility);
        DefenseEffectApplicator.ApplySkillEffects(target.gameObject, skill, hitPoint);
        TryExecuteLinkedSkill(skill, hitPoint, target, tower, targetMobility, linkDepth);
    }

    public static void ApplySplash(
        DefenseSkillData skill,
        Vector3 center,
        float damage,
        Transform excludeTarget,
        string targetMobility)
    {
        if (skill == null || skill.splashRadius <= 0f)
            return;

        float radius = skill.splashRadius;
        var overlaps = Physics.OverlapSphere(
            center,
            radius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlaps.Length; i++)
        {
            var overlap = overlaps[i];
            if (!overlap.CompareTag("Enemy"))
                continue;

            if (!IsEnemyAttackable(overlap, targetMobility))
                continue;

            if (excludeTarget != null && IsSameEnemyHierarchy(overlap.transform, excludeTarget))
                continue;

            if (!IsInsideHorizontalRadius(center, overlap.transform.position, radius))
                continue;

            var hitPoint = overlap.ClosestPoint(center);
            MonsterStatusCombatResolver.ApplyAoEDamageToEnemy(
                overlap.gameObject,
                damage,
                skill.element,
                hitPoint);
            DefenseEffectApplicator.ApplySkillEffects(overlap.gameObject, skill, center);
        }
    }

    private static bool IsInsideHorizontalRadius(Vector3 center, Vector3 worldPoint, float radius)
    {
        var flatCenter = new Vector3(center.x, 0f, center.z);
        var flatPoint = new Vector3(worldPoint.x, 0f, worldPoint.z);
        return (flatPoint - flatCenter).sqrMagnitude <= radius * radius;
    }

    private static bool IsSameEnemyHierarchy(Transform colliderTransform, Transform other)
    {
        if (colliderTransform == null || other == null)
            return false;

        if (colliderTransform == other || colliderTransform.root == other.root)
            return true;

        var enemyA = colliderTransform.GetComponentInParent<Monster>() ?? colliderTransform.GetComponent<Monster>();
        var enemyB = other.GetComponentInParent<Monster>() ?? other.GetComponent<Monster>();
        return enemyA != null && enemyA == enemyB;
    }

    public static void TryExecuteLinkedSkill(
        DefenseSkillData sourceSkill,
        Vector3 origin,
        Transform preferredTarget,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int linkDepth,
        float airBurstHeight = 0f)
    {
        if (sourceSkill == null || !sourceSkill.HasSummonPrefab)
            return;

        if (linkDepth >= MaxLinkedSkillDepth)
            return;

        Transform target = preferredTarget;
        if (target == null || !target.gameObject.activeInHierarchy)
            target = FindNearestEnemy(origin, 12f, targetMobility);

        LinkedSkillSpawner.TrySpawn(LinkedSkillSpawnContext.Create(
            sourceSkill,
            origin,
            target,
            tower,
            targetMobility,
            linkDepth + 1,
            airBurstHeight));
    }

    public static void TryExecuteLinkedSkill(
        DefenseSkillData sourceSkill,
        Vector3 origin,
        Transform preferredTarget,
        float towerBaseDamage,
        string targetMobility,
        int linkDepth,
        float fallbackMissileSpeed = 35f)
    {
        TryExecuteLinkedSkill(
            sourceSkill,
            origin,
            preferredTarget,
            DefenseTowerCombatContext.FromLegacy(towerBaseDamage, fallbackMissileSpeed),
            targetMobility,
            linkDepth);
    }

    public static Transform FindNearestEnemy(Vector3 origin, float range, string targetMobility)
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;
        float nearestSqr = range * range;

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

    public static bool IsEnemyAttackable(Collider enemyCollider, string targetMobility)
    {
        return DefenseEnemyQuery.IsAttackableCollider(enemyCollider, out _, targetMobility);
    }

    private static bool UsesDirectLightningStrike(DefenseSkillData skill)
    {
        if (skill == null || skill.element != DefenseSkillElement.Lightning)
            return false;

        if (DefenseSkillCombatTable.IsArcRepeaterChainSkill(skill)
            || DefenseSkillCombatTable.IsArcHomingMissileSkill(skill))
            return false;

        if (skill.moveType == DefenseMoveType.Parabola
            || skill.moveType == DefenseMoveType.Fixed
            || skill.moveType == DefenseMoveType.StormCloud)
            return false;

        // M열 소환 프리팹이 있으면 미사일 비행 후 명중 시 스폰합니다.
        if (skill.HasSummonPrefab)
            return false;

        return true;
    }
}
