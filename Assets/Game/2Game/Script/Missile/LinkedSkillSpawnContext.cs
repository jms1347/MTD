using UnityEngine;

/// <summary>
/// 1탄 미사일 명중 후 M열 소환 프리팹에 전달하는 컨텍스트.
/// 소환체는 보통 미사일 스킬(sourceSkill) + 발사 타워 수치를 함께 참조합니다.
/// </summary>
public struct LinkedSkillSpawnContext
{
    public const float SummonLifetimeFromTowerFireIntervalRatio = 0.7f;
    public const float DefaultSummonTickInterval = 1.2f;
    public const float FallbackSummonLifetime = 3f;
    public const float DefaultSummonStrikeRadius = 5f;
    public const float SummonRadiusFromTowerRangeRatio = 0.4f;

    /// <summary>명중한 미사일 스킬 행 — 속성·배율·이펙트 그룹·소환 키 등.</summary>
    public DefenseSkillData sourceSkill;

    /// <summary>M열 소환 프리팹 키.</summary>
    public string summonPrefabKey;

    /// <summary>발사한 타워의 시트 ID (0이면 미지).</summary>
    public int towerSheetId;

    /// <summary>타워 baseDamage (스킬 배율 적용 전).</summary>
    public float towerBaseDamage;

    /// <summary>타워 공격 간격(초).</summary>
    public float towerFireInterval;

    /// <summary>타워 사거리.</summary>
    public float towerAttackRange;

    /// <summary>1탄 미사일 비행 속도(폴백).</summary>
    public float sourceMissileSpeed;

    public string targetMobility;

    /// <summary>명중 지점.</summary>
    public Vector3 spawnOrigin;

    /// <summary>공중 폭발 높이(지면 기준). 0이면 VFX에서 추정.</summary>
    public float airBurstHeight;

    /// <summary>우선 추적 대상.</summary>
    public Transform anchorTarget;

    public int linkDepth;

    public DefenseSkillData SourceSkill => sourceSkill;

    public static LinkedSkillSpawnContext Create(
        DefenseSkillData sourceSkill,
        Vector3 origin,
        Transform preferredTarget,
        DefenseTowerCombatContext tower,
        string targetMobility,
        int linkDepth,
        float airBurstHeight = 0f)
    {
        return new LinkedSkillSpawnContext
        {
            sourceSkill = sourceSkill,
            summonPrefabKey = sourceSkill != null ? sourceSkill.summonPrefabKey.Trim() : string.Empty,
            towerSheetId = tower.towerSheetId,
            towerBaseDamage = tower.baseDamage,
            towerFireInterval = tower.fireInterval,
            towerAttackRange = tower.attackRange,
            sourceMissileSpeed = tower.missileSpeed,
            targetMobility = string.IsNullOrWhiteSpace(targetMobility)
                ? DefenseTargetMobilityUtility.GroundLabel
                : targetMobility,
            spawnOrigin = origin,
            airBurstHeight = airBurstHeight,
            anchorTarget = preferredTarget,
            linkDepth = linkDepth
        };
    }

    public string ResolveTargetMobility()
    {
        return string.IsNullOrWhiteSpace(targetMobility)
            ? DefenseTargetMobilityUtility.GroundLabel
            : targetMobility;
    }

    /// <summary>타워 기본 공격력 × 미사일 스킬 damageMultiplier.</summary>
    public float ResolveDamage()
    {
        if (sourceSkill == null)
            return towerBaseDamage;

        return towerBaseDamage * sourceSkill.damageMultiplier;
    }

    public float ResolveShellDamage() => ResolveDamage();

    public DefenseSkillElement ResolveElement()
    {
        return sourceSkill != null ? sourceSkill.element : DefenseSkillElement.Physical;
    }

    public DamageElement ResolveDamageElement()
    {
        return sourceSkill != null
            ? sourceSkill.DamageElement
            : DamageElement.Physical;
    }

    /// <summary>소환체 지속 시간 — 타워 공격속도의 70%.</summary>
    public float ResolveLifetime()
    {
        if (towerFireInterval > 0.05f)
            return towerFireInterval * SummonLifetimeFromTowerFireIntervalRatio;

        return FallbackSummonLifetime;
    }

    /// <summary>장판 지속 시간 — M열 지면 소환 스킬의 양수 expDuration만 사용.</summary>
    public float ResolveZoneLifetime()
    {
        if (sourceSkill != null
            && sourceSkill.HasSummonPrefab
            && sourceSkill.expDuration > 0.05f
            && !DefenseSkillCombatTable.IsAirAnchorExpDuration(sourceSkill.expDuration))
            return sourceSkill.expDuration;

        return ResolveLifetime();
    }

    /// <summary>소환체 자체 공격 간격. 미사일/타워 공격속도와는 별도.</summary>
    public float ResolveSummonTickInterval()
    {
        return DefaultSummonTickInterval;
    }

    /// <summary>스킬 splashRadius + 타워 사거리 기반 소환체 타격 범위.</summary>
    public float ResolveStrikeRadius()
    {
        float fromSkill = sourceSkill != null && sourceSkill.splashRadius > 0f
            ? sourceSkill.splashRadius
            : 0f;
        float fromTower = towerAttackRange > 0.05f
            ? towerAttackRange * SummonRadiusFromTowerRangeRatio
            : 0f;

        return Mathf.Max(DefaultSummonStrikeRadius, fromSkill, fromTower);
    }

    public DefenseTowerCombatContext ToTowerCombatContext()
    {
        return new DefenseTowerCombatContext
        {
            towerSheetId = towerSheetId,
            baseDamage = towerBaseDamage,
            fireInterval = towerFireInterval > 0.05f ? towerFireInterval : 1f,
            attackRange = towerAttackRange > 0.05f ? towerAttackRange : 18f,
            missileSpeed = sourceMissileSpeed > 0.05f ? sourceMissileSpeed : 35f
        };
    }
}
