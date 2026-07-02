using System;

/// <summary>
/// 보스 면역/약점 그룹용 속성 문자열 파싱 (한글·영문).
/// </summary>
public static class BossElementUtility
{
    public static bool TryParseElement(string text, out DefenseSkillElement element)
    {
        element = DefenseSkillElement.Physical;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var value = text.Trim();
        if (Enum.TryParse(value, true, out DefenseSkillElement parsed) && parsed != DefenseSkillElement.Physical)
        {
            element = parsed;
            return true;
        }

        switch (value)
        {
            case "노멀":
            case "물리":
                element = DefenseSkillElement.Physical;
                return true;
            case "화염":
                element = DefenseSkillElement.Fire;
                return true;
            case "전기":
                element = DefenseSkillElement.Lightning;
                return true;
            case "얼음":
                element = DefenseSkillElement.Ice;
                return true;
            case "물":
                element = DefenseSkillElement.Water;
                return true;
            case "독":
                element = DefenseSkillElement.Poison;
                return true;
            case "바람":
                element = DefenseSkillElement.Wind;
                return true;
            default:
                return false;
        }
    }

    public static DefenseSkillElement ResolveEffectElement(DefenseEffectData effect)
    {
        if (effect == null)
            return DefenseSkillElement.Physical;

        if (DefenseElementalStatusMapping.IsElemental(effect.element))
            return effect.element;

        return effect.effectType switch
        {
            DefenseEffectType.Fire => DefenseSkillElement.Fire,
            DefenseEffectType.Poison => DefenseSkillElement.Poison,
            DefenseEffectType.Lightning => DefenseSkillElement.Lightning,
            DefenseEffectType.Water => DefenseSkillElement.Water,
            DefenseEffectType.Slow when effect.element == DefenseSkillElement.Ice => DefenseSkillElement.Ice,
            DefenseEffectType.Root when effect.element == DefenseSkillElement.Ice => DefenseSkillElement.Ice,
            _ => DefenseSkillElement.Physical
        };
    }
}
