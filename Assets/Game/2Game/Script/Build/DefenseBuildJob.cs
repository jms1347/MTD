using UnityEngine;

public class DefenseBuildJob
{
    public bool IsWall { get; }
    public int TowerSheetId { get; }
    public DefenseBuildType LegacyType { get; }
    public Vector3 SitePosition { get; }
    public Vector2Int Cell { get; }
    public long GoldCost { get; }
    public float DurationSeconds { get; }
    public bool GoldSpent { get; set; }

    public static DefenseBuildJob CreateWall(
        Vector3 sitePosition,
        Vector2Int cell,
        long goldCost,
        float durationSeconds)
    {
        return new DefenseBuildJob(true, 0, DefenseBuildType.Wall, sitePosition, cell, goldCost, durationSeconds);
    }

    public static DefenseBuildJob CreateTower(
        int towerSheetId,
        Vector3 sitePosition,
        Vector2Int cell,
        long goldCost,
        float durationSeconds)
    {
        return new DefenseBuildJob(false, towerSheetId, DefenseBuildType.StandardTower, sitePosition, cell, goldCost, durationSeconds);
    }

    public DefenseBuildJob(
        DefenseBuildType type,
        Vector3 sitePosition,
        Vector2Int cell,
        long goldCost,
        float durationSeconds)
        : this(type == DefenseBuildType.Wall, 0, type, sitePosition, cell, goldCost, durationSeconds)
    {
    }

    private DefenseBuildJob(
        bool isWall,
        int towerSheetId,
        DefenseBuildType legacyType,
        Vector3 sitePosition,
        Vector2Int cell,
        long goldCost,
        float durationSeconds)
    {
        IsWall = isWall;
        TowerSheetId = towerSheetId;
        LegacyType = legacyType;
        SitePosition = sitePosition;
        Cell = cell;
        GoldCost = goldCost;
        DurationSeconds = durationSeconds;
    }
}
