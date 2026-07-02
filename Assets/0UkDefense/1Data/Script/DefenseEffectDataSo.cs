using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DefenseEffectData
{
    public int effectId;
    public string effectName;
    public DefenseEffectType effectType;
    public DefenseSkillElement element;
    public float duration;
    /// <summary>슬로우: 이동속도 비율(70=70%). 밀어냄: 밀어내 거리(m). 그 외 타입은 무시.</summary>
    public float magnitude;
    public float tickDamage;
    public string description;

    public DamageElement DamageElement => DefenseSkillElementUtility.ToDamageElement(element);
}

[CreateAssetMenu(fileName = "DefenseEffectDataSo", menuName = "UkDefense/Defense Effect Data")]
public class DefenseEffectDataSo : ScriptableObject
{
    public List<DefenseEffectData> list = new();

    private Dictionary<int, DefenseEffectData> lookupById;

    public IReadOnlyList<DefenseEffectData> All => list;

    public void ImportFromTsv(string tsv)
    {
        SetData(ParseTsv(tsv));
    }

    public static List<DefenseEffectData> ParseTsv(string tsv)
    {
        var result = new List<DefenseEffectData>();
        if (string.IsNullOrWhiteSpace(tsv))
            return result;

        var rows = tsv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < rows.Length; i++)
        {
            var cells = rows[i].Split('\t');
            if (cells.Length < 4)
                continue;

            if (!SheetCodeUtility.TryResolveSheetId(cells[0], out var effectId) || effectId <= 0)
                continue;

            var duration = 0f;
            if (cells.Length > 4)
                SheetParseUtility.TryParseLeadingFloat(cells[4], out duration);

            var magnitude = 0f;
            if (cells.Length > 5)
                SheetParseUtility.TryParseLeadingFloat(cells[5], out magnitude);

            var tickDamage = 0f;
            if (cells.Length > 6)
                SheetParseUtility.TryParseLeadingFloat(cells[6], out tickDamage);

            var description = cells.Length > 7 ? cells[7].Trim() : string.Empty;

            result.Add(new DefenseEffectData
            {
                effectId = effectId,
                effectName = cells[1].Trim(),
                effectType = DefenseEffectSheetParser.ParseEffectType(cells[2]),
                element = DefenseSkillSheetParser.ParseElement(cells[3]),
                duration = Mathf.Max(0f, duration),
                magnitude = magnitude,
                tickDamage = Mathf.Max(0f, tickDamage),
                description = description
            });
        }

        return result;
    }

    public void SetData(IEnumerable<DefenseEffectData> source)
    {
        list.Clear();
        lookupById = null;

        if (source == null)
            return;

        foreach (var effect in source)
        {
            if (effect == null || effect.effectId <= 0)
                continue;

            if (ContainsId(effect.effectId))
            {
                Debug.LogWarning($"[DefenseEffectDataSo] duplicate effect id ignored: {effect.effectId}");
                continue;
            }

            list.Add(effect);
        }
    }

    public void RebuildLookup()
    {
        lookupById = new Dictionary<int, DefenseEffectData>(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            var effect = list[i];
            if (effect == null || effect.effectId <= 0)
                continue;

            if (!lookupById.ContainsKey(effect.effectId))
                lookupById.Add(effect.effectId, effect);
        }
    }

    public bool TryGet(int effectId, out DefenseEffectData effect)
    {
        EnsureLookup();
        effect = null;
        if (effectId <= 0)
            return false;

        return lookupById.TryGetValue(effectId, out effect);
    }

    private bool ContainsId(int effectId)
    {
        EnsureLookup();
        return lookupById.ContainsKey(effectId);
    }

    private void EnsureLookup()
    {
        if (lookupById == null)
            RebuildLookup();
    }
}
