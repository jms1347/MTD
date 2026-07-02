using System.Collections.Generic;
using UnityEngine;

public struct CoopTankDefinition
{
    public string Code;
    public string DisplayName;
    public Color HullColor;
    public Color TurretColor;
    public Color TrackColor;
    public float HullLength;
    public float HullWidth;
    public float HullHeight;
    public float MoveSpeed;
    public float AttackRange;
}

public static class CoopTankCatalog
{
    private static readonly CoopTankDefinition[] All =
    {
        new()
        {
            Code = "TANK-SCOUT",
            DisplayName = "스카웃",
            HullColor = new Color(0.35f, 0.72f, 0.38f),
            TurretColor = new Color(0.28f, 0.55f, 0.3f),
            TrackColor = new Color(0.18f, 0.18f, 0.2f),
            HullLength = 1.15f,
            HullWidth = 0.82f,
            HullHeight = 0.42f,
            MoveSpeed = 7.5f,
            AttackRange = 11f
        },
        new()
        {
            Code = "TANK-RIFLE",
            DisplayName = "라이플",
            HullColor = new Color(0.42f, 0.5f, 0.62f),
            TurretColor = new Color(0.32f, 0.38f, 0.48f),
            TrackColor = new Color(0.16f, 0.16f, 0.18f),
            HullLength = 1.35f,
            HullWidth = 0.92f,
            HullHeight = 0.48f,
            MoveSpeed = 6f,
            AttackRange = 13f
        },
        new()
        {
            Code = "TANK-ASSAULT",
            DisplayName = "어썰트",
            HullColor = new Color(0.78f, 0.42f, 0.22f),
            TurretColor = new Color(0.58f, 0.3f, 0.16f),
            TrackColor = new Color(0.2f, 0.18f, 0.16f),
            HullLength = 1.5f,
            HullWidth = 1f,
            HullHeight = 0.52f,
            MoveSpeed = 5.2f,
            AttackRange = 12f
        },
        new()
        {
            Code = "TANK-SIEGE",
            DisplayName = "시즈",
            HullColor = new Color(0.55f, 0.38f, 0.72f),
            TurretColor = new Color(0.42f, 0.28f, 0.58f),
            TrackColor = new Color(0.14f, 0.14f, 0.16f),
            HullLength = 1.75f,
            HullWidth = 1.08f,
            HullHeight = 0.58f,
            MoveSpeed = 4.2f,
            AttackRange = 16f
        },
        new()
        {
            Code = "TANK-STRIKER",
            DisplayName = "스트라이커",
            HullColor = new Color(0.85f, 0.28f, 0.24f),
            TurretColor = new Color(0.62f, 0.2f, 0.18f),
            TrackColor = new Color(0.18f, 0.16f, 0.16f),
            HullLength = 1.25f,
            HullWidth = 0.88f,
            HullHeight = 0.44f,
            MoveSpeed = 6.8f,
            AttackRange = 10f
        },
        new()
        {
            Code = "TANK-BULWARK",
            DisplayName = "벌워크",
            HullColor = new Color(0.62f, 0.64f, 0.68f),
            TurretColor = new Color(0.48f, 0.5f, 0.54f),
            TrackColor = new Color(0.15f, 0.15f, 0.17f),
            HullLength = 1.6f,
            HullWidth = 1.12f,
            HullHeight = 0.55f,
            MoveSpeed = 4.8f,
            AttackRange = 14f
        }
    };

    public static IReadOnlyList<CoopTankDefinition> AllTanks => All;

    public static bool TryGet(string code, out CoopTankDefinition definition)
    {
        for (var i = 0; i < All.Length; i++)
        {
            if (All[i].Code == code)
            {
                definition = All[i];
                return true;
            }
        }

        definition = All[0];
        return false;
    }

    public static CoopTankDefinition GetRandom(System.Random random)
        => All[random.Next(All.Length)];

    public static string[] CreateShuffledCodes(System.Random random)
    {
        var codes = new string[All.Length];
        for (var i = 0; i < All.Length; i++)
            codes[i] = All[i].Code;

        for (var i = codes.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (codes[i], codes[j]) = (codes[j], codes[i]);
        }

        return codes;
    }

    public static void ApplyBaseStats(CoopPlayerState player, CoopTankDefinition tank)
    {
        player.towerCode = tank.Code;
        player.towerMaxHp = CoopGameProtocol.BaseHealth;
        player.towerHp = CoopGameProtocol.BaseHealth;
        player.attack = CoopGameProtocol.BaseAttack;
        player.fireInterval = CoopGameProtocol.BaseFireInterval;
        player.penetration = CoopGameProtocol.BasePenetration;
    }
}
