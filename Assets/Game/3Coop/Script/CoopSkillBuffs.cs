using System.Collections.Generic;
using UnityEngine;

public static class CoopSkillBuffs
{
    private static readonly Dictionary<string, float> AttackMultiplier = new();
    private static readonly Dictionary<string, float> AttackUntil = new();
    private static readonly Dictionary<string, float> FireIntervalMultiplier = new();
    private static readonly Dictionary<string, float> FireIntervalUntil = new();

    public static void SetAttackPowerBoost(string playerId, float multiplier, float durationSeconds)
    {
        if (string.IsNullOrEmpty(playerId))
            return;

        AttackMultiplier[playerId] = Mathf.Max(1f, multiplier);
        AttackUntil[playerId] = Time.time + durationSeconds;
    }

    public static void SetAttackSpeedBoost(string playerId, float multiplier, float durationSeconds)
    {
        if (string.IsNullOrEmpty(playerId))
            return;

        FireIntervalMultiplier[playerId] = Mathf.Clamp(multiplier, 0.05f, 1f);
        FireIntervalUntil[playerId] = Time.time + durationSeconds;
    }

    public static float GetAttackMultiplier(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
            return 1f;

        if (!AttackUntil.TryGetValue(playerId, out var until) || Time.time > until)
        {
            AttackMultiplier.Remove(playerId);
            AttackUntil.Remove(playerId);
            return 1f;
        }

        return AttackMultiplier.TryGetValue(playerId, out var mult) ? mult : 1f;
    }

    public static float GetFireIntervalMultiplier(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
            return 1f;

        if (!FireIntervalUntil.TryGetValue(playerId, out var until) || Time.time > until)
        {
            FireIntervalMultiplier.Remove(playerId);
            FireIntervalUntil.Remove(playerId);
            return 1f;
        }

        return FireIntervalMultiplier.TryGetValue(playerId, out var mult) ? mult : 1f;
    }

    public static void Clear()
    {
        AttackMultiplier.Clear();
        AttackUntil.Clear();
        FireIntervalMultiplier.Clear();
        FireIntervalUntil.Clear();
    }
}
