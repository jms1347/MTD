using UnityEngine;

/// <summary>공격속도 배율 버프(쿨다운/연사 간격에 사용).</summary>
public class CwslAttackSpeedBuff : MonoBehaviour
{
    private float buffUntil;
    private float multiplier = 1f;

    public float AttackSpeedMultiplier
    {
        get
        {
            if (Time.time >= buffUntil)
            {
                multiplier = 1f;
                return 1f;
            }

            return multiplier;
        }
    }

    public static CwslAttackSpeedBuff Ensure(Component target)
    {
        if (target == null)
            return null;

        var existing = target.GetComponent<CwslAttackSpeedBuff>();
        if (existing != null)
            return existing;

        return target.gameObject.AddComponent<CwslAttackSpeedBuff>();
    }

    public void ApplyBuff(float attackSpeedMultiplier, float durationSeconds)
    {
        multiplier = Mathf.Clamp(attackSpeedMultiplier, 1f, 3f);
        buffUntil = Time.time + Mathf.Max(0.05f, durationSeconds);
    }
}
