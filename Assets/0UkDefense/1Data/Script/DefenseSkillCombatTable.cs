using UnityEngine;

/// <summary>
/// 스킬 데이터 → 어드레서블 미사일 프리팹 연결.
/// </summary>
public static class DefenseSkillCombatTable
{
    public static DefenseMissileId GetMissileIdForSkill(DefenseSkillData skill)
    {
        if (skill == null)
            return DefenseMissileId.Physical;

        return skill.element switch
        {
            DefenseSkillElement.Fire => DefenseMissileId.Fire,
            DefenseSkillElement.Lightning => DefenseMissileId.Lightning,
            DefenseSkillElement.Ice => DefenseMissileId.Ice,
            DefenseSkillElement.Water => DefenseMissileId.Water,
            DefenseSkillElement.Poison => DefenseMissileId.Poison,
            _ => DefenseMissileId.Physical
        };
    }

    /// <summary>
    /// N열 미사일 프리팹 Key. prefabKey → skillCode → skillId 순.
    /// </summary>
    public static string ResolveMissilePrefabKey(DefenseSkillData skill)
    {
        if (skill == null)
            return null;

        if (!string.IsNullOrWhiteSpace(skill.prefabKey))
            return skill.prefabKey.Trim();

        if (!string.IsNullOrWhiteSpace(skill.skillCode))
            return skill.skillCode.Trim();

        return skill.skillId > 0 ? skill.skillId.ToString() : null;
    }

    /// <summary>G열 ExpDuration &lt; 0 — 공중 폭발(하늘 타겟) 미사일. 지연 타이머가 아닙니다.</summary>
    public const float AirAnchorExpDuration = -1f;

    public static bool IsAirAnchorExpDuration(float expDuration)
    {
        return expDuration < 0f;
    }

    /// <summary>눈보라·번개구름 등 — 처음 조준한 지점의 하늘 좌표에서 폭발합니다.</summary>
    public static bool UsesSkyBurstTargeting(DefenseSkillData skill)
    {
        return IsBlizzardAirburstSkill(skill) || IsStormMissileAnchorSkill(skill);
    }

    public static Vector3 ResolveSkyBurstPoint(Vector3 origin, Vector3 groundPoint, DefenseSkillData skill)
    {
        groundPoint = DefenseBallisticUtility.ProjectToGround(groundPoint);
        float horizontal = Vector3.Distance(
            new Vector3(origin.x, 0f, origin.z),
            new Vector3(groundPoint.x, 0f, groundPoint.z));
        float heightAboveGround = Mathf.Clamp(horizontal * 0.38f + 4.5f, 5f, 13f);

        if (IsBlizzardAirburstSkill(skill) && skill.splashRadius > 0.05f)
            heightAboveGround = Mathf.Clamp(skill.splashRadius * 0.85f + 2.5f, 5f, 12f);

        return groundPoint + Vector3.up * heightAboveGround;
    }

    public static bool IsStormMissileAnchorSkill(DefenseSkillData skill)
    {
        if (skill == null || skill.skillType != DefenseSkillType.Missile)
            return false;

        return IsAirAnchorExpDuration(skill.expDuration);
    }

    public static bool UsesLinkedShellDetonation(DefenseSkillData skill)
    {
        return UsesAirAnchorLinkedSummon(skill) || IsStormMissileAnchorSkill(skill);
    }

    public static bool UsesAirAnchorLinkedSummon(DefenseSkillData skill)
    {
        if (skill == null || !skill.HasSummonPrefab)
            return false;

        return string.Equals(
            skill.summonPrefabKey?.Trim(),
            "Zone_StormArc",
            System.StringComparison.OrdinalIgnoreCase)
            || IsStormMissileAnchorSkill(skill);
    }

    public static bool UsesGroundLinkedZoneSummon(DefenseSkillData skill)
    {
        return skill != null && skill.HasSummonPrefab && !UsesAirAnchorLinkedSummon(skill);
    }

