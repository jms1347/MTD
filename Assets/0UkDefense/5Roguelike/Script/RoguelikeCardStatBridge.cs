public static class RoguelikeCardStatBridge
{
    public static float ApplyDamage(float baseDamage)
    {
        var modifiers = RoguelikeCardManager.Instance?.Modifiers;
        return modifiers != null ? modifiers.ApplyDamage(baseDamage) : baseDamage;
    }

    public static float ApplyRange(float baseRange)
    {
        var modifiers = RoguelikeCardManager.Instance?.Modifiers;
        return modifiers != null ? modifiers.ApplyRange(baseRange) : baseRange;
    }

    public static float ApplyFireInterval(float baseInterval)
    {
        var modifiers = RoguelikeCardManager.Instance?.Modifiers;
        return modifiers != null ? modifiers.ApplyFireInterval(baseInterval) : baseInterval;
    }
}
