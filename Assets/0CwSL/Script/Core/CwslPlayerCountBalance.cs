using UnityEngine;

/// <summary>참여 인원(2~7)에 따른 방어 모드 밸런스 프로파일.</summary>
public static class CwslPlayerCountBalance
{
    public readonly struct Profile
    {
        public readonly int PlayerCount;
        public readonly float SpawnIntervalMultiplier;
        public readonly float MaxAliveMultiplier;
        public readonly float MonsterDamageMultiplier;
        public readonly float InitialBaseCountMultiplier;
        public readonly float MinuteEscalationMultiplier;

        public Profile(
            int playerCount,
            float spawnIntervalMultiplier,
            float maxAliveMultiplier,
            float monsterDamageMultiplier,
            float initialBaseCountMultiplier,
            float minuteEscalationMultiplier)
        {
            PlayerCount = playerCount;
            SpawnIntervalMultiplier = spawnIntervalMultiplier;
            MaxAliveMultiplier = maxAliveMultiplier;
            MonsterDamageMultiplier = monsterDamageMultiplier;
            InitialBaseCountMultiplier = initialBaseCountMultiplier;
            MinuteEscalationMultiplier = minuteEscalationMultiplier;
        }
    }

    private static readonly Profile[] Profiles =
    {
        new(2, 1.35f, 0.65f, 0.85f, 0.80f, 0.85f),
        new(3, 1.15f, 0.80f, 0.92f, 0.90f, 0.92f),
        new(4, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f),
        new(5, 0.92f, 1.12f, 1.06f, 1.05f, 1.05f),
        new(6, 0.85f, 1.22f, 1.12f, 1.10f, 1.08f),
        new(7, 0.78f, 1.32f, 1.18f, 1.15f, 1.12f),
    };

    public static Profile Get(int playerCount)
    {
        var clamped = Mathf.Clamp(playerCount, 2, 7);
        for (var i = 0; i < Profiles.Length; i++)
        {
            if (Profiles[i].PlayerCount == clamped)
                return Profiles[i];
        }

        return Profiles[2];
    }
}
