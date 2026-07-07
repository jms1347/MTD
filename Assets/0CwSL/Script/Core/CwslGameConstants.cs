using UnityEngine;

public static class CwslGameConstants
{
    public const string GameSceneName = "CwslGameScene";
    public const string LobbySceneName = "LobbyScene";
    public const int LobbyPort = 7777;
    public const ushort GameNetcodePort = 7778;
    public const ushort GamePort = GameNetcodePort;
    public const int MaxPlayers = 5;

    public const int PoolHighChurnInitialSize = 100;
    public const int PoolHighChurnExpandSize = 16;
    public const int PoolBossInitialSize = 4;
    public const int PoolBossExpandSize = 2;
    public const int PoolFallbackInitialSize = 32;
    public const int PoolFallbackExpandSize = 8;

    public const float NexusDefaultHealth = 20000f;
    public const float NexusTeamVisionRadius = 11f;
    public const float EnemyBaseDefaultHealth = 3000f;

    public const float MonsterMaxHealth = 50f;
    public const float PlayerMaxHealth = 600f;
    public const float PlayerAttackDamage = 15f;
    public const float PlayerDefense = 15f;
    public const float PlayerVisionRadius = 16f;
    public const float PlayerMaxStamina = 100f;
    public const float PlayerStaminaRegenPerSecond = 5f;
    public const float SkillCooldownMultiplier = 2f;
    public const int SkillsPerCharacter = 4;
    public const float DefaultSkillStaminaCost = 22f;
    public const int StartingGold = 50;
    public const int SkillGoldCost = 1;

    // 골드 드롭 (실제 1 = UI 100만 원)
    public const int GoldDropNormal = 1;
    public const int GoldDropExecutive = 10;
    public const int MonsterGoldDropMin = 0;
    public const int MonsterGoldDropMax = 3;
    public const float PillDropChance = 0.14f;
    public const float PillBuffDurationSeconds = 10f;
    public const float PillYellowHealDurationSeconds = 10f;
    public const float PillBlueSpeedMultiplier = 1.4f;
    public const float PillGreenHealRatio = 0.2f;
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
    public const float ReviveProximityRadius = 2.6f;

    public const float BaseMoveSpeed = 6.5f;
    public const float RammerMaxSpeed = 17f;
    public const float RammerAccelPerSecond = 4.1f;
    public const float RammerDecelPerSecond = 8.2f;
    public const float RammerSteerTurnRateHigh = 390f;
    public const float RammerSteerTurnRateLow = 62f;
    public const float RammerSteerTurnSpeedExponent = 0.82f;
    public const float RammerSteerDirectionSnap = 0.15f;
    public const float RammerSteerRpcIntervalSeconds = 0.07f;
    public const float RammerArrivalDistance = 0.3f;
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
    public const float RammerWallStunDuration = 2f;
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
    public const float MissileTankPowerBoostDuration = 10f;
    public const float MissileTankPowerBoostSpeedMultiplier = 2f;
    public const int MissileTankPowerBoostMaxPierce = 99;
    public const float MissileTankSmokeDashDistance = 4.2f;
    public const float MissileTankSmokeDashDuration = 0.24f;
    public const float MissileTankSmokeZoneRadius = 3.6f;
    public const float MissileTankSmokeZoneDuration = 5f;
    public const float MissileTankSmokeStunDuration = 5f;
    public const float AttackCooldown = 0.45f;
    public const float AttackDamage = PlayerAttackDamage;
    public const int TankHitGoldCost = 1;
    public const int MissileDualWieldGoldCost = 3;
    public const int MeteorGoldCost = 5;
    public const int HudCanvasSortOrder = 100;
    public const int VisionOverlaySortOrder = 50;
    public const float InkBlindDurationSeconds = 3f;

    public const float BlindVisionRadius = 2.8f;
    public const float RedMageMeteorScryRadius = 5.8f;
    public const float RedMageMeteorScryDuration = 2.8f;

    public const float RedMageFrozenOrbSpeed = 8f;
    public const float RedMageFrozenOrbRange = 22f;
    public const float RedMageFrozenOrbCastDuration = 0.4f;
    public const float RedMageFrozenOrbShardSpeed = 16f;
    public const float RedMageFrozenOrbShardEmitInterval = 0.05f;
    public const float RedMageFrozenOrbShardDamageRatio = 0.24f;
    public const float RedMageFrozenOrbShardLifetime = 0.7f;
    public const float RedMageFrozenOrbScaleDrainPerShot = 0.03f;
    public const int RedMageFrozenOrbEmitDirections = 8;
    public const int RedMageFrozenOrbFrostStacks = 2;
    public const float RedMageFrozenOrbFrostDuration = 2.5f;

