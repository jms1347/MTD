using System;
using System.Collections.Generic;
using UnityEngine;

public enum DefenseSkillPresentationType
{
    None = 0,
    SustainedFlamethrower = 1,
    SustainedLaser = 2,
    SustainedSpray = 3,
}

/// <summary>
/// 스킬 코드별 타워 연속 연출(화염방사·레이저·분사) Addressables 키.
/// </summary>
public static class DefenseSkillPresentationCatalog
{
    private struct Entry
    {
        public DefenseSkillPresentationType type;
        public string effectKey;
    }

    private static readonly Dictionary<string, float> CombatReachCache =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, Entry> BySkillCode =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["M-F-0002"] = new Entry
            {
                type = DefenseSkillPresentationType.SustainedFlamethrower,
                effectKey = "FlamethrowerCartoonyFire"
            },
            ["M-I-0002"] = new Entry
            {
                type = DefenseSkillPresentationType.SustainedFlamethrower,
                effectKey = "FlamethrowerCartoonyBlue"
            },
            ["M-P-0002"] = new Entry
            {
                type = DefenseSkillPresentationType.SustainedSpray,
                effectKey = "FlamethrowerCartoonyGreen"
            },
            ["M-N-0004"] = new Entry
            {
                type = DefenseSkillPresentationType.SustainedLaser,
                effectKey = "LaserBlueOBJ"
            },
            ["M-L-0004"] = new Entry
            {
                type = DefenseSkillPresentationType.SustainedLaser,
                effectKey = "LaserMissileBlue"
            },
        };

    public static bool TryGet(DefenseSkillData skill, out DefenseSkillPresentationType type, out string effectKey)
    {
        type = DefenseSkillPresentationType.None;
        effectKey = null;

        if (skill == null || string.IsNullOrWhiteSpace(skill.skillCode))
            return false;

        if (!BySkillCode.TryGetValue(skill.skillCode.Trim(), out var entry))
            return false;

        type = entry.type;
        effectKey = entry.effectKey;
        return type != DefenseSkillPresentationType.None && !string.IsNullOrWhiteSpace(effectKey);
    }

    /// <summary>
    /// 화염방사·독액 분사처럼 지속 파티클만 쓰고 투사체 비주얼은 숨깁니다.
    /// </summary>
    public static bool UsesSustainedBeamWithoutMissile(DefenseSkillData skill)
    {
        if (!TryGet(skill, out var type, out _))
            return false;

        return type == DefenseSkillPresentationType.SustainedFlamethrower
            || type == DefenseSkillPresentationType.SustainedSpray;
    }

    /// <summary>
    /// 분사 연출이 닿는 최대 거리(월드 유닛). 시트 사거리 상한으로 씁니다.
    /// </summary>
    public static bool TryGetCombatReach(DefenseSkillData skill, out float reach)
    {
        reach = 0f;
        if (!TryGet(skill, out var type, out var effectKey))
            return false;

        if (!UsesSustainedBeamWithoutMissile(skill))
            return false;

        if (CombatReachCache.TryGetValue(effectKey, out reach))
            return reach > 0.05f;

        if (DefenseTowerSkillVfx.TryEstimatePresentationReach(effectKey, type, out reach))
        {
            CombatReachCache[effectKey] = reach;
            return true;
        }

        reach = type == DefenseSkillPresentationType.SustainedSpray ? 8f : 9f;
        CombatReachCache[effectKey] = reach;
        return true;
    }

    public static float ResolveAttackRange(DefenseSkillData skill, float sheetAttackRange)
    {
        if (!TryGetCombatReach(skill, out float presentationReach))
            return sheetAttackRange;

        if (sheetAttackRange <= 0.05f)
            return presentationReach;

        // 시트 AttackRange를 따르되, 분사 VFX가 닿는 거리를 넘지 않습니다.
        return Mathf.Min(sheetAttackRange, presentationReach);
    }

    public static float ResolveAttackRangeForTower(TowerData tower)
    {
        if (tower == null)
            return 18f;

        if (tower.skillId > 0 &&
            DataManager.Instance != null &&
            DataManager.Instance.TryGetSkill(tower.skillId, out var skill))
        {
            return ResolveAttackRange(skill, tower.attackRange);
        }

        return tower.attackRange;
    }
}
