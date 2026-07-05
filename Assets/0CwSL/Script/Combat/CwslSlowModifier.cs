using UnityEngine;

/// <summary>
/// 서버 전용 이동 속도 감소. 끌모 차지 중 적·투사체에 적용.
/// </summary>
public class CwslSlowModifier : MonoBehaviour
{
    private float slowUntil;
    private float multiplier = 1f;

    public float SpeedMultiplier
    {
        get
        {
            if (Time.time >= slowUntil)
            {
                multiplier = 1f;
                return 1f;
            }

            return multiplier;
        }
    }

    public bool IsSlowed => SpeedMultiplier < 0.999f;

    public static CwslSlowModifier Ensure(Component target)
    {
        if (target == null)
            return null;

        var existing = target.GetComponent<CwslSlowModifier>();
        if (existing != null)
            return existing;

        return target.gameObject.AddComponent<CwslSlowModifier>();
    }

    public void ApplySlow(float speedMultiplier, float durationSeconds)
    {
        multiplier = Mathf.Clamp(speedMultiplier, 0.05f, 1f);
        slowUntil = Time.time + Mathf.Max(0.05f, durationSeconds);
    }

    public void ClearSlow()
    {
        multiplier = 1f;
        slowUntil = 0f;
    }
}
