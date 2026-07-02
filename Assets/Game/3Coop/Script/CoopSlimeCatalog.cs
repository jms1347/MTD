using System;
using UnityEngine;

[Serializable]
public struct CoopSlimeWaveStats
{
    public string slimeKey;
    public CoopEnemyArchetype archetype;
    public float maxHp;
    public int defense;
    public float moveSpeed;
    public float contactDamage;
    public float explosionRadius;
    public float explosionDamage;
    public float touchRadius;
    public float meleeInterval;
    public int goldReward;
    public bool isBoss;
}

/// <summary>
/// 협동 모드 슬라임 종류·아키타입·웨이브별 스탯 밸런싱.
/// </summary>
public static class CoopSlimeCatalog
{
    private static readonly (CoopEnemyArchetype archetype, float weight)[] EarlyPool =
    {
        (CoopEnemyArchetype.Grunt, 0.45f),
        (CoopEnemyArchetype.Rusher, 0.55f)
    };

    private static readonly (CoopEnemyArchetype archetype, float weight)[] MidPool =
    {
        (CoopEnemyArchetype.Grunt, 0.25f),
        (CoopEnemyArchetype.Rusher, 0.25f),
        (CoopEnemyArchetype.Tank, 0.2f),
        (CoopEnemyArchetype.Bomber, 0.2f),
        (CoopEnemyArchetype.Missile, 0.1f)
    };

    private static readonly (CoopEnemyArchetype archetype, float weight)[] LatePool =
    {
        (CoopEnemyArchetype.Grunt, 0.12f),
        (CoopEnemyArchetype.Rusher, 0.18f),
        (CoopEnemyArchetype.Tank, 0.2f),
        (CoopEnemyArchetype.Bomber, 0.18f),
        (CoopEnemyArchetype.Missile, 0.17f),
        (CoopEnemyArchetype.HeavyBomber, 0.15f)
    };

    public static CoopSlimeWaveStats Resolve(int wave, bool forceBoss, System.Random random)
    {
        wave = Mathf.Max(1, wave);
        var isBoss = forceBoss || (wave % 5 == 0 && random.NextDouble() < 0.28d);
        var archetype = isBoss ? PickBossArchetype(wave, random) : PickArchetype(wave, random);
        var slimeKey = PickSlimeKey(archetype, wave, isBoss, random);
        var stats = BuildStats(wave, archetype, isBoss);
        stats.slimeKey = slimeKey;
        stats.archetype = archetype;
        return stats;
    }

    public static string ResolveDisplayName(string slimeKey, CoopEnemyArchetype archetype)
    {
        var typeName = CoopEnemyArchetypeUtil.ResolveDisplayName(archetype);
        var slimeName = slimeKey switch
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

        return $"{typeName} {slimeName}";
    }

    private static CoopEnemyArchetype PickBossArchetype(int wave, System.Random random)
    {
        if (wave >= 12 && random.NextDouble() < 0.35d)
            return CoopEnemyArchetype.HeavyBomber;
        if (random.NextDouble() < 0.55d)
            return CoopEnemyArchetype.Tank;
        return CoopEnemyArchetype.Grunt;
    }

    private static CoopEnemyArchetype PickArchetype(int wave, System.Random random)
    {
        var pool = wave <= 2 ? EarlyPool : wave <= 6 ? MidPool : LatePool;
        var roll = random.NextDouble();
        var cumulative = 0d;

        for (var i = 0; i < pool.Length; i++)
        {
            cumulative += pool[i].weight;
            if (roll <= cumulative)
                return pool[i].archetype;
        }

        return pool[pool.Length - 1].archetype;
    }

    private static string PickSlimeKey(CoopEnemyArchetype archetype, int wave, bool isBoss, System.Random random)
    {
        if (isBoss)
            return wave >= 10 && random.NextDouble() < 0.45d ? "SLIME-03-KING" : "SLIME-01-KING";

        return archetype switch
        {
            CoopEnemyArchetype.Rusher => wave <= 4 ? "SLIME-03-SPROUT" : "SLIME-02",
            CoopEnemyArchetype.Tank => wave <= 5 ? "SLIME-01-VIKING" : "SLIME-01-METAL",
            CoopEnemyArchetype.Bomber => "SLIME-02",
            CoopEnemyArchetype.Missile => "SLIME-03-SPROUT",
            CoopEnemyArchetype.HeavyBomber => wave <= 8 ? "SLIME-03" : "SLIME-03-LEAF",
            CoopEnemyArchetype.Grunt when wave <= 3 => "SLIME-01",
            CoopEnemyArchetype.Grunt when wave <= 7 => random.NextDouble() < 0.5d ? "SLIME-01" : "SLIME-03-LEAF",
            CoopEnemyArchetype.Grunt => "SLIME-03",
            _ => "SLIME-01"
        };
    }

