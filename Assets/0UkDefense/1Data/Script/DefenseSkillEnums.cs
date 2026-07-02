public enum DefenseSkillType
{
    Missile = 0,
    Field = 1
}

public enum DefenseMoveType
{
    Straight = 0,
    Parabola = 1,
    InstantHit = 2,
    Fixed = 3,
    /// <summary>머리 위 구름 생성 후 범위 내 적에게 주기적 낙뢰.</summary>
    StormCloud = 4
}

public enum DefenseSkillElement
{
    Physical = 0,
    Fire = 1,
    Lightning = 2,
    Ice = 3,
    Water = 4,
    Poison = 5,
    Wind = 6
}

public static class DefenseSkillElementUtility
{
    public static DamageElement ToDamageElement(DefenseSkillElement element)
    {
        return element switch
        {
            DefenseSkillElement.Fire => DamageElement.Fire,
            DefenseSkillElement.Lightning => DamageElement.Lightning,
            DefenseSkillElement.Ice => DamageElement.Blue,
            DefenseSkillElement.Water => DamageElement.Green,
            DefenseSkillElement.Poison => DamageElement.Pink,
            DefenseSkillElement.Wind => DamageElement.Physical,
            _ => DamageElement.Physical
        };
    }
}
