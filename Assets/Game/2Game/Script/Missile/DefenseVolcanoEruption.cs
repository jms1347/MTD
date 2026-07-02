using System.Collections;
using UnityEngine;

/// <summary>
/// 화산식 — 타워 주변으로 분수처럼 포물선 잔탄을 뿜고, 낙하 중 적 발견 시 직선 추격·지면 착지 시 폭발 연출을 냅니다.
/// </summary>
public static class DefenseVolcanoEruption
{
    private const float MainBurstScalePerSplash = 0.17f;
    private const float MainBurstScaleMin = 0.32f;
    private const float MainBurstScaleMax = 0.46f;
    private const float DefaultScatterRadiusMultiplier = 1.45f;
    private const float FallInterceptRangeMultiplier = 2f;
    private const float FallInterceptSpeedMultiplier = 3f;
    private const int DefaultFountainShotCount = 6;
    private const float DefaultRockSplashRadius = 1f;
    private const float DefaultRockVisualScale = 0.58f;
    private const string DefaultRockMissileKey = "NukeFireOBJ";

    public static void FireFountainAroundSelf(
        Vector3 towerOrigin,
        DefenseSkillData skill,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int linkDepth)
    {
        if (skill == null)
            return;

        var context = DefenseSkillProjectileContext.Create(
            skill,
            tower,
            linkDepth,
            null,
            targetMobility);

        FireFountainAroundSelf(towerOrigin, skill, context, targetMobility);
    }

    public static void FireFountainAroundSelf(
        Vector3 towerOrigin,
        DefenseSkillData skill,
        DefenseSkillProjectileContext context,
        string targetMobility)
    {
        if (skill == null)
            return;

        Vector3 eruptCenter = DefenseBallisticUtility.ProjectToGround(towerOrigin);
        PlayCenterBurst(eruptCenter, skill, context, targetMobility);

        if (MissilePoolManager.Instance == null)
            return;

        if (!TryResolveRockSkill(skill, out var rockSkill))
            return;

        if (!TryResolveRockPrefab(rockSkill, out var rockPrefab))
            return;

        float scatterRadius = Mathf.Max(1.8f, skill.splashRadius * DefaultScatterRadiusMultiplier);
        float interceptSearchBase = context.towerAttackRange > 0.05f
            ? context.towerAttackRange
            : scatterRadius;
        float fallInterceptSearchRange = rockSkill.isHoming
            ? interceptSearchBase * FallInterceptRangeMultiplier
            : 0f;
        float rockDamage = context.ResolveDamage(rockSkill);
        int shotCount = rockSkill.maxHit > 0
            ? rockSkill.maxHit
            : DefaultFountainShotCount;
        float rockSplash = rockSkill.splashRadius > 0.05f
            ? rockSkill.splashRadius
            : DefaultRockSplashRadius;
        float rockVisualScale = rockSkill.expDuration > 0.05f
            ? Mathf.Clamp(rockSkill.expDuration, 0.4f, 0.85f)
            : DefaultRockVisualScale;

        MissilePoolManager.Instance.StartCoroutine(
            LaunchFountainShots(
                towerOrigin,
                eruptCenter,
                scatterRadius,
                fallInterceptSearchRange,
                shotCount,
                rockDamage,
                rockSplash,
                rockVisualScale,
                rockSkill,
                rockPrefab,
                targetMobility));
    }

    private static void PlayCenterBurst(
        Vector3 eruptCenter,
        DefenseSkillData skill,
        DefenseSkillProjectileContext context,
        string targetMobility)
    {
        float burstScale = Mathf.Clamp(
            skill.splashRadius * MainBurstScalePerSplash,
            MainBurstScaleMin,
            MainBurstScaleMax);

        DefenseCombatVfxSpawn.TrySpawnGroundBurst("GasExplosionFire", eruptCenter, 1.1f, burstScale);
        DefenseCombatVfxSpawn.TrySpawnGroundFireMark(
            eruptCenter,
            skill.splashRadius,
            DefenseCombatVfxSpawn.DefaultFireGroundLifetime);

        DefenseSkillExecutor.ApplySplash(
            skill,
            eruptCenter,
            context.ResolveDamage(skill),
            null,
            targetMobility);
    }

