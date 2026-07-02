using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TowerData
{
    public int towerId;
    public string code;
    /// <summary>B열 — Addressables Prefab 논리 키.</summary>
    public string prefabKey;
    public string towerName;
    public string targetMobility = DefenseTargetMobilityUtility.GroundLabel;
    public int cost;
    public float buildTime;
    public float baseDamage;
    public float fireInterval;
    public float attackRange;
    public int skillId;
    public string description;

    public string ResolvePrefabKey()
    {
        if (!string.IsNullOrWhiteSpace(prefabKey))
            return prefabKey.Trim();

        if (!string.IsNullOrWhiteSpace(code))
            return code.Trim();

        return towerId > 0 ? towerId.ToString() : string.Empty;
    }
}

[CreateAssetMenu(fileName = "TowerDataSo", menuName = "UkDefense/Tower Data")]
public class TowerDataSo : ScriptableObject
{
    public List<TowerData> list = new();

    private Dictionary<string, TowerData> lookupByCode;
    private Dictionary<int, TowerData> lookupById;

    public IReadOnlyList<TowerData> All => list;

    public void ImportFromTsv(string tsv)
    {
        SetData(ParseTsv(tsv));
    }

    public static List<TowerData> ParseTsv(string tsv)
    {
        var result = new List<TowerData>();
        if (string.IsNullOrWhiteSpace(tsv))
            return result;

        var rows = tsv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < rows.Length; i++)
        {
            var cells = rows[i].Split('\t');
            if (cells.Length < 7)
                continue;

            var idText = cells[0].Trim();
            if (string.IsNullOrEmpty(idText))
                continue;

            if (!SheetCodeUtility.TryResolveSheetId(idText, out var towerId))
                continue;

            // 신규: towerId | prefabKey(B) | towerName | cost | buildTime | baseDamage | fireInterval | attackRange | skillId | description
            // 구버전: towerId | towerName | cost | ...  또는 targetMobility(C) 열 포함
            string prefabKey = string.Empty;
            string towerName;
            int statIndex;

            if (cells.Length >= 8 && IsLegacyTargetMobilityColumn(cells))
            {
                towerName = cells[1].Trim();
                statIndex = 3;
            }
            else if (cells.Length >= 8
                     && !SheetParseUtility.TryParseInt(cells[2], out _)
                     && SheetParseUtility.TryParseInt(cells[3], out _))
            {
                prefabKey = cells[1].Trim();
                towerName = cells[2].Trim();
                statIndex = 3;
            }
            else
            {
                towerName = cells[1].Trim();
                statIndex = 2;
            }

            if (cells.Length <= statIndex + 4)
                continue;

            if (!SheetParseUtility.TryParseInt(cells[statIndex], out var cost))
                continue;

            if (!SheetParseUtility.TryParseFloat(cells[statIndex + 1], out var buildTime))
                continue;

            if (!SheetParseUtility.TryParseFloat(cells[statIndex + 2], out var baseDamage))
                continue;

            if (!SheetParseUtility.TryParseFloat(cells[statIndex + 3], out var fireInterval))
                continue;

            if (!SheetParseUtility.TryParseFloat(cells[statIndex + 4], out var attackRange))
                continue;

            var skillIndex = statIndex + 5;
            var descriptionIndex = statIndex + 6;

            if (IsLegacyManualYnColumn(cells, skillIndex))
            {
                skillIndex++;
                descriptionIndex++;
            }

            var skillId = 0;
            if (cells.Length > skillIndex && !string.IsNullOrWhiteSpace(cells[skillIndex]))
                SheetCodeUtility.TryResolveSheetId(cells[skillIndex], out skillId);

            var description = cells.Length > descriptionIndex ? cells[descriptionIndex].Trim() : string.Empty;

            result.Add(new TowerData
            {
                towerId = towerId,
                code = SheetCodeUtility.NormalizeCode(idText, towerId),
                prefabKey = prefabKey,
                towerName = towerName,
                targetMobility = DefenseTargetMobilityUtility.GroundLabel,
                cost = cost,
                buildTime = buildTime,
                baseDamage = baseDamage,
                fireInterval = fireInterval,
                attackRange = attackRange,
                skillId = skillId,
                description = description
            });
        }

        return result;
    }

    private static bool IsLegacyTargetMobilityColumn(string[] cells)
    {
        if (cells.Length < 8)
            return false;

        if (SheetParseUtility.TryParseInt(cells[2], out _))
            return false;

        var value = cells[2].Trim();
        return value.Contains("지상", StringComparison.Ordinal)
            || value.Contains("공중", StringComparison.Ordinal)
            || value.Contains("지대공", StringComparison.Ordinal);
    }

    private static bool IsLegacyManualYnColumn(string[] cells, int index)
    {
        if (cells.Length <= index)
            return false;

        var value = cells[index].Trim();
        if (string.IsNullOrEmpty(value))
            return false;

        if (SheetParseUtility.TryParseInt(value, out _))
            return false;

        return value.Equals("Y", StringComparison.OrdinalIgnoreCase)
            || value.Equals("N", StringComparison.OrdinalIgnoreCase)
            || value.Equals("YES", StringComparison.OrdinalIgnoreCase)
            || value.Equals("NO", StringComparison.OrdinalIgnoreCase);
    }

    public void SetData(IEnumerable<TowerData> source)
    {
        list.Clear();
        lookupByCode = null;
        lookupById = null;

        if (source == null)
            return;

        foreach (var tower in source)
        {
            if (tower == null || tower.towerId <= 0)
                continue;

            if (string.IsNullOrWhiteSpace(tower.code))
                tower.code = tower.towerId.ToString();
            else
                tower.code = tower.code.Trim();

            if (ContainsCode(tower.code))
            {
                Debug.LogWarning($"[TowerDataSo] duplicate tower id ignored: {tower.towerId}");
                continue;
            }

            list.Add(tower);
        }
    }

    public void RebuildLookup()
    {
        lookupByCode = new Dictionary<string, TowerData>(list.Count);
        lookupById = new Dictionary<int, TowerData>(list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            var tower = list[i];
            if (tower == null || tower.towerId <= 0)
                continue;

            if (string.IsNullOrWhiteSpace(tower.code))
                tower.code = tower.towerId.ToString();
            else
                tower.code = tower.code.Trim();

            if (!lookupByCode.ContainsKey(tower.code))
                lookupByCode.Add(tower.code, tower);

            if (!lookupById.ContainsKey(tower.towerId))
                lookupById.Add(tower.towerId, tower);
        }
    }

    public bool TryGet(string code, out TowerData tower)
    {
        EnsureLookup();
        tower = null;
        if (string.IsNullOrWhiteSpace(code))
            return false;

        return lookupByCode.TryGetValue(code.Trim(), out tower);
    }

    public bool TryGet(int towerId, out TowerData tower)
    {
        EnsureLookup();
        tower = null;
        if (towerId <= 0)
            return false;

        return lookupById.TryGetValue(towerId, out tower);
    }

    private bool ContainsCode(string code)
    {
        EnsureLookup();
        return lookupByCode.ContainsKey(code);
    }

    private void EnsureLookup()
    {
        if (lookupByCode == null || lookupById == null)
            RebuildLookup();
    }
}

