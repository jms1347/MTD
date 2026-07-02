public enum CoopEnemyArchetype
{
    Grunt,
    Rusher,
    Tank,
    Bomber,
    Missile,
    HeavyBomber
}

public static class CoopEnemyArchetypeUtil
{
    public static bool IsSuicide(this CoopEnemyArchetype archetype)
    {
        return archetype is CoopEnemyArchetype.Bomber
            or CoopEnemyArchetype.Missile
            or CoopEnemyArchetype.HeavyBomber;
    }

    public static string ToId(CoopEnemyArchetype archetype)
    {
        return archetype switch
        {
            CoopEnemyArchetype.Rusher => "rusher",
            CoopEnemyArchetype.Tank => "tank",
            CoopEnemyArchetype.Bomber => "bomber",
            CoopEnemyArchetype.Missile => "missile",
            CoopEnemyArchetype.HeavyBomber => "heavy_bomber",
            _ => "grunt"
        };
    }

    public static bool TryParse(string id, out CoopEnemyArchetype archetype)
    {
        archetype = CoopEnemyArchetype.Grunt;
        if (string.IsNullOrWhiteSpace(id))
            return false;

        switch (id.Trim().ToLowerInvariant())
        {
            case "rusher":
                archetype = CoopEnemyArchetype.Rusher;
                return true;
            case "tank":
                archetype = CoopEnemyArchetype.Tank;
                return true;
            case "bomber":
                archetype = CoopEnemyArchetype.Bomber;
                return true;
            case "missile":
                archetype = CoopEnemyArchetype.Missile;
                return true;
            case "heavy_bomber":
            case "heavybomber":
                archetype = CoopEnemyArchetype.HeavyBomber;
                return true;
            case "grunt":
                archetype = CoopEnemyArchetype.Grunt;
                return true;
            default:
                return false;
        }
    }

    public static string ResolveDisplayName(CoopEnemyArchetype archetype)
    {
        return archetype switch
        {
            CoopEnemyArchetype.Rusher => "러셔",
            CoopEnemyArchetype.Tank => "탱커",
            CoopEnemyArchetype.Bomber => "폭탄 슬라임",
            CoopEnemyArchetype.Missile => "미사일 슬라임",
            CoopEnemyArchetype.HeavyBomber => "중폭탄 슬라임",
            _ => "그런트"
        };
    }
}
