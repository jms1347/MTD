using UnityEngine;

/// <summary>
/// 몬스터 비주얼·스폰 Y를 맵 지면에 맞춥니다.
/// </summary>
public static class MonsterGroundPlacement
{
    /// <summary>DefenseMapBuilder 타일 윗면 Y.</summary>
    public const float MapSurfaceY = 0.04f;

    public static float ResolveGroundY(Vector3 groundPoint)
    {
        return Mathf.Max(groundPoint.y, MapSurfaceY);
    }

    /// <summary>비주얼 메시 바닥을 부모 로컬 Y=0(지면)에 맞춥니다. 비주얼 높이(월드)를 반환합니다.</summary>
    public static float AlignVisualFeetToLocalGround(Transform visualRoot)
    {
        if (visualRoot == null)
            return 1f;

        var renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return 1f;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        var parent = visualRoot.parent;
        if (parent == null)
            return bounds.size.y;

        var localBottom = parent.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
        if (Mathf.Abs(localBottom.y) > 0.0001f)
            visualRoot.localPosition += new Vector3(0f, -localBottom.y, 0f);

        return bounds.size.y;
    }

    public static float ResolveHeadOffset(Transform root, float fallback = 0.9f)
    {
        if (root == null)
            return fallback;

        var visual = root.Find("Visual");
        if (visual == null)
            return fallback;

        var renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return fallback;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        var localTop = root.InverseTransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
        return Mathf.Max(fallback, localTop.y + 0.08f);
    }
}
