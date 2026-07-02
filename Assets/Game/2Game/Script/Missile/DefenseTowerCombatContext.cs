using UnityEngine;

/// <summary>
/// 타워에서 스킬 실행 시 넘기는 전투 수치 (밸런싱용).
/// </summary>
public struct DefenseTowerCombatContext
{
    public int towerSheetId;
    public float baseDamage;
    public float fireInterval;
    public float attackRange;
    public float missileSpeed;

    public static DefenseTowerCombatContext FromLegacy(float baseDamage, float missileSpeed = 35f)
    {
        return new DefenseTowerCombatContext
        {
            baseDamage = baseDamage,
            missileSpeed = missileSpeed,
            fireInterval = 1f,
            attackRange = 18f
        };
    }
}
