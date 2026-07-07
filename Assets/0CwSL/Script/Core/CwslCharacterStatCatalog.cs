using UnityEngine;

/// <summary>플레이어 캐릭터별 전투·이동 스탯.</summary>
public static class CwslCharacterStatCatalog
{
    public readonly struct Entry
    {
        public readonly float MaxHealth;
        public readonly float AttackPower;
        public readonly float Defense;
        public readonly float MoveSpeed;

        public Entry(float maxHealth, float attackPower, float defense, float moveSpeed)
        {
            MaxHealth = maxHealth;
            AttackPower = attackPower;
            Defense = defense;
            MoveSpeed = moveSpeed;
        }
    }

    public static Entry Get(CwslCharacterId id)
    {
        return id switch
        {
            CwslCharacterId.MissileTank => new Entry(150f, 35f, 2f, 5.5f),
            CwslCharacterId.RedMage => new Entry(120f, 60f, 0f, 5f),
            CwslCharacterId.MomentumRammer => new Entry(350f, 25f, 8f, 7f),
            CwslCharacterId.CrowdGatherer => new Entry(250f, 18f, 5f, 5f),
            _ => new Entry(600f, 15f, 15f, 4.5f)
        };
    }

    public static float GetMaxHealth(CwslCharacterId id) => Get(id).MaxHealth;
    public static float GetAttackPower(CwslCharacterId id) => Get(id).AttackPower;
    public static float GetDefense(CwslCharacterId id) => Get(id).Defense;
    public static float GetMoveSpeed(CwslCharacterId id) => Get(id).MoveSpeed;

    public static float GetAttackCooldown(CwslCharacterId id)
    {
        return id switch
        {
            CwslCharacterId.Tank => 3f,
            _ => CwslGameConstants.AttackCooldown
        };
    }
}