    public const string VolcanoEruptionSkillCode = "M-F-0004";
    public const string DelayedMeteorBeaconSkillCode = "M-F-0005";
    /// <summary>지연 유성 낙하 시 기본 실탄 스킬 (O열 미지정 폴백).</summary>
    public const string MeteorStrikeSkillCode = "M-F-0003";
    public const string BlizzardProjectileSkillCode = "M-I-0003";
    public const string BlizzardSnowZonePrefabKey = "Zone_BlizzardSnow";
    public const string BlizzardStormVfxKey = "NovaFrost";
    public const string ArcRepeaterSkillCode = "M-L-0003";
    public const string ArcHomingSkillCode = "M-L-0006";
    public const string PoisonStingerSwarmSkillCode = "M-P-0005";
    public const string RoguelikeBlizzardSkillCode = "M-I-0003";
    public const string RoguelikeStormSkillCode = "M-L-0002";
    public const string RoguelikeMeteorSkillCode = "M-F-0003";
    public const float RoguelikeGroundFieldDuration = 5f;

    public static bool IsPoisonStingerSwarmSkill(DefenseSkillData skill)
    {
        return skill != null
            && string.Equals(
                skill.skillCode?.Trim(),
                PoisonStingerSwarmSkillCode,
                System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsRoguelikeGroundFieldSkill(DefenseSkillData skill)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.skillCode))
            return false;

        var code = skill.skillCode.Trim();
        return string.Equals(code, RoguelikeBlizzardSkillCode, System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, RoguelikeStormSkillCode, System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, RoguelikeMeteorSkillCode, System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsArcRepeaterChainSkill(DefenseSkillData skill)
    {
        return skill != null
            && string.Equals(
                skill.skillCode?.Trim(),
                ArcRepeaterSkillCode,
                System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsArcHomingMissileSkill(DefenseSkillData skill)
    {
        return skill != null
            && string.Equals(
                skill.skillCode?.Trim(),
                ArcHomingSkillCode,
                System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool SkipsMissileSplash(DefenseSkillData skill)
    {
        return IsArcHomingMissileSkill(skill) || IsArcRepeaterChainSkill(skill);
    }

    public static bool TryResolveFollowUpSkill(DefenseSkillData source, out DefenseSkillData followUp)
    {
        followUp = null;
        if (source == null || !source.HasFollowUpSkill || DataManager.Instance == null)
            return false;

        return DataManager.Instance.TryGetSkillByCode(source.followUpSkillCode, out followUp);
    }

    public static bool IsVolcanoEruptionSkill(DefenseSkillData skill)
    {
        return skill != null
            && string.Equals(
                skill.skillCode?.Trim(),
                VolcanoEruptionSkillCode,
                System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDelayedMeteorBeaconSkill(DefenseSkillData skill)
    {
        return skill != null
            && string.Equals(
                skill.skillCode?.Trim(),
                DelayedMeteorBeaconSkillCode,
                System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsBlizzardAirburstSkill(DefenseSkillData skill)
    {
        return skill != null
            && string.Equals(
                skill.skillCode?.Trim(),
                BlizzardProjectileSkillCode,
                System.StringComparison.OrdinalIgnoreCase)
            && skill.HasSummonPrefab;
    }

    public static bool UsesStormMissileAnchorInFlight(DefenseSkillData skill)
    {
        return IsStormMissileAnchorSkill(skill);
    }

    public static bool NeedsMissilePrefab(DefenseSkillData skill)
    {
        if (skill == null || skill.skillType != DefenseSkillType.Missile)
            return false;

        return skill.moveType != DefenseMoveType.StormCloud
            && skill.moveType != DefenseMoveType.InstantHit;
    }

    public static GameObject GetMissilePrefabForSkill(DefenseSkillData skill)
    {
        if (skill == null || !NeedsMissilePrefab(skill))
            return null;

        var key = ResolveMissilePrefabKey(skill);
        if (!string.IsNullOrEmpty(key) && DefenseAddressableLoader.TryLoadMissile(key, out var addressablePrefab))
            return addressablePrefab;

        return DefenseMissileResolver.GetPrefab(GetMissileIdForSkill(skill));
    }

    public static float ResolveMissileSpeed(DefenseSkillData skill, float fallbackSpeed)
    {
        if (skill == null)
            return fallbackSpeed;

        if (skill.moveType == DefenseMoveType.StormCloud)
            return fallbackSpeed;

        return skill.speed > 0f ? skill.speed : fallbackSpeed;
    }
}
