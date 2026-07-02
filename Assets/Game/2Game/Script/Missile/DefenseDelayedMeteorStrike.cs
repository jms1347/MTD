using System.Collections;
using UnityEngine;

/// <summary>
/// 표식 탄 착지 지점에 경고 영역을 표시한 뒤 유성을 낙하시킵니다.
/// </summary>
public static class DefenseDelayedMeteorStrike
{
    private const float DefaultWarningDelay = 2.5f;
    private const float MeteorDropHeight = 20f;
    private const float MeteorFallSpeed = 36f;
    private const float MeteorDiagonalSpread = 0.55f;
    private const float ExplosionVisualScalePerRadius = 0.28f;

    private static readonly Color WarningColor = new Color(1f, 0.15f, 0.05f, 0.45f);

    public static void Schedule(
        Vector3 groundPoint,
        DefenseSkillData skill,
        DefenseSkillProjectileContext context)
    {
        if (skill == null)
            return;

        DefenseCombatSequenceHost.Ensure()
            .StartCoroutine(RunStrike(groundPoint, skill, context));
    }

    public static void DropMeteorAt(
        Vector3 groundPoint,
        DefenseSkillData skill,
        float damage,
        string targetMobility,
        DefenseSkillProjectileContext context)
    {
        if (skill == null)
            return;

        groundPoint = DefenseBallisticUtility.ProjectToGround(groundPoint);
        var strikeSkill = ResolveMeteorStrikeSkill(skill);
        float radius = ResolveStrikeRadius(strikeSkill);
        SpawnFallingMeteor(groundPoint, radius, damage, strikeSkill, targetMobility, context);
    }

    private static DefenseSkillData ResolveMeteorStrikeSkill(DefenseSkillData beaconSkill)
    {
        if (DefenseSkillCombatTable.TryResolveFollowUpSkill(beaconSkill, out var followUp))
            return followUp;

        if (DataManager.Instance != null
            && DataManager.Instance.TryGetSkillByCode(
                DefenseSkillCombatTable.MeteorStrikeSkillCode,
                out var strikeSkill))
        {
            return strikeSkill;
        }

        return beaconSkill;
    }

    private static float ResolveStrikeRadius(DefenseSkillData strikeSkill)
    {
        if (strikeSkill != null && strikeSkill.splashRadius > 0.05f)
            return strikeSkill.splashRadius;

        return 0.8f;
    }

    private static IEnumerator RunStrike(
        Vector3 groundPoint,
        DefenseSkillData skill,
        DefenseSkillProjectileContext context)
    {
        groundPoint = DefenseBallisticUtility.ProjectToGround(groundPoint);
        var strikeSkill = ResolveMeteorStrikeSkill(skill);
        float radius = ResolveStrikeRadius(strikeSkill);
        float damage = context.ResolveDamage(strikeSkill);
        string targetMobility = context.targetMobility;

        var warningZone = DefenseStrikeWarningZone.Create(groundPoint, radius, WarningColor);
        float warningDelay = DefaultWarningDelay;
        float elapsed = 0f;

        while (elapsed < warningDelay)
        {
            elapsed += Time.deltaTime;
            DefenseStrikeWarningZone.Pulse(warningZone, elapsed / warningDelay);
            yield return null;
        }

        DefenseStrikeWarningZone.DestroyZone(warningZone);
        SpawnFallingMeteor(groundPoint, radius, damage, strikeSkill, targetMobility, context);
    }

    private static void SpawnFallingMeteor(
        Vector3 strikeGround,
        float impactRadius,
        float damage,
        DefenseSkillData skill,
        string targetMobility,
        DefenseSkillProjectileContext context)
    {
        var catalog = DefenseCombatCatalog.Active ?? DefenseCombatCatalog.LoadFallback();
        var missilePrefab = catalog != null ? catalog.meteorMissilePrefab : null;
        var explosionPrefab = catalog != null ? catalog.meteorExplosionPrefab : null;

        if (missilePrefab == null
            && DefenseAddressableLoader.TryLoadMissile("NukeMissileFire", out missilePrefab))
        {
        }

        if (explosionPrefab == null
            && DefenseAddressableLoader.TryLoadEffect("NukeExplosionFire", out explosionPrefab))
        {
        }

        Vector3 towerOrigin = context.ToTowerCombatContext().towerSheetId > 0
            ? Vector3.zero
            : Vector3.zero;

        if (missilePrefab == null)
        {
            ImpactAt(strikeGround, impactRadius, damage, skill, targetMobility, explosionPrefab);
            return;
        }

        Vector3 spawnPosition = strikeGround + GetDiagonalSpawnOffset(strikeGround, towerOrigin);
        Vector3 fallDirection = (strikeGround - spawnPosition).normalized;
        var meteor = Object.Instantiate(missilePrefab, spawnPosition, Quaternion.LookRotation(fallDirection));

        var projectile = meteor.GetComponent<MeteorStrikeProjectile>();
        if (projectile == null)
            projectile = meteor.AddComponent<MeteorStrikeProjectile>();

        projectile.Launch(
            fallDirection * MeteorFallSpeed,
            0f,
            impactRadius,
            explosionPrefab,
            ExplosionVisualScalePerRadius,
            point => ImpactAt(point, impactRadius, damage, skill, targetMobility, explosionPrefab));
    }

    private static void ImpactAt(
        Vector3 groundPoint,
        float impactRadius,
        float damage,
        DefenseSkillData skill,
        string targetMobility,
        GameObject explosionPrefab)
    {
        SpawnMeteorExplosion(groundPoint, impactRadius, explosionPrefab);

        DefenseSkillExecutor.ApplySplash(skill, groundPoint, damage, null, targetMobility);
        DefenseCombatVfxSpawn.TrySpawnGroundFireMark(
            groundPoint,
            impactRadius,
            DefenseCombatVfxSpawn.DefaultFireGroundLifetime);
    }

    private static void SpawnMeteorExplosion(Vector3 groundPoint, float impactRadius, GameObject explosionPrefab)
    {
        if (explosionPrefab == null
            && !DefenseCombatVfxSpawn.TryLoadBurstPrefab("NukeExplosionFire", out explosionPrefab))
        {
            return;
        }

        if (explosionPrefab == null)
            return;

        var explosion = Object.Instantiate(
            explosionPrefab,
            groundPoint,
            DefenseCombatVfxSpawn.ResolveGroundBurstRotation(explosionPrefab));
        float scale = Mathf.Clamp(impactRadius * ExplosionVisualScalePerRadius, 0.55f, 1.05f);
        explosion.transform.localScale = Vector3.one * scale;
        DefenseCombatVfxSpawn.EnsureVisualOnly(explosion);
        Object.Destroy(explosion, 6f);
    }

    private static Vector3 GetDiagonalSpawnOffset(Vector3 strikeGround, Vector3 towerOrigin)
    {
        Vector3 away = strikeGround - towerOrigin;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
            away = Vector3.forward;

        away.Normalize();
        return away * (MeteorDropHeight * MeteorDiagonalSpread) + Vector3.up * MeteorDropHeight;
    }
}
