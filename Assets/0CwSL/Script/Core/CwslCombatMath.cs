using UnityEngine;

public static class CwslCombatMath
{
    /// <summary>최종 피해량 = Max(1, 공격력 - 방어력)</summary>
    public static float ResolveDamage(float attackPower, float defense)
    {
        return Mathf.Max(1f, attackPower - defense);
    }
}
