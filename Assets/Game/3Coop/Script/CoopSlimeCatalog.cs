using System;
using UnityEngine;

[Serializable]
public struct CoopSlimeWaveStats
{
    public string slimeKey;
    public float maxHp;
    public int defense;
    public float moveSpeed;
    public float contactDamage;
    public int goldReward;
    public bool isBoss;
}

/// <summary>
/// 협동 모드 슬라임 종류·웨이브별 스탯 밸런싱.
/// </summary>
public static class CoopSlimeCatalog
{
    private static readonly string[] EarlySlimes = { "SLIME-01", "SLIME-03-SPROUT" };
    private static readonly string[] MidSlimes = { "SLIME-02", "SLIME-03-LEAF", "SLIME-01" };
    private static readonly string[] LateSlimes = { "SLIME-03", "SLIME-01-VIKING", "SLIME-01-METAL" };
    private static readonly string[] EliteSlimes =
    {
        "SLIME-03", "SLIME-01-VIKING", "SLIME-01-METAL", "SLIME-02", "SLIME-03-LEAF"
    };

    public static CoopSlimeWaveStats Resolve(int wave, bool forceBoss, System.Random random)
    {
        wave = Mathf.Max(1, wave);
        var isBoss = forceBoss || (wave % 5 == 0 && random.NextDouble() < 0.28d);
        var slimeKey = PickSlimeKey(wave, isBoss, random);
        var stats = BuildBaseStats(wave, isBoss);
        stats.slimeKey = slimeKey;
        return stats;
    }

    public static string ResolveDisplayName(string slimeKey)
    {
        return slimeKey switch
        {
            "SLIME-01" => "슬라임",
            "SLIME-02" => "블루 슬라임",
            "SLIME-03" => "퍼플 슬라임",
            "SLIME-01-KING" => "킹 슬라임",
            "SLIME-03-KING" => "퍼플 킹",
            "SLIME-01-VIKING" => "바이킹 슬라임",
            "SLIME-01-METAL" => "메탈 슬라임",
            "SLIME-03-LEAF" => "리프 슬라임",
            "SLIME-03-SPROUT" => "새싹 슬라임",
            _ => slimeKey
        };
    }

    private static string PickSlimeKey(int wave, bool isBoss, System.Random random)
    {
        if (isBoss)
            return wave >= 10 && random.NextDouble() < 0.45d ? "SLIME-03-KING" : "SLIME-01-KING";

        var pool = wave <= 2 ? EarlySlimes
            : wave <= 5 ? MidSlimes
            : wave <= 9 ? LateSlimes
            : EliteSlimes;

        return pool[random.Next(pool.Length)];
    }

    private static CoopSlimeWaveStats BuildBaseStats(int wave, bool isBoss)
    {
        var hp = 30f + wave * 14f + wave * wave * 0.55f;
        var defense = Mathf.FloorToInt(wave * 0.35f);
        var speed = Mathf.Clamp(2.4f + wave * 0.065f, 2.4f, 5.6f);
        var contact = 4f + wave * 1.15f;
        var gold = 10 + wave * 3;

        if (isBoss)
        {
            return new CoopSlimeWaveStats
            {
                maxHp = hp * 3.2f,
                defense = defense + 3,
                moveSpeed = speed * 0.78f,
                contactDamage = contact * 2.1f,
                goldReward = 35 + wave * 12,
                isBoss = true
            };
        }

        return new CoopSlimeWaveStats
        {
            maxHp = hp,
            defense = defense,
            moveSpeed = speed,
            contactDamage = contact,
            goldReward = gold,
            isBoss = false
        };
    }
}
