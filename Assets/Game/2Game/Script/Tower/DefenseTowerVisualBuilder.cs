using UnityEngine;

/// <summary>
/// 타워 비주얼: 속성·종류별 프로시저럴 실루엣 + TurretPivot / FirePoint.
/// </summary>
public static class DefenseTowerVisualBuilder
{
    public const string TurretPivotName = "TurretPivot";
    public const string FirePointName = "FirePoint";

    private static readonly Vector3 DefaultBarrelLocalPosition = new(0f, 0.12f, 0.34f);
    private static readonly Vector3 DefaultBarrelLocalScale = new(0.14f, 0.14f, 0.52f);
    private static readonly Vector3 DefaultFirePointLocalPosition = new(0f, 0.12f, 0.64f);

    public static Transform Build(
        Transform root,
        TowerSpawnData data,
        int sheetTowerId,
        TowerKind kind,
        string prefabKeyOverride = null)
    {
        var visualRoot = new GameObject("Visual").transform;
        visualRoot.SetParent(root, false);

        string prefabKey = prefabKeyOverride;
        if (string.IsNullOrWhiteSpace(prefabKey)
            && DataManager.Instance != null
            && sheetTowerId > 0
            && DataManager.Instance.TryGetTower(sheetTowerId, out var towerData))
        {
            prefabKey = towerData.ResolvePrefabKey();
        }

        var element = ResolveTowerElement(sheetTowerId, data, kind);
        var archetype = DefenseTowerVisualArchetypes.Resolve(prefabKey, sheetTowerId, kind, element);
        Vector3 fireLocal = BuildArchetype(visualRoot, archetype, element);

        return EnsureFirePoint(root, fireLocal);
    }

    public static void AttachAimController(GameObject tower, TowerController towerController)
    {
        if (tower == null || towerController == null)
            return;

        var pivot = FindTurretPivot(tower.transform);
        if (pivot == null)
            return;

        var aim = tower.GetComponent<DefenseTowerAimController>();
        if (aim == null)
            aim = tower.AddComponent<DefenseTowerAimController>();

        aim.ConfigureFromTower(towerController);
        DefenseTowerSustainedPresentation.TryAttach(tower, towerController);
    }

    public static void AttachAimController(GameObject tower, float attackRange, string targetMobility = null)
    {
        if (tower == null)
            return;

        var pivot = FindTurretPivot(tower.transform);
        if (pivot == null)
            return;

        var aim = tower.GetComponent<DefenseTowerAimController>();
        if (aim == null)
            aim = tower.AddComponent<DefenseTowerAimController>();

        aim.Configure(pivot, attackRange, targetMobility);
    }

    public static Transform FindFirePoint(Transform towerRoot)
    {
        var pivot = FindTurretPivot(towerRoot);
        if (pivot != null)
        {
            var onPivot = pivot.Find(FirePointName);
            if (onPivot != null)
                return onPivot;
        }

        var visual = towerRoot.Find("Visual");
        if (visual != null)
        {
            var onVisual = visual.Find(FirePointName);
            if (onVisual != null)
                return onVisual;
        }

        return towerRoot.Find(FirePointName);
    }

    public static Transform FindTurretPivot(Transform towerRoot)
    {
        var visual = towerRoot.Find("Visual");
        return visual != null ? visual.Find(TurretPivotName) : null;
    }

    private static Vector3 BuildArchetype(
        Transform visualRoot,
        DefenseTowerVisualArchetype archetype,
        DefenseSkillElement element)
    {
        return archetype switch
        {
            DefenseTowerVisualArchetype.GatlingBunker => BuildGatlingBunker(visualRoot, element),
            DefenseTowerVisualArchetype.TwinGatling => BuildTwinGatling(visualRoot, element),
            DefenseTowerVisualArchetype.SiegeMortar => BuildSiegeMortar(visualRoot, element),
            DefenseTowerVisualArchetype.RailSentry => BuildRailSentry(visualRoot, element),
            DefenseTowerVisualArchetype.MinigunStriker => BuildMinigunStriker(visualRoot, element),
            DefenseTowerVisualArchetype.ForgeMortar => BuildForgeMortar(visualRoot, element),
            DefenseTowerVisualArchetype.InfernoNozzle => BuildInfernoNozzle(visualRoot, element),
            DefenseTowerVisualArchetype.EmberCatapult => BuildEmberCatapult(visualRoot, element),
            DefenseTowerVisualArchetype.CrystalSpire => BuildCrystalSpire(visualRoot, element),
            DefenseTowerVisualArchetype.GlacialLance => BuildGlacialLance(visualRoot, element),
            DefenseTowerVisualArchetype.BlizzardPod => BuildBlizzardPod(visualRoot, element),
            DefenseTowerVisualArchetype.TeslaCoil => BuildTeslaCoil(visualRoot, element),
            DefenseTowerVisualArchetype.StormDish => BuildStormDish(visualRoot, element),
            DefenseTowerVisualArchetype.ArcRepeater => BuildArcRepeater(visualRoot, element),
            DefenseTowerVisualArchetype.ToxicLab => BuildToxicLab(visualRoot, element),
            DefenseTowerVisualArchetype.VenomSprayer => BuildVenomSprayer(visualRoot, element),
            DefenseTowerVisualArchetype.PlagueMortar => BuildPlagueMortar(visualRoot, element),
            DefenseTowerVisualArchetype.MagmaGeyser => BuildMagmaGeyser(visualRoot, element),
            DefenseTowerVisualArchetype.SolarSpinner => BuildSolarSpinner(visualRoot, element),
            DefenseTowerVisualArchetype.HellgateCannon => BuildHellgateCannon(visualRoot, element),
            DefenseTowerVisualArchetype.GlacierCannon => BuildGlacierCannon(visualRoot, element),
            DefenseTowerVisualArchetype.PermafrostBell => BuildPermafrostBell(visualRoot, element),
            DefenseTowerVisualArchetype.RimeWidow => BuildRimeWidow(visualRoot, element),
            DefenseTowerVisualArchetype.BoltSpear => BuildBoltSpear(visualRoot, element),
            DefenseTowerVisualArchetype.StaticNimbus => BuildStaticNimbus(visualRoot, element),
            DefenseTowerVisualArchetype.TeslaMaul => BuildTeslaMaul(visualRoot, element),
            DefenseTowerVisualArchetype.CorrosionPit => BuildCorrosionPit(visualRoot, element),
            DefenseTowerVisualArchetype.StingerHive => BuildStingerHive(visualRoot, element),
            DefenseTowerVisualArchetype.PandemicBomb => BuildPandemicBomb(visualRoot, element),
            DefenseTowerVisualArchetype.DiabloOrbSpire => BuildDiabloOrbSpire(visualRoot, element),
            DefenseTowerVisualArchetype.MeteorBeacon => BuildMeteorBeacon(visualRoot, element),
            DefenseTowerVisualArchetype.ChainSpire => BuildChainSpire(visualRoot, element),
            DefenseTowerVisualArchetype.SummonBarracks => BuildSummonBarracks(visualRoot, element),
            _ => BuildCannonTower(visualRoot, element)
        };
    }

