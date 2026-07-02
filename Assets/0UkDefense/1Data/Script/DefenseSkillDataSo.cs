using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DefenseSkillData
{
    public int skillId;
    public string skillCode;
    public string skillName;
    public DefenseSkillType skillType;
    public DefenseMoveType moveType;
    public DefenseSkillElement element;
    public float speed;
    /// <summary>G열. 0=접촉/착지 폭발, &gt;0=발사 후 N초 폭발, -1=공중 정지 후 낙뢰.</summary>
    public float expDuration;
    public float damageMultiplier = 1f;
    public bool isHoming;
    public int maxHit = 1;
    public float splashRadius;
    public string effectGroupCode;
    /// <summary>M열 — 명중 후 소환할 행동 프리팹 Addressables 키 (예: CloudBlack).</summary>
    public string summonPrefabKey;
    /// <summary>N열 — 미사일 비주얼 Addressables 키. 비어 있으면 skillCode 사용.</summary>
    public string prefabKey;
    /// <summary>O열 — 명중 후 이어지는 2차 스킬 코드 (용암 돌·유성 낙하 등).</summary>
    public string followUpSkillCode;

    public bool HasSummonPrefab => !string.IsNullOrWhiteSpace(summonPrefabKey);
    public bool HasFollowUpSkill => !string.IsNullOrWhiteSpace(followUpSkillCode);

    public DamageElement DamageElement => DefenseSkillElementUtility.ToDamageElement(element);
}

[CreateAssetMenu(fileName = "DefenseSkillDataSo", menuName = "UkDefense/Defense Skill Data")]
public class DefenseSkillDataSo : ScriptableObject
{
    public List<DefenseSkillData> list = new();

    private Dictionary<int, DefenseSkillData> lookupById;

    public IReadOnlyList<DefenseSkillData> All => list;

    public void ImportFromTsv(string tsv)
    {
        SetData(ParseTsv(tsv));
    }

    /// <summary>
    /// A=Skill_ID B=Name C=SkillType D=MoveType E=Element F=Speed G=ExpDuration H=DamageMult
    /// I=IsHoming J=MaxHit K=Splash L=EffectGroup M=SummonPrefabKey N=MissilePrefabKey O=FollowUpSkillCode
    /// </summary>
    public static List<DefenseSkillData> ParseTsv(string tsv)
    {
        var result = new List<DefenseSkillData>();
        if (string.IsNullOrWhiteSpace(tsv))
            return result;

        var rows = tsv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < rows.Length; i++)
        {
            var cells = rows[i].Split('\t');
            if (cells.Length < 5)
                continue;

            if (!SheetCodeUtility.TryResolveSheetId(cells[0], out var skillId) || skillId <= 0)
                continue;

            var speed = 0f;
            if (cells.Length > 5)
                SheetParseUtility.TryParseFloat(cells[5], out speed);

            var expDuration = 0f;
            if (cells.Length > 6)
                SheetParseUtility.TryParseFloat(cells[6], out expDuration);

            var damageMultiplier = 1f;
            if (cells.Length > 7)
                SheetParseUtility.TryParseLeadingFloat(cells[7], out damageMultiplier);

            var isHoming = cells.Length > 8 && DefenseSkillSheetParser.ParseHomingFlag(cells[8]);

            var maxHit = 1;
            if (cells.Length > 9)
                SheetParseUtility.TryParseInt(cells[9], out maxHit);

            var splashRadius = 0f;
            if (cells.Length > 10)
                SheetParseUtility.TryParseFloat(cells[10], out splashRadius);

            var effectGroupCode = cells.Length > 11 ? cells[11].Trim() : string.Empty;
            var summonPrefabKey = cells.Length > 12 ? cells[12].Trim() : string.Empty;
            var missilePrefabKey = cells.Length > 13 ? cells[13].Trim() : string.Empty;
            var followUpSkillCode = cells.Length > 14 ? cells[14].Trim() : string.Empty;

            result.Add(new DefenseSkillData
            {
                skillId = skillId,
                skillCode = SheetCodeUtility.NormalizeCode(cells[0], skillId),
                skillName = cells[1].Trim(),
                skillType = DefenseSkillSheetParser.ParseSkillType(cells[2]),
                moveType = DefenseSkillSheetParser.ParseMoveType(cells[3]),
                element = DefenseSkillSheetParser.ParseElement(cells[4]),
                speed = speed,
                expDuration = expDuration,
                damageMultiplier = Mathf.Max(0f, damageMultiplier),
                isHoming = isHoming,
                maxHit = Mathf.Max(0, maxHit),
                splashRadius = Mathf.Max(0f, splashRadius),
                effectGroupCode = effectGroupCode,
                summonPrefabKey = summonPrefabKey,
                prefabKey = missilePrefabKey,
                followUpSkillCode = followUpSkillCode
            });
        }

        return result;
    }

    public void SetData(IEnumerable<DefenseSkillData> source)
    {
        list.Clear();
        lookupById = null;

        if (source == null)
            return;

        foreach (var skill in source)
        {
            if (skill == null || skill.skillId <= 0)
                continue;

            if (ContainsId(skill.skillId))
            {
                Debug.LogWarning($"[DefenseSkillDataSo] duplicate skill id ignored: {skill.skillId}");
                continue;
            }

            list.Add(skill);
        }
    }

    public void RebuildLookup()
    {
        lookupById = new Dictionary<int, DefenseSkillData>(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            var skill = list[i];
            if (skill == null || skill.skillId <= 0)
                continue;

            if (!lookupById.ContainsKey(skill.skillId))
                lookupById.Add(skill.skillId, skill);
        }
    }

    public bool TryGet(int skillId, out DefenseSkillData skill)
    {
        EnsureLookup();
        skill = null;
        if (skillId <= 0)
            return false;

        return lookupById.TryGetValue(skillId, out skill);
    }

    public bool TryGetByCode(string skillCode, out DefenseSkillData skill)
    {
        skill = null;
        if (string.IsNullOrWhiteSpace(skillCode))
            return false;

        var code = skillCode.Trim();
        for (int i = 0; i < list.Count; i++)
        {
            var entry = list[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.skillCode))
                continue;

            if (string.Equals(entry.skillCode.Trim(), code, StringComparison.OrdinalIgnoreCase))
            {
                skill = entry;
                return true;
            }
        }

        return false;
    }

    private bool ContainsId(int skillId)
    {
        EnsureLookup();
        return lookupById.ContainsKey(skillId);
    }

    private void EnsureLookup()
    {
        if (lookupById == null)
            RebuildLookup();
    }
}
