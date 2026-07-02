#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;

/// <summary>로컬 SO → 구글 시트 TSV 행 (import 파서와 동일 컬럼 순서).</summary>
public static class DefenseSheetSoTsvExporter
{
    public static string ExportEffects(DefenseEffectDataSo asset)
    {
        if (asset == null || asset.list == null)
            return string.Empty;

        var builder = new StringBuilder();
        for (int i = 0; i < asset.list.Count; i++)
        {
            var e = asset.list[i];
            if (e == null || e.effectId <= 0)
                continue;

            var code = ResolveEffectCode(e.effectId, e.effectName);
            AppendLine(builder,
                code,
                e.effectName,
                ToEffectTypeLabel(e.effectType),
                ToElementLabel(e.element),
                e.duration.ToString("0.##"),
                e.magnitude.ToString("0.##"),
                e.tickDamage.ToString("0.##"),
                e.description);
        }

        return builder.ToString();
    }

    public static string ExportEffectGroups(DefenseEffectGroupDataSo asset, DefenseEffectDataSo effects)
    {
        if (asset == null || asset.list == null)
            return string.Empty;

        var effectCodeById = BuildEffectCodeLookup(effects);
        var builder = new StringBuilder();
        for (int i = 0; i < asset.list.Count; i++)
        {
            var g = asset.list[i];
            if (g == null || string.IsNullOrWhiteSpace(g.effectGroupCode) || g.effectId <= 0)
                continue;

            if (!effectCodeById.TryGetValue(g.effectId, out var effectCode))
                effectCode = g.effectId.ToString();

            AppendLine(builder, g.effectGroupCode, g.groupName, effectCode);
        }

        return builder.ToString();
    }

    public static string ExportSkills(DefenseSkillDataSo asset)
    {
        if (asset == null || asset.list == null)
            return string.Empty;

        var builder = new StringBuilder();
        for (int i = 0; i < asset.list.Count; i++)
        {
            var s = asset.list[i];
            if (s == null || s.skillId <= 0)
                continue;

            var code = string.IsNullOrWhiteSpace(s.skillCode) ? s.skillId.ToString() : s.skillCode;
            AppendLine(builder,
                code,
                s.skillName,
                ToSkillTypeLabel(s.skillType),
                ToMoveTypeLabel(s.moveType),
                ToSkillElementLabel(s.element),
                s.speed.ToString("0.##"),
                s.expDuration.ToString("0.##"),
                s.damageMultiplier.ToString("0.##"),
                s.isHoming ? "1" : "0",
                s.maxHit.ToString(),
                s.splashRadius.ToString("0.##"),
                s.effectGroupCode ?? string.Empty,
                s.summonPrefabKey ?? string.Empty,
                s.prefabKey ?? string.Empty,
                s.followUpSkillCode ?? string.Empty);
        }

        return builder.ToString();
    }

    public static string ExportTowers(TowerDataSo asset, DefenseSkillDataSo skills)
    {
        if (asset == null || asset.list == null)
            return string.Empty;

        var skillCodeById = BuildSkillCodeLookup(skills);
        var builder = new StringBuilder();
        for (int i = 0; i < asset.list.Count; i++)
        {
            var t = asset.list[i];
            if (t == null || t.towerId <= 0)
                continue;

            var code = string.IsNullOrWhiteSpace(t.code) ? t.towerId.ToString() : t.code;
            var prefabKey = string.IsNullOrWhiteSpace(t.prefabKey) ? code : t.prefabKey;
            if (!skillCodeById.TryGetValue(t.skillId, out var skillCode))
                skillCode = t.skillId > 0 ? t.skillId.ToString() : string.Empty;

            AppendLine(builder,
                code,
                prefabKey,
                t.towerName,
                t.cost.ToString(),
                t.buildTime.ToString("0.##"),
                t.baseDamage.ToString("0.##"),
                t.fireInterval.ToString("0.##"),
                t.attackRange.ToString("0.##"),
                skillCode,
                t.description);
        }

        return builder.ToString();
    }

    private static Dictionary<int, string> BuildSkillCodeLookup(DefenseSkillDataSo asset)
    {
        var map = new Dictionary<int, string>();
        if (asset?.list == null)
            return map;

        for (int i = 0; i < asset.list.Count; i++)
        {
            var s = asset.list[i];
            if (s == null || s.skillId <= 0)
                continue;

            map[s.skillId] = string.IsNullOrWhiteSpace(s.skillCode) ? s.skillId.ToString() : s.skillCode;
        }

        return map;
    }

