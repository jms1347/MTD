using System.Collections.Generic;
using UnityEngine;

public static class RoguelikeCardPool
{
    public static List<RoguelikeCardData> RollChoices(
        IReadOnlyList<RoguelikeCardData> source,
        int count,
        RoguelikeRunState runState,
        int maxMagicHandSize)
    {
        var result = new List<RoguelikeCardData>(count);
        if (source == null || source.Count == 0 || count <= 0)
            return result;

        var bag = new List<RoguelikeCardData>();
        int totalWeight = 0;

        for (int i = 0; i < source.Count; i++)
        {
            var card = source[i];
            if (card == null)
                continue;

            if (card.cardType == RoguelikeCardType.Magic
                && runState != null
                && runState.MagicHand.Count >= maxMagicHandSize)
                continue;

            bag.Add(card);
            totalWeight += Mathf.Max(1, card.poolWeight);
        }

        while (result.Count < count && bag.Count > 0)
        {
            var picked = PickWeighted(bag, totalWeight);
            if (picked == null)
                break;

            result.Add(picked);
            totalWeight -= Mathf.Max(1, picked.poolWeight);
            bag.Remove(picked);
        }

        return result;
    }

    private static RoguelikeCardData PickWeighted(List<RoguelikeCardData> bag, int totalWeight)
    {
        if (bag.Count == 0 || totalWeight <= 0)
            return null;

        int roll = Random.Range(0, totalWeight);
        int cursor = 0;
        for (int i = 0; i < bag.Count; i++)
        {
            cursor += Mathf.Max(1, bag[i].poolWeight);
            if (roll < cursor)
                return bag[i];
        }

        return bag[bag.Count - 1];
    }
}
