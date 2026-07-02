using UnityEngine;

/// <summary>
/// 구름·공중 미사일 등 정전기 낙뢰 공통 로직.
/// </summary>
public static class DefenseStormStrikeLogic
{
    public const float HoverHeightAboveGround = 3.2f;

    public static Vector3 ResolveHoverPosition(Vector3 groundPoint)
    {
        return new Vector3(groundPoint.x, groundPoint.y + HoverHeightAboveGround, groundPoint.z);
    }

    public static Transform ResolveStrikeTarget(in LinkedSkillSpawnContext context, Vector3 anchorFlatOrigin)
    {
        var targetMobility = context.ResolveTargetMobility();
        var radius = context.ResolveStrikeRadius();

        if (TryResolveAnchorTarget(context.anchorTarget, anchorFlatOrigin, radius, targetMobility, out var anchored))
            return anchored;

        var nearest = DefenseSkillExecutor.FindNearestEnemy(
            new Vector3(anchorFlatOrigin.x, 0f, anchorFlatOrigin.z),
            radius,
            targetMobility);

        if (nearest == null || !nearest.gameObject.activeInHierarchy)
            return null;

        if (!IsInsideHorizontalRange(anchorFlatOrigin, nearest.position, radius))
            return null;

        var collider = nearest.GetComponentInChildren<Collider>();
        if (collider != null && !DefenseSkillExecutor.IsEnemyAttackable(collider, targetMobility))
            return null;

        return nearest;
    }

    public static void StrikeEnemy(
        in LinkedSkillSpawnContext context,
        Vector3 anchorFlatOrigin,
        Vector3 lightningOrigin,
        Transform enemyTransform)
    {
        var skill = context.SourceSkill;
        if (enemyTransform == null || skill == null)
            return;

        var enemyCollider = enemyTransform.GetComponentInChildren<Collider>();
        if (enemyCollider == null)
            return;

        Vector3 hitPoint = DefenseCombatTargeting.ResolveEnemyAimPoint(enemyTransform);
        PlayLightningStrike(lightningOrigin, hitPoint);

        MonsterStatusCombatResolver.ApplyDamageToEnemy(
            enemyCollider.gameObject,
            context.ResolveDamage(),
            context.ResolveElement(),
            hitPoint);

        DefenseEffectApplicator.ApplySkillEffects(enemyCollider.gameObject, skill, anchorFlatOrigin);
    }

    public static void PlayLightningStrike(Vector3 origin, Vector3 hitPoint)
    {
        if (DefenseCombatCatalog.Active != null)
        {
            DefenseLightningStrike.PlayStrikeVfx(origin, hitPoint);
        }
        else if (TryLoadEffectPrefab(DefenseStormCloud.LightningEffectKey, out var tallFx) && tallFx != null)
        {
            var fx = Object.Instantiate(tallFx, hitPoint, Quaternion.identity);
            Object.Destroy(fx, DefenseStormCloud.LightningFxLifetime);
        }
        else if (TryLoadEffectPrefab(DefenseStormCloud.FallbackLightningEffectKey, out var boltPrefab) && boltPrefab != null)
        {
            ChainLightningVisual.PlayBolt(origin, hitPoint, boltPrefab, 0.35f);
        }

        if (DefenseCombatCatalog.Active == null)
            DefenseCombatVfxSpawn.TrySpawnLightningStrikeScorch(hitPoint);
    }

    private static bool TryResolveAnchorTarget(
        Transform candidate,
        Vector3 anchorFlatOrigin,
        float radius,
        string targetMobility,
        out Transform valid)
    {
        valid = null;
        if (candidate == null || !candidate.gameObject.activeInHierarchy)
            return false;

        if (!IsInsideHorizontalRange(anchorFlatOrigin, candidate.position, radius))
            return false;

        var collider = candidate.GetComponentInChildren<Collider>();
        if (collider != null && !DefenseSkillExecutor.IsEnemyAttackable(collider, targetMobility))
            return false;

        valid = candidate;
        return true;
    }

    private static bool IsInsideHorizontalRange(Vector3 anchorFlatOrigin, Vector3 enemyPosition, float radius)
    {
        var enemyFlat = new Vector3(enemyPosition.x, 0f, enemyPosition.z);
        var anchorFlat = new Vector3(anchorFlatOrigin.x, 0f, anchorFlatOrigin.z);
        return (enemyFlat - anchorFlat).sqrMagnitude <= radius * radius;
    }

    private static bool TryLoadEffectPrefab(string key, out GameObject prefab)
    {
        prefab = null;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (DefenseAddressableLoader.TryLoadEffect(key, out prefab) && prefab != null)
            return true;

        return DefenseAddressableLoader.TryLoadPrefab(key, out prefab) && prefab != null;
    }
}