    private static Dictionary<int, string> BuildEffectCodeLookup(DefenseEffectDataSo asset)
    {
        var map = new Dictionary<int, string>();
        if (asset?.list == null)
            return map;

        for (int i = 0; i < asset.list.Count; i++)
        {
            var e = asset.list[i];
            if (e == null || e.effectId <= 0)
                continue;

            map[e.effectId] = ResolveEffectCode(e.effectId, e.effectName);
        }

        return map;
    }

    private static string ResolveEffectCode(int effectId, string effectName)
    {
        foreach (var known in KnownEffectCodes)
        {
            if (SheetCodeUtility.ToStablePositiveId(known) == effectId)
                return known;
        }

        return effectId.ToString();
    }

    private static readonly string[] KnownEffectCodes =
    {
        "Burn", "Frost", "Shock", "Poison",
        "Burn2", "Burn3", "Frost2", "Frost3", "Shock2", "Shock3", "Poison2", "Poison3"
    };

    private static void AppendLine(StringBuilder builder, params string[] cells)
    {
        if (builder.Length > 0)
            builder.Append('\n');

        for (int i = 0; i < cells.Length; i++)
        {
            if (i > 0)
                builder.Append('\t');
            builder.Append(cells[i] ?? string.Empty);
        }
    }

    private static string ToSkillTypeLabel(DefenseSkillType type) =>
        type == DefenseSkillType.Field ? "장판" : "미사일";

    private static string ToMoveTypeLabel(DefenseMoveType type) => type switch
    {
        DefenseMoveType.Parabola => "포물선",
        DefenseMoveType.InstantHit => "즉시타격",
        DefenseMoveType.Fixed => "고정",
        DefenseMoveType.StormCloud => "구름",
        _ => "직선"
    };

    private static string ToSkillElementLabel(DefenseSkillElement element) => element switch
    {
        DefenseSkillElement.Fire => "화염",
        DefenseSkillElement.Lightning => "전기",
        DefenseSkillElement.Ice => "얼음",
        DefenseSkillElement.Water => "물",
        DefenseSkillElement.Poison => "독",
        DefenseSkillElement.Wind => "바람",
        _ => "물리"
    };

    private static string ToElementLabel(DefenseSkillElement element) => element switch
    {
        DefenseSkillElement.Fire => "Fire",
        DefenseSkillElement.Lightning => "Lightning",
        DefenseSkillElement.Ice => "Ice",
        DefenseSkillElement.Water => "Water",
        DefenseSkillElement.Poison => "Poison",
        DefenseSkillElement.Wind => "Wind",
        _ => "Physical"
    };

    private static string ToEffectTypeLabel(DefenseEffectType type) => type switch
    {
        DefenseEffectType.Ground => "Ground",
        DefenseEffectType.Root => "Root",
        DefenseEffectType.Stun => "Stun",
        DefenseEffectType.Poison => "Poison",
        DefenseEffectType.Water => "Water",
        DefenseEffectType.Lightning => "Lightning",
        DefenseEffectType.Slow => "Slow",
        DefenseEffectType.Knockback => "Knockback",
        _ => "Fire"
    };

    public static string LoadLocalEffectTsv() =>
        ExportEffects(AssetDatabase.LoadAssetAtPath<DefenseEffectDataSo>(GoogleSheetDefinitions.EffectDataAssetPath));

    public static string LoadLocalEffectGroupTsv()
    {
        var groups = AssetDatabase.LoadAssetAtPath<DefenseEffectGroupDataSo>(GoogleSheetDefinitions.EffectGroupDataAssetPath);
        var effects = AssetDatabase.LoadAssetAtPath<DefenseEffectDataSo>(GoogleSheetDefinitions.EffectDataAssetPath);
        return ExportEffectGroups(groups, effects);
    }

    public static string LoadLocalSkillTsv() =>
        ExportSkills(AssetDatabase.LoadAssetAtPath<DefenseSkillDataSo>(GoogleSheetDefinitions.SkillDataAssetPath));

    public static string LoadLocalTowerTsv()
    {
        var towers = AssetDatabase.LoadAssetAtPath<TowerDataSo>(GoogleSheetDefinitions.TowerDataAssetPath);
        var skills = AssetDatabase.LoadAssetAtPath<DefenseSkillDataSo>(GoogleSheetDefinitions.SkillDataAssetPath);
        return ExportTowers(towers, skills);
    }
}
#endif
