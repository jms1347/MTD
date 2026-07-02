using System;

public static class DefenseSkillSheetParser
{
    public static DefenseSkillType ParseSkillType(string raw)
    {
        return Normalize(raw) switch
        {
            "미사일" => DefenseSkillType.Missile,
            "장판" => DefenseSkillType.Field,
            _ => DefenseSkillType.Missile
        };
    }

    public static DefenseMoveType ParseMoveType(string raw)
    {
        return Normalize(raw) switch
        {
            "직선" => DefenseMoveType.Straight,
            "포물선" => DefenseMoveType.Parabola,
            "즉시타격" => DefenseMoveType.InstantHit,
            "고정" => DefenseMoveType.Fixed,
            "구름" or "stormcloud" or "storm" => DefenseMoveType.StormCloud,
            _ => DefenseMoveType.Straight
        };
    }

    public static DefenseSkillElement ParseElement(string raw)
    {
        return Normalize(raw) switch
        {
            "물리" or "physical" => DefenseSkillElement.Physical,
            "화염" or "fire" => DefenseSkillElement.Fire,
            "전기" or "lightning" or "electric" or "thunder" => DefenseSkillElement.Lightning,
            "얼음" or "ice" or "frost" => DefenseSkillElement.Ice,
            "물" or "water" => DefenseSkillElement.Water,
            "독" or "poison" => DefenseSkillElement.Poison,
            "바람" or "wind" => DefenseSkillElement.Wind,
            _ => DefenseSkillElement.Physical
        };
    }

    public static bool ParseHomingFlag(string raw)
    {
        if (SheetParseUtility.ParseYn(raw))
            return true;

        return SheetParseUtility.TryParseInt(raw, out var value) && value != 0;
    }

    private static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        return raw.Trim().Replace(" ", string.Empty).ToLowerInvariant();
    }
}