    private static CoopSlimeWaveStats BuildStats(int wave, CoopEnemyArchetype archetype, bool isBoss)
    {
        var baseHp = 30f + wave * 14f + wave * wave * 0.55f;
        var baseDefense = Mathf.FloorToInt(wave * 0.35f);
        var baseSpeed = Mathf.Clamp(2.4f + wave * 0.065f, 2.4f, 5.6f);
        var baseContact = 4f + wave * 1.15f;
        var baseGold = 10 + wave * 3;

        if (isBoss)
        {
            var boss = new CoopSlimeWaveStats
            {
                maxHp = baseHp * 3.2f,
                defense = baseDefense + 3,
                moveSpeed = baseSpeed * 0.78f,
                contactDamage = baseContact * 2.1f,
                explosionRadius = 3.8f,
                explosionDamage = baseContact * 2.8f,
                touchRadius = 1.8f,
                meleeInterval = 1.1f,
                goldReward = 35 + wave * 12,
                isBoss = true,
                archetype = archetype
            };

            switch (archetype)
            {
                case CoopEnemyArchetype.Tank:
                    boss.maxHp *= 1.35f;
                    boss.defense += 2;
                    boss.moveSpeed *= 0.82f;
                    boss.contactDamage *= 1.25f;
                    boss.meleeInterval = 0.95f;
                    break;
                case CoopEnemyArchetype.HeavyBomber:
                    boss.moveSpeed *= 0.72f;
                    boss.explosionRadius = 5.2f;
                    boss.explosionDamage = baseContact * 4.8f;
                    boss.contactDamage = 0f;
                    break;
                case CoopEnemyArchetype.Rusher:
                    boss.maxHp *= 0.75f;
                    boss.moveSpeed = Mathf.Min(baseSpeed * 1.45f, 7.5f);
                    boss.contactDamage *= 1.5f;
                    break;
            }

            return boss;
        }

        switch (archetype)
        {
            case CoopEnemyArchetype.Rusher:
                return new CoopSlimeWaveStats
                {
                    maxHp = baseHp * 0.55f,
                    defense = Mathf.Max(0, Mathf.RoundToInt(baseDefense * 0.45f)),
                    moveSpeed = Mathf.Min(baseSpeed * 1.7f, 8.5f),
                    contactDamage = baseContact * 0.85f,
                    explosionRadius = 0f,
                    explosionDamage = 0f,
                    touchRadius = 1.15f,
                    meleeInterval = 0f,
                    goldReward = Mathf.RoundToInt(baseGold * 0.85f),
                    isBoss = false,
                    archetype = archetype
                };

            case CoopEnemyArchetype.Tank:
                return new CoopSlimeWaveStats
                {
                    maxHp = baseHp * 2.75f,
                    defense = baseDefense + 2,
                    moveSpeed = baseSpeed * 0.58f,
                    contactDamage = baseContact * 1.35f,
                    explosionRadius = 0f,
                    explosionDamage = 0f,
                    touchRadius = 1.65f,
                    meleeInterval = 1.45f,
                    goldReward = Mathf.RoundToInt(baseGold * 1.35f),
                    isBoss = false,
                    archetype = archetype
                };

            case CoopEnemyArchetype.Bomber:
                return new CoopSlimeWaveStats
                {
                    maxHp = baseHp * 0.72f,
                    defense = Mathf.Max(0, Mathf.RoundToInt(baseDefense * 0.55f)),
                    moveSpeed = baseSpeed * 1.05f,
                    contactDamage = 0f,
                    explosionRadius = 2.6f,
                    explosionDamage = baseContact * 1.35f,
                    touchRadius = 1.25f,
                    meleeInterval = 0f,
                    goldReward = Mathf.RoundToInt(baseGold * 0.95f),
                    isBoss = false,
                    archetype = archetype
                };

            case CoopEnemyArchetype.Missile:
                return new CoopSlimeWaveStats
                {
                    maxHp = baseHp * 0.38f,
                    defense = 0,
                    moveSpeed = Mathf.Min(baseSpeed * 2.15f, 10f),
                    contactDamage = 0f,
                    explosionRadius = 2.1f,
                    explosionDamage = baseContact * 1.05f,
                    touchRadius = 1.05f,
                    meleeInterval = 0f,
                    goldReward = Mathf.RoundToInt(baseGold * 0.75f),
                    isBoss = false,
                    archetype = archetype
                };

            case CoopEnemyArchetype.HeavyBomber:
                return new CoopSlimeWaveStats
                {
                    maxHp = baseHp * 1.05f,
                    defense = Mathf.Max(0, Mathf.RoundToInt(baseDefense * 0.7f)),
                    moveSpeed = baseSpeed * 0.5f,
                    contactDamage = 0f,
                    explosionRadius = 4.2f,
                    explosionDamage = baseContact * 3.6f,
                    touchRadius = 1.75f,
                    meleeInterval = 0f,
                    goldReward = Mathf.RoundToInt(baseGold * 1.25f),
                    isBoss = false,
                    archetype = archetype
                };

            default:
                return new CoopSlimeWaveStats
                {
                    maxHp = baseHp,
                    defense = baseDefense,
                    moveSpeed = baseSpeed,
                    contactDamage = baseContact,
                    explosionRadius = 0f,
                    explosionDamage = 0f,
                    touchRadius = 1.35f,
                    meleeInterval = 0f,
                    goldReward = baseGold,
                    isBoss = false,
                    archetype = CoopEnemyArchetype.Grunt
                };
        }
    }
}
