using UnityEngine;

/// <summary>플레이어 캐릭터별 전투·이동 스탯 (방어 모드 5분 기준).</summary>
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
            CwslCharacterId.MissileTank => new Entry(220f, 55f, 14f, 5.5f),
            CwslCharacterId.RedMage => new Entry(190f, 72f, 10f, 5f),
            CwslCharacterId.MomentumRammer => new Entry(400f, 36f, 32f, 7f),
            CwslCharacterId.CrowdGatherer => new Entry(300f, 32f, 22f, 5f),
            CwslCharacterId.Barricade => new Entry(520f, 28f, 48f, 4.2f),
            CwslCharacterId.Healer => new Entry(200f, 26f, 12f, 5.2f),
            _ => new Entry(720f, 30f, 70f, 4.5f)
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
            CwslCharacterId.Tank => 2.4f,
            CwslCharacterId.CrowdGatherer => CwslGameConstants.GathererMissileCooldown,
            CwslCharacterId.Barricade => CwslGameConstants.BarricadeMeleeCooldown,
            CwslCharacterId.Healer => CwslGameConstants.HealerMissileCooldown,
            _ => CwslGameConstants.AttackCooldown
        };
    }
}
