using UnityEngine;

public static class RoguelikeCardEffectApplier
{
    public static void ApplyImmediate(RoguelikeCardData card, RoguelikeRunState runState)
    {
        if (card == null || runState == null)
            return;

        switch (card.effectType)
        {
            case RoguelikeCardEffectType.TowerDamagePercent:
                runState.Modifiers.towerDamagePercent += card.effectValue;
                break;
            case RoguelikeCardEffectType.TowerRangePercent:
                runState.Modifiers.towerRangePercent += card.effectValue;
                break;
            case RoguelikeCardEffectType.TowerFireRatePercent:
                runState.Modifiers.towerFireRatePercent += card.effectValue;
                break;
            case RoguelikeCardEffectType.GoldFlat:
                GameManager.Instance?.AddMoney(Mathf.RoundToInt(card.effectValue));
                break;
            case RoguelikeCardEffectType.NexusMaxHealthFlat:
                ApplyNexusMaxHealth(card.effectValue);
                break;
        }
    }

    public static bool TryConsumeInstantMagic(RoguelikeOwnedMagicCard owned)
    {
        if (owned?.card == null || owned.card.magicUseMode != RoguelikeMagicUseMode.Instant)
            return false;

        switch (owned.card.effectType)
        {
            case RoguelikeCardEffectType.MagicGoldBurst:
            case RoguelikeCardEffectType.GoldFlat:
                GameManager.Instance?.AddMoney(Mathf.RoundToInt(owned.card.effectValue));
                return true;
            case RoguelikeCardEffectType.MagicNexusMaxHealth:
            case RoguelikeCardEffectType.NexusMaxHealthFlat:
                ApplyNexusMaxHealth(owned.card.effectValue);
                return true;
            default:
                return false;
        }
    }

    private static void ApplyNexusMaxHealth(float amount)
    {
        if (amount <= 0f)
            return;

        var nexus = NexusManager.Instance?.NexusTransform;
        if (nexus == null)
            return;

        var health = nexus.GetComponent<Health>();
        health?.IncreaseMaxHealth(amount, healBySameAmount: true);
    }
}
