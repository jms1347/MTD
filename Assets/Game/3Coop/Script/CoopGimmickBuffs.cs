using System.Collections.Generic;
using UnityEngine;

public static class CoopGimmickBuffs
{
    private static readonly Dictionary<string, float> MoveMultiplier = new();
    private static readonly Dictionary<string, float> MoveUntil = new();

    public static void SetMoveBoost(string playerId, float multiplier, float durationSeconds)
    {
        if (string.IsNullOrEmpty(playerId))
            return;

        MoveMultiplier[playerId] = Mathf.Max(1f, multiplier);
        MoveUntil[playerId] = Time.time + durationSeconds;
    }

    public static float GetMoveMultiplier(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
            return 1f;

        if (!MoveUntil.TryGetValue(playerId, out var until) || Time.time > until)
        {
            MoveMultiplier.Remove(playerId);
            MoveUntil.Remove(playerId);
            return 1f;
        }

        return MoveMultiplier.TryGetValue(playerId, out var mult) ? mult : 1f;
    }

    public static void Clear()
    {
        MoveMultiplier.Clear();
        MoveUntil.Clear();
    }
}
