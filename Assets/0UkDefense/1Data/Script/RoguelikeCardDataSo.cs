using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoguelikeCardData
{
    public int cardId;
    public string cardCode;
    public string cardName;
    public RoguelikeCardType cardType;
    public RoguelikeCardColor cardColor;
    public string description;
    public RoguelikeCardEffectType effectType;
    public float effectValue;
    public RoguelikeConditionType conditionType;
    public int conditionValue;
    public RoguelikeMagicUseMode magicUseMode;
    public string skillCode;
    [Min(1)] public int poolWeight = 1;

    public bool IsGroundTargetMagic =>
        cardType == RoguelikeCardType.Magic
        && magicUseMode == RoguelikeMagicUseMode.GroundTarget;

    public bool IsInstantMagic =>
        cardType == RoguelikeCardType.Magic
        && magicUseMode == RoguelikeMagicUseMode.Instant;
}

[CreateAssetMenu(fileName = "RoguelikeCardDataSo", menuName = "UkDefense/Roguelike Card Data")]
public class RoguelikeCardDataSo : ScriptableObject
{
    public List<RoguelikeCardData> list = new();

    private Dictionary<int, RoguelikeCardData> lookupById;
    private Dictionary<string, RoguelikeCardData> lookupByCode;

    public IReadOnlyList<RoguelikeCardData> All => list;

    /// <summary>
    /// A=CardCode B=Name C=Type D=Color E=Description F=EffectType G=EffectValue
    /// H=ConditionType I=ConditionValue J=MagicUseMode K=SkillCode L=PoolWeight
    /// </summary>
    public static List<RoguelikeCardData> ParseTsv(string tsv)
    {
        var result = new List<RoguelikeCardData>();
        if (string.IsNullOrWhiteSpace(tsv))
            return result;

        var rows = tsv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < rows.Length; i++)
        {
            var cells = rows[i].Split('\t');
            if (cells.Length < 7)
                continue;

            if (!SheetCodeUtility.TryResolveSheetId(cells[0], out var cardId) || cardId <= 0)
                continue;

            var codeCell = cells[0].Trim();
            if (RoguelikeCardSheetParser.IsHeaderRow(codeCell))
                continue;

            var effectValue = 0f;
            if (cells.Length > 6)
                SheetParseUtility.TryParseFloat(cells[6], out effectValue);

            var conditionValue = 0;
            if (cells.Length > 8)
                SheetParseUtility.TryParseInt(cells[8], out conditionValue);

            var poolWeight = 1;
            if (cells.Length > 11)
                SheetParseUtility.TryParseInt(cells[11], out poolWeight);

            var cardType = RoguelikeCardSheetParser.ParseCardType(cells[2]);
            var magicUseMode = cells.Length > 9
                ? RoguelikeCardSheetParser.ParseMagicUseMode(cells[9])
                : RoguelikeMagicUseMode.None;
            var skillCode = cells.Length > 10 ? cells[10].Trim() : string.Empty;
            var colorRaw = cells.Length > 3 ? cells[3].Trim() : string.Empty;

            result.Add(new RoguelikeCardData
            {
                cardId = cardId,
                cardCode = SheetCodeUtility.NormalizeCode(cells[0], cardId),
                cardName = cells[1].Trim(),
                cardType = cardType,
                cardColor = RoguelikeCardSheetParser.ResolveCardColor(cardType, magicUseMode, colorRaw),
                description = cells.Length > 4 ? cells[4].Trim() : string.Empty,
                effectType = RoguelikeCardSheetParser.ParseEffectType(cells[5]),
                effectValue = effectValue,
                conditionType = cells.Length > 7
                    ? RoguelikeCardSheetParser.ParseConditionType(cells[7])
                    : RoguelikeConditionType.None,
                conditionValue = conditionValue,
                magicUseMode = magicUseMode,
                skillCode = skillCode,
                poolWeight = Mathf.Max(1, poolWeight)
            });
        }

        return result;
    }

    public void ImportFromTsv(string tsv)
    {
        SetData(ParseTsv(tsv));
    }

    public void SetData(IEnumerable<RoguelikeCardData> source)
    {
        list.Clear();
        if (source == null)
        {
            RebuildLookup();
            return;
        }

        foreach (var row in source)
        {
            if (row == null || row.cardId <= 0)
                continue;

            list.Add(row);
        }

        RebuildLookup();
    }

    public void RebuildLookup()
    {
        lookupById = new Dictionary<int, RoguelikeCardData>();
        lookupByCode = new Dictionary<string, RoguelikeCardData>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < list.Count; i++)
        {
            var row = list[i];
            if (row == null || row.cardId <= 0)
                continue;

            lookupById[row.cardId] = row;

            if (!string.IsNullOrWhiteSpace(row.cardCode))
                lookupByCode[row.cardCode.Trim()] = row;
        }
    }

    public bool TryGet(int cardId, out RoguelikeCardData data)
    {
        data = null;
        if (lookupById == null)
            RebuildLookup();

        return lookupById != null && lookupById.TryGetValue(cardId, out data);
    }

    public bool TryGetByCode(string cardCode, out RoguelikeCardData data)
    {
        data = null;
        if (string.IsNullOrWhiteSpace(cardCode))
            return false;

        if (lookupByCode == null)
            RebuildLookup();

        return lookupByCode != null && lookupByCode.TryGetValue(cardCode.Trim(), out data);
    }
}

