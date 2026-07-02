/// <summary>
/// 타워 이름 → 표준 미사일 ID.
/// </summary>
public static class DefenseTowerCombatTable
{
    public static DefenseMissileId GetStandardMissileForTower(string towerName)
    {
        if (string.IsNullOrWhiteSpace(towerName))
            return DefenseMissileId.Physical;

        return towerName switch
        {
            "Tower_East" => DefenseMissileId.Water,
            "Tower_South" => DefenseMissileId.Poison,
            "Tower_West" => DefenseMissileId.Fire,
            "Tower_North" => DefenseMissileId.Physical,
            _ => DefenseMissileId.Physical
        };
    }

    public static DamageElement MissileIdToElement(DefenseMissileId missileId)
    {
        return missileId switch
        {
            DefenseMissileId.Water => DamageElement.Blue,
            DefenseMissileId.Poison => DamageElement.Pink,
            DefenseMissileId.Fire => DamageElement.Fire,
            DefenseMissileId.Ice => DamageElement.Blue,
            DefenseMissileId.Lightning => DamageElement.Lightning,
            _ => DamageElement.Physical
        };
    }
}
