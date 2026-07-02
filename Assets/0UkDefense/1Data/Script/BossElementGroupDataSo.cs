using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BossElementGroupEntry
{
    public string groupCode;
    public string groupName;
    public DefenseSkillElement element;
}

[Serializable]
public class BossElementGroup
{
    public string groupCode;
    public string groupName;
    public List<DefenseSkillElement> elements = new();
}

[CreateAssetMenu(fileName = "BossElementGroupDataSo", menuName = "UkDefense/Boss Element Group Data")]
public class BossElementGroupDataSo : ScriptableObject
{
    public List<BossElementGroupEntry> list = new();

    private Dictionary<string, BossElementGroup> lookupByGroupCode;

    public IReadOnlyList<BossElementGroupEntry> All => list;

    public void ImportFromTsv(string tsv)
    {
        SetData(ParseTsv(tsv));
    }

    public static List<BossElementGroupEntry> ParseTsv(string tsv)
    {
        var result = new List<BossElementGroupEntry>();
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

            if (!BossElementUtility.TryParseElement(cells[2], out var element))
                continue;

            result.Add(new BossElementGroupEntry
            {
                groupCode = groupCode,
                groupName = cells[1].Trim(),
                element = element
            });
        }

        return result;
    }

    public void SetData(IEnumerable<BossElementGroupEntry> source)
    {
        list.Clear();
        lookupByGroupCode = null;

        if (source == null)
            return;

        foreach (var entry in source)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.groupCode))
                continue;

            entry.groupCode = entry.groupCode.Trim();
            list.Add(entry);
        }
    }

    public void RebuildLookup()
    {
        lookupByGroupCode = new Dictionary<string, BossElementGroup>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < list.Count; i++)
        {
            var entry = list[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.groupCode))
                continue;

            var code = entry.groupCode.Trim();
            if (!lookupByGroupCode.TryGetValue(code, out var group))
            {
                group = new BossElementGroup
                {
                    groupCode = code,
                    groupName = entry.groupName
                };
                lookupByGroupCode.Add(code, group);
            }

            if (string.IsNullOrWhiteSpace(group.groupName) && !string.IsNullOrWhiteSpace(entry.groupName))
                group.groupName = entry.groupName;

            if (!group.elements.Contains(entry.element))
                group.elements.Add(entry.element);
        }
    }

    public bool TryGetGroup(string groupCode, out BossElementGroup group)
    {
        EnsureLookup();
        group = null;
        if (string.IsNullOrWhiteSpace(groupCode))
            return false;

        return lookupByGroupCode.TryGetValue(groupCode.Trim(), out group);
    }

    public bool TryGetElements(string groupCode, out IReadOnlyList<DefenseSkillElement> elements)
    {
        elements = null;
        if (!TryGetGroup(groupCode, out var group) || group.elements.Count == 0)
            return false;

        elements = group.elements;
        return true;
    }

    private void EnsureLookup()
    {
        if (lookupByGroupCode == null)
            RebuildLookup();
    }
}