    public const float RedMageFrozenOrbHitRadius = 0.62f;
    public const float RedMageFrozenOrbGroundTrailInterval = 0.2f;
    public const float RedMageFrozenOrbGroundTrailMinDistance = 0.42f;
    public const float RedMageLightningOrbVisualScale = 1.9f;
    public const float RedMageLightningOrbTravelSpeed = 4.5f;
    public const float RedMageLightningOrbTravelDistance = 15f;
    public const float RedMageLightningOrbStrikeInterval = 0.35f;
    public const float RedMageLightningOrbStrikeRadius = 3.4f;
    public const float RedMageLightningOrbGroundRadiusVfxDiameterDivisor = 8f;
    public const float RedMageLightningOrbStrikeDamageRatio = 0.42f;
    public const float RedMageLightningOrbForwardDistance = 3.2f;
    public const float RedMageLightningOrbHeight = 1.4f;
    public const float RedMageLightningOrbLifetime = 1.2f;
    public const float RedMageLightningOrbChargeSeconds = 0.18f;
    public const float RedMageLightningOrbCastDuration = 0.45f;
    public const float RedMageLightningChainRadius = 4.5f;
    public const int RedMageLightningChainMaxHits = 5;
    public const float RedMageLightningShockDuration = 1.6f;

    public const float RedMageTeleportDistance = 7f;
    public const float RedMageTeleportCastDuration = 0.35f;
    public const float RedMageTeleportArrivalDelay = 0.2f;
    public const float RedMageTeleportDepartPortalLifetime = 0.3f;
    public const float RedMageTeleportArrivePortalLifetime = 0.55f;
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
    public const float FortifyEmpoweredAttackDamageMultiplier = 3f;
    public const float FortifyEmpoweredAttackRadius = 3.2f;

    public const float TankShieldDashRaiseTime = 0.14f;
    public const float TankShieldDashSlamDownTime = 0.11f;
    public const float TankShieldDashRecoverTime = 0.22f;
    public const float TankShieldDashDistance = 5.5f;
    public const float TankShieldDashDuration = 0.24f;
    public const float TankShieldDashCastDuration = 1f;
    public const float TankShieldDashSpeed = 24f;
    public const float TankShieldDashPushRadius = 1.2f;
    public const float TankShieldDashPushDistance = 6.5f;
    public const float TankShieldDashPushDuration = 0.3f;
    public const float TankShieldDashEmpoweredPushDistance = 9.5f;
    public const float TankShieldDashEmpoweredPushDuration = 0.38f;
    public const float TankShieldDashShieldPushDistance = 6.8f;
    public const float TankShieldDashShieldPushDuration = 0.36f;

    public const float TankSkillEmpowerRadiusMultiplier = 3f;
    public const float TankSkillEmpowerPowerMultiplier = 3f;

    public const float TankShieldSlamRadius = 3.2f;
    public const float TankShieldSlamStunDuration = 2f;
    public const float TankShieldSlamWindup = 0.52f;
    public const float TankShieldSlamSlamDownTime = 0.12f;
    public const float TankShieldSlamRaiseHeight = 0.88f;
    public const float TankShieldSlamRaiseHeightEmpowered = 1.12f;
    public const float TankShieldSlamBodyJumpLift = 0.34f;
    public const float TankShieldSlamBodyJumpLiftEmpowered = 0.44f;
    public const float TankShieldSlamBodySlamDip = -0.03f;
    public const float TankShieldSlamBodySlamDipEmpowered = -0.05f;
    public const float TankShieldSlamBodySlamForward = 0.14f;
    public static readonly Vector3 TankShieldSlamVerticalHoldOffset = new(0f, 1.16f, -0.6f);
    public static readonly Vector3 TankShieldSlamVerticalHoldEmpoweredOffset = new(0f, 0.42f, -0.04f);
    public static readonly Vector3 TankShieldSlamVerticalLocalEuler = new(-90f, 0f, 0f);
    public const float TankShieldSlamSoftVfxScale = 0.68f;
    public const float TankShieldSlamCartoonyVfxScale = 1.45f;
    public const float TankShieldSlamCastDuration = 0.91f;
    public const float TankShieldSlamShakeDuration = 0.38f;
    public const float TankShieldSlamShakeMagnitude = 0.24f;

