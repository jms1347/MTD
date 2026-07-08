#if UNITY_EDITOR
using System.IO;
using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using AssetKits.ParticleImage;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class CwslGameSceneSetup
{
    private const string RootFolder = "Assets/0CwSL";
    private const string ScenePath = RootFolder + "/Scenes/CwslGameScene.unity";
    private const string AssetsPath = RootFolder + "/Data/CwslGameAssets.asset";
    private const string NetworkPrefabsPath = RootFolder + "/Data/CwslNetworkPrefabs.asset";

    [MenuItem("Tools/CwSL/Setup Game Scene", false, 10)]
    public static void SetupGameScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolders();
        EnsureLayers();
        EnsureGoldFlyCoinPrefab();
        var assets = EnsureGameAssets();
        var playerPrefab = BuildPlayerPrefab();
        var rangedPrefab = BuildMonsterPrefab(CwslMonsterType.Ranged, typeof(CwslRangedMonster), 0.55f);
        var inkSniperPrefab = BuildMonsterPrefab(CwslMonsterType.InkSniper, typeof(CwslInkSniperMonster), 0.52f);
        var suicidePrefab = BuildMonsterPrefab(CwslMonsterType.Suicide, typeof(CwslSuicideMonster), 0.5f);
        var meleePrefab = BuildMonsterPrefab(CwslMonsterType.Melee, typeof(CwslMeleeMonster), 0.6f);
        var nexusMeleePrefab = BuildMonsterPrefab(CwslMonsterType.NexusMelee, typeof(CwslMeleeMonster), 1.45f);
        var koreaSoldierPrefab = BuildMonsterPrefab(
            CwslMonsterType.KoreaUniversitySoldier, typeof(CwslMeleeMonster), 0.6f);
        var stickySuicidePrefab = BuildMonsterPrefab(
            CwslMonsterType.StickySuicide, typeof(CwslStickySuicideMonster), 0.45f);
        var midBossPrefab = BuildMonsterPrefab(CwslMonsterType.MidBoss, typeof(CwslDefenseMidBoss), 0.85f);
        var defenseBossPrefab = BuildMonsterPrefab(CwslMonsterType.DefenseBoss, typeof(CwslDefenseBoss), 1.1f);
        var seniorCoachPrefab = BuildMonsterPrefab(CwslMonsterType.SeniorCoach, typeof(CwslSeniorCoachMonster), 0.78f);
        var bossPrefab = BuildBossPrefab();
        var projectilePrefab = BuildProjectilePrefab();
        var bossSkillProjectilePrefab = BuildBossSkillProjectilePrefab();
        var playerMissilePrefab = BuildPlayerMissilePrefab();
        var frozenOrbPrefab = BuildFrozenOrbPrefab();
        var goldPickupPrefab = BuildGoldPickupPrefab();
        var pillPickupPrefab = BuildPillPickupPrefab();
        var graveVisualPrefab = BuildGraveVisualPrefab();
        var nexusPrefab = BuildNexusPrefab();
        var enemyBasePrefab = BuildEnemyBasePrefab();

        assets.playerPrefab = playerPrefab;
        assets.rangedMonsterPrefab = rangedPrefab;
        assets.inkSniperMonsterPrefab = inkSniperPrefab;
        assets.suicideMonsterPrefab = suicidePrefab;
        assets.meleeMonsterPrefab = meleePrefab;
        assets.nexusMeleeMonsterPrefab = nexusMeleePrefab;
        assets.slimeMeleeModelPrefab = LoadPrefab(CwslSlimeAssetPaths.Slime01);
        assets.slimeNexusMeleeModelPrefab = LoadPrefab(CwslSlimeAssetPaths.SlimeViking);
        assets.slimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CwslSlimeAssetPaths.AnimatorController);
        assets.koreaUniversitySoldierPrefab = koreaSoldierPrefab;
        assets.stickySuicideMonsterPrefab = stickySuicidePrefab;
        assets.midBossMonsterPrefab = midBossPrefab;
        assets.defenseBossMonsterPrefab = defenseBossPrefab;
        assets.seniorCoachMonsterPrefab = seniorCoachPrefab;
        assets.bossPrefab = bossPrefab;
        assets.projectilePrefab = projectilePrefab;
        assets.bossSkillProjectilePrefab = bossSkillProjectilePrefab;
        assets.playerMissilePrefab = playerMissilePrefab;
        assets.frozenOrbPrefab = frozenOrbPrefab;
        assets.goldPickupPrefab = goldPickupPrefab;
        assets.pillPickupPrefab = pillPickupPrefab;
        assets.graveVisualPrefab = graveVisualPrefab;
        assets.nexusPrefab = nexusPrefab;
        assets.enemyBasePrefab = enemyBasePrefab;
        assets.darkMissileVfx = LoadPrefab(CwslVfxPaths.RangedProjectileVisual);
        assets.rangedTankProjectileVfx = LoadPrefab(CwslVfxPaths.RangedTankProjectileVisual);
        assets.rangedTankMuzzleVfx = LoadPrefab(CwslVfxPaths.RangedTankMuzzleFlash);
        assets.rangedTankProjectileHitVfx = LoadPrefab(CwslVfxPaths.RangedTankProjectileHit);
        assets.inkBlindAuraVfx = LoadPrefab(CwslVfxPaths.InkBlindAura);
        assets.shadowProjectileHitVfx = LoadPrefab(CwslVfxPaths.ShadowProjectileHit);
        assets.shadowMuzzleVfx = LoadPrefab(CwslVfxPaths.ShadowMuzzleFlash);
        assets.playerMissileVfx = LoadPrefab(CwslVfxPaths.PlayerMissileVisual);
        assets.missileTankPowerBoostVfx = LoadPrefab(CwslVfxPaths.MissileTankPowerBoostGlow);
        assets.missileTankSmokeDashTrailVfx = LoadPrefab(CwslVfxPaths.MissileTankSmokeDashTrail);
        assets.missileTankSmokeBombVfx = LoadPrefab(CwslVfxPaths.MissileTankSmokeBomb);
        assets.missileTankSmokeZoneVfx = LoadPrefab(CwslVfxPaths.MissileTankSmokeZone);
        assets.missileTankFireAmmoVfx = LoadPrefab(CwslVfxPaths.MissileTankFireAmmo);
        assets.missileTankPoisonAmmoVfx = LoadPrefab(CwslVfxPaths.MissileTankPoisonAmmo);
        assets.missileTankLightningAmmoVfx = LoadPrefab(CwslVfxPaths.MissileTankLightningAmmo);
        assets.gunMuzzleVfx = LoadPrefab(CwslVfxPaths.GunMuzzleFlash);
        assets.fortifyAuraVfx = LoadPrefab(CwslVfxPaths.FortifyAura);
        assets.fortifyBlockVfx = LoadPrefab(CwslVfxPaths.FortifyBlock);
        assets.shieldDashWaveVfx = LoadPrefab(CwslVfxPaths.ShieldDashWave);
        assets.shieldWhirlwindVfx = LoadPrefab(CwslVfxPaths.ShieldWhirlwind);
        assets.shieldSlamSoftVfx = LoadPrefab(CwslVfxPaths.ShieldSlamSoft);
        assets.shieldSlamCartoonyVfx = LoadPrefab(CwslVfxPaths.ShieldSlamCartoony);
        assets.monsterBurnStatusVfx = LoadPrefab(CwslVfxPaths.MonsterBurnStatus);
        assets.monsterSlowStatusVfx = LoadPrefab(CwslVfxPaths.MonsterSlowStatus);
        assets.monsterShockStatusVfx = LoadPrefab(CwslVfxPaths.MonsterShockStatus);
        assets.monsterPoisonStatusVfx = LoadPrefab(CwslVfxPaths.MonsterPoisonStatus);
        assets.meteorFallVfx = LoadPrefab(CwslVfxPaths.MeteorFall);
        assets.meteorImpactVfx = LoadPrefab(CwslVfxPaths.MeteorImpact);
        assets.meteorBurnVfx = LoadPrefab(CwslVfxPaths.MeteorBurn);
        assets.meteorGroundFireSoftAbVfx = LoadPrefab(CwslVfxPaths.MeteorGroundFireSoftAb);
        assets.meteorGroundFireSoftBigVfx = LoadPrefab(CwslVfxPaths.MeteorGroundFireSoftBig);
        assets.meteorGroundFireAdditiveVfx = LoadPrefab(CwslVfxPaths.MeteorGroundFireAdditive);
        assets.rammerStunExplosionVfx = LoadPrefab(CwslVfxPaths.RammerStunExplosion);
        assets.rammerStunStarsVfx = LoadPrefab(CwslVfxPaths.RammerStunStars);
        assets.gatherChargeCircleVfx = LoadPrefab(CwslVfxPaths.GatherChargeCircle);
        assets.gatherMaxReadyVfx = LoadPrefab(CwslVfxPaths.GatherMaxReady);
        assets.gatherPullVortexVfx = LoadPrefab(CwslVfxPaths.GatherPullVortex);
        assets.gatherPullBurstVfx = LoadPrefab(CwslVfxPaths.GatherPullBurst);
        assets.gatherSlowEnchantVfx = LoadPrefab(CwslVfxPaths.GatherSlowEnchant);
        assets.gathererMissileVfx = LoadPrefab(CwslVfxPaths.GathererMissileVisual);
        assets.gathererYankBurstVfx = LoadPrefab(CwslVfxPaths.GathererYankBurst);
        assets.gathererSwapPortalVfx = LoadPrefab(CwslVfxPaths.GathererSwapPortal);
        assets.gathererBlackHoleVfx = LoadPrefab(CwslVfxPaths.GathererBlackHole);
        assets.rammerBrakeBurstVfx = LoadPrefab(CwslVfxPaths.RammerBrakeBurst);
        assets.rammerRopeAttachVfx = LoadPrefab(CwslVfxPaths.RammerRopeAttach);
        assets.rammerRopeFlingVfx = LoadPrefab(CwslVfxPaths.RammerRopeFling);
        assets.rammerFireTrailVfx = LoadPrefab(CwslVfxPaths.RammerFireTrail);
        assets.rammerFireTrailZoneVfx = LoadPrefab(CwslVfxPaths.RammerFireTrailZone);
        assets.barricadeDetonateExplosionVfx = LoadPrefab(CwslVfxPaths.BarricadeDetonateExplosion);
        assets.barricadeJumpPadAuraVfx = LoadPrefab(CwslVfxPaths.BarricadeJumpPadAura);
        assets.barricadeRepairSparksVfx = LoadPrefab(CwslVfxPaths.BarricadeRepairSparks);
        assets.healerMissileVfx = LoadPrefab(CwslVfxPaths.HealerMissileVisual);
        assets.healerHealPadVfx = LoadPrefab(CwslVfxPaths.HealerHealPad);
        assets.healerHealBurstVfx = LoadPrefab(CwslVfxPaths.HealerHealBurst);
        assets.healerPoisonPadVfx = LoadPrefab(CwslVfxPaths.HealerPoisonPad);
        assets.healerHasteBuffVfx = LoadPrefab(CwslVfxPaths.HealerHasteBuff);
        assets.bossTeleportDepartVfx = LoadPrefab(CwslVfxPaths.BossTeleportDepart);
        assets.bossTeleportArriveVfx = LoadPrefab(CwslVfxPaths.BossTeleportArrive);
        assets.bossPhaseTransitionVfx = LoadPrefab(CwslVfxPaths.BossPhaseTransition);
        assets.fightZoneAuraVfx = LoadPrefab(CwslVfxPaths.FightZoneAura);
        assets.teamBallVisualVfx = LoadPrefab(CwslVfxPaths.TeamBallVisual);
        assets.teamBallTrailVfx = LoadPrefab(CwslVfxPaths.TeamBallTrail);
        assets.teamBallHitVfx = LoadPrefab(CwslVfxPaths.TeamBallHit);
        assets.cornerStoneBreakVfx = LoadPrefab(CwslVfxPaths.CornerStoneBreak);
        assets.karmaMilestoneVfx = LoadPrefab(CwslVfxPaths.KarmaMilestone);
        assets.silhouetteAuraVfx = LoadPrefab(CwslVfxPaths.SilhouetteAura);
        assets.pressConferenceRingVfx = LoadPrefab(CwslVfxPaths.PressConferenceRing);
        assets.finalPhaseRingVfx = LoadPrefab(CwslVfxPaths.FinalPhaseRing);
        assets.fogZoneHeavyVfx = LoadPrefab(CwslVfxPaths.FogZoneHeavy);
        assets.fogZoneLivelyVfx = LoadPrefab(CwslVfxPaths.FogZoneLively);
        assets.fogZoneLocalVfx = LoadPrefab(CwslVfxPaths.FogZoneLocal);
        assets.blackHoleVortexVfx = LoadPrefab(CwslVfxPaths.BlackHoleVortex);
        assets.trapPadAuraVfx = LoadPrefab(CwslVfxPaths.TrapPadAura);
        assets.trapPadTriggerVfx = LoadPrefab(CwslVfxPaths.TrapPadTrigger);
        assets.karmaHalfZoneAuraVfx = LoadPrefab(CwslVfxPaths.KarmaHalfZoneAura);
        assets.tianyuanAuraVfx = LoadPrefab(CwslVfxPaths.TianyuanAura);
        assets.lighthouseGlowVfx = LoadPrefab(CwslVfxPaths.LighthouseGlow);
        assets.watchGlareVfx = LoadPrefab(CwslVfxPaths.WatchGlare);
        assets.watchSparkleVfx = LoadPrefab(CwslVfxPaths.WatchSparkle);
        assets.bossFightShieldVfx = LoadPrefab(CwslVfxPaths.BossFightShield);
        assets.fakeGoldExplosionVfx = LoadPrefab(CwslVfxPaths.FakeGoldExplosion);
        assets.badGrassAuraVfx = LoadPrefab(CwslVfxPaths.BadGrassAura);
        assets.healingSpringAuraVfx = LoadPrefab(CwslVfxPaths.HealingSpringAura);
        assets.tailwindGrassAuraVfx = LoadPrefab(CwslVfxPaths.TailwindGrassAura);
        assets.rallyZoneAuraVfx = LoadPrefab(CwslVfxPaths.RallyZoneAura);
        assets.goldSpringAuraVfx = LoadPrefab(CwslVfxPaths.GoldSpringAura);
        assets.goldSpringBurstVfx = LoadPrefab(CwslVfxPaths.GoldSpringBurst);
        assets.donationPadGlowVfx = LoadPrefab(CwslVfxPaths.DonationPadGlow);
        assets.offsideLaserMissileVfx = LoadPrefab(CwslVfxPaths.OffsideLaserMissile);
        assets.lightningStrikeVfx = LoadPrefab(CwslVfxPaths.LightningStrike);
        assets.lightningOrbVfx = LoadPrefab(CwslVfxPaths.LightningOrb);
        assets.lightningMissileVfx = LoadPrefab(CwslVfxPaths.LightningMissile);
        assets.lightningStunExplosionVfx = LoadPrefab(CwslVfxPaths.LightningStunExplosion);
        assets.lightningStunStrikeVfx = LoadPrefab(CwslVfxPaths.LightningStunStrike);
        assets.lightningZoneAuraVfx = LoadPrefab(CwslVfxPaths.LightningZoneAura);
        assets.redMageLightningOrbVfx = LoadPrefab(CwslVfxPaths.RedMageLightningOrb);
        assets.redMageLightningOrbRadiusVfx = LoadPrefab(CwslVfxPaths.RedMageLightningOrbRadius);
        assets.redMageLightningBoltVfx = LoadPrefab(CwslVfxPaths.RedMageLightningBolt);
        assets.redMageLightningStrikeVfx = LoadPrefab(CwslVfxPaths.RedMageLightningStrike);
        assets.redMageLightningStrikeTallVfx = LoadPrefab(CwslVfxPaths.RedMageLightningStrikeTall);
        assets.redMageLightningExplosionVfx = LoadPrefab(CwslVfxPaths.RedMageLightningExplosion);
        assets.redMageTeleportPortalVfx = LoadPrefab(CwslVfxPaths.RedMageTeleportPortal);
        assets.frozenOrbIceBallVfx = LoadPrefab(CwslVfxPaths.FrozenOrbIceBall);
        assets.frozenOrbHitAirVfx = LoadPrefab(CwslVfxPaths.FrozenOrbHitAir);
        assets.frozenOrbGroundTrailVfx = LoadPrefab(CwslVfxPaths.FrozenOrbGroundTrail);
        assets.hazardAcidPadVfx = LoadPrefab(CwslVfxPaths.HazardAcidPad);
        assets.hazardLavaPadVfx = LoadPrefab(CwslVfxPaths.HazardLavaPad);
        assets.hazardWaterPadVfx = LoadPrefab(CwslVfxPaths.HazardWaterPad);
        assets.pillBuffBlueVfx = LoadPrefab(CwslVfxPaths.PillBuffBlue);
        assets.pillBuffGreenVfx = LoadPrefab(CwslVfxPaths.PillBuffGreen);
        assets.pillBuffYellowVfx = LoadPrefab(CwslVfxPaths.PillBuffYellow);
        assets.pillSphereBlueVfx = LoadPrefab(CwslVfxPaths.PillSphereBlue);
        assets.pillSphereYellowVfx = LoadPrefab(CwslVfxPaths.PillSphereYellow);
        assets.suicideExplosionVfx = LoadPrefab(CwslVfxPaths.SuicideExplosion);
        assets.suicideBomberDeathVfx = LoadPrefab(CwslVfxPaths.SuicideBomberDeath);
        assets.bombFuseVfx = LoadPrefab(CwslVfxPaths.BombFuse);
        assets.meleeHitVfx = LoadPrefab(CwslVfxPaths.MeleeHit);
        assets.enemyDeathVfx = LoadPrefab(CwslVfxPaths.EnemyDeath);
        assets.bossDeathVfx = LoadPrefab(CwslVfxPaths.BossDeath);
        assets.playerDeathVfx = LoadPrefab(CwslVfxPaths.PlayerDeath);
        assets.goldBurstVfx = LoadPrefab(CwslVfxPaths.GoldBurst);
        assets.goldMagnetTrailVfx = LoadPrefab(CwslVfxPaths.GoldMagnetTrail);
        assets.goldPickupSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.CoinDropSound);
        assets.horseGallopSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.HorseGallopSound);
        assets.rammerStunSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.RammerStunSound);
        assets.rammerBrakeNeighSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.RammerBrakeNeighSound);
        assets.bossTeleportCastSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.BossTeleportCastSound);
        assets.bossTeleportArriveSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.BossTeleportArriveSound);
        assets.bossPhaseShiftSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.BossPhaseShiftSound);
        assets.teamBallRollSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.TeamBallRollSound);
        assets.teamBallHitSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.TeamBallHitSound);
        assets.cornerStoneBreakSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.CornerStoneBreakSound);
        assets.bossWatchStartSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.BossWatchStartSound);
        assets.gatherChargeCastSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.GatherChargeCastSound);
        assets.gatherChargeLoopSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.GatherChargeLoopSound);
        assets.gatherChargeEndSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.GatherChargeEndSound);
        assets.skillGoldFailSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.SkillGoldFailSound);
        assets.redMageFrozenOrbCastSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.RedMageFrozenOrbCastSound);
        assets.redMageFrozenOrbTravelSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.RedMageFrozenOrbTravelSound);
        assets.redMageLightningOrbCastSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.RedMageLightningOrbCastSound);
        assets.redMageLightningOrbStrikeSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.RedMageLightningOrbStrikeSound);
        assets.redMageLightningOrbImpactSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.RedMageLightningOrbImpactSound);
        assets.redMageTeleportCastSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.RedMageTeleportCastSound);
        assets.tankShieldSlamGroundImpactSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.TankShieldSlamGroundImpactSound);
        assets.tankShieldWhirlwindFanLoopSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.TankShieldWhirlwindFanLoopSound);
        assets.tankShieldWhirlwindPowerPunchSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.TankShieldWhirlwindPowerPunchSound);
        assets.tankShieldDashImpactSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.TankShieldDashImpactSound);
        assets.tankShieldFortifyQSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.TankShieldFortifyQSound);
        assets.barricadeJumpPadSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.BarricadeJumpPadSound);
        assets.offsideHornSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.OffsideHornSound);
        EditorUtility.SetDirty(assets);

        var networkPrefabs = EnsureNetworkPrefabsList();
        RegisterNetworkPrefab(networkPrefabs, playerPrefab);
        RegisterNetworkPrefab(networkPrefabs, rangedPrefab);
        RegisterNetworkPrefab(networkPrefabs, inkSniperPrefab);
        RegisterNetworkPrefab(networkPrefabs, suicidePrefab);
        RegisterNetworkPrefab(networkPrefabs, meleePrefab);
        RegisterNetworkPrefab(networkPrefabs, nexusMeleePrefab);
        RegisterNetworkPrefab(networkPrefabs, koreaSoldierPrefab);
        RegisterNetworkPrefab(networkPrefabs, stickySuicidePrefab);
        RegisterNetworkPrefab(networkPrefabs, midBossPrefab);
        RegisterNetworkPrefab(networkPrefabs, defenseBossPrefab);
        RegisterNetworkPrefab(networkPrefabs, seniorCoachPrefab);
        RegisterNetworkPrefab(networkPrefabs, bossPrefab);
        RegisterNetworkPrefab(networkPrefabs, projectilePrefab);
        RegisterNetworkPrefab(networkPrefabs, bossSkillProjectilePrefab);
        RegisterNetworkPrefab(networkPrefabs, playerMissilePrefab);
        RegisterNetworkPrefab(networkPrefabs, frozenOrbPrefab);
        RegisterNetworkPrefab(networkPrefabs, goldPickupPrefab);
        RegisterNetworkPrefab(networkPrefabs, pillPickupPrefab);
        RegisterNetworkPrefab(networkPrefabs, nexusPrefab);
        RegisterNetworkPrefab(networkPrefabs, enemyBasePrefab);
        EditorUtility.SetDirty(networkPrefabs);

        BuildScene(assets, networkPrefabs);
        UpdateBuildSettings();
        WireLobbySceneName();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CwSL] 게임 씬·프리팹·네트워크 설정 완료. LobbyScene → CwslGameScene 흐름이 연결되었습니다.");
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(RootFolder + "/Prefabs");
        CwslPrefabPaths.EnsureFoldersExist();
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(AssetsPath)!);
        Directory.CreateDirectory($"{RootFolder}/Resources/CwslGold");
    }

    private static void EnsureGoldFlyCoinPrefab()
    {
        const string outputPath = RootFolder + "/Resources/CwslGold/CwslGoldFlyCoin.prefab";
        const string coinSpritePath = "Assets/AssetKits/ParticleImage/Demo/Sprites/Coin.png";

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(coinSpritePath);
        if (sprite == null)
        {
            Debug.LogWarning($"[CwSL] 코인 스프라이트를 찾을 수 없습니다: {coinSpritePath}");
            return;
        }

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
        GameObject temp;
        if (existing != null)
            temp = (GameObject)PrefabUtility.InstantiatePrefab(existing);
        else
            temp = BuildGoldFlyCoinObject(sprite);

        ConfigureGoldFlyCoinPrefab(temp, sprite);
        PrefabUtility.SaveAsPrefabAsset(temp, outputPath);
        Object.DestroyImmediate(temp);
    }

    private static GameObject BuildGoldFlyCoinObject(Sprite sprite)
    {
        var coinObject = new GameObject(
            "CwslGoldFlyCoin",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CwslGoldFlyCoin));

        var rect = coinObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(20f, 20f);

        var image = coinObject.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.color = Color.white;

        var trailObject = new GameObject("Trail", typeof(RectTransform), typeof(CwslGoldFlyCoinTrail));
        trailObject.transform.SetParent(coinObject.transform, false);
        return coinObject;
    }

    private static void ConfigureGoldFlyCoinPrefab(GameObject root, Sprite sprite)
    {
        root.name = "CwslGoldFlyCoin";

        var image = root.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = sprite;
            image.raycastTarget = false;
        }

        if (root.GetComponent<CwslGoldFlyCoin>() == null)
            root.AddComponent<CwslGoldFlyCoin>();

        var trail = root.GetComponentInChildren<CwslGoldFlyCoinTrail>(true);
        if (trail == null)
        {
            var trailObject = new GameObject("Trail", typeof(RectTransform), typeof(CwslGoldFlyCoinTrail));
            trailObject.transform.SetParent(root.transform, false);
        }
    }

    private static void EnsureGoldCoinFlyPrefab()
    {
        const string outputPath = RootFolder + "/Resources/CwslGold/CoinFlyParticle.prefab";
        const string sourcePath = "Assets/AssetKits/ParticleImage/Demo/Prefabs/CoinAttraction.prefab";

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
        if (existing != null)
        {
            var tempExisting = (GameObject)PrefabUtility.InstantiatePrefab(existing);
            SanitizeCoinFlyPrefab(tempExisting);
            PrefabUtility.SaveAsPrefabAsset(tempExisting, outputPath);
            Object.DestroyImmediate(tempExisting);
            return;
        }

        var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
        {
            Debug.LogWarning($"[CwSL] CoinAttraction 프리팹을 찾을 수 없습니다: {sourcePath}");
            return;
        }

        var particle = source.transform.Find("Particle Image");
        if (particle == null)
        {
            Debug.LogWarning("[CwSL] CoinAttraction 안의 Particle Image를 찾을 수 없습니다.");
            return;
        }

        var temp = Object.Instantiate(particle.gameObject);
        temp.name = "CoinFlyParticle";
        SanitizeCoinFlyPrefab(temp);
        PrefabUtility.SaveAsPrefabAsset(temp, outputPath);
        Object.DestroyImmediate(temp);
    }

    private static void SanitizeCoinFlyPrefab(GameObject root)
    {
        var particle = root.GetComponent<ParticleImage>();
        if (particle == null)
            return;

        particle.PlayMode = AssetKits.ParticleImage.Enumerations.PlayMode.None;
        particle.attractorEnabled = false;
        particle.attractorTarget = null;
    }

    private static void EnsureLayers()
    {
        EnsureLayer(CwslGameConstants.LayerPlayer);
        EnsureLayer(CwslGameConstants.LayerMonster);
        EnsureLayer(CwslGameConstants.LayerProjectile);
        EnsureLayer(CwslGameConstants.LayerGold);
    }

    private static void EnsureLayer(string layerName)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (var i = 8; i < 32; i++)
        {
            var property = layers.GetArrayElementAtIndex(i);
            if (property.stringValue == layerName)
                return;
        }

        for (var i = 8; i < 32; i++)
        {
            var property = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(property.stringValue))
                continue;

            property.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            return;
        }

        Debug.LogWarning($"[CwSL] 레이어 슬롯이 부족합니다: {layerName}");
    }

    private static CwslGameAssets EnsureGameAssets()
    {
        var assets = AssetDatabase.LoadAssetAtPath<CwslGameAssets>(AssetsPath);
        if (assets != null)
            return assets;

        assets = ScriptableObject.CreateInstance<CwslGameAssets>();
        AssetDatabase.CreateAsset(assets, AssetsPath);
        return assets;
    }

    private static NetworkPrefabsList EnsureNetworkPrefabsList()
    {
        var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NetworkPrefabsPath);
        if (list != null)
            return list;

        list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
        AssetDatabase.CreateAsset(list, NetworkPrefabsPath);
        return list;
    }

    public static void RegisterNetworkPrefab(NetworkPrefabsList list, GameObject prefab)
    {
        if (prefab == null)
            return;

        if (prefab.GetComponent<NetworkObject>() == null)
            return;

        if (list.Contains(prefab))
            return;

        list.Add(new NetworkPrefab { Prefab = prefab });
    }

    private static GameObject LoadPrefab(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

    private static GameObject BuildPlayerPrefab()
    {
        var root = new GameObject("CwslPlayer");
        root.layer = LayerMask.NameToLayer(CwslGameConstants.LayerPlayer);

        var agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.height = 1.74f;
        agent.radius = CwslGameConstants.PlayerBodyColliderRadiusDefault;
        agent.baseOffset = 0.87f;
        agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.LowQualityObstacleAvoidance;

        var bodyCollider = root.AddComponent<CapsuleCollider>();
        bodyCollider.height = 1.74f;
        bodyCollider.radius = CwslGameConstants.PlayerBodyColliderRadiusDefault;
        bodyCollider.center = new Vector3(0f, 0.87f, 0f);

        root.AddComponent<NetworkObject>();
        root.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        root.AddComponent<CwslPlayerHealth>();
        root.AddComponent<CwslPlayerGold>();
        root.AddComponent<CwslPlayerMovement>();
        root.AddComponent<CwslPlayerBodyCollider>();
        root.AddComponent<CwslPlayerVisualScale>();
        root.AddComponent<CwslPlayerSelection>();
        root.AddComponent<CwslPlayerController>();
        root.AddComponent<CwslPlayerCombat>();
        root.AddComponent<CwslPlayerInput>();
        root.AddComponent<CwslPlayerSkills>();
        root.AddComponent<CwslPlayerCharacter>();
        root.AddComponent<CwslTankFortifySkill>();
        root.AddComponent<CwslTankShieldAttack>();
        root.AddComponent<CwslTankShieldDashSkill>();
        root.AddComponent<CwslTankShieldSlamSkill>();
        root.AddComponent<CwslTankShieldWhirlwindSkill>();
        root.AddComponent<CwslMissileTankSkill>();
        root.AddComponent<CwslMissileTankAmmoController>();
        root.AddComponent<CwslMissileTankPowerBoostSkill>();
        root.AddComponent<CwslMissileTankSmokeDashSkill>();
        root.AddComponent<CwslRedMageMeteorSkill>();
        root.AddComponent<CwslRedMageFrozenOrbSkill>();
        root.AddComponent<CwslRedMageLightningOrbSkill>();
        root.AddComponent<CwslRedMageTeleportSkill>();
        root.AddComponent<CwslMomentumRammerSkill>();
        root.AddComponent<CwslRammerBrakeSkill>();
        root.AddComponent<CwslRammerRopeSkill>();
        root.AddComponent<CwslRammerFireTrailSkill>();
        root.AddComponent<CwslCrowdGatherSkill>();
        root.AddComponent<CwslGathererYankSkill>();
        root.AddComponent<CwslGathererSwapSkill>();
        root.AddComponent<CwslGathererBlackHoleSkill>();
        root.AddComponent<CwslGathererMissileAttack>();
        root.AddComponent<CwslBarricadeWallSkill>();
        root.AddComponent<CwslBarricadeJumpPadSkill>();
        root.AddComponent<CwslBarricadeRepairSkill>();
        root.AddComponent<CwslBarricadeDetonateSkill>();
        root.AddComponent<CwslBarricadeMeleeAttack>();
        root.AddComponent<CwslHealerHealPadSkill>();
        root.AddComponent<CwslHealerPoisonPadSkill>();
        root.AddComponent<CwslHealerBurstHealSkill>();
        root.AddComponent<CwslHealerHasteBuffSkill>();
        root.AddComponent<CwslHealerMissileAttack>();
        root.AddComponent<CwslPlayerStun>();
        root.AddComponent<CwslPlayerLightningStunVisual>();
        root.AddComponent<CwslPlayerCannonAim>();
        root.AddComponent<CwslPlayerShieldFortifyVisual>();
        root.AddComponent<CwslPlayerShieldBubble>();
        root.AddComponent<CwslPlayerFortifyVfx>();
        root.AddComponent<CwslPlayerGoldGift>();
        root.AddComponent<CwslPlayerGrave>();
        root.AddComponent<CwslPlayerHealthBar>();
        root.AddComponent<CwslPlayerSpawnVisuals>();
        root.AddComponent<CwslPlayerSpawnOffset>();
        root.AddComponent<CwslPlayerVision>();
        root.AddComponent<CwslPlayerVisionDebuff>();
        root.AddComponent<CwslPlayerInkBlindVisual>();
        root.AddComponent<CwslPlayerBossDebuff>();
        root.AddComponent<CwslPlayerPillBuff>();
        root.AddComponent<CwslBlackHoleEscape>();
        root.AddComponent<CwslPlayerProfile>();
        root.AddComponent<CwslLocalPlayerHud>();

        return SavePrefab(root, CwslPrefabPaths.Characters.Player);
    }

    private static GameObject BuildGoldPickupPrefab()
    {
        var root = new GameObject("CwslGoldPickup");
        root.layer = LayerMask.NameToLayer(CwslGameConstants.LayerGold);

        var collider = root.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = CwslGameConstants.GoldCoinClaimRadius;
        collider.center = new Vector3(0f, 0.35f, 0f);

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = "Coin";
        coin.transform.SetParent(root.transform, false);
        coin.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        coin.transform.localScale = new Vector3(0.42f, 0.05f, 0.42f);
        Object.DestroyImmediate(coin.GetComponent<Collider>());
        var renderer = coin.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = CwslMaterialUtil.CreateColored(new Color(1f, 0.84f, 0.12f));
        coin.AddComponent<CwslGoldCoinWorldVisual>();
        coin.AddComponent<CwslFakeGoldVisual>();
        coin.AddComponent<CwslGoldCoinMaterialFix>();

        root.AddComponent<NetworkObject>();
        root.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        root.AddComponent<CwslGoldPickup>();

        return SavePrefab(root, CwslPrefabPaths.Pickups.Gold);
    }

    private static GameObject BuildPillPickupPrefab()
    {
        var root = new GameObject("CwslPillPickup");
        root.layer = LayerMask.NameToLayer(CwslGameConstants.LayerGold);

        var collider = root.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = CwslGameConstants.GoldCoinClaimRadius;
        collider.center = new Vector3(0f, 0.35f, 0f);

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var pill = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pill.name = "Pill";
        pill.transform.SetParent(root.transform, false);
        pill.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        pill.transform.localRotation = Quaternion.identity;
        pill.transform.localScale = new Vector3(0.28f, 0.14f, 0.28f);
        Object.DestroyImmediate(pill.GetComponent<Collider>());
        CwslMaterialUtil.ApplyColor(pill.GetComponent<Renderer>(), CwslPillWorldVisual.ResolveColor(CwslPillType.Blue));
        pill.AddComponent<CwslPillWorldVisual>();

        root.AddComponent<NetworkObject>();
        root.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        root.AddComponent<CwslPillPickup>();

        return SavePrefab(root, CwslPrefabPaths.Pickups.Pill);
    }

    private static GameObject BuildGraveVisualPrefab()
    {
        var root = new GameObject("CwslGraveVisual");
        CwslGraveVisualBuilder.Build(root.transform);
        return SavePrefab(root, CwslPrefabPaths.Visuals.Grave);
    }

    private static GameObject BuildMonsterPrefab(CwslMonsterType type, System.Type behaviourType, float radius)
    {
        CwslSurfaceTextureAssetBuilder.EnsureGenerated();
        var root = new GameObject($"CwslMonster_{type}");
        root.layer = LayerMask.NameToLayer(CwslGameConstants.LayerMonster);

        var collider = root.AddComponent<CapsuleCollider>();
        if (type == CwslMonsterType.NexusMelee)
        {
            collider.height = 4.6f;
            collider.radius = Mathf.Max(radius, 1.45f);
            collider.center = new Vector3(0f, 2.3f, 0f);
        }
        else
        {
            collider.height = CwslGameConstants.MonsterHitHeight;
            collider.radius = Mathf.Max(radius, CwslGameConstants.MonsterHitMinRadius);
            collider.center = new Vector3(0f, CwslGameConstants.MonsterHitCenterY, 0f);
        }
        collider.isTrigger = true;

        if (type == CwslMonsterType.Suicide || type == CwslMonsterType.StickySuicide)
        {
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        root.AddComponent<NetworkObject>();
        root.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        root.AddComponent<CwslMonsterHealth>();
        root.AddComponent<CwslMonsterKnockback>();
        root.AddComponent<CwslMonsterStun>();
        root.AddComponent(behaviourType);
        if (type == CwslMonsterType.Ranged || type == CwslMonsterType.InkSniper)
            root.AddComponent<CwslRangedCannonAim>();

        CwslMonsterVisualBuilder.Build(root.transform, type);
        var monster = root.GetComponent<CwslMonsterBase>();
        monster?.Initialize(type);

        return SavePrefab(root, CwslPrefabPaths.GetMonsterPrefabPath(type));
    }

    public static GameObject BuildMonsterPrefabForEditor(
        CwslMonsterType type,
        System.Type behaviourType,
        float radius) =>
        BuildMonsterPrefab(type, behaviourType, radius);

    private static GameObject BuildBossPrefab()
    {
        var root = new GameObject("CwslBoss_Hongmyeongbo");
        root.layer = LayerMask.NameToLayer(CwslGameConstants.LayerMonster);

        var collider = root.AddComponent<CapsuleCollider>();
        collider.height = 4.2f;
        collider.radius = 1.4f;
        collider.center = new Vector3(0f, 2.1f, 0f);
        collider.isTrigger = true;

        root.AddComponent<NetworkObject>();
        root.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        root.AddComponent<CwslMonsterHealth>();
        root.AddComponent<CwslBossHongmyeongbo>();
        root.AddComponent<BossController>();
        CwslMonsterVisualBuilder.Build(root.transform, CwslMonsterType.BossHongmyeongbo);

        return SavePrefab(root, CwslPrefabPaths.Boss.Hongmyeongbo);
    }

    private static GameObject BuildProjectilePrefab()
    {
        var root = new GameObject("CwslProjectile");
        root.layer = LayerMask.NameToLayer(CwslGameConstants.LayerProjectile);

        var collider = root.AddComponent<SphereCollider>();
        collider.radius = 0.25f;
        collider.isTrigger = true;

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "HitProxy";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = Vector3.one * 0.2f;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.GetComponent<Renderer>().enabled = false;

        root.AddComponent<NetworkObject>();
        root.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        root.AddComponent<CwslMonsterProjectile>();
        root.AddComponent<CwslProjectileVisual>();

        return SavePrefab(root, CwslPrefabPaths.Projectiles.Monster);
    }

    private static GameObject BuildBossSkillProjectilePrefab()
    {
        var root = new GameObject("CwslBossSkillProjectile");
        root.layer = LayerMask.NameToLayer(CwslGameConstants.LayerProjectile);

        var collider = root.AddComponent<SphereCollider>();
        collider.radius = 0.3f;
        collider.isTrigger = true;

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "HitProxy";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = Vector3.one * 0.25f;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.GetComponent<Renderer>().enabled = false;

        root.AddComponent<NetworkObject>();
        root.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        root.AddComponent<CwslBossSkillProjectile>();

        return SavePrefab(root, CwslPrefabPaths.Projectiles.BossSkill);
    }

    private static GameObject BuildPlayerMissilePrefab()
    {
        var root = new GameObject("CwslPlayerMissile");
        root.layer = LayerMask.NameToLayer(CwslGameConstants.LayerProjectile);

        var collider = root.AddComponent<SphereCollider>();
        collider.radius = 0.35f;
        collider.isTrigger = true;

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shaft.name = "ArrowShaft";
        shaft.transform.SetParent(root.transform, false);
        shaft.transform.localPosition = new Vector3(0f, 0f, 0.18f);
        shaft.transform.localScale = new Vector3(0.1f, 0.1f, 0.42f);
        Object.DestroyImmediate(shaft.GetComponent<Collider>());
        CwslMaterialUtil.ApplyColor(shaft.GetComponent<Renderer>(), new Color(0.85f, 0.72f, 0.35f));

        var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "ArrowHead";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 0f, 0.44f);
        head.transform.localScale = new Vector3(0.14f, 0.14f, 0.12f);
        Object.DestroyImmediate(head.GetComponent<Collider>());
        CwslMaterialUtil.ApplyColor(head.GetComponent<Renderer>(), new Color(0.72f, 0.76f, 0.82f));

        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "HitProxy";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = Vector3.one * 0.12f;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.GetComponent<Renderer>().enabled = false;

        root.AddComponent<NetworkObject>();
        root.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        root.AddComponent<CwslPlayerProjectile>();
        root.AddComponent<CwslPlayerProjectileVisual>();

        return SavePrefab(root, CwslPrefabPaths.Projectiles.PlayerMissile);
    }

    private static GameObject BuildFrozenOrbPrefab()
    {
        var root = new GameObject("CwslFrozenOrb");
        root.layer = LayerMask.NameToLayer(CwslGameConstants.LayerPlayer);

        var collider = root.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.42f;

        root.AddComponent<CwslFrozenOrbEmitter>();

        root.AddComponent<NetworkObject>();
        root.AddComponent<Unity.Netcode.Components.NetworkTransform>();
        root.AddComponent<CwslFrozenOrbProjectile>();
        root.AddComponent<CwslFrozenOrbVisual>();

        return SavePrefab(root, CwslPrefabPaths.Projectiles.FrozenOrb);
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        CwslMonsterVisualMaterialBake.Bake(root);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        CwslMonsterVisualMaterialBake.EmbedIntoSavedPrefab(path);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void BuildScene(CwslGameAssets assets, NetworkPrefabsList networkPrefabs)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "ArenaPlane";
        plane.transform.localScale = new Vector3(10f, 1f, 10f);
        var planeRenderer = plane.GetComponent<Renderer>();
        if (planeRenderer != null)
            planeRenderer.sharedMaterial = CwslMaterialUtil.CreateMatteColored(CwslGameConstants.ArenaFloorColor);

        var navMeshSurface = plane.AddComponent<NavMeshSurface>();
        navMeshSurface.center = new Vector3(0f, 0f, 0f);
        navMeshSurface.size = new Vector3(100f, 20f, 100f);
        navMeshSurface.BuildNavMesh();

        var light = new GameObject("Directional Light");
        var directional = light.AddComponent<Light>();
        directional.type = LightType.Directional;
        directional.intensity = 0.45f;
        directional.color = new Color(0.45f, 0.5f, 0.65f);
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.12f, 0.13f, 0.16f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.05f);
        // 카메라 거리(~24) 기준 — 플레이어 주변은 보이고 멀리만 어두움
        RenderSettings.fogStartDistance = 26f;
        RenderSettings.fogEndDistance = 40f;

        var networkRoot = new GameObject("NetworkManager");
        var networkManager = networkRoot.AddComponent<NetworkManager>();
        var transport = networkRoot.AddComponent<UnityTransport>();
        transport.ConnectionData.Port = CwslGameConstants.GameNetcodePort;
        networkManager.NetworkConfig.PlayerPrefab = assets.playerPrefab;
        networkManager.NetworkConfig.NetworkTransport = transport;
        networkManager.NetworkConfig.ConnectionApproval = true;
        if (networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists == null)
            networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists = new System.Collections.Generic.List<NetworkPrefabsList>();
        networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
        networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(networkPrefabs);

        var bootstrap = networkRoot.AddComponent<CwslNetworkBootstrap>();
        var bootstrapSerialized = new SerializedObject(bootstrap);
        bootstrapSerialized.FindProperty("gameAssets").objectReferenceValue = assets;
        bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

        var systems = new GameObject("CwslGameSystems");
        systems.AddComponent<NetworkObject>();
        systems.AddComponent<CwslTeamGoldCollectedSystem>();
        systems.AddComponent<CwslMonsterSpawner>();
        systems.AddComponent<CwslGameSession>();
        systems.AddComponent<CwslGameFlow>();
        systems.AddComponent<CwslNetworkPoolService>();

        if (CwslGameConstants.UseDefenseMode)
        {
            systems.AddComponent<CwslMonsterManager>();
            systems.AddComponent<CwslDefenseModeController>();
        }
        else
        {
            systems.AddComponent<CwslKarmaSystem>();
            systems.AddComponent<CwslBossWatchState>();
            systems.AddComponent<CwslArenaGimmickSystem>();
            systems.AddComponent<CwslArenaTrapSystem>();
            systems.AddComponent<CwslArenaHazardPadSystem>();
            systems.AddComponent<CwslArenaBuffSystem>();
            systems.AddComponent<CwslArenaDynamicZoneSystem>();
        }

        var session = systems.GetComponent<CwslGameSession>();
        var sessionSerialized = new SerializedObject(session);
        sessionSerialized.FindProperty("assets").objectReferenceValue = assets;
        sessionSerialized.FindProperty("monsterSpawner").objectReferenceValue = systems.GetComponent<CwslMonsterSpawner>();
        sessionSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static GameObject BuildNexusPrefab()
    {
        var root = new GameObject("CwslNexus");

        CwslNexusVisualBuilder.Build(root.transform);

        var collider = root.AddComponent<CapsuleCollider>();
        CwslNexusVisualBuilder.ConfigureHitCollider(collider);

        root.AddComponent<NetworkObject>();
        root.AddComponent<CwslNexus>();
        root.AddComponent<CwslNexusVisual>();

        return SavePrefab(root, CwslPrefabPaths.Defense.Nexus);
    }

    private static GameObject BuildEnemyBasePrefab()
    {
        var root = new GameObject("CwslEnemyBase");

        CwslEnemyBaseVisualBuilder.Build(root.transform);

        var collider = root.AddComponent<CapsuleCollider>();
        collider.isTrigger = true;
        collider.direction = 1;
        collider.center = new Vector3(0f, 1.35f, 0f);
        collider.height = 2.7f;
        collider.radius = 1.15f;

        root.AddComponent<NetworkObject>();
        root.AddComponent<CwslEnemyBase>();

        return SavePrefab(root, CwslPrefabPaths.Defense.EnemyBase);
    }

    private static void UpdateBuildSettings()
    {
        var scenes = new[]
        {
            "Assets/Game/0Scene/LobbyScene.unity",
            ScenePath,
            "Assets/Game/0Scene/SplashScene.unity",
            "Assets/Game/0Scene/LoadingScene.unity",
            "Assets/Game/0Scene/TestScene.unity"
        };

        var buildScenes = new EditorBuildSettingsScene[scenes.Length];
        for (var i = 0; i < scenes.Length; i++)
            buildScenes[i] = new EditorBuildSettingsScene(scenes[i], i <= 1);

        EditorBuildSettings.scenes = buildScenes;
    }

    private static void WireLobbySceneName()
    {
        var lobbyScene = EditorSceneManager.OpenScene("Assets/Game/0Scene/LobbyScene.unity", OpenSceneMode.Single);
        var bootstrap = Object.FindFirstObjectByType<LobbySceneBootstrap>();
        if (bootstrap == null)
            return;

        var network = Object.FindFirstObjectByType<LobbyNetworkManager>();
        if (network == null)
        {
            var managerObject = new GameObject("LobbyNetworkManager");
            network = managerObject.AddComponent<LobbyNetworkManager>();
        }

        network.GameSceneName = CwslGameConstants.GameSceneName;
        EditorUtility.SetDirty(network);
        EditorSceneManager.SaveScene(lobbyScene);
    }
}
#endif
