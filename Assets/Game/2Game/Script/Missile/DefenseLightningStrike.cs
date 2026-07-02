using UnityEngine;

/// <summary>
/// 시트 스킬 번개 — ChainLightning 타워와 동일한 볼트·히트 VFX.
/// </summary>
public static class DefenseLightningStrike
{
    public static void Execute(
        DefenseSkillData skill,
        Vector3 origin,
        Transform target,
        float towerBaseDamage,
        string targetMobility,
        int linkDepth)
    {
        if (skill == null || target == null)
            return;

        var collider = target.GetComponent<Collider>();
        if (collider == null || !DefenseSkillExecutor.IsEnemyAttackable(collider, targetMobility))
            return;

        Vector3 hitPoint = DefenseCombatTargeting.ResolveEnemyAimPoint(target);
        PlayStrikeVfx(origin, hitPoint);

        float damage = towerBaseDamage * skill.damageMultiplier;
        MonsterStatusCombatResolver.ApplyDamageToEnemy(
            target.gameObject,
            damage,
            skill.element,
            hitPoint);

        DefenseSkillExecutor.ApplySplash(skill, hitPoint, damage, target, targetMobility);
        DefenseEffectApplicator.ApplySkillEffects(target.gameObject, skill, hitPoint);
        DefenseSkillExecutor.TryExecuteLinkedSkill(
            skill,
            hitPoint,
            target,
            towerBaseDamage,
            targetMobility,
            linkDepth);
    }

    public static void PlayStrikeVfx(Vector3 from, Vector3 to)
    {
        var catalog = DefenseCombatCatalog.Active;
        if (catalog == null)
            return;

        ChainLightningVisual.PlayBolt(from, to, catalog.chainBoltPrefab);

        if (catalog.chainHitExplosionPrefab == null)
            return;

        var hitFx = Object.Instantiate(catalog.chainHitExplosionPrefab, to, Quaternion.identity);
        Object.Destroy(hitFx, 2f);

        DefenseCombatVfxSpawn.TrySpawnLightningStrikeScorch(to);
    }
}
