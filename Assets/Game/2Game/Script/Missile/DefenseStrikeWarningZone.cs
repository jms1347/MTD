using UnityEngine;

/// <summary>
/// 메테오·유성 낙하 전 지면 경고 원형 영역.
/// </summary>
public static class DefenseStrikeWarningZone
{
    private const float GroundY = 0.05f;

    public static readonly Color BlizzardZoneColor = new(0.32f, 0.72f, 1f, 0.34f);
    public static readonly Color StormStrikeZoneColor = new(0.72f, 0.58f, 1f, 0.36f);

    public static GameObject Create(Vector3 groundPoint, float radius, Color color)
    {
        return CreateSustained(groundPoint, radius, color, null);
    }

    /// <summary>장판·눈보라 등 지속 스킬의 실제 splash 범위를 지면에 표시합니다.</summary>
    public static GameObject CreateSustained(
        Vector3 groundPoint,
        float radius,
        Color color,
        Transform parent = null)
    {
        var zone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        zone.name = parent != null ? "SkillAoEZone" : "StrikeWarningZone";
        zone.transform.localScale = new Vector3(radius * 2f, 0.04f, radius * 2f);

        if (parent != null)
        {
            zone.transform.SetParent(parent, false);
            zone.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            zone.transform.localRotation = Quaternion.identity;
        }
        else
            zone.transform.position = new Vector3(groundPoint.x, GroundY + 0.04f, groundPoint.z);

        var collider = zone.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        var renderer = zone.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = CreateTransparentMaterial(color);

        return zone;
    }

    public static void Pulse(GameObject zone, float normalizedTime)
    {
        if (zone == null)
            return;

        var renderer = zone.GetComponent<Renderer>();
        if (renderer == null || renderer.material == null)
            return;

        float pulse = 0.55f + Mathf.Sin(normalizedTime * Mathf.PI * 8f) * 0.25f;
        var color = renderer.material.color;
        color.a = Mathf.Clamp01(0.45f * pulse);
        renderer.material.color = color;
    }

    public static void DestroyZone(GameObject zone)
    {
        if (zone == null)
            return;

        var renderer = zone.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
            Object.Destroy(renderer.material);

        Object.Destroy(zone);
    }

    private static Material CreateTransparentMaterial(Color color)
    {
        var material = new Material(Shader.Find("Standard"));
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        material.color = color;
        return material;
    }
}