    public const float TankShieldWhirlwindDuration = 4f;
    public const float TankShieldWhirlwindRadius = 2.9f;
    public const float TankShieldWhirlwindTickInterval = 0.5f;
    public const float TankShieldWhirlwindDamagePerTick = 0.55f;
    public const float MeteorCastDuration = 0.55f;
    public const int MeteorGroundFirePatchCountMin = 5;
    public const int MeteorGroundFirePatchCountMax = 9;
    public const float MeteorGroundFireLifetimeMin = 2f;
    public const float MeteorGroundFireLifetimeMax = 4f;
    public const float MonsterBurnDuration = 3f;
    public const float MonsterBurnTotalDamage = 18f;
    public const float MonsterShockDuration = 1.4f;
    public const float MonsterPoisonDuration = 5f;
    public const float MonsterPoisonTickDamage = 12f;
    public const float MonsterPoisonArmorPerStack = 5f;
    public const float TankShieldWhirlwindSpinSpeed = 900f;
    public const float TankShieldWhirlwindMoveSpeedMultiplier = 1.2f;

    public const float TankFortifyMissileBlockStaminaCost = 1f;
    public const float TankSkillStaminaCost = 20f;
    public const float TankWhirlwindStaminaCost = 30f;

    public const string LayerGold = "CwslGold";

    /// <summary>UI 3.8억 = 업보 3,800 (1업보 = 100만 원 표기). 보스 등장 기준.</summary>
    public const long BossKarmaThreshold = 3_800L;
    public const long KarmaPickupAmount = 10L;
    public const long CheatKarmaIncrement = 100L; // TODO(릴리즈): U키 치트용 — 정식 버전 전 제거

    public const float BossMaxHealth = 15000f;
    /// <summary>고려대 모델 기준 보스 시각·콜라이더 배율.</summary>
    public const float BossVisualScale = 5f;
    public const float MidBossKuVisualScale = 3f;
    public const float SeniorCoachKuVisualScale = 3f;

    // --- 홍명보 보스 스킬 ---
    public const int BossReverseZoneCountMin = 5;
    public const int BossReverseZoneCountMax = 7;
    public const float BossReverseZoneRadius = 3.2f;
    public const float BossReverseTelegraphSeconds = 1.5f;
    public const float BossReverseCastBuffer = 0.5f;
    public const float BossReverseExplosionDamage = 80f;
    public const float BossReverseControlDuration = 3f;
    public const float BossBarrageDuration = 5f;
    public const float BossBarrageInterval = 0.5f;
    public const int BossBarrageProjectileCount = 36;
    public const float BossBarrageProjectileSpeed = 14f;
    public const float BossBarrageProjectileLifetime = 6f;
    public const float BossBarrageDamage = 35f;
    public const float BossSafeZoneRadius = 3f;
    public const int BossInfectionOrbCount = 3;
    public const float BossInfectionOrbSpeed = 6f;
    public const float BossInfectionOrbLifetime = 9f;
    public const float BossInfectionOrbRadius = 1.4f;
    public const float BossInfectedDuration = 3f;
    public const float BossInfectedSpikeInterval = 1f;
    public const float BossInfectedSpikeSpeed = 18f;
    public const float BossInfectedSpikeLifetime = 2.5f;
    public const float BossInfectedSpikeDamage = 25f;

    public const float BossAttackIntervalPhase1 = 6.5f;
    public const float BossAttackIntervalPhase2 = 5.5f;
    public const float BossAttackIntervalPhase3 = 4.5f;
    public const float BossAttackIntervalPhase4 = 3.8f;
    public const float BossSlamRadius = 14f;
    public const float BossSlamDamage = 150f;
    public const float BossRingBurstRadius = 24f;
    public const float BossRingBurstDamage = 100f;
    public const int BossProjectileFanCount = 5;
    public const float BossProjectileSpreadDegrees = 50f;
    public const float BossProjectileSpeed = 16f;
    public const float BossProjectileLifetime = 7f;
    public const int BossSummonCountMin = 4;
    public const int BossSummonCountMax = 7;
    public const float BossSummonSpread = 10f;
    public const float BossSummonMinRadius = 18f;
    public const float BossPhase2Hp = 11250f;
    public const float BossPhase3Hp = 7500f;
    public const float BossPhase4Hp = 3750f;
    public const float BossPhase1TeleportCooldown = 15f;
    public const float BossPhase2BallInterval = 8f;
    public const int BossPhase2BallCountMin = 2;
    public const int BossPhase2BallCountMax = 3;
    public const int TeamBallGoldSteal = 50;
    public const float BossFightZoneHpDrainPerSecond = 2f;
    public const float BossFinalPhaseDuration = 38f;

