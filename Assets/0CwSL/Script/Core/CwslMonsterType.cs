public enum CwslMonsterType : byte
{
    Ranged = 0,
    Suicide = 1,
    Melee = 2,
    BossHongmyeongbo = 3,
    NexusMelee = 4,
    NexusRanged = 5,
    NexusSuicide = 6,
    MidBoss = 7,
    DefenseBoss = 8,
    KoreaUniversitySoldier = 9,
    StickySuicide = 10,
    InkSniper = 11,
    NexusInkSniper = 12,
    SeniorCoach = 13
}

public static class CwslMonsterTypeUtil
{
    public static bool IsSuicideBomber(CwslMonsterType type)
    {
        return type is CwslMonsterType.Suicide
            or CwslMonsterType.NexusSuicide
            or CwslMonsterType.StickySuicide;
    }

    public static bool IsNexusPriority(CwslMonsterType type)
    {
        return type is CwslMonsterType.NexusMelee
            or CwslMonsterType.NexusRanged
            or CwslMonsterType.NexusSuicide
            or CwslMonsterType.NexusInkSniper;
    }

    public static bool IsElite(CwslMonsterType type)
    {
        return type is CwslMonsterType.MidBoss
            or CwslMonsterType.DefenseBoss
            or CwslMonsterType.SeniorCoach
            or CwslMonsterType.BossHongmyeongbo;
    }

    public static CwslMonsterType ResolveCombatPrefabType(CwslMonsterType type)
    {
        return type switch
        {
            CwslMonsterType.NexusMelee or CwslMonsterType.MidBoss
                or CwslMonsterType.DefenseBoss or CwslMonsterType.SeniorCoach => CwslMonsterType.Melee,
            CwslMonsterType.KoreaUniversitySoldier => CwslMonsterType.Melee,
            CwslMonsterType.NexusRanged => CwslMonsterType.Ranged,
            CwslMonsterType.NexusInkSniper => CwslMonsterType.InkSniper,
            CwslMonsterType.NexusSuicide => CwslMonsterType.Suicide,
            _ => type
        };
    }

    public static CwslMonsterTargetingMode GetDefaultTargeting(CwslMonsterType type)
    {
        if (type is CwslMonsterType.MidBoss or CwslMonsterType.DefenseBoss or CwslMonsterType.SeniorCoach)
            return CwslMonsterTargetingMode.Nearest;

        return IsNexusPriority(type) ? CwslMonsterTargetingMode.NexusFirst : CwslMonsterTargetingMode.Nearest;
    }
}
