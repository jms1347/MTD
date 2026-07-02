using System.Collections.Generic;
using UnityEngine;

public static class DefenseBuildCatalog
{
    /// <summary>릴리즈 — false면 시트 cost·buildTime으로 골드 건설.</summary>
    public const bool FreeBuildForTesting = false;

    /// <summary>테스트용 건설 소요 시간(초).</summary>
    public const float TestBuildDurationSeconds = 0.1f;

    public static IReadOnlyList<int> GetBuildableTowerIds()
    {
        var result = new List<int>();
        var towers = DataManager.Instance?.Towers;
        if (towers == null)
            return result;

        foreach (var tower in towers.All)
        {
            if (tower == null || tower.towerId <= 0)
                continue;

            result.Add(tower.towerId);
        }

        return result;
    }

    public static long GetTowerCost(int towerSheetId)
    {
        if (FreeBuildForTesting)
            return 0;

        if (DefenseTowerSheetTable.TryGetData(towerSheetId, out var data))
            return data.cost;

        return 100;
    }

    public static string GetTowerDisplayName(int towerSheetId)
    {
        if (DefenseTowerSheetTable.TryGetData(towerSheetId, out var data))
        {
            if (IsUsableDisplayName(data.towerName, towerSheetId))
                return CompactTowerLabel(data.towerName);

            if (!string.IsNullOrWhiteSpace(data.code))
                return data.code.Trim();

            if (IsUsableDisplayName(data.description, towerSheetId))
                return CompactTowerLabel(data.description);
        }

        return $"타워 {towerSheetId}";
    }

    public static Color GetTowerPreviewColor(int towerSheetId)
    {
        return DefenseTowerBuildTable.GetThemeColor(towerSheetId);
    }

    private static bool IsUsableDisplayName(string value, int towerSheetId)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (trimmed == towerSheetId.ToString())
            return false;

        return !int.TryParse(trimmed, out _);
    }

    private static string CompactTowerLabel(string name)
    {
        const string suffix = " 타워";
        if (name.EndsWith(suffix, System.StringComparison.Ordinal))
            return name.Substring(0, name.Length - suffix.Length).Trim();

        return name.Trim();
    }

    public static float GetTowerBuildDurationSeconds(int towerSheetId)
    {
        if (FreeBuildForTesting)
            return TestBuildDurationSeconds;

        if (DefenseTowerSheetTable.TryGetData(towerSheetId, out var data))
            return Mathf.Max(0.1f, data.buildTime);

        return GetTowerCost(towerSheetId);
    }

    public static long GetCost(DefenseBuildType type)
    {
        if (FreeBuildForTesting)
            return 0;

        if (DefenseTowerSheetTable.TryGetData(type, out var data))
            return data.cost;

        return type switch
        {
            DefenseBuildType.Wall => 1,
            DefenseBuildType.StandardTower => 5,
            DefenseBuildType.MeteorTower => 10,
            DefenseBuildType.ChainLightningTower => 20,
            DefenseBuildType.SummonTower => 20,
            _ => 0
        };
    }

    public static string GetDisplayName(DefenseBuildType type)
    {
        if (DefenseTowerSheetTable.TryGetData(type, out var data) &&
            !string.IsNullOrWhiteSpace(data.towerName))
            return data.towerName;

        return type switch
        {
            DefenseBuildType.Wall => "성벽",
            DefenseBuildType.StandardTower => "기본 타워",
            DefenseBuildType.MeteorTower => "유성 타워",
            DefenseBuildType.ChainLightningTower => "체인 타워",
            DefenseBuildType.SummonTower => "소환 타워",
            _ => type.ToString()
        };
    }

    public static Color GetPreviewColor(DefenseBuildType type)
    {
        return type switch
        {
            DefenseBuildType.Wall => new Color(0.5f, 0.41f, 0.3f, 0.55f),
            DefenseBuildType.StandardTower => new Color(0.2f, 0.45f, 1f, 0.55f),
            DefenseBuildType.MeteorTower => new Color(0.92f, 0.28f, 0.08f, 0.55f),
            DefenseBuildType.ChainLightningTower => new Color(0.35f, 0.65f, 1f, 0.55f),
            DefenseBuildType.SummonTower => new Color(0.22f, 0.78f, 0.38f, 0.55f),
            _ => new Color(1f, 1f, 1f, 0.5f)
        };
    }

    public static float GetBuildDurationSeconds(DefenseBuildType type)
    {
        if (FreeBuildForTesting)
            return TestBuildDurationSeconds;

        if (DefenseTowerSheetTable.TryGetData(type, out var data))
            return Mathf.Max(0.1f, data.buildTime);

        return GetCost(type);
    }
}