    public const float ArenaHalfExtent = 36f;
    /// <summary>아레나 바닥(Plane scale 10 → 100×100) 실제 가장자리 — 벽 스턴·적 기지·몬스터 스폰.</summary>
    public const float ArenaMapHalfExtent = 50f;
    /// <summary>맵 벽 직전 스폰 오프셋(벽에 붙어 보이게).</summary>
    public const float MapEdgeSpawnInset = 1.4f;
    /// <summary>몬스터 젠 안쪽 금지 구역(플레이어 중심 활동/시야 사각형).</summary>
    public const float MonsterSpawnInnerHalfExtent = 24f;
    /// <summary>몬스터 젠 바깥 한계(맵 가장자리 직전 사각형).</summary>
    public const float MonsterSpawnOuterHalfExtent = ArenaHalfExtent - 1f;
    public static readonly UnityEngine.Color ArenaFloorColor = new(0.72f, 0.53f, 0.30f, 1f);
    public const float SpawnHeight = 0.5f;

    /// <summary>true면 카르마·아레나 기믹 대신 5분 넥서스 방어 모드.</summary>
    public const bool UseDefenseMode = true;

    /// <summary>모든 플레이어가 시작 발판에 서면 진행되는 카운트다운(초).</summary>
    public const float DefenseStartCountdownSeconds = 3f;

    /// <summary>준비 구역 원형 벽 반경 — 라인업·시작 발판을 포함.</summary>
    public const float DefensePrepBarrierRadius = 15.5f;
    public const float DefensePrepBarrierHeight = 3.2f;
    public const float DefensePrepBarrierThickness = 0.7f;

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
    public const float BlackHoleEscapeClickPush = 0.52f;
    public const float BlackHoleEscapeAwayDotThreshold = 0.25f;
    public const float BlackHoleEscapeClickCooldown = 0.04f;

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

    // --- 아군 버프 존 ---
    public const int HealingSpringCount = 3;
    public const float HealingSpringRadius = 3f;
    public const float HealingSpringHpPerSecond = 5f;

    public const int TailwindGrassCount = 4;
    public const float TailwindGrassRadius = 2.9f;
    public const float TailwindGrassSpeedMultiplier = 1.35f;

    public const int RallyZoneCount = 2;
    public const float RallyZoneRadius = 5.5f;
    public const int RallyZoneMinAllies = 2;
    public const float RallyZoneDamageMultiplier = 1.25f;
    public const float RallyZoneVisionBonus = 3f;

    public const int GoldSpringCount = 2;
    public const float GoldSpringRadius = 2.6f;
    public const float GoldSpringIntervalSeconds = 2.5f;
    public const int GoldSpringAmount = 2;

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
    public const float HazardPadWarningSeconds = 3f;

    public const float DynamicGimmickInitialDelaySeconds = 10f;
    public const float DynamicGimmickSpawnIntervalMin = 22f;
    public const float DynamicGimmickSpawnIntervalMax = 34f;
    public const float DynamicGimmickWarningSeconds = 3f;
    public const float DynamicGimmickDurationMin = 42f;
    public const float DynamicGimmickDurationMax = 58f;
    public const int DynamicGimmickMaxAliveTotal = 6;
    public const float DynamicGimmickMinSeparation = 9f;
    public const int DynamicGimmickSpawnAttempts = 16;

    public const bool SkillsConsumeGold = false;
    public const bool SkillsUseStamina = true;
    public const float HazardAcidDamagePerSecond = 8f;
    public const float HazardLavaGoldLeakIntervalSeconds = 0.85f;
    public const int HazardLavaGoldLeakAmount = 1;
    public const float HazardWaterSlowMultiplier = 0.45f;

    public const float SpawnIntervalSeconds = 1.35f;
    /// <summary>먹물 스나이퍼(일반·넥서스) 스폰 확률 — 약 1%.</summary>
    public const float InkSniperSpawnChance = 0.01f;
    public const float InkSniperFireCooldownSeconds = 10f;

    public const float SeniorCoachFrenzyAuraRadius = 10f;
    public const float SeniorCoachFrenzySpeedMultiplier = 1.3f;
    public const float SeniorCoachIronShieldInterval = 20f;
    public const int SeniorCoachIronShieldTargetCount = 2;
    public const float SeniorCoachAceSpotlightDuration = 7f;
    public const float SeniorCoachAceSpotlightCooldown = 14f;
    public const float SeniorCoachOrbitSpeed = 2.6f;
    public const float SeniorCoachOrbitInset = 2.1f;
    public const int MaxAliveMonsters = 65;
    public const float MonsterSpawnWarningSeconds = 2f;
    public const float MonsterSpawnWarningRadius = 1.8f;
    public const float SuicideExplosionScale = 0.32f;

    public const string LayerPlayer = "CwslPlayer";
    public const string LayerMonster = "CwslMonster";
    public const string LayerProjectile = "CwslProjectile";
}
