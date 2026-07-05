using UnityEngine;

/// <summary>서버 전용 이동 속도 증가 버프.</summary>
public class CwslMoveSpeedBuff : MonoBehaviour
{
    private float buffUntil;
    private float multiplier = 1f;

    public float SpeedMultiplier
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

    public bool IsBuffed => SpeedMultiplier > 1.001f;

    public static CwslMoveSpeedBuff Ensure(Component target)
    {
        if (target == null)
            return null;

        var existing = target.GetComponent<CwslMoveSpeedBuff>();
        if (existing != null)
            return existing;

        return target.gameObject.AddComponent<CwslMoveSpeedBuff>();
    }

    public void ApplyBuff(float speedMultiplier, float durationSeconds)
    {
        multiplier = Mathf.Clamp(speedMultiplier, 1f, 2.5f);
        buffUntil = Time.time + Mathf.Max(0.05f, durationSeconds);
    }

    public void ClearBuff()
    {
        multiplier = 1f;
        buffUntil = 0f;
    }
}
