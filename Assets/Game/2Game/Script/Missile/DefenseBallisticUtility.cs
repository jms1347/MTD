using UnityEngine;

/// <summary>
/// 포물선(Parabola) 미사일 초기 속도 계산.
/// </summary>
public static class DefenseBallisticUtility
{
    public static Vector3 ComputeArcVelocity(Vector3 origin, Vector3 landPoint, float arcHeightAbovePeak = 0f)
    {
        landPoint.y = Mathf.Max(landPoint.y, 0.05f);

        float gravity = Mathf.Abs(Physics.gravity.y);
        if (gravity < 0.01f)
            gravity = 9.81f;

        Vector3 toTarget = landPoint - origin;
        Vector3 toTargetFlat = new Vector3(toTarget.x, 0f, toTarget.z);
        float horizontalDistance = toTargetFlat.magnitude;

        if (horizontalDistance < 0.05f)
        {
            float dropSpeed = Mathf.Sqrt(2f * gravity * Mathf.Max(0.5f, origin.y - landPoint.y));
            return Vector3.down * dropSpeed;
        }

        float peakY = Mathf.Max(origin.y, landPoint.y) + Mathf.Max(1.5f, arcHeightAbovePeak);
        float velocityY = Mathf.Sqrt(2f * gravity * Mathf.Max(0.1f, peakY - origin.y));
        float timeToPeak = velocityY / gravity;
        float timeFromPeak = Mathf.Sqrt(2f * Mathf.Max(0.1f, peakY - landPoint.y) / gravity);
        float totalTime = timeToPeak + timeFromPeak;
        if (totalTime < 0.05f)
            totalTime = 0.05f;

        Vector3 velocity = toTargetFlat.normalized * (horizontalDistance / totalTime);
        velocity.y = velocityY;
        return velocity;
    }

    public static Vector3 ProjectToGround(Vector3 worldPoint, float groundY = 0.05f)
    {
        worldPoint.y = groundY;
        return worldPoint;
    }
}