    private static Vector3 BuildGatlingBunker(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(element);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.42f);

        CreatePart(baseRoot, "BunkerSlab", Vector3.zero, new Vector3(1.18f, 0.16f, 1.12f), colors.Secondary);
        CreatePart(baseRoot, "BunkerBody", new Vector3(0f, 0.14f, -0.04f), new Vector3(0.82f, 0.28f, 0.72f), colors.Base);
        CreatePart(baseRoot, "Sandbag_L", new Vector3(-0.46f, 0.08f, 0.34f), new Vector3(0.22f, 0.12f, 0.16f), colors.Secondary);
        CreatePart(baseRoot, "Sandbag_R", new Vector3(0.46f, 0.08f, 0.34f), new Vector3(0.22f, 0.12f, 0.16f), colors.Secondary);
        CreatePart(baseRoot, "Antenna", new Vector3(0.34f, 0.34f, -0.28f), new Vector3(0.04f, 0.28f, 0.04f), colors.Accent, true);

        CreatePart(turret, "GunMount", new Vector3(0f, 0.04f, 0f), new Vector3(0.48f, 0.18f, 0.42f), colors.Base);
        CreatePart(turret, "Barrel_L", new Vector3(-0.14f, 0.1f, 0.34f), new Vector3(0.1f, 0.1f, 0.46f), colors.Accent, true);
        CreatePart(turret, "Barrel_R", new Vector3(0.14f, 0.1f, 0.34f), new Vector3(0.1f, 0.1f, 0.46f), colors.Accent, true);
        CreatePart(turret, "AmmoBox", new Vector3(0f, 0.12f, -0.12f), new Vector3(0.24f, 0.14f, 0.18f), colors.Secondary);

