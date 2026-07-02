using System.Collections.Generic;
using UnityEngine;

public struct CoopSkillDefinition
{
    public string Id;
    public string DisplayName;
    public string Description;
    public bool RequiresGroundTarget;
}

public static class CoopSkillCatalog
{
    public const float CooldownSeconds = 180f;
    public const float BuffDurationSeconds = 15f;
    public const float GroundFieldDuration = 5f;
    public const float GroundFieldRadius = 4.5f;

    public const string Lightning = "lightning";
    public const string Blizzard = "blizzard";
    public const string Meteor = "meteor";
    public const string MoveBoost = "move_boost";
    public const string AttackSpeedBoost = "attack_speed_boost";
    public const string AttackPowerBoost = "attack_power_boost";
    public const string PoisonBees = "poison_bees";

    private static readonly CoopSkillDefinition[] All =
    {
        new()
        {
            Id = Lightning,
            DisplayName = "번개",
            Description = "지정 지점에 5초간 낙뢰 장판",
            RequiresGroundTarget = true
        },
        new()
        {
            Id = Blizzard,
            DisplayName = "블리자드",
            Description = "지정 지점에 5초간 눈보라 장판",
            RequiresGroundTarget = true
        },
        new()
        {
            Id = Meteor,
            DisplayName = "메테오",
            Description = "지정 지점에 5초간 유성 낙하",
            RequiresGroundTarget = true
        },
        new()
        {
            Id = MoveBoost,
            DisplayName = "가속",
            Description = "15초간 이동속도 3배",
            RequiresGroundTarget = false
        },
        new()
        {
            Id = AttackSpeedBoost,
            DisplayName = "연사",
            Description = "15초간 공격속도 3배",
            RequiresGroundTarget = false
        },
        new()
        {
            Id = AttackPowerBoost,
            DisplayName = "강화",
            Description = "15초간 공격력 3배",
            RequiresGroundTarget = false
        },
        new()
        {
            Id = PoisonBees,
            DisplayName = "독벌",
            Description = "독벌 3마리 소환",
            RequiresGroundTarget = false
        }
    };

    public static IReadOnlyList<CoopSkillDefinition> AllSkills => All;

    public static string PickRandom(System.Random random)
    {
        return All[random.Next(All.Length)].Id;
    }

    public static bool TryGet(string skillId, out CoopSkillDefinition definition)
    {
        for (var i = 0; i < All.Length; i++)
        {
            if (All[i].Id == skillId)
            {
                definition = All[i];
                return true;
            }
        }

        definition = All[0];
        return false;
    }

    public static bool RequiresGroundTarget(string skillId)
        => TryGet(skillId, out var definition) && definition.RequiresGroundTarget;

    public static string ResolveDisplayName(string skillId)
        => TryGet(skillId, out var definition) ? definition.DisplayName : skillId;
}
