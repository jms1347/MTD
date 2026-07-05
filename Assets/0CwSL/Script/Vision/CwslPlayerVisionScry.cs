using UnityEngine;

/// <summary>
/// 빨간 마법사 메테오 등 — 찍은 지점에 잠깐 열리는 추가 시야.
/// </summary>
public struct CwslPlayerVisionScry
{
    public Vector3 Center;
    public float Radius;
    public float EndTime;

    public bool IsActive => Time.time < EndTime;

    public static CwslPlayerVisionScry Create(Vector3 center, float radius, float duration)
    {
        center.y = 0f;
        return new CwslPlayerVisionScry
        {
            Center = center,
            Radius = radius,
            EndTime = Time.time + duration
        };
    }

    public float EvaluateVisibility(Vector3 worldPosition, bool isProjectile)
    {
        if (!IsActive)
            return 0f;

        return CwslLocalVisionSystem.EvaluateVisibility(
            Center,
            worldPosition,
            Radius,
            blind: false,
            isProjectile);
    }
}
