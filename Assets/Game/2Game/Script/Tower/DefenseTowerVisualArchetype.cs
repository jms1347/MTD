/// <summary>
/// 타워 비주얼 실루엣 종류.
/// </summary>
public enum DefenseTowerVisualArchetype
{
    Cannon,
    GatlingBunker,
    TwinGatling,
    SiegeMortar,
    RailSentry,
    MinigunStriker,
    ForgeMortar,
    InfernoNozzle,
    EmberCatapult,
    CrystalSpire,
    GlacialLance,
    BlizzardPod,
    TeslaCoil,
    StormDish,
    ArcRepeater,
    ToxicLab,
    VenomSprayer,
    PlagueMortar,
    MagmaGeyser,
    SolarSpinner,
    HellgateCannon,
    GlacierCannon,
    PermafrostBell,
    RimeWidow,
    BoltSpear,
    StaticNimbus,
    TeslaMaul,
    CorrosionPit,
    StingerHive,
    PandemicBomb,
    DiabloOrbSpire,
    MeteorBeacon,
    ChainSpire,
    SummonBarracks
}

public static class DefenseTowerVisualArchetypes
{
    public static DefenseTowerVisualArchetype Resolve(
        string prefabKey,
        int sheetTowerId,
        TowerKind kind,
        DefenseSkillElement element)
    {
        if (!string.IsNullOrWhiteSpace(prefabKey))
        {
            switch (prefabKey.Trim().ToUpperInvariant())
            {
                case "N-0001": return DefenseTowerVisualArchetype.GatlingBunker;
                case "N-0002": return DefenseTowerVisualArchetype.TwinGatling;
                case "N-0003": return DefenseTowerVisualArchetype.SiegeMortar;
                case "N-0004": return DefenseTowerVisualArchetype.RailSentry;
                case "N-0005": return DefenseTowerVisualArchetype.MinigunStriker;
                case "F-0001": return DefenseTowerVisualArchetype.ForgeMortar;
                case "F-0002": return DefenseTowerVisualArchetype.InfernoNozzle;
                case "F-0003": return DefenseTowerVisualArchetype.EmberCatapult;
                case "I-0001": return DefenseTowerVisualArchetype.CrystalSpire;
                case "I-0002": return DefenseTowerVisualArchetype.InfernoNozzle;
                case "I-0003": return DefenseTowerVisualArchetype.BlizzardPod;
                case "L-0001": return DefenseTowerVisualArchetype.TeslaCoil;
                case "L-0002": return DefenseTowerVisualArchetype.StormDish;
                case "L-0003": return DefenseTowerVisualArchetype.ArcRepeater;
                case "P-0001": return DefenseTowerVisualArchetype.ToxicLab;
                case "P-0002": return DefenseTowerVisualArchetype.VenomSprayer;
                case "P-0003": return DefenseTowerVisualArchetype.PlagueMortar;
                case "F-0004": return DefenseTowerVisualArchetype.MagmaGeyser;
                case "F-0005": return DefenseTowerVisualArchetype.MeteorBeacon;
                case "F-0006": return DefenseTowerVisualArchetype.HellgateCannon;
                case "I-0004": return DefenseTowerVisualArchetype.GlacierCannon;
                case "I-0005": return DefenseTowerVisualArchetype.PermafrostBell;
                case "I-0006": return DefenseTowerVisualArchetype.RimeWidow;
                case "I-0007": return DefenseTowerVisualArchetype.DiabloOrbSpire;
                case "L-0004": return DefenseTowerVisualArchetype.BoltSpear;
                case "L-0005": return DefenseTowerVisualArchetype.StaticNimbus;
                case "L-0006": return DefenseTowerVisualArchetype.TeslaMaul;
                case "P-0004": return DefenseTowerVisualArchetype.CorrosionPit;
                case "P-0005": return DefenseTowerVisualArchetype.StingerHive;
                case "P-0006": return DefenseTowerVisualArchetype.PandemicBomb;
            }
        }

        if (kind == TowerKind.Meteor)
            return DefenseTowerVisualArchetype.MeteorBeacon;
        if (kind == TowerKind.ChainLightning)
            return DefenseTowerVisualArchetype.ChainSpire;
        if (kind == TowerKind.Summon)
            return DefenseTowerVisualArchetype.SummonBarracks;

        return element switch
        {
            DefenseSkillElement.Fire => DefenseTowerVisualArchetype.ForgeMortar,
            DefenseSkillElement.Ice => DefenseTowerVisualArchetype.CrystalSpire,
            DefenseSkillElement.Lightning => DefenseTowerVisualArchetype.TeslaCoil,
            DefenseSkillElement.Poison => DefenseTowerVisualArchetype.ToxicLab,
            DefenseSkillElement.Water => DefenseTowerVisualArchetype.ToxicLab,
            _ => DefenseTowerVisualArchetype.GatlingBunker
        };
    }
}
