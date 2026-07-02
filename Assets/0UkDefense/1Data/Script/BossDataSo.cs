using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BossDataSo", menuName = "UkDefense/Boss Data")]
public class BossDataSo : ScriptableObject
{
    public List<BossData> list = new();

    private Dictionary<string, BossData> lookupByBossCode;
    private Dictionary<string, BossData> lookupByMonsterCode;

    public IReadOnlyList<BossData> All => list;

    public void ImportFromTsv(string tsv)
    {
        SetData(ParseTsv(tsv));
    }

    public static List<BossData> ParseTsv(string tsv)
    {
        var result = new List<BossData>();
        if (string.IsNullOrWhiteSpace(tsv))
            return result;

        var rows = tsv.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < rows.Length; i++)
        {
            var cells = rows[i].Split('\t');
            if (cells.Length < 8)
                continue;

            var bossCode = cells[0].Trim();
            var monsterCode = cells[1].Trim();
            if (string.IsNullOrEmpty(bossCode) || string.IsNullOrEmpty(monsterCode))
                continue;

            if (!SheetParseUtility.TryParseInt(cells[4], out var hp))
                continue;
            if (!SheetParseUtility.TryParseInt(cells[5], out var attack))
                continue;
            if (!SheetParseUtility.TryParseInt(cells[6], out var defense))
                continue;
            if (!SheetParseUtility.TryParseFloat(cells[7], out var secondsPerAttack) || secondsPerAttack <= 0f)
                continue;

            result.Add(new BossData
            {
                bossCode = bossCode,
                monsterCode = monsterCode,
                immunityGroupCode = cells[2].Trim(),
                weaknessGroupCode = cells[3].Trim(),
                hp = hp,
                attack = attack,
                defense = defense,
                attackSpeed = 1f / secondsPerAttack
            });
        }

        return result;
    }

    public void SetData(IEnumerable<BossData> source)
    {
        list.Clear();
        lookupByBossCode = null;
        lookupByMonsterCode = null;

        if (source == null)
            return;

        foreach (var entry in source)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.bossCode))
                continue;

            entry.bossCode = entry.bossCode.Trim();
            entry.monsterCode = entry.monsterCode?.Trim();
            list.Add(entry);
        }
    }

    public void RebuildLookup()
    {
        lookupByBossCode = new Dictionary<string, BossData>(list.Count);
        lookupByMonsterCode = new Dictionary<string, BossData>(list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            var entry = list[i];
            if (entry == null)
                continue;

            if (!string.IsNullOrWhiteSpace(entry.bossCode) && !lookupByBossCode.ContainsKey(entry.bossCode))
                lookupByBossCode.Add(entry.bossCode, entry);

            if (!string.IsNullOrWhiteSpace(entry.monsterCode) && !lookupByMonsterCode.ContainsKey(entry.monsterCode))
                lookupByMonsterCode.Add(entry.monsterCode, entry);
        }
    }

    public bool TryGetByBossCode(string bossCode, out BossData data)
    {
        EnsureLookup();
        data = null;
        if (string.IsNullOrWhiteSpace(bossCode))
            return false;

        return lookupByBossCode.TryGetValue(bossCode.Trim(), out data);
    }

    public bool TryGetByMonsterCode(string monsterCode, out BossData data)
    {
        EnsureLookup();
        data = null;
        if (string.IsNullOrWhiteSpace(monsterCode))
            return false;

        return lookupByMonsterCode.TryGetValue(monsterCode.Trim(), out data);
    }

    private void EnsureLookup()
    {
        if (lookupByBossCode == null || lookupByMonsterCode == null)
            RebuildLookup();
    }
}
