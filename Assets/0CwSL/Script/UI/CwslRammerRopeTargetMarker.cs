using UnityEngine;
using UnityEngine.Rendering;

/// <summary>말 E — 밧줄 연결 범위 미리보기 (E 홀드 중).</summary>
public static class CwslRammerRopeTargetMarker
{
    private static GameObject marker;
    private static Transform innerDisc;
    private static Transform outerRing;
    private static Material innerMaterial;
    private static Material outerMaterial;

    public static void Show(Vector3 worldPoint, float radius)
    {
        Ensure();
        marker.SetActive(true);
        marker.transform.position = worldPoint + Vector3.up * 0.12f;

        var diameter = Mathf.Max(0.6f, radius * 2f);
        innerDisc.localScale = new Vector3(diameter, 0.1f, diameter);
        outerRing.localScale = new Vector3(diameter * 1.08f, 0.06f, diameter * 1.08f);
    }

    public static void Hide()
    {
        if (marker != null)
            marker.SetActive(false);
    }

    private static void Ensure()
    {
        if (marker != null)
            return;

        marker = new GameObject("CwslRammerRopeTargetMarker");
        Object.DontDestroyOnLoad(marker);

        innerDisc = CreateDisc("Inner", new Color(0.72f, 0.72f, 0.72f, 0.42f), out innerMaterial);
        innerDisc.SetParent(marker.transform, false);

        outerRing = CreateDisc("Outer", new Color(0.95f, 0.95f, 0.95f, 0.88f), out outerMaterial);
        outerRing.SetParent(marker.transform, false);
        outerRing.localPosition = Vector3.up * 0.02f;

        marker.SetActive(false);
    }

    private static Transform CreateDisc(string name, Color color, out Material material)
    {
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = name;
        Object.Destroy(disc.GetComponent<Collider>());

        material = CreateTransparentMaterial(color);
        var renderer = disc.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return disc.transform;
    }

    private static Material CreateTransparentMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Color");
        var material = new Material(shader);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        material.color = color;

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetInt("_ZWrite", 0);

        material.renderQueue = 3000;
        return material;
    }
}