    private static bool TryResolveRockSkill(DefenseSkillData mainSkill, out DefenseSkillData rockSkill)
    {
        if (DefenseSkillCombatTable.TryResolveFollowUpSkill(mainSkill, out rockSkill))
            return true;

        if (DataManager.Instance != null
            && DataManager.Instance.TryGetSkillByCode("M-F-0004R", out rockSkill))
        {
            return true;
        }

        rockSkill = null;
        return false;
    }

    private static bool TryResolveRockPrefab(DefenseSkillData rockSkill, out GameObject rockPrefab)
    {
        rockPrefab = DefenseSkillCombatTable.GetMissilePrefabForSkill(rockSkill);
        if (rockPrefab != null)
            return true;

        var key = DefenseSkillCombatTable.ResolveMissilePrefabKey(rockSkill);
        if (!string.IsNullOrWhiteSpace(key)
            && DefenseAddressableLoader.TryLoadMissile(key, out rockPrefab)
            && rockPrefab != null)
        {
            return true;
        }

        return DefenseAddressableLoader.TryLoadMissile(DefaultRockMissileKey, out rockPrefab)
            && rockPrefab != null;
    }

    private static IEnumerator LaunchFountainShots(
        Vector3 towerOrigin,
        Vector3 eruptCenter,
        float scatterRadius,
        float fallInterceptSearchRange,
        int shotCount,
        float rockDamage,
        float rockSplashRadius,
        float rockVisualScale,
        DefenseSkillData rockSkill,
        GameObject rockPrefab,
        string targetMobility)
    {
        float angleStep = shotCount > 0 ? 360f / shotCount : 360f;
        float spinOffset = Random.Range(0f, 360f);

        for (int i = 0; i < shotCount; i++)
        {
            float yaw = spinOffset + angleStep * i + Random.Range(-10f, 10f);
            LaunchBallisticShot(
                towerOrigin,
                eruptCenter,
                yaw,
                scatterRadius,
                rockDamage,
                rockSplashRadius,
                rockVisualScale,
                rockSkill,
                rockPrefab,
                rockSkill.isHoming,
                fallInterceptSearchRange,
                targetMobility);

            yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));
        }
    }

    private static void LaunchBallisticShot(
        Vector3 towerOrigin,
        Vector3 eruptCenter,
        float yaw,
        float scatterRadius,
        float rockDamage,
        float rockSplashRadius,
        float rockVisualScale,
        DefenseSkillData rockSkill,
        GameObject rockPrefab,
        bool enableFallIntercept,
        float fallInterceptSearchRange,
        string targetMobility)
    {
        float landDistance = Random.Range(scatterRadius * 0.55f, scatterRadius);
        Vector3 landOffset = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * landDistance;
        Vector3 landPoint = eruptCenter + landOffset;

        float horizontal = HorizontalDistance(towerOrigin, landPoint);
        float arcHeight = enableFallIntercept
            ? Mathf.Clamp(horizontal * 0.28f + Random.Range(5.2f, 8.4f), 6.5f, 14f)
            : Mathf.Clamp(horizontal * 0.34f + Random.Range(2.4f, 4.2f), 3.5f, 9f);
        Vector3 velocity = DefenseBallisticUtility.ComputeArcVelocity(towerOrigin, landPoint, arcHeight);
        var lookRotation = velocity.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(velocity.normalized)
            : Quaternion.identity;

        MissilePoolManager.Instance.SpawnScatterRock(
            rockPrefab,
            towerOrigin,
            lookRotation,
            rockDamage,
            velocity,
            landPoint,
            rockSplashRadius,
            rockVisualScale,
            rockSkill,
            enableFallIntercept,
            eruptCenter,
            fallInterceptSearchRange,
            targetMobility,
            enableFallIntercept ? FallInterceptSpeedMultiplier : 1f);
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
