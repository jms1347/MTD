using System;

[Serializable]
public class MonsterData
{
    public string code;
    public string prefabKey;
    public string monsterType;
    public string attackMethod;
    public int hp;
    public int attack;
    public int defense;
    public float attackSpeed;
    /// <summary>루트 스케일 배율. 0 이하면 monsterType 기반 자동.</summary>
    public float scale;
    /// <summary>이동 속도(유닛/초). 0 이하면 monsterType 기반 자동.</summary>
    public float moveSpeed;

    public bool IsGroundUnit => true;

    public bool IsAirUnit => false;

    public bool IsMeleeAttacker =>
        !string.IsNullOrEmpty(attackMethod) &&
        attackMethod.Contains("근접", StringComparison.Ordinal);

    public bool IsSuicideAttacker =>
        !string.IsNullOrEmpty(attackMethod) &&
        attackMethod.Contains("자폭", StringComparison.Ordinal);

    /// <summary>prefabKey에 KING이 포함된 몬스터만 보스로 취급합니다.</summary>
    public bool IsBoss =>
        !string.IsNullOrEmpty(prefabKey) &&
        prefabKey.Contains("KING", StringComparison.OrdinalIgnoreCase);

    public MonsterData WithBossStats(BossData bossData)
    {
        if (bossData == null)
            return this;

        return new MonsterData
        {
            code = code,
            prefabKey = prefabKey,
            monsterType = monsterType,
            attackMethod = attackMethod,
            hp = bossData.hp,
            attack = bossData.attack,
            defense = bossData.defense,
            attackSpeed = bossData.attackSpeed,
            scale = scale,
            moveSpeed = moveSpeed
        };
    }

    public float GetMoveSpeed()
    {
        if (moveSpeed > 0f)
            return moveSpeed;

        if (IsBoss)
            return 2f;

        if (!string.IsNullOrEmpty(monsterType) && monsterType.Contains("날렵", StringComparison.Ordinal))
            return 5f;

        if (!string.IsNullOrEmpty(monsterType) && monsterType.Contains("뚱", StringComparison.Ordinal))
            return 2.5f;

        return 3.5f;
    }

    public float GetScaleMultiplier()
    {
        if (scale > 0f)
            return scale;

        if (IsBoss)
            return 2.5f;

        if (!string.IsNullOrEmpty(monsterType) && monsterType.Contains("뚱", StringComparison.Ordinal))
            return 2f;

        if (!string.IsNullOrEmpty(monsterType) && monsterType.Contains("날렵", StringComparison.Ordinal))
            return 0.5f;

        return 1f;
    }

    public float GetAttackCooldown()
    {
        if (attackSpeed <= 0f)
            return 1f;

        return 1f / attackSpeed;
    }
}
