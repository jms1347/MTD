using UnityEngine;

public struct DefenseSkillProjectileContext
{
    public int skillId;
    public float towerBaseDamage;
    public float fallbackMissileSpeed;
    public int linkDepth;
    public int pierceRemaining;
    public Transform homingTarget;
    public string targetMobility;
    public DefenseMoveType moveType;
    public Vector3 ballisticLandPoint;
    public bool hasBallisticLandPoint;
    public Vector3 skyBurstPoint;
    public bool hasSkyBurstPoint;
    public int towerSheetId;
    public float towerFireInterval;
    public float towerAttackRange;
    /// <summary>
    /// 시트 ExpDuration.
    /// 0=접촉 폭발, &gt;0=지연 폭발(유성 표식 등) 또는 장판 지속(눈보라),
    /// &lt;0=공중 폭발 플래그(번개구름 — 지연 초가 아님).
    /// </summary>
    public float expDuration;
    public bool isFrozenOrbShard;
    public float visualScaleMultiplier;
    public float frozenOrbShardLifetime;

    public static DefenseSkillProjectileContext Create(
        DefenseSkillData skill,
        DefenseTowerCombatContext tower,
        int linkDepth,
        Transform homingTarget,
        string targetMobility)
    {
        return new DefenseSkillProjectileContext
        {
            skillId = skill.skillId,
            towerBaseDamage = tower.baseDamage,
            fallbackMissileSpeed = tower.missileSpeed,
            linkDepth = linkDepth,
            pierceRemaining = skill.maxHit,
            homingTarget = homingTarget,
            targetMobility = string.IsNullOrWhiteSpace(targetMobility)
                ? DefenseTargetMobilityUtility.GroundLabel
                : targetMobility,
            moveType = skill.moveType,
            ballisticLandPoint = Vector3.zero,
            towerSheetId = tower.towerSheetId,
            towerFireInterval = tower.fireInterval,
            towerAttackRange = tower.attackRange,
            expDuration = skill.expDuration
        };
    }

    public static DefenseSkillProjectileContext Create(
        DefenseSkillData skill,
        float towerBaseDamage,
        float fallbackMissileSpeed,
        int linkDepth,
        Transform homingTarget,
        string targetMobility)
    {
        return Create(
            skill,
            DefenseTowerCombatContext.FromLegacy(towerBaseDamage, fallbackMissileSpeed),
            linkDepth,
            homingTarget,
            targetMobility);
    }

    public DefenseTowerCombatContext ToTowerCombatContext()
    {
        return new DefenseTowerCombatContext
        {
            towerSheetId = towerSheetId,
            baseDamage = towerBaseDamage,
            fireInterval = towerFireInterval > 0.05f ? towerFireInterval : 1f,
            attackRange = towerAttackRange > 0.05f ? towerAttackRange : 18f,
            missileSpeed = fallbackMissileSpeed
        };
    }

    public float ResolveDamage(DefenseSkillData skill)
    {
        if (skill == null)
            return towerBaseDamage;

        return towerBaseDamage * skill.damageMultiplier;
    }
}
