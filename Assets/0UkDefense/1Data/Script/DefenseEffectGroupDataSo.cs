using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DefenseEffectGroupEntry
{
    public string effectGroupCode;
    public string groupName;
    public int effectId;
}

[Serializable]
public class DefenseEffectGroup
{
    public string effectGroupCode;
    public string groupName;
    public List<int> effectIds = new();
}

[CreateAssetMenu(fileName = "DefenseEffectGroupDataSo", menuName = "UkDefense/Defense Effect Group Data")]
public class DefenseEffectGroupDataSo : ScriptableObject
{
    public List<DefenseEffectGroupEntry> list = new();

    private Dictionary<string, DefenseEffectGroup> lookupByGroupCode;

    public IReadOnlyList<DefenseEffectGroupEntry> All => list;

    public void ImportFromTsv(string tsv)
    {
        SetData(ParseTsv(tsv));
    }

    public static List<DefenseEffectGroupEntry> ParseTsv(string tsv)
    {
        var result = new List<DefenseEffectGroupEntry>();
        if (string.IsNullOrWhiteSpace(tsv))
            return result;

        var rows = tsv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < rows.Length; i++)
        {
            var cells = rows[i].Split('\t');
            if (cells.Length < 3)
                continue;

            var groupCode = cells[0].Trim();
            if (string.IsNullOrEmpty(groupCode))
                continue;

            if (!SheetCodeUtility.TryResolveSheetId(cells[2], out var effectId) || effectId <= 0)
                continue;

            result.Add(new DefenseEffectGroupEntry
            {
                effectGroupCode = groupCode,
                groupName = cells[1].Trim(),
                effectId = effectId
            });
        }

        return result;
    }

    public void SetData(IEnumerable<DefenseEffectGroupEntry> source)
    {
        list.Clear();
        lookupByGroupCode = null;

        if (source == null)
            return;

        foreach (var entry in source)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.effectGroupCode) || entry.effectId <= 0)
                continue;

            entry.effectGroupCode = entry.effectGroupCode.Trim();
            list.Add(entry);
        }
    }

    public void RebuildLookup()
    {
        lookupByGroupCode = new Dictionary<string, DefenseEffectGroup>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < list.Count; i++)
        {
            var entry = list[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.effectGroupCode) || entry.effectId <= 0)
                continue;

            var code = entry.effectGroupCode.Trim();
            if (!lookupByGroupCode.TryGetValue(code, out var group))
            {
                group = new DefenseEffectGroup
                {
                    effectGroupCode = code,
                    groupName = entry.groupName
                };
                lookupByGroupCode.Add(code, group);
            }

            if (string.IsNullOrWhiteSpace(group.groupName) && !string.IsNullOrWhiteSpace(entry.groupName))
                group.groupName = entry.groupName;

            if (!group.effectIds.Contains(entry.effectId))
                group.effectIds.Add(entry.effectId);
        }
    }

    public bool TryGetGroup(string effectGroupCode, out DefenseEffectGroup group)
    {
        EnsureLookup();
        group = null;
        if (string.IsNullOrWhiteSpace(effectGroupCode))
            return false;

        return lookupByGroupCode.TryGetValue(effectGroupCode.Trim(), out group);
    }

    public bool TryGetEffectIds(string effectGroupCode, out IReadOnlyList<int> effectIds)
    {
        effectIds = null;
        if (!TryGetGroup(effectGroupCode, out var group) || group.effectIds.Count == 0)
            return false;

        effectIds = group.effectIds;
        return true;
    }

    private void EnsureLookup()
    {
        if (lookupByGroupCode == null)
            RebuildLookup();
    }
}
