using UnityEngine;

/// <summary>몬스터 타입별 HP·공격·이동 스탯 (스케일업 밸런스).</summary>
public static class CwslMonsterStatCatalog
{
    /// <summary>전체 몬스터 이동 속도 배율.</summary>
    public const float GlobalMoveSpeedMultiplier = 0.68f;

    public const float MeleeBaseHealth = 50f;
    public const float MeleeMoveSpeed = 3.2f;
    public const float MeleeAttackPower = 12f;

    public const float RangedBaseHealth = 40f;
    public const float RangedMoveSpeed = 2.5f;
    public const float RangedProjectileDamage = 15f;

    public const float SuicideBaseHealth = 120f;
    public const float SuicideMoveSpeed = 2.9f;
    public const float SuicideExplosionDamage = 80f;
    public const float SuicideExplosionRadius = 2.5f;
    public const float SuicideNexusExplosionDamage = 100f;

    public const float NexusVariantHealth = 150f;
    public const float MidBossHealth = 1500f;
    public const float MidBossAttackPower = 40f;
    public const float MidBossMoveSpeed = 2.2f;

    public const float DefenseBossHealth = 3000f;
    public const float DefenseBossMoveSpeed = 1.85f;
    public const float DefenseBossSlamDamage = 80f;
    public const float DefenseBossMissileDamage = 40f;

    public const float BossHongmyeongboHealth = 15000f;
    public const float BossHongmyeongboMoveSpeed = 3.4f;
    public const float BossHongmyeongboSlamDamage = 150f;
    public const float BossHongmyeongboRingDamage = 100f;

    public static float GetMaxHealth(CwslMonsterType type)
    {
        return type switch
        {
            CwslMonsterType.Ranged => RangedBaseHealth,
            CwslMonsterType.Suicide => SuicideBaseHealth,
            CwslMonsterType.NexusMelee or CwslMonsterType.NexusRanged or CwslMonsterType.NexusSuicide => NexusVariantHealth,
            CwslMonsterType.MidBoss => MidBossHealth,
            CwslMonsterType.DefenseBoss => DefenseBossHealth,
            CwslMonsterType.BossHongmyeongbo => BossHongmyeongboHealth,
            _ => MeleeBaseHealth
        };
    }

    public static float GetMoveSpeed(CwslMonsterType type, float nexusSpeedMultiplier = 0.72f, float midBossSpeedMultiplier = 0.667f)
    {
        return type switch
        {
            CwslMonsterType.Ranged => RangedMoveSpeed,
            CwslMonsterType.NexusRanged => RangedMoveSpeed * nexusSpeedMultiplier,
            CwslMonsterType.Suicide => SuicideMoveSpeed,
            CwslMonsterType.NexusSuicide => SuicideMoveSpeed * nexusSpeedMultiplier,
            CwslMonsterType.NexusMelee => MeleeMoveSpeed * nexusSpeedMultiplier,
            CwslMonsterType.MidBoss => MeleeMoveSpeed * midBossSpeedMultiplier,
            CwslMonsterType.DefenseBoss => DefenseBossMoveSpeed,
            CwslMonsterType.BossHongmyeongbo => BossHongmyeongboMoveSpeed,
            _ => MeleeMoveSpeed
        };
    }

    public static float GetMeleeAttackPower(CwslMonsterType type)
    {
        return type == CwslMonsterType.MidBoss ? MidBossAttackPower : MeleeAttackPower;
    }
}
