using UnityEngine;

/// <summary>런타임 몬스터 스탯 배율 (중간보스 버프 등).</summary>
public class CwslMonsterRuntimeStats : MonoBehaviour
{
    private float damageMultiplier = 1f;
    private float defenseMultiplier = 1f;
    private float speedMultiplier = 1f;
    private float damageBuffUntil;
    private float defenseBuffUntil;
    private float speedBuffUntil;

    public float DamageMultiplier => damageMultiplier;
    public float DefenseMultiplier => defenseMultiplier;
    public float SpeedMultiplier => speedMultiplier;

    private void Update()
    {
        var now = Time.time;
        if (now > damageBuffUntil)
            damageMultiplier = 1f;
        if (now > defenseBuffUntil)
            defenseMultiplier = 1f;
        if (now > speedBuffUntil)
            speedMultiplier = 1f;
    }

    public void SetTimedDamageMultiplier(float multiplier, float durationSeconds)
    {
        damageMultiplier = multiplier;
        damageBuffUntil = Time.time + durationSeconds;
    }

    public void SetTimedDefenseMultiplier(float multiplier, float durationSeconds)
    {
        defenseMultiplier = multiplier;
        defenseBuffUntil = Time.time + durationSeconds;
    }

    public void SetTimedSpeedMultiplier(float multiplier, float durationSeconds)
    {
        speedMultiplier = multiplier;
        speedBuffUntil = Time.time + durationSeconds;
    }
}
