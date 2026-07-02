using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoguelikeRunModifiers
{
    public float towerDamagePercent;
    public float towerRangePercent;
    public float towerFireRatePercent;

    public float ApplyDamage(float baseDamage)
    {
        return baseDamage * (1f + towerDamagePercent * 0.01f);
    }

    public float ApplyRange(float baseRange)
    {
        return baseRange * (1f + towerRangePercent * 0.01f);
    }

    public float ApplyFireInterval(float baseInterval)
    {
        float multiplier = Mathf.Max(0.1f, 1f - towerFireRatePercent * 0.01f);
        return Mathf.Max(0.05f, baseInterval * multiplier);
    }
}

[Serializable]
public class RoguelikeConditionalProgress
{
    public RoguelikeCardData card;
    public int currentValue;
    public bool isFulfilled;
}

[Serializable]
public class RoguelikeOwnedMagicCard
{
    public RoguelikeCardData card;
}

public class RoguelikeRunState
{
    public readonly RoguelikeRunModifiers Modifiers = new();
    public readonly List<RoguelikeOwnedMagicCard> MagicHand = new();
    public readonly List<RoguelikeConditionalProgress> Conditionals = new();

    public int killCount;
    public int stagesCleared;
    public long goldSpent;
    public int towersBuilt;

    public void ResetRun()
    {
        Modifiers.towerDamagePercent = 0f;
        Modifiers.towerRangePercent = 0f;
        Modifiers.towerFireRatePercent = 0f;
        MagicHand.Clear();
        Conditionals.Clear();
        killCount = 0;
        stagesCleared = 0;
        goldSpent = 0;
        towersBuilt = 0;
    }
}