        return new Vector3(0f, 0.1f, 0.62f);
    }

    private static Vector3 BuildTwinGatling(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(element);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.4f);

        CreatePart(baseRoot, "BunkerSlab", Vector3.zero, new Vector3(1.24f, 0.18f, 1.16f), colors.Secondary);
        CreatePart(baseRoot, "AmmoDrum", new Vector3(0.36f, 0.2f, -0.18f), new Vector3(0.28f, 0.28f, 0.28f), colors.Base, false, PrimitiveType.Cylinder);
        CreatePart(turret, "RotorHub", new Vector3(0f, 0.08f, 0f), new Vector3(0.56f, 0.12f, 0.56f), colors.Base);
        CreatePart(turret, "Barrel_A", new Vector3(-0.18f, 0.12f, 0.36f), new Vector3(0.08f, 0.08f, 0.52f), colors.Accent, true);
        CreatePart(turret, "Barrel_B", new Vector3(-0.06f, 0.12f, 0.38f), new Vector3(0.08f, 0.08f, 0.52f), colors.Accent, true);
        CreatePart(turret, "Barrel_C", new Vector3(0.06f, 0.12f, 0.38f), new Vector3(0.08f, 0.08f, 0.52f), colors.Accent, true);
        CreatePart(turret, "Barrel_D", new Vector3(0.18f, 0.12f, 0.36f), new Vector3(0.08f, 0.08f, 0.52f), colors.Accent, true);
        CreatePart(turret, "SightRail", new Vector3(0f, 0.22f, 0.12f), new Vector3(0.34f, 0.04f, 0.06f), colors.Secondary, true);

        return new Vector3(0f, 0.12f, 0.66f);
    }

    private static Vector3 BuildSiegeMortar(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(element);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.38f);

        CreatePart(baseRoot, "Track_L", new Vector3(-0.42f, 0.06f, 0f), new Vector3(0.14f, 0.1f, 1.1f), colors.Secondary);
        CreatePart(baseRoot, "Track_R", new Vector3(0.42f, 0.06f, 0f), new Vector3(0.14f, 0.1f, 1.1f), colors.Secondary);
        CreatePart(baseRoot, "Hull", new Vector3(0f, 0.18f, 0f), new Vector3(0.92f, 0.34f, 0.78f), colors.Base);
        CreatePart(baseRoot, "ArmorPlate_L", new Vector3(-0.48f, 0.22f, 0.08f), new Vector3(0.08f, 0.28f, 0.52f), colors.Secondary);
        CreatePart(baseRoot, "ArmorPlate_R", new Vector3(0.48f, 0.22f, 0.08f), new Vector3(0.08f, 0.28f, 0.52f), colors.Secondary);
        CreatePart(turret, "SiegeTube", new Vector3(0f, 0.16f, 0.22f), new Vector3(0.22f, 0.22f, 0.58f), Quaternion.Euler(42f, 0f, 0f), colors.Accent, true);
        CreatePart(turret, "Breech", new Vector3(0f, 0.08f, -0.08f), new Vector3(0.28f, 0.2f, 0.24f), colors.Base);

        return new Vector3(0f, 0.24f, 0.56f);
    }

    private static Vector3 BuildRailSentry(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(element);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.22f);

        CreatePart(baseRoot, "RailPad", Vector3.zero, new Vector3(0.88f, 0.12f, 1.28f), colors.Secondary);
        CreatePart(baseRoot, "Pillar_L", new Vector3(-0.28f, 0.28f, -0.34f), new Vector3(0.1f, 0.56f, 0.1f), colors.Base);
        CreatePart(baseRoot, "Pillar_R", new Vector3(0.28f, 0.28f, -0.34f), new Vector3(0.1f, 0.56f, 0.1f), colors.Base);
        CreatePart(turret, "RailBody", new Vector3(0f, 0.24f, 0f), new Vector3(0.18f, 0.18f, 0.82f), colors.Base);
        CreatePart(turret, "RailGlow", new Vector3(0f, 0.24f, 0.18f), new Vector3(0.08f, 0.08f, 0.62f), colors.Accent, true);
        CreatePart(turret, "Scope", new Vector3(0f, 0.34f, -0.06f), new Vector3(0.12f, 0.1f, 0.18f), colors.Secondary, true);
        CreatePart(turret, "Capacitor", new Vector3(0f, 0.42f, 0.1f), new Vector3(0.14f, 0.14f, 0.14f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.26f, 0.72f);
    }

    private static Vector3 BuildMinigunStriker(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(element);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.38f);

        CreatePart(baseRoot, "StrikerPad", Vector3.zero, new Vector3(1.14f, 0.14f, 1.02f), colors.Secondary);
        CreatePart(baseRoot, "AmmoCrate", new Vector3(0.38f, 0.1f, -0.18f), new Vector3(0.24f, 0.16f, 0.2f), colors.Base);
        CreatePart(turret, "RotorCore", new Vector3(0f, 0.1f, 0f), new Vector3(0.5f, 0.14f, 0.5f), colors.Base, false, PrimitiveType.Cylinder);
        CreatePart(turret, "Barrel_1", new Vector3(-0.16f, 0.14f, 0.34f), new Vector3(0.07f, 0.07f, 0.44f), colors.Accent, true);
        CreatePart(turret, "Barrel_2", new Vector3(-0.05f, 0.14f, 0.38f), new Vector3(0.07f, 0.07f, 0.44f), colors.Accent, true);
        CreatePart(turret, "Barrel_3", new Vector3(0.05f, 0.14f, 0.38f), new Vector3(0.07f, 0.07f, 0.44f), colors.Accent, true);
        CreatePart(turret, "Barrel_4", new Vector3(0.16f, 0.14f, 0.34f), new Vector3(0.07f, 0.07f, 0.44f), colors.Accent, true);
        CreatePart(turret, "Sight", new Vector3(0f, 0.24f, 0.1f), new Vector3(0.18f, 0.05f, 0.08f), colors.Secondary, true);

        return new Vector3(0f, 0.14f, 0.62f);
    }

    private static Vector3 BuildInfernoNozzle(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(element);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.36f);

        CreatePart(baseRoot, "HeatPlate", Vector3.zero, new Vector3(1.02f, 0.14f, 0.92f), colors.Secondary);
        CreatePart(baseRoot, "FuelTank_L", new Vector3(-0.32f, 0.16f, -0.12f), new Vector3(0.18f, 0.28f, 0.18f), colors.Base, false, PrimitiveType.Cylinder);
        CreatePart(baseRoot, "FuelTank_R", new Vector3(0.32f, 0.16f, -0.12f), new Vector3(0.18f, 0.28f, 0.18f), colors.Base, false, PrimitiveType.Cylinder);
        CreatePart(turret, "NozzleBody", new Vector3(0f, 0.1f, 0.04f), new Vector3(0.42f, 0.22f, 0.36f), colors.Base);
        CreatePart(turret, "FlamePipe_A", new Vector3(-0.1f, 0.12f, 0.32f), new Vector3(0.08f, 0.08f, 0.34f), colors.Accent, true);
        CreatePart(turret, "FlamePipe_B", new Vector3(0.1f, 0.12f, 0.32f), new Vector3(0.08f, 0.08f, 0.34f), colors.Accent, true);
        CreatePart(turret, "Igniter", new Vector3(0f, 0.16f, 0.46f), new Vector3(0.12f, 0.12f, 0.12f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.14f, 0.58f);
    }

    private static Vector3 BuildEmberCatapult(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Fire);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.34f);

        CreatePart(baseRoot, "CatapultBase", Vector3.zero, new Vector3(1.08f, 0.16f, 1.02f), colors.Secondary);
        CreatePart(baseRoot, "Wheel_L", new Vector3(-0.42f, 0.1f, -0.2f), new Vector3(0.16f, 0.16f, 0.08f), colors.Base, false, PrimitiveType.Cylinder);
        CreatePart(baseRoot, "Wheel_R", new Vector3(0.42f, 0.1f, -0.2f), new Vector3(0.16f, 0.16f, 0.08f), colors.Base, false, PrimitiveType.Cylinder);
        CreatePart(turret, "ThrowArm", new Vector3(0f, 0.18f, 0.04f), new Vector3(0.12f, 0.12f, 0.62f), Quaternion.Euler(48f, 0f, 0f), colors.Base);
        CreatePart(turret, "EmberCup", new Vector3(0f, 0.34f, 0.28f), new Vector3(0.22f, 0.1f, 0.22f), colors.Accent, true, PrimitiveType.Cylinder);
        CreatePart(turret, "PilotFlame", new Vector3(0f, 0.38f, 0.36f), new Vector3(0.1f, 0.1f, 0.1f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.3f, 0.5f);
    }

    private static Vector3 BuildGlacialLance(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Ice);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.3f);

        CreatePart(baseRoot, "FrostPad", Vector3.zero, new Vector3(0.96f, 0.12f, 0.96f), colors.Secondary);
        CreatePart(baseRoot, "IceFin_L", new Vector3(-0.3f, 0.1f, 0.2f), new Vector3(0.08f, 0.24f, 0.2f), colors.Accent, true);
        CreatePart(baseRoot, "IceFin_R", new Vector3(0.3f, 0.1f, 0.2f), new Vector3(0.08f, 0.24f, 0.2f), colors.Accent, true);
        CreatePart(turret, "LanceMount", new Vector3(0f, 0.08f, 0f), new Vector3(0.28f, 0.16f, 0.28f), colors.Base);
        CreatePart(turret, "LanceBeam", new Vector3(0f, 0.14f, 0.42f), new Vector3(0.06f, 0.06f, 0.72f), colors.Accent, true);
        CreatePart(turret, "LanceTip", new Vector3(0f, 0.14f, 0.78f), new Vector3(0.1f, 0.1f, 0.1f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.14f, 0.82f);
    }

    private static Vector3 BuildBlizzardPod(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Ice);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.32f);

        CreatePart(baseRoot, "SnowRing", Vector3.zero, new Vector3(1.12f, 0.1f, 1.12f), colors.Secondary);
        CreatePart(baseRoot, "CrystalCluster", new Vector3(0f, 0.12f, -0.18f), new Vector3(0.34f, 0.18f, 0.18f), colors.Accent, true);
        CreatePart(turret, "PodStem", new Vector3(0f, 0.14f, 0f), new Vector3(0.12f, 0.22f, 0.12f), colors.Base);
        CreatePart(turret, "BlizzardDome", new Vector3(0f, 0.3f, 0f), new Vector3(0.62f, 0.34f, 0.62f), colors.Accent, true, PrimitiveType.Sphere);
        CreatePart(turret, "SnowVent", new Vector3(0f, 0.42f, 0.16f), new Vector3(0.16f, 0.06f, 0.16f), colors.Secondary, true, PrimitiveType.Cylinder);

        return new Vector3(0f, 0.38f, 0.22f);
    }

    private static Vector3 BuildArcRepeater(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Lightning);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.28f);

        CreatePart(baseRoot, "ArcBase", Vector3.zero, new Vector3(0.94f, 0.14f, 0.94f), colors.Base);
        CreatePart(baseRoot, "Conduit_L", new Vector3(-0.34f, 0.12f, 0f), new Vector3(0.06f, 0.06f, 0.5f), colors.Secondary);
        CreatePart(baseRoot, "Conduit_R", new Vector3(0.34f, 0.12f, 0f), new Vector3(0.06f, 0.06f, 0.5f), colors.Secondary);
        CreatePart(turret, "RepeaterPole", new Vector3(0f, 0.24f, 0f), new Vector3(0.1f, 0.48f, 0.1f), colors.Secondary, false, PrimitiveType.Cylinder);
        CreatePart(turret, "ArcRing_A", new Vector3(0f, 0.18f, 0f), new Vector3(0.38f, 0.04f, 0.38f), colors.Accent, true);
        CreatePart(turret, "ArcRing_B", new Vector3(0f, 0.36f, 0f), new Vector3(0.28f, 0.04f, 0.28f), colors.Accent, true);
        CreatePart(turret, "ArcTip", new Vector3(0f, 0.56f, 0f), new Vector3(0.08f, 0.08f, 0.08f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.58f, 0f);
    }

    private static Vector3 BuildVenomSprayer(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Poison);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.36f);

        CreatePart(baseRoot, "LabPad", Vector3.zero, new Vector3(1.04f, 0.14f, 1.02f), colors.Secondary);
        CreatePart(baseRoot, "ReagentJar", new Vector3(-0.34f, 0.18f, -0.1f), new Vector3(0.16f, 0.22f, 0.16f), colors.Accent, true, PrimitiveType.Cylinder);
        CreatePart(turret, "SprayerBody", new Vector3(0f, 0.12f, 0f), new Vector3(0.4f, 0.2f, 0.34f), colors.Base);
        CreatePart(turret, "SprayNozzle_L", new Vector3(-0.12f, 0.1f, 0.34f), new Vector3(0.08f, 0.08f, 0.28f), colors.Accent, true);
        CreatePart(turret, "SprayNozzle_R", new Vector3(0.12f, 0.1f, 0.34f), new Vector3(0.08f, 0.08f, 0.28f), colors.Accent, true);
        CreatePart(turret, "ToxicBulb", new Vector3(0f, 0.18f, 0.42f), new Vector3(0.14f, 0.14f, 0.14f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.12f, 0.54f);
    }

    private static Vector3 BuildPlagueMortar(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Poison);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.34f);

        CreatePart(baseRoot, "CauldronBase", Vector3.zero, new Vector3(1.02f, 0.16f, 1.02f), colors.Secondary);
        CreatePart(baseRoot, "BonesPile", new Vector3(0.34f, 0.08f, -0.24f), new Vector3(0.22f, 0.08f, 0.16f), colors.Base);
        CreatePart(turret, "Cauldron", new Vector3(0f, 0.14f, 0f), new Vector3(0.42f, 0.22f, 0.42f), colors.Base, false, PrimitiveType.Cylinder);
        CreatePart(turret, "BubblingCore", new Vector3(0f, 0.28f, 0f), new Vector3(0.24f, 0.12f, 0.24f), colors.Accent, true, PrimitiveType.Sphere);
        CreatePart(turret, "LobArm", new Vector3(0f, 0.2f, 0.18f), new Vector3(0.1f, 0.1f, 0.36f), Quaternion.Euler(36f, 0f, 0f), colors.Secondary);

        return new Vector3(0f, 0.28f, 0.46f);
    }

    private static Vector3 BuildMagmaGeyser(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Fire);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.32f);

        CreatePart(baseRoot, "CrackRing", Vector3.zero, new Vector3(1.06f, 0.1f, 1.06f), colors.Secondary);
        CreatePart(baseRoot, "Vent_L", new Vector3(-0.28f, 0.06f, 0.12f), new Vector3(0.14f, 0.08f, 0.14f), colors.Base);
        CreatePart(baseRoot, "Vent_R", new Vector3(0.28f, 0.06f, -0.1f), new Vector3(0.14f, 0.08f, 0.14f), colors.Base);
        CreatePart(turret, "GeyserStack", new Vector3(0f, 0.12f, 0f), new Vector3(0.2f, 0.34f, 0.2f), colors.Base, false, PrimitiveType.Cylinder);
        CreatePart(turret, "LavaMouth", new Vector3(0f, 0.32f, 0.08f), new Vector3(0.28f, 0.08f, 0.28f), colors.Accent, true, PrimitiveType.Cylinder);
        CreatePart(turret, "MagmaGlow", new Vector3(0f, 0.38f, 0.1f), new Vector3(0.12f, 0.12f, 0.12f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.36f, 0.22f);
    }

    private static Vector3 BuildSolarSpinner(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Fire);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.3f);

        CreatePart(baseRoot, "SunPad", Vector3.zero, new Vector3(0.98f, 0.12f, 0.98f), colors.Secondary);
        CreatePart(baseRoot, "RayFin_A", new Vector3(0f, 0.08f, 0.38f), new Vector3(0.06f, 0.06f, 0.22f), colors.Accent, true);
        CreatePart(baseRoot, "RayFin_B", new Vector3(0.38f, 0.08f, 0f), new Vector3(0.22f, 0.06f, 0.06f), colors.Accent, true);
        CreatePart(turret, "SpinnerHub", new Vector3(0f, 0.16f, 0f), new Vector3(0.34f, 0.1f, 0.34f), colors.Base, false, PrimitiveType.Cylinder);
        CreatePart(turret, "FlameSpoke_A", new Vector3(0f, 0.18f, 0.28f), new Vector3(0.08f, 0.08f, 0.36f), colors.Accent, true);
        CreatePart(turret, "FlameSpoke_B", new Vector3(0.28f, 0.18f, 0f), new Vector3(0.36f, 0.08f, 0.08f), colors.Accent, true);
        CreatePart(turret, "CoreFlare", new Vector3(0f, 0.22f, 0f), new Vector3(0.14f, 0.14f, 0.14f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.2f, 0.52f);
    }

    private static Vector3 BuildHellgateCannon(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Fire);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.28f);

        CreatePart(baseRoot, "RuneSlab", Vector3.zero, new Vector3(1.1f, 0.14f, 1.1f), colors.Secondary);
        CreatePart(baseRoot, "Pillar_L", new Vector3(-0.36f, 0.22f, -0.2f), new Vector3(0.1f, 0.44f, 0.1f), colors.Base);
        CreatePart(baseRoot, "Pillar_R", new Vector3(0.36f, 0.22f, -0.2f), new Vector3(0.1f, 0.44f, 0.1f), colors.Base);
        CreatePart(turret, "GateFrame", new Vector3(0f, 0.28f, 0.04f), new Vector3(0.52f, 0.48f, 0.1f), colors.Base);
        CreatePart(turret, "HellPortal", new Vector3(0f, 0.3f, 0.12f), new Vector3(0.28f, 0.36f, 0.06f), colors.Accent, true);
        CreatePart(turret, "InfernoEye", new Vector3(0f, 0.34f, 0.2f), new Vector3(0.12f, 0.12f, 0.12f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.34f, 0.28f);
    }

    private static Vector3 BuildGlacierCannon(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Ice);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.28f);

        CreatePart(baseRoot, "IceBergBase", Vector3.zero, new Vector3(1.08f, 0.16f, 0.92f), colors.Secondary);
        CreatePart(baseRoot, "Shard_L", new Vector3(-0.34f, 0.14f, 0.1f), new Vector3(0.1f, 0.28f, 0.1f), colors.Accent, true);
        CreatePart(baseRoot, "Shard_R", new Vector3(0.34f, 0.12f, -0.06f), new Vector3(0.1f, 0.24f, 0.1f), colors.Accent, true);
        CreatePart(turret, "CannonBore", new Vector3(0f, 0.14f, 0.1f), new Vector3(0.16f, 0.16f, 0.58f), colors.Base);
        CreatePart(turret, "IceRail", new Vector3(0f, 0.14f, 0.42f), new Vector3(0.06f, 0.06f, 0.48f), colors.Accent, true);
        CreatePart(turret, "FrostCap", new Vector3(0f, 0.14f, 0.72f), new Vector3(0.12f, 0.12f, 0.12f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.14f, 0.78f);
    }

    private static Vector3 BuildPermafrostBell(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Ice);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.26f);

        CreatePart(baseRoot, "BellPlatform", Vector3.zero, new Vector3(1.04f, 0.12f, 1.04f), colors.Secondary);
        CreatePart(baseRoot, "IcePillar", new Vector3(0f, 0.2f, -0.18f), new Vector3(0.14f, 0.4f, 0.14f), colors.Base);
        CreatePart(turret, "BellStem", new Vector3(0f, 0.18f, 0f), new Vector3(0.1f, 0.2f, 0.1f), colors.Base);
        CreatePart(turret, "FrostBell", new Vector3(0f, 0.34f, 0f), new Vector3(0.48f, 0.32f, 0.48f), colors.Accent, true, PrimitiveType.Sphere);
        CreatePart(turret, "SnowClapper", new Vector3(0f, 0.22f, 0.16f), new Vector3(0.08f, 0.08f, 0.2f), colors.Secondary, true);

        return new Vector3(0f, 0.28f, 0.24f);
    }

    private static Vector3 BuildRimeWidow(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Ice);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.3f);

        CreatePart(baseRoot, "WebPad", Vector3.zero, new Vector3(1f, 0.1f, 1f), colors.Secondary);
        CreatePart(baseRoot, "Leg_A", new Vector3(-0.32f, 0.08f, 0.28f), new Vector3(0.06f, 0.06f, 0.34f), Quaternion.Euler(28f, -24f, 0f), colors.Base);
        CreatePart(baseRoot, "Leg_B", new Vector3(0.32f, 0.08f, 0.28f), new Vector3(0.06f, 0.06f, 0.34f), Quaternion.Euler(28f, 24f, 0f), colors.Base);
        CreatePart(turret, "WidowBody", new Vector3(0f, 0.16f, 0f), new Vector3(0.34f, 0.18f, 0.28f), colors.Base);
        CreatePart(turret, "IceAbdomen", new Vector3(0f, 0.14f, -0.12f), new Vector3(0.24f, 0.16f, 0.22f), colors.Accent, true, PrimitiveType.Sphere);
        CreatePart(turret, "ShardFang", new Vector3(0f, 0.18f, 0.28f), new Vector3(0.06f, 0.06f, 0.24f), colors.Accent, true);

        return new Vector3(0f, 0.18f, 0.42f);
    }

    private static Vector3 BuildBoltSpear(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Lightning);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.26f);

        CreatePart(baseRoot, "SpearDeck", Vector3.zero, new Vector3(0.96f, 0.12f, 1.12f), colors.Secondary);
        CreatePart(baseRoot, "Capacitor_L", new Vector3(-0.3f, 0.14f, -0.16f), new Vector3(0.1f, 0.22f, 0.1f), colors.Base);
        CreatePart(baseRoot, "Capacitor_R", new Vector3(0.3f, 0.14f, -0.16f), new Vector3(0.1f, 0.22f, 0.1f), colors.Base);
        CreatePart(turret, "SpearLauncher", new Vector3(0f, 0.12f, 0f), new Vector3(0.22f, 0.18f, 0.34f), colors.Base);
        CreatePart(turret, "BoltRail", new Vector3(0f, 0.16f, 0.38f), new Vector3(0.05f, 0.05f, 0.66f), colors.Accent, true);
        CreatePart(turret, "ArcTip", new Vector3(0f, 0.16f, 0.74f), new Vector3(0.1f, 0.1f, 0.1f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.16f, 0.8f);
    }

    private static Vector3 BuildStaticNimbus(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Lightning);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.24f);

        CreatePart(baseRoot, "CloudPad", Vector3.zero, new Vector3(1.06f, 0.1f, 1.06f), colors.Secondary);
        CreatePart(baseRoot, "GroundCoil", new Vector3(0f, 0.08f, 0f), new Vector3(0.5f, 0.06f, 0.5f), colors.Base);
        CreatePart(turret, "NimbusStem", new Vector3(0f, 0.2f, 0f), new Vector3(0.1f, 0.28f, 0.1f), colors.Base);
        CreatePart(turret, "StormCloud", new Vector3(0f, 0.38f, 0f), new Vector3(0.62f, 0.24f, 0.62f), colors.Accent, true, PrimitiveType.Sphere);
        CreatePart(turret, "StaticWisp", new Vector3(0.16f, 0.44f, 0.1f), new Vector3(0.12f, 0.12f, 0.12f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.42f, 0.14f);
    }

    private static Vector3 BuildTeslaMaul(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Lightning);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.3f);

        CreatePart(baseRoot, "MaulBase", Vector3.zero, new Vector3(1.02f, 0.14f, 0.98f), colors.Secondary);
        CreatePart(baseRoot, "BatteryPack", new Vector3(-0.34f, 0.12f, -0.1f), new Vector3(0.16f, 0.24f, 0.2f), colors.Base);
        CreatePart(turret, "MaulArm", new Vector3(0f, 0.18f, 0.02f), new Vector3(0.12f, 0.12f, 0.48f), Quaternion.Euler(18f, 0f, 0f), colors.Base);
        CreatePart(turret, "TeslaHead", new Vector3(0f, 0.28f, 0.28f), new Vector3(0.22f, 0.22f, 0.22f), colors.Accent, true, PrimitiveType.Sphere);
        CreatePart(turret, "ArcProng", new Vector3(0f, 0.34f, 0.38f), new Vector3(0.04f, 0.14f, 0.04f), colors.Accent, true);

        return new Vector3(0f, 0.32f, 0.44f);
    }

    private static Vector3 BuildCorrosionPit(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Poison);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.32f);

        CreatePart(baseRoot, "ToxicBasin", Vector3.zero, new Vector3(1.08f, 0.08f, 1.08f), colors.Secondary);
        CreatePart(baseRoot, "Rim_L", new Vector3(-0.42f, 0.06f, 0f), new Vector3(0.08f, 0.12f, 0.5f), colors.Base);
        CreatePart(baseRoot, "Rim_R", new Vector3(0.42f, 0.06f, 0f), new Vector3(0.08f, 0.12f, 0.5f), colors.Base);
        CreatePart(turret, "AcidPump", new Vector3(0f, 0.12f, 0f), new Vector3(0.18f, 0.24f, 0.18f), colors.Base, false, PrimitiveType.Cylinder);
        CreatePart(turret, "DripNozzle", new Vector3(0f, 0.28f, 0.14f), new Vector3(0.1f, 0.1f, 0.18f), colors.Accent, true);
        CreatePart(turret, "BubbleCore", new Vector3(0f, 0.2f, 0.02f), new Vector3(0.16f, 0.08f, 0.16f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.26f, 0.28f);
    }

    private static Vector3 BuildStingerHive(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Poison);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.34f);

        CreatePart(baseRoot, "HivePad", Vector3.zero, new Vector3(0.96f, 0.12f, 0.96f), colors.Secondary);
        CreatePart(baseRoot, "Honeycomb", new Vector3(0f, 0.1f, -0.08f), new Vector3(0.42f, 0.14f, 0.22f), colors.Base);
        CreatePart(turret, "HiveDome", new Vector3(0f, 0.2f, 0f), new Vector3(0.44f, 0.28f, 0.44f), colors.Accent, true, PrimitiveType.Sphere);
        CreatePart(turret, "Stinger_A", new Vector3(-0.1f, 0.14f, 0.28f), new Vector3(0.05f, 0.05f, 0.22f), colors.Accent, true);
        CreatePart(turret, "Stinger_B", new Vector3(0.1f, 0.14f, 0.3f), new Vector3(0.05f, 0.05f, 0.22f), colors.Accent, true);
        CreatePart(turret, "Stinger_C", new Vector3(0f, 0.16f, 0.32f), new Vector3(0.05f, 0.05f, 0.24f), colors.Accent, true);

        return new Vector3(0f, 0.16f, 0.46f);
    }

    private static Vector3 BuildPandemicBomb(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Poison);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.32f);

        CreatePart(baseRoot, "BioLabPad", Vector3.zero, new Vector3(1.04f, 0.14f, 1.04f), colors.Secondary);
        CreatePart(baseRoot, "Canister_L", new Vector3(-0.3f, 0.16f, -0.12f), new Vector3(0.12f, 0.28f, 0.12f), colors.Base, false, PrimitiveType.Cylinder);
        CreatePart(baseRoot, "Canister_R", new Vector3(0.3f, 0.16f, -0.12f), new Vector3(0.12f, 0.28f, 0.12f), colors.Base, false, PrimitiveType.Cylinder);
        CreatePart(turret, "BombRack", new Vector3(0f, 0.12f, 0.04f), new Vector3(0.38f, 0.16f, 0.3f), colors.Base);
        CreatePart(turret, "PlagueOrb", new Vector3(0f, 0.24f, 0.18f), new Vector3(0.22f, 0.22f, 0.22f), colors.Accent, true, PrimitiveType.Sphere);
        CreatePart(turret, "VentStack", new Vector3(0f, 0.34f, 0.08f), new Vector3(0.08f, 0.12f, 0.08f), colors.Secondary, true);

        return new Vector3(0f, 0.28f, 0.34f);
    }

    private static Vector3 BuildForgeMortar(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(element);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.36f);

        CreatePart(baseRoot, "ForgeBase", Vector3.zero, new Vector3(1.02f, 0.2f, 1.02f), colors.Secondary);
        CreatePart(baseRoot, "BrickRing", new Vector3(0f, 0.12f, 0f), new Vector3(0.78f, 0.16f, 0.78f), colors.Base);
        CreatePart(baseRoot, "Chimney", new Vector3(-0.28f, 0.34f, -0.22f), new Vector3(0.16f, 0.42f, 0.16f), colors.Base);
        CreatePart(baseRoot, "EmberVent", new Vector3(-0.28f, 0.58f, -0.22f), new Vector3(0.1f, 0.06f, 0.1f), colors.Accent, true);

        CreatePart(turret, "MortarBowl", new Vector3(0f, 0.06f, 0f), new Vector3(0.52f, 0.14f, 0.52f), colors.Base);
        CreatePart(
            turret,
            "MortarTube",
            new Vector3(0f, 0.18f, 0.18f),
            new Vector3(0.18f, 0.18f, 0.34f),
            Quaternion.Euler(38f, 0f, 0f),
            colors.Accent,
            true);
        CreatePart(turret, "HeatCoil", new Vector3(0.22f, 0.08f, -0.08f), new Vector3(0.08f, 0.08f, 0.08f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.22f, 0.48f);
    }

    private static Vector3 BuildCrystalSpire(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(element);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.32f);

        CreatePart(baseRoot, "IcePad", Vector3.zero, new Vector3(1.08f, 0.14f, 1.08f), colors.Secondary);
        CreatePart(baseRoot, "Shard_FL", new Vector3(-0.34f, 0.12f, 0.34f), new Vector3(0.12f, 0.28f, 0.12f), Quaternion.Euler(0f, 35f, 12f), colors.Accent, true);
        CreatePart(baseRoot, "Shard_FR", new Vector3(0.34f, 0.12f, 0.34f), new Vector3(0.12f, 0.28f, 0.12f), Quaternion.Euler(0f, -35f, -12f), colors.Accent, true);
        CreatePart(baseRoot, "Shard_BL", new Vector3(-0.34f, 0.12f, -0.28f), new Vector3(0.1f, 0.22f, 0.1f), Quaternion.Euler(8f, 20f, 0f), colors.Accent, true);
        CreatePart(baseRoot, "Shard_BR", new Vector3(0.34f, 0.12f, -0.28f), new Vector3(0.1f, 0.22f, 0.1f), Quaternion.Euler(-8f, -20f, 0f), colors.Accent, true);

        CreatePart(turret, "CrystalBase", new Vector3(0f, 0.04f, 0f), new Vector3(0.34f, 0.12f, 0.34f), colors.Base);
        CreatePart(turret, "CrystalSpire", new Vector3(0f, 0.34f, 0f), new Vector3(0.14f, 0.62f, 0.14f), colors.Accent, true);
        CreatePart(turret, "CrystalCap", new Vector3(0f, 0.68f, 0f), new Vector3(0.08f, 0.12f, 0.08f), colors.Secondary, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.72f, 0.08f);
    }

    private static Vector3 BuildTeslaCoil(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(element);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.28f);

        CreatePart(baseRoot, "CoilPlatform", Vector3.zero, new Vector3(0.92f, 0.16f, 0.92f), colors.Base);
        CreatePart(baseRoot, "Post_FL", new Vector3(-0.34f, 0.18f, 0.34f), new Vector3(0.06f, 0.28f, 0.06f), colors.Secondary);
        CreatePart(baseRoot, "Post_FR", new Vector3(0.34f, 0.18f, 0.34f), new Vector3(0.06f, 0.28f, 0.06f), colors.Secondary);
        CreatePart(baseRoot, "Post_BL", new Vector3(-0.34f, 0.18f, -0.34f), new Vector3(0.06f, 0.28f, 0.06f), colors.Secondary);
        CreatePart(baseRoot, "Post_BR", new Vector3(0.34f, 0.18f, -0.34f), new Vector3(0.06f, 0.28f, 0.06f), colors.Secondary);

        CreatePart(turret, "CoilBody", new Vector3(0f, 0.18f, 0f), new Vector3(0.22f, 0.36f, 0.22f), colors.Secondary, false, PrimitiveType.Cylinder);
        CreatePart(turret, "CoilRing", new Vector3(0f, 0.34f, 0f), new Vector3(0.46f, 0.06f, 0.46f), colors.Accent, true);
        CreatePart(turret, "SparkOrb", new Vector3(0f, 0.52f, 0f), new Vector3(0.14f, 0.14f, 0.14f), colors.Accent, true, PrimitiveType.Sphere);
        CreatePart(turret, "RodTip", new Vector3(0f, 0.64f, 0f), new Vector3(0.04f, 0.12f, 0.04f), colors.Accent, true);

        return new Vector3(0f, 0.7f, 0f);
    }

    private static Vector3 BuildStormDish(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(element);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.34f);

        CreatePart(baseRoot, "TripodHub", Vector3.zero, new Vector3(0.42f, 0.12f, 0.42f), colors.Base);
        CreatePart(baseRoot, "Leg_A", new Vector3(0f, 0.06f, 0.38f), new Vector3(0.08f, 0.08f, 0.42f), Quaternion.Euler(18f, 0f, 0f), colors.Secondary);
        CreatePart(baseRoot, "Leg_B", new Vector3(-0.33f, 0.06f, -0.2f), new Vector3(0.08f, 0.08f, 0.42f), Quaternion.Euler(18f, 120f, 0f), colors.Secondary);
        CreatePart(baseRoot, "Leg_C", new Vector3(0.33f, 0.06f, -0.2f), new Vector3(0.08f, 0.08f, 0.42f), Quaternion.Euler(18f, -120f, 0f), colors.Secondary);

        CreatePart(turret, "Mast", new Vector3(0f, 0.12f, 0f), new Vector3(0.1f, 0.24f, 0.1f), colors.Secondary);
        CreatePart(turret, "StormDish", new Vector3(0f, 0.28f, 0f), new Vector3(0.72f, 0.06f, 0.72f), colors.Accent, true, PrimitiveType.Cylinder);
        CreatePart(turret, "DishCore", new Vector3(0f, 0.34f, 0f), new Vector3(0.18f, 0.1f, 0.18f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.38f, 0.18f);
    }

    private static Vector3 BuildToxicLab(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(element);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.38f);

        CreatePart(baseRoot, "LabFloor", Vector3.zero, new Vector3(1.06f, 0.14f, 1.06f), colors.Secondary);
        CreatePart(baseRoot, "Vat_L", new Vector3(-0.34f, 0.18f, 0.08f), new Vector3(0.24f, 0.24f, 0.24f), colors.Accent, true, PrimitiveType.Sphere);
        CreatePart(baseRoot, "Vat_R", new Vector3(0.34f, 0.18f, 0.08f), new Vector3(0.24f, 0.24f, 0.24f), colors.Accent, true, PrimitiveType.Sphere);
        CreatePart(baseRoot, "Pipe_L", new Vector3(-0.34f, 0.08f, 0.24f), new Vector3(0.06f, 0.06f, 0.18f), colors.Base);
        CreatePart(baseRoot, "Pipe_R", new Vector3(0.34f, 0.08f, 0.24f), new Vector3(0.06f, 0.06f, 0.18f), colors.Base);

        CreatePart(turret, "FunnelTop", new Vector3(0f, 0.16f, 0f), new Vector3(0.34f, 0.12f, 0.34f), colors.Base);
        CreatePart(turret, "FunnelNeck", new Vector3(0f, 0.04f, 0.04f), new Vector3(0.18f, 0.16f, 0.18f), colors.Secondary);
        CreatePart(turret, "DripNozzle", new Vector3(0f, 0.08f, 0.28f), new Vector3(0.1f, 0.1f, 0.22f), colors.Accent, true);
        CreatePart(turret, "DripBulb", new Vector3(0f, 0.06f, 0.42f), new Vector3(0.12f, 0.12f, 0.12f), colors.Accent, true, PrimitiveType.Sphere);

        return new Vector3(0f, 0.08f, 0.52f);
    }

    private static Vector3 BuildDiabloOrbSpire(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Ice);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.34f);

        CreatePart(baseRoot, "OrbPedestal", Vector3.zero, new Vector3(1.04f, 0.14f, 1.04f), colors.Secondary);
        CreatePart(baseRoot, "RuneRing", new Vector3(0f, 0.08f, 0f), new Vector3(0.88f, 0.04f, 0.88f), colors.Base, false, PrimitiveType.Cylinder);
        CreatePart(baseRoot, "Pillar_L", new Vector3(-0.36f, 0.18f, -0.1f), new Vector3(0.1f, 0.28f, 0.1f), colors.Base);
        CreatePart(baseRoot, "Pillar_R", new Vector3(0.36f, 0.18f, -0.1f), new Vector3(0.1f, 0.28f, 0.1f), colors.Base);

        CreatePart(turret, "FocusArm_L", new Vector3(-0.22f, 0.14f, 0.08f), new Vector3(0.06f, 0.06f, 0.28f), colors.Secondary, true);
        CreatePart(turret, "FocusArm_R", new Vector3(0.22f, 0.14f, 0.08f), new Vector3(0.06f, 0.06f, 0.28f), colors.Secondary, true);
        CreatePart(turret, "OrbSocket", new Vector3(0f, 0.16f, 0.12f), new Vector3(0.28f, 0.28f, 0.28f), colors.Base, false, PrimitiveType.Sphere);
        CreatePart(turret, "OrbCore", new Vector3(0f, 0.2f, 0.28f), new Vector3(0.18f, 0.18f, 0.18f), colors.Accent, true, PrimitiveType.Sphere);
        CreatePart(turret, "OrbHalo", new Vector3(0f, 0.2f, 0.28f), new Vector3(0.34f, 0.04f, 0.34f), Quaternion.Euler(90f, 0f, 0f), colors.Accent, true, PrimitiveType.Cylinder);

        return new Vector3(0f, 0.2f, 0.5f);
    }

    private static Vector3 BuildMeteorBeacon(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Fire);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.3f);

        CreatePart(baseRoot, "CraterPad", Vector3.zero, new Vector3(1.1f, 0.12f, 1.1f), colors.Secondary);
        CreatePart(baseRoot, "Rock_L", new Vector3(-0.4f, 0.08f, -0.2f), new Vector3(0.18f, 0.12f, 0.14f), colors.Base);
        CreatePart(baseRoot, "Rock_R", new Vector3(0.36f, 0.08f, 0.18f), new Vector3(0.16f, 0.1f, 0.12f), colors.Base);

        CreatePart(turret, "RadarMast", new Vector3(0f, 0.14f, 0f), new Vector3(0.08f, 0.28f, 0.08f), colors.Base);
        CreatePart(turret, "RadarDish", new Vector3(0f, 0.3f, 0.06f), new Vector3(0.44f, 0.05f, 0.44f), Quaternion.Euler(28f, 0f, 0f), colors.Accent, true, PrimitiveType.Cylinder);
        CreatePart(turret, "TargetMarker", new Vector3(0f, 0.08f, 0.22f), new Vector3(0.16f, 0.16f, 0.16f), colors.Accent, true);

        return new Vector3(0f, 0.34f, 0.28f);
    }

    private static Vector3 BuildChainSpire(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(DefenseSkillElement.Lightning);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.26f);

        CreatePart(baseRoot, "SpireBase", Vector3.zero, new Vector3(0.88f, 0.16f, 0.88f), colors.Base);
        CreatePart(baseRoot, "ArcPad", new Vector3(0f, 0.1f, 0f), new Vector3(0.62f, 0.06f, 0.62f), colors.Secondary);

        CreatePart(turret, "SpirePole", new Vector3(0f, 0.28f, 0f), new Vector3(0.12f, 0.56f, 0.12f), colors.Secondary);
        CreatePart(turret, "Orb_Low", new Vector3(0f, 0.12f, 0f), new Vector3(0.14f, 0.14f, 0.14f), colors.Accent, true, PrimitiveType.Sphere);
        CreatePart(turret, "Orb_Mid", new Vector3(0f, 0.34f, 0f), new Vector3(0.12f, 0.12f, 0.12f), colors.Accent, true, PrimitiveType.Sphere);
        CreatePart(turret, "Orb_High", new Vector3(0f, 0.54f, 0f), new Vector3(0.1f, 0.1f, 0.1f), colors.Accent, true, PrimitiveType.Sphere);
        CreatePart(turret, "LightningRod", new Vector3(0f, 0.68f, 0f), new Vector3(0.05f, 0.14f, 0.05f), colors.Accent, true);

        return new Vector3(0f, 0.74f, 0f);
    }

    private static Vector3 BuildSummonBarracks(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = new DefenseTowerElementPalette.TowerColors(
            new Color(0.18f, 0.34f, 0.2f),
            new Color(0.34f, 0.88f, 0.42f),
            new Color(0.12f, 0.24f, 0.14f));
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.24f);

        CreatePart(baseRoot, "BarracksPad", Vector3.zero, new Vector3(1.04f, 0.12f, 1.04f), colors.Secondary);
        CreatePart(baseRoot, "BarracksBody", new Vector3(0f, 0.2f, -0.06f), new Vector3(0.72f, 0.32f, 0.62f), colors.Base);
        CreatePart(baseRoot, "BarracksRoof", new Vector3(0f, 0.4f, -0.06f), new Vector3(0.8f, 0.08f, 0.7f), colors.Secondary);
        CreatePart(baseRoot, "Door", new Vector3(0f, 0.12f, 0.24f), new Vector3(0.18f, 0.22f, 0.04f), colors.Accent);

        CreatePart(turret, "SpawnGate", new Vector3(0f, 0.08f, 0.28f), new Vector3(0.34f, 0.06f, 0.34f), colors.Accent, true);
        CreatePart(turret, "Banner", new Vector3(0f, 0.22f, 0.3f), new Vector3(0.04f, 0.18f, 0.04f), colors.Secondary);
        CreatePart(turret, "BannerFlag", new Vector3(0f, 0.28f, 0.34f), new Vector3(0.16f, 0.1f, 0.02f), colors.Accent, true);

        return new Vector3(0f, 0.12f, 0.42f);
    }

    private static Vector3 BuildCannonTower(Transform visualRoot, DefenseSkillElement element)
    {
        var colors = DefenseTowerElementPalette.Get(element);
        var (baseRoot, turret) = CreatePivotStructure(visualRoot, 0.34f);

        CreatePart(baseRoot, "Platform", Vector3.zero, new Vector3(1.05f, 0.22f, 1.05f), colors.Base);
        CreatePart(baseRoot, "Foot_L", new Vector3(-0.36f, 0.06f, -0.28f), new Vector3(0.12f, 0.12f, 0.12f), colors.Secondary);
        CreatePart(baseRoot, "Foot_R", new Vector3(0.36f, 0.06f, -0.28f), new Vector3(0.12f, 0.12f, 0.12f), colors.Secondary);
        CreatePart(turret, "TurretBody", new Vector3(0f, 0.1f, 0f), new Vector3(0.62f, 0.38f, 0.62f), colors.Base);
        CreatePart(turret, "Barrel", DefaultBarrelLocalPosition, DefaultBarrelLocalScale, colors.Accent, true);
        CreatePart(turret, "BarrelCollar", new Vector3(0f, 0.12f, 0.14f), new Vector3(0.2f, 0.2f, 0.1f), colors.Secondary, true);

        return DefaultFirePointLocalPosition;
    }

    private static Transform EnsureFirePoint(Transform towerRoot, Vector3 localPosition)
    {
        var pivot = FindTurretPivot(towerRoot);
        if (pivot == null)
            return null;

        var existing = pivot.Find(FirePointName);
        if (existing != null)
            return existing;

        var firePoint = new GameObject(FirePointName).transform;
        firePoint.SetParent(pivot, false);
        firePoint.localPosition = localPosition;
        firePoint.localRotation = Quaternion.identity;
        return firePoint;
    }

    private static DefenseSkillElement ResolveTowerElement(int sheetTowerId, TowerSpawnData data, TowerKind kind)
    {
        if (kind == TowerKind.Meteor)
            return DefenseSkillElement.Fire;
        if (kind == TowerKind.ChainLightning)
            return DefenseSkillElement.Lightning;

        if (DataManager.Instance != null && sheetTowerId > 0)
        {
            if (DataManager.Instance.TryGetTower(sheetTowerId, out var towerData))
            {
                if (towerData.skillId > 0 &&
                    DataManager.Instance.TryGetSkill(towerData.skillId, out var skill))
                    return skill.element;

                return InferElementFromText(towerData.towerName + " " + towerData.description);
            }
        }

        if (data != null)
            return InferElementFromText(data.towerName);

        return DefenseSkillElement.Physical;
    }

    private static DefenseSkillElement InferElementFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return DefenseSkillElement.Physical;

        text = text.ToLowerInvariant();
        if (text.Contains("독") || text.Contains("poison"))
            return DefenseSkillElement.Poison;
        if (text.Contains("얼음") || text.Contains("ice") || text.Contains("냉"))
            return DefenseSkillElement.Ice;
        if (text.Contains("물") || text.Contains("water"))
            return DefenseSkillElement.Water;
        if (text.Contains("화염") || text.Contains("불") || text.Contains("fire") || text.Contains("메테오") || text.Contains("meteor"))
            return DefenseSkillElement.Fire;
        if (text.Contains("번개") || text.Contains("전기") || text.Contains("lightning") || text.Contains("체인"))
            return DefenseSkillElement.Lightning;

        return DefenseSkillElement.Physical;
    }

    private static (Transform baseRoot, Transform turret) CreatePivotStructure(Transform visualRoot, float pivotHeight = 0.34f)
    {
        var baseRoot = new GameObject("Base").transform;
        baseRoot.SetParent(visualRoot, false);

        var turret = new GameObject(TurretPivotName).transform;
        turret.SetParent(visualRoot, false);
        turret.localPosition = new Vector3(0f, pivotHeight, 0f);
        return (baseRoot, turret);
    }

    private static void CreatePart(
        Transform parent,
        string name,
        Vector3 localPos,
        Vector3 scale,
        Color color,
        bool emissive = false,
        PrimitiveType primitive = PrimitiveType.Cube)
    {
        CreatePart(parent, name, localPos, scale, Quaternion.identity, color, emissive, primitive);
    }

    private static void CreatePart(
        Transform parent,
        string name,
        Vector3 localPos,
        Vector3 scale,
        Quaternion localRot,
        Color color,
        bool emissive = false,
        PrimitiveType primitive = PrimitiveType.Cube)
    {
        var part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localRotation = localRot;
        part.transform.localScale = scale;

        var partCollider = part.GetComponent<Collider>();
        if (partCollider != null)
            DestroySafe(partCollider);

        var renderer = part.GetComponent<Renderer>();
        if (renderer == null)
            return;

        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.55f);
        }

        renderer.sharedMaterial = material;
    }

    private static void DestroySafe(Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(obj);
        else
            Object.DestroyImmediate(obj);
    }
}
