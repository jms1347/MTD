public static class DefenseEffectSheetParser
{
    public static DefenseEffectType ParseEffectType(string raw)
    {
        return Normalize(raw) switch
        {
            "화염" or "fire" or "burn" => DefenseEffectType.Fire,
            "그라운드" or "ground" => DefenseEffectType.Ground,
            "이동불가" or "root" or "immobilize" => DefenseEffectType.Root,
            "행동불가" or "stun" or "shock" => DefenseEffectType.Stun,
            "독" or "poison" => DefenseEffectType.Poison,
            "물" or "water" or "wet" => DefenseEffectType.Water,
            "전기" or "lightning" or "electric" or "thunder" => DefenseEffectType.Lightning,
            "슬로우" or "slow" or "frost" => DefenseEffectType.Slow,
            "밀어냄" or "knockback" or "knock" => DefenseEffectType.Knockback,
            _ => DefenseEffectType.Fire
        };
    }

    private static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        return raw.Trim().Replace(" ", string.Empty).ToLowerInvariant();
    }
}
