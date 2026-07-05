using UnityEngine;

/// <summary>
/// 화면·월드 시야를 원형으로 판정/투영.
/// </summary>
public static class CwslVisionShape
{
    public static float GetCircularFlatDistance(Vector3 origin, Vector3 worldPosition)
    {
        var offset = worldPosition - origin;
        offset.y = 0f;
        return offset.magnitude;
    }

    public static void ProjectViewportRadii(
        Camera camera,
        Vector3 worldAnchor,
        float visionRadiusWorld,
        bool blindVision,
        out float innerRadius,
        out float outerRadius)
    {
        var centerViewport = camera.WorldToViewportPoint(worldAnchor);
        var right = camera.transform.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.right;
        else
            right.Normalize();

        var edgeViewport = camera.WorldToViewportPoint(worldAnchor + right * visionRadiusWorld);
        var radius = Mathf.Clamp(
            Vector2.Distance(
                new Vector2(centerViewport.x, centerViewport.y),
                new Vector2(edgeViewport.x, edgeViewport.y)),
            0.04f,
            0.98f);

        if (blindVision)
        {
            innerRadius = radius * 0.25f;
            outerRadius = radius * 1.15f;
        }
        else
        {
            innerRadius = radius * 0.82f;
            outerRadius = radius * 1.34f;
        }
    }

    public static float GetScreenAspect(Camera camera)
    {
        if (camera == null || camera.pixelHeight <= 0)
            return 16f / 9f;

        return (float)camera.pixelWidth / camera.pixelHeight;
    }
}
