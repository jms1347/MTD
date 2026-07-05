public static class CwslGameConstants
{
    public const string GameSceneName = "CwslGameScene";
    public const ushort GamePort = 7777;
    public const int MaxPlayers = 5;

    public const float MonsterMaxHealth = 1f;
    public const float PlayerMaxHealth = 100f;
    public const int StartingGold = 50;
    public const int SkillGoldCost = 1;

    // 골드 드롭 (실제 1 = UI 100만 원)
    public const int GoldDropNormal = 1;
    public const int GoldDropExecutive = 10;
    public const float ExecutiveSpawnChance = 0.05f;
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
    public const float RammerWingSpreadMaxScale = 2.8f;
    public const float RammerWingSpreadGrowSeconds = 1.55f;
    public const int RammerWingSpreadStartGoldCost = 3;
    public const float RammerWingSpreadGoldIntervalSeconds = 0.5f;
    public const int RammerWingSpreadTickGoldCost = 1;
    public const float RammerWingSpreadBaseRadius = 0.72f;
    public const float RammerWingSpreadDamage = 1f;
    public const float RammerWingSpreadHitCooldown = 0.38f;
    public const float RammerWingSpreadMinScaleForDamage = 1.12f;
    public const float RammerWallStunDuration = 3f;
    public const float RammerWallStunMinSpeed = 5f;
    public const float RammerAllyStunCooldown = 1.2f;
    public const float GatherReferenceRadius = 4.8f;
    public const float GatherMaxRadius = GatherReferenceRadius * 2f;
    public const float GatherMinRadius = 1.4f;
    public const float GatherChargeSeconds = 1.35f;
    public const int GatherStartGoldCost = 5;
    public const float GatherPullSeconds = 0.48f;
    public const float GatherCooldown = 4f;
    public const float GatherSlowMultiplier = 0.32f;
    public const float GatherSlowRefreshSeconds = 0.45f;
    public const int GatherSlowGoldPerTarget = 1;
    public const float GatherSlowGoldIntervalSeconds = 0.5f;
    public const float AttackRange = 2.8f;
    public const float MissileTankRange = 24f;
    public const float AttackCooldown = 0.45f;
    public const float AttackDamage = 1f;
    public const int TankHitGoldCost = 1;
    public const int MissileDualWieldGoldCost = 3;
    public const int MeteorGoldCost = 5;
    public const int HudCanvasSortOrder = 100;
    public const int VisionOverlaySortOrder = 50;
    public const float BlindVisionRadius = 2.8f;
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

    /// <summary>UI 38억 = 실제 업보 3,800 (1업보 = 100만 원 표기).</summary>
    public const long BossKarmaThreshold = 3_800L;
    public const long CheatKarmaIncrement = 100L; // TODO(릴리즈): U키 치트용 — 정식 버전 전 제거

    public const float BossMaxHealth = 380f;
    public const float BossPhase2Hp = 285f;
    public const float BossPhase3Hp = 190f;
    public const float BossPhase4Hp = 95f;
    public const float BossPhase1TeleportCooldown = 15f;
    public const float BossPhase2BallInterval = 8f;
    public const int BossPhase2BallCountMin = 2;
    public const int BossPhase2BallCountMax = 3;
    public const int TeamBallGoldSteal = 50;
    public const float BossFightZoneHpDrainPerSecond = 2f;
    public const float BossFinalPhaseDuration = 38f;

    public const float ArenaHalfExtent = 36f;
    public const float SpawnHeight = 0.5f;

    // --- 아레나 기믹 ---
    public const float FightZoneCenterX = -18f;
    public const float FightZoneCenterZ = 18f;
    public const float FightZoneHalfSize = 14f;
    public const float FightZoneEnemySpeedMultiplier = 1.5f;

    public const float TeamBallSpeed = 15f;
    public const float TeamBallPathLength = 68f;
    public const float TeamBallRadius = 0.72f;
    public const float TeamBallDamage = 10f;
    public const float TeamBallHitCooldown = 0.4f;
    public const float TeamBallTrailWidth = 1.8f;
    public const float TeamBallPeriodicInterval = 15f;
    public const float TeamBallBossInterval = 26f;

    public const float BossWatchCooldown = 120f;
    public const float BossWatchDuration = 10f;
    public const float BossTeleportCooldown = 32f;
    public const float BossTeleportCastSeconds = 1.1f;

    public const long KarmaMilestoneShake1 = 1_000L;
    public const long KarmaMilestoneShake2 = 2_000L;
    public const long KarmaMilestoneShake3 = 3_000L;
    public const long KarmaSilhouetteThreshold = 3_500L;
    public const long KarmaPressConferenceThreshold = 3_750L;
    public const float PressConferenceRadius = 5f;
    public const float PressConferenceDuration = 120f;
    public const int PressConferenceSkillGoldPenalty = 3;

    public const int LighthouseCount = 8;
    public const float LighthouseRadius = 3.2f;
    public const float LighthouseVisionBonus = 7f;
    public const float LighthouseActivateSeconds = 5f;
    public const float LighthouseDuration = 30f;

    public const float FogVortexCenterX = 22f;
    public const float FogVortexCenterZ = -14f;
    public const float FogVortexRadius = 11f;
    public const float FogVortexCenterX2 = -24f;
    public const float FogVortexCenterZ2 = -20f;

    public const float BlackHoleZoneCenterX = 24f;
    public const float BlackHoleZoneCenterZ = 20f;
    public const float BlackHoleZoneHalfSize = 9f;
    public const float BlackHolePullSpeed = 2.6f;

    public const float KarmaHalfZoneCenterX = -22f;
    public const float KarmaHalfZoneCenterZ = -22f;
    public const float KarmaHalfZoneHalfSize = 10f;
    public const float KarmaHalfMultiplier = 0.5f;

    public const float TianyuanRadius = 9f;
    public const float TianyuanBossDamageBonus = 0.3f;
    public const int TianyuanOutsideSkillCostPenalty = 1;

    public const int TrapPadCount = 6;
    public const float TrapPadRadius = 1.85f;
    public const float TrapPadCooldownSeconds = 42f;
    public const int TrapPadSpawnMin = 5;
    public const int TrapPadSpawnMax = 10;
    public const float TrapPadSpawnSpread = 16.5f;

    public const float GoBoardFadeStartKarma = 3_000L;

    // --- 함정 / 광역 이벤트 ---
    public const int FakeGoldMaxAlive = 4;
    public const float FakeGoldRespawnInterval = 22f;
    public const int FakeGoldSuicideSpawnMin = 3;
    public const int FakeGoldSuicideSpawnMax = 5;
    public const float FakeGoldSpawnMinDistance = 11f;
    public const float FakeGoldSpawnMaxDistance = 19f;

    public const int DonationPadCount = 2;
    public const float DonationPadRadius = 1.75f;
    public const float DonationPadCooldownSeconds = 55f;

    public const int BadGrassPatchCount = 8;
    public const float BadGrassPatchRadius = 2.9f;
    public const float BadGrassSlowMultiplier = 0.3f;

    public const float OffsideLaserIntervalSeconds = 48f;
    public const float OffsideLaserWarningSeconds = 3f;
    public const float OffsideBlindDurationSeconds = 5f;
    public const float OffsideLineHitWidth = 1.35f;

    public const float RandomEventIntervalMin = 20f;
    public const float RandomEventIntervalMax = 20f;
    public const int MeteorShowerHitCount = 5;
    public const int MeteorShowerGoldPerHit = 2;
    public const float MeteorShowerWarningSeconds = 3.5f;
    public const float MeteorShowerWarningRadius = 3.4f;
    public const float MeteorShowerFallDelay = 0.55f;
    public const float MeteorHazardPadChance = 0.38f;
    public const float MeteorHazardPadDelaySeconds = 2f;
    public const float MeteorHazardPadRadius = 2.75f;
    public const float MeteorHazardPadDurationSeconds = 18f;
    public const float LightningModeWarningSeconds = 3f;
    public const float LightningModeDurationSeconds = 6f;
    public const float LightningStrikeIntervalSeconds = 0.42f;
    public const float LightningStrikeRadius = 2.6f;
    public const float LightningModeZoneRadius = 13f;
    public const float LightningOrbHeight = 2.2f;
    public const float LightningMissileSpeed = 24f;
    public const float LightningStunDurationSeconds = 1.6f;

    public const int HazardPadMaxAlive = 6;
    public const float HazardPadInitialDelaySeconds = 12f;
    public const float HazardPadSpawnIntervalMin = 18f;
    public const float HazardPadSpawnIntervalMax = 32f;
    public const float HazardPadDurationSeconds = 22f;
    public const float HazardPadRadius = 3.2f;
    public const float HazardPadMinSeparation = 5.5f;
    public const float HazardAcidDamagePerSecond = 8f;
    public const float HazardLavaGoldLeakIntervalSeconds = 0.85f;
    public const int HazardLavaGoldLeakAmount = 1;
    public const float HazardWaterSlowMultiplier = 0.45f;

    public const float SpawnIntervalSeconds = 2.25f;
    public const int MaxAliveMonsters = 40;
    public const float SuicideExplosionScale = 0.32f;

    public const string LayerPlayer = "CwslPlayer";
    public const string LayerMonster = "CwslMonster";
    public const string LayerProjectile = "CwslProjectile";
}
