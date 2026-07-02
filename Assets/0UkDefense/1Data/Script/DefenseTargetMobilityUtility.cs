using System;
using UnityEngine;

/// <summary>
/// 타워 DB TargetMobility (지상/공중/지대공) ↔ 몬스터 mobility 판정.
/// </summary>
public enum DefenseTargetMobility
{
    Ground,
    Air,
    Both
}

public static class DefenseTargetMobilityUtility
{
    public const string GroundLabel = "지상";
    public const string AirLabel = "공중";
    public const string BothLabel = "지대공";

    public static DefenseTargetMobility Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return DefenseTargetMobility.Ground;

        if (text.Contains(BothLabel, StringComparison.Ordinal)
            || text.Contains("전체", StringComparison.Ordinal)
            || (text.Contains(GroundLabel, StringComparison.Ordinal)
                && text.Contains(AirLabel, StringComparison.Ordinal)))
            return DefenseTargetMobility.Both;

        if (text.Contains(AirLabel, StringComparison.Ordinal))
            return DefenseTargetMobility.Air;

        if (text.Contains(GroundLabel, StringComparison.Ordinal))
            return DefenseTargetMobility.Ground;

        return DefenseTargetMobility.Ground;
    }

    public static bool CanTarget(string targetMobility, Monster monster)
    {
        return CanTarget(Parse(targetMobility), monster);
    }

    public static bool CanTarget(DefenseTargetMobility targetMobility, Monster monster)
    {
        if (monster == null)
            return true;

        return targetMobility switch
        {
            DefenseTargetMobility.Ground =>
                monster.IsGroundUnit || (monster.IsAirUnit && monster.IsLanded),
            DefenseTargetMobility.Air =>
                monster.IsAirUnit && !monster.IsLanded,
            DefenseTargetMobility.Both =>
                monster.IsGroundUnit || monster.IsAirUnit,
            _ => monster.IsGroundUnit || (monster.IsAirUnit && monster.IsLanded)
        };
    }

    public static bool CanTargetCollider(string targetMobility, Collider collider)
    {
        if (!DefenseEnemyQuery.TryGetEnemyRoot(collider, out var enemyRoot))
            return false;

        var monster = enemyRoot.GetComponent<Monster>();
        if (monster == null)
            return true;

        return CanTarget(targetMobility, monster);
    }
}
