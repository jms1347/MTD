public static class CwslGameConstants
{
    public const string GameSceneName = "CwslGameScene";
    public const ushort GamePort = 7777;

    public const float MonsterMaxHealth = 1f;
    public const float PlayerMaxHealth = 100f;
    public const int StartingGold = 50;
    public const int SkillGoldCost = 1;
    public const int GoldDropMin = 1;
    public const int GoldDropMax = 10;
    public const float GoldDropSpreadRadius = 2.8f;
    public const float GoldCoinSpreadDuration = 0.48f;
    public const float GoldCoinClaimRadius = 0.9f;
    public const float GoldMagnetRadius = 4.5f;
    public const float GoldMagnetSpeed = 16f;
    public const float GoldPickupRadius = GoldCoinClaimRadius;
    public const int GiftGoldMinInterval = 1;
    public const float GiftGoldStartInterval = 0.5f;
    public const float GiftGoldMinIntervalSeconds = 0.05f;
    public const float GiftGoldAccelDuration = 3f;

    public const float BaseMoveSpeed = 6.5f;
    public const float RammerMaxSpeed = 17f;
    public const float RammerAccelPerSecond = 4.2f;
    public const float RammerDecelPerSecond = 9f;
    public const float RammerStopSpeed = 0.85f;
    public const float RammerDamageSpeedThreshold = 9.5f;
    public const float RammerCollisionDamage = 1f;
    public const float RammerCollisionCooldown = 0.45f;
    public const float RammerBrakeCooldown = 2.5f;
    public const float RammerBrakeTurnBoostDuration = 1.4f;
    public const float RammerWallStunDuration = 3f;
    public const float RammerWallStunMinSpeed = 5f;
    public const float RammerAllyStunCooldown = 1.2f;
    public const float AttackRange = 2.8f;
    public const float MissileTankRange = 24f;
    public const float AttackCooldown = 0.45f;
    public const float AttackDamage = 1f;
    public const int HudCanvasSortOrder = 100;
    public const int VisionOverlaySortOrder = 50;
    public const float RedMageMeteorScryRadius = 5.8f;
    public const float RedMageMeteorScryDuration = 2.8f;
    public const float PlayerArrowSpawnForwardOffset = 0.55f;
    public const float PlayerBulletSpawnMinOffset = 0.02f;
    public const float PlayerArrowMinHitDistance = 0.03f;
    public const float PlayerArrowMinHitDelay = 0.02f;
    public const float PlayerBulletHitRadius = 0.55f;
    public const float PlayerBulletHomingStrength = 14f;
    public const float MonsterHitCenterY = 1.05f;
    public const float MonsterHitHeight = 2.1f;
    public const float MonsterHitMinRadius = 0.58f;
    public const float PlayerBodyHitSlop = 0.1f;
    public const float PlayerBodyColliderRadiusDefault = 0.32f;
    public const float FortifyBodyScale = 1.12f;
    public const float FortifyShieldScale = 3.4f;
    public const float FortifyShieldBlockRadius = 2.8f;
    public const float FortifyShieldGrowSmoothTime = 0.52f;
    public const float FortifyShieldShrinkSmoothTime = 0.08f;

    public const string LayerGold = "CwslGold";

    /// <summary>38억 — 보스 등장 업보 (보스전은 추후 구현).</summary>
    public const long BossKarmaThreshold = 3_800_000_000L;

    public const float ArenaHalfExtent = 36f;
    public const float SpawnHeight = 0.5f;

    public const float SpawnIntervalSeconds = 2.25f;
    public const int MaxAliveMonsters = 40;
    public const float SuicideExplosionScale = 0.32f;

    public const string LayerPlayer = "CwslPlayer";
    public const string LayerMonster = "CwslMonster";
    public const string LayerProjectile = "CwslProjectile";
}
