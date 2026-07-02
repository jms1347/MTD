using UnityEngine;

/// <summary>
/// 시트 Tower_ID 기준 건설·프리뷰·스폰 데이터.
/// </summary>
public static class DefenseTowerBuildTable
{
    public static TowerSpawnData CreateSpawnData(int towerSheetId, int buildIndex)
    {
        var data = new TowerSpawnData
        {
            towerName = $"Player_{towerSheetId}_{buildIndex:D3}",
            towerSheetId = towerSheetId,
            kind = TowerKind.Standard,
            color = GetThemeColor(towerSheetId),
            scaleMultiplier = Vector3.one
        };

        if (DefenseTowerSheetTable.TryGetData(towerSheetId, out var sheet) &&
            !string.IsNullOrWhiteSpace(sheet.towerName))
            data.towerName = $"Player_{sheet.towerName}_{buildIndex:D3}";

        return data;
    }

    public static Color GetThemeColor(int towerSheetId)
    {
        var element = ResolveTowerElement(towerSheetId);
        var colors = DefenseTowerElementPalette.Get(element);
        return new Color(colors.Accent.r, colors.Accent.g, colors.Accent.b, 0.55f);
    }

    /// <summary>건설 UI 버튼 라벨용 속성 색.</summary>
    public static Color GetUiTextColor(int towerSheetId, bool affordable = true)
    {
        var color = GetUiTextColor(ResolveTowerElement(towerSheetId));
        if (!affordable)
            color = Color.Lerp(color, new Color(0.45f, 0.45f, 0.45f), 0.55f);

        return color;
    }

    public static Color GetUiTextColor(DefenseSkillElement element)
    {
        return element switch
        {
            DefenseSkillElement.Fire => new Color(1f, 0.58f, 0.32f),
            DefenseSkillElement.Ice => new Color(0.58f, 0.92f, 1f),
            DefenseSkillElement.Lightning => new Color(1f, 0.94f, 0.38f),
            DefenseSkillElement.Poison => new Color(0.82f, 0.48f, 1f),
            DefenseSkillElement.Water => new Color(0.42f, 0.78f, 1f),
            _ => new Color(0.9f, 0.92f, 0.96f)
        };
    }

    public static DefenseSkillElement ResolveTowerElement(int towerSheetId)
    {
        if (DataManager.Instance != null && towerSheetId > 0 &&
            DataManager.Instance.TryGetTower(towerSheetId, out var tower) &&
            tower.skillId > 0 &&
            DataManager.Instance.TryGetSkill(tower.skillId, out var skill))
        {
            return skill.element;
        }

        if (DefenseTowerSheetTable.TryGetData(towerSheetId, out var sheetData) &&
            TryResolveElementFromCode(sheetData.code, out var fromCode))
            return fromCode;

        if (towerSheetId >= 1500)
            return DefenseSkillElement.Fire;
        if (towerSheetId >= 1400)
            return DefenseSkillElement.Lightning;
        if (towerSheetId >= 1300)
            return DefenseSkillElement.Poison;
        if (towerSheetId >= 1200)
            return DefenseSkillElement.Ice;
        if (towerSheetId >= 1100)
            return DefenseSkillElement.Fire;

        return DefenseSkillElement.Physical;
    }

    private static bool TryResolveElementFromCode(string code, out DefenseSkillElement element)
    {
        element = DefenseSkillElement.Physical;
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var prefix = code.Trim().ToUpperInvariant();
        if (prefix.Length < 2)
            return false;

        if (prefix.StartsWith("F-", System.StringComparison.Ordinal))
            element = DefenseSkillElement.Fire;
        else if (prefix.StartsWith("I-", System.StringComparison.Ordinal))
            element = DefenseSkillElement.Ice;
        else if (prefix.StartsWith("L-", System.StringComparison.Ordinal))
            element = DefenseSkillElement.Lightning;
        else if (prefix.StartsWith("P-", System.StringComparison.Ordinal))
            element = DefenseSkillElement.Poison;
        else if (prefix.StartsWith("N-", System.StringComparison.Ordinal))
            element = DefenseSkillElement.Physical;
        else
            return false;

        return true;
    }

    private static DefenseSkillElement ResolveSkillElement(int towerSheetId)
    {
        return ResolveTowerElement(towerSheetId);
    }
}
