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
    StickySuicide = 10
}

public static class CwslMonsterTypeUtil
{
    public static bool IsNexusPriority(CwslMonsterType type)
    {
        return type is CwslMonsterType.NexusMelee
            or CwslMonsterType.NexusRanged
            or CwslMonsterType.NexusSuicide;
    }

    public static bool IsElite(CwslMonsterType type)
    {
        return type is CwslMonsterType.MidBoss or CwslMonsterType.DefenseBoss or CwslMonsterType.BossHongmyeongbo;
    }

    public static CwslMonsterType ResolveCombatPrefabType(CwslMonsterType type)
    {
        return type switch
        {
            CwslMonsterType.NexusMelee or CwslMonsterType.MidBoss or CwslMonsterType.DefenseBoss => CwslMonsterType.Melee,
            CwslMonsterType.KoreaUniversitySoldier => CwslMonsterType.Melee,
            CwslMonsterType.NexusRanged => CwslMonsterType.Ranged,
            CwslMonsterType.NexusSuicide => CwslMonsterType.Suicide,
            _ => type
        };
    }

    public static CwslMonsterTargetingMode GetDefaultTargeting(CwslMonsterType type)
    {
        if (type is CwslMonsterType.MidBoss or CwslMonsterType.DefenseBoss)
            return CwslMonsterTargetingMode.NexusFirst;

        return IsNexusPriority(type) ? CwslMonsterTargetingMode.NexusFirst : CwslMonsterTargetingMode.Nearest;
    }
}
