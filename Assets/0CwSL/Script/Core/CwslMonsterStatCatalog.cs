using UnityEngine;

/// <summary>몬스터 타입별 HP·공격·방어 스탯. 정예 HP 배율은 CwslMonsterManager + Configure healthMultiplier.</summary>
public static class CwslMonsterStatCatalog
{
    public const float GlobalMoveSpeedMultiplier = 0.68f;

    public const float MeleeBaseHealth = 170f;
    public const float MeleeMoveSpeed = 3.2f;
    public const float MeleeAttackPower = 24f;
    public const float MeleeDefense = 12f;

    public const float RangedBaseHealth = 100f;
    public const float RangedMoveSpeed = 2.5f;
    public const float RangedProjectileDamage = 20f;
    public const float RangedDefense = 0f;

    public const float SuicideBaseHealth = 115f;
    public const float SuicideMoveSpeed = 2.9f;
    public const float SuicideExplosionDamage = 48f;
    public const float SuicideExplosionRadius = 2.5f;
    public const float SuicideFuseIgniteDistance = 5.5f;
    public const float SuicideNexusExplosionDamage = 70f;

    public const float MidBossBaseHealth = 550f;
    public const float MidBossAttackPower = 38f;
    public const float MidBossDefense = 28f;
    public const float MidBossMoveSpeed = 2.2f;

    public const float SeniorCoachBaseHealth = 500f;
    public const float SeniorCoachAttackPower = 28f;
    public const float SeniorCoachDefense = 22f;
    public const float SeniorCoachMoveSpeed = 2.8f;

    public const float DefenseBossBaseHealth = 750f;
    public const float DefenseBossMoveSpeed = 1.85f;
    public const float DefenseBossDefense = 35f;
    public const float DefenseBossSlamDamage = 52f;
    public const float DefenseBossMissileDamage = 28f;

    public const float BossHongmyeongboHealth = 12000f;
    public const float BossHongmyeongboMoveSpeed = 3.4f;
    public const float BossHongmyeongboSlamDamage = 120f;
    public const float BossHongmyeongboRingDamage = 85f;

    public static float GetMaxHealth(CwslMonsterType type)
    {
        return type switch
        {
            CwslMonsterType.Ranged => RangedBaseHealth,
            CwslMonsterType.InkSniper or CwslMonsterType.NexusInkSniper => RangedBaseHealth,
            CwslMonsterType.Suicide or CwslMonsterType.StickySuicide => SuicideBaseHealth,
            CwslMonsterType.NexusMelee or CwslMonsterType.NexusRanged or CwslMonsterType.NexusSuicide
                or CwslMonsterType.NexusInkSniper => MeleeBaseHealth,
            CwslMonsterType.MidBoss => MidBossBaseHealth,
            CwslMonsterType.SeniorCoach => SeniorCoachBaseHealth,
            CwslMonsterType.DefenseBoss => DefenseBossBaseHealth,
            CwslMonsterType.BossHongmyeongbo => BossHongmyeongboHealth,
            _ => MeleeBaseHealth
        };
    }

    public static float GetDefense(CwslMonsterType type)
    {
        return type switch
        {
            CwslMonsterType.Ranged or CwslMonsterType.InkSniper => RangedDefense,
            CwslMonsterType.NexusRanged or CwslMonsterType.NexusInkSniper => 8f,
            CwslMonsterType.Suicide or CwslMonsterType.StickySuicide or CwslMonsterType.NexusSuicide => 0f,
            CwslMonsterType.NexusMelee => 16f,
            CwslMonsterType.MidBoss => MidBossDefense,
            CwslMonsterType.SeniorCoach => SeniorCoachDefense,
            CwslMonsterType.DefenseBoss => DefenseBossDefense,
            CwslMonsterType.BossHongmyeongbo => 45f,
            _ => MeleeDefense
        };
    }

    public static float GetMoveSpeed(CwslMonsterType type, float nexusSpeedMultiplier = 0.72f, float midBossSpeedMultiplier = 0.667f)
    {
        return type switch
        {
            CwslMonsterType.Ranged => RangedMoveSpeed,
            CwslMonsterType.InkSniper => RangedMoveSpeed,
            CwslMonsterType.NexusRanged => RangedMoveSpeed * nexusSpeedMultiplier,
            CwslMonsterType.NexusInkSniper => RangedMoveSpeed * nexusSpeedMultiplier,
            CwslMonsterType.Suicide => SuicideMoveSpeed,
            CwslMonsterType.NexusSuicide => SuicideMoveSpeed * nexusSpeedMultiplier,
            CwslMonsterType.NexusMelee => MeleeMoveSpeed * nexusSpeedMultiplier,
            CwslMonsterType.KoreaUniversitySoldier => MeleeMoveSpeed,
            CwslMonsterType.StickySuicide => SuicideMoveSpeed,
            CwslMonsterType.MidBoss => MeleeMoveSpeed * midBossSpeedMultiplier,
            CwslMonsterType.SeniorCoach => SeniorCoachMoveSpeed,
            CwslMonsterType.DefenseBoss => DefenseBossMoveSpeed,
            CwslMonsterType.BossHongmyeongbo => BossHongmyeongboMoveSpeed,
            _ => MeleeMoveSpeed
        };
    }

    public static float GetMeleeAttackPower(CwslMonsterType type)
    {
        return type switch
        {
            CwslMonsterType.NexusMelee => MeleeAttackPower + 2f,
            CwslMonsterType.MidBoss => MidBossAttackPower,
            CwslMonsterType.SeniorCoach => SeniorCoachAttackPower,
            CwslMonsterType.KoreaUniversitySoldier => MeleeAttackPower + 4f,
            _ => MeleeAttackPower
        };
    }

    public static float GetRangedAttackPower(CwslMonsterType type)
    {
        return type is CwslMonsterType.NexusRanged or CwslMonsterType.NexusInkSniper
            ? RangedProjectileDamage + 2f
            : RangedProjectileDamage;
    }
}