public static class RoguelikeCardSheetParser
{
    public static bool IsHeaderRow(string codeCell)
    {
        if (string.IsNullOrWhiteSpace(codeCell))
            return false;

        var normalized = codeCell.Trim();
        return normalized.Equals("CardCode", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("카드코드", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Card_ID", StringComparison.OrdinalIgnoreCase);
    }

    public static RoguelikeCardType ParseCardType(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return RoguelikeCardType.Passive;

        var normalized = raw.Trim();
        if (normalized.Contains("마법") || normalized.Equals("Magic", StringComparison.OrdinalIgnoreCase))
            return RoguelikeCardType.Magic;

        if (normalized.Contains("조건") || normalized.Equals("Conditional", StringComparison.OrdinalIgnoreCase))
            return RoguelikeCardType.Conditional;

        return RoguelikeCardType.Passive;
    }

    public static RoguelikeCardColor ResolveCardColor(
        RoguelikeCardType cardType,
        RoguelikeMagicUseMode magicUseMode,
        string rawColor)
    {
        if (!string.IsNullOrWhiteSpace(rawColor)
            && !rawColor.Equals("Auto", StringComparison.OrdinalIgnoreCase)
            && !rawColor.Equals("자동", StringComparison.OrdinalIgnoreCase))
        {
            return ParseCardColor(rawColor);
        }

        return cardType switch
        {
            RoguelikeCardType.Passive => RoguelikeCardColor.Red,
            RoguelikeCardType.Conditional => RoguelikeCardColor.Yellow,
            RoguelikeCardType.Magic when magicUseMode == RoguelikeMagicUseMode.GroundTarget => RoguelikeCardColor.Blue,
            RoguelikeCardType.Magic => RoguelikeCardColor.Green,
            _ => RoguelikeCardColor.Red
        };
    }

    public static RoguelikeCardColor ParseCardColor(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return RoguelikeCardColor.Red;

        var normalized = raw.Trim();
        if (normalized.Contains("Blue") || normalized.Contains("파랑") || normalized.Contains("청"))
            return RoguelikeCardColor.Blue;

        if (normalized.Contains("Green") || normalized.Contains("초록") || normalized.Contains("녹"))
            return RoguelikeCardColor.Green;

        if (normalized.Contains("Yellow") || normalized.Contains("노랑") || normalized.Contains("황"))
            return RoguelikeCardColor.Yellow;

        return RoguelikeCardColor.Red;
    }

    public static RoguelikeMagicUseMode ParseMagicUseMode(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return RoguelikeMagicUseMode.None;

        var normalized = raw.Trim();
        if (normalized.Contains("Ground") || normalized.Contains("지면") || normalized.Contains("조준"))
            return RoguelikeMagicUseMode.GroundTarget;

        if (normalized.Contains("Instant") || normalized.Contains("즉시"))
            return RoguelikeMagicUseMode.Instant;

        return RoguelikeMagicUseMode.None;
    }

    public static RoguelikeCardEffectType ParseEffectType(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return RoguelikeCardEffectType.None;

        var normalized = raw.Trim();
        if (normalized.Equals("TowerDamagePercent", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("타워피해"))
            return RoguelikeCardEffectType.TowerDamagePercent;

        if (normalized.Equals("TowerRangePercent", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("사거리"))
            return RoguelikeCardEffectType.TowerRangePercent;

        if (normalized.Equals("TowerFireRatePercent", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("연사"))
            return RoguelikeCardEffectType.TowerFireRatePercent;

        if (normalized.Equals("GoldFlat", StringComparison.OrdinalIgnoreCase))
            return RoguelikeCardEffectType.GoldFlat;

        if (normalized.Equals("NexusMaxHealthFlat", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("넥서스최대체력"))
            return RoguelikeCardEffectType.NexusMaxHealthFlat;

        if (normalized.Equals("MagicGoldBurst", StringComparison.OrdinalIgnoreCase))
            return RoguelikeCardEffectType.MagicGoldBurst;

        if (normalized.Equals("MagicNexusMaxHealth", StringComparison.OrdinalIgnoreCase))
            return RoguelikeCardEffectType.MagicNexusMaxHealth;

        if (normalized.Equals("MagicSkill", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("스킬"))
            return RoguelikeCardEffectType.MagicSkill;

        return RoguelikeCardEffectType.None;
    }

    public static RoguelikeConditionType ParseConditionType(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return RoguelikeConditionType.None;

        var normalized = raw.Trim();
        if (normalized.Equals("KillEnemies", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("처치"))
            return RoguelikeConditionType.KillEnemies;

        if (normalized.Equals("ClearStages", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("스테이지"))
            return RoguelikeConditionType.ClearStages;

        if (normalized.Equals("SpendGold", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("소비"))
            return RoguelikeConditionType.SpendGold;

        if (normalized.Equals("BuildTowers", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("건설"))
            return RoguelikeConditionType.BuildTowers;

        return RoguelikeConditionType.None;
    }
}
