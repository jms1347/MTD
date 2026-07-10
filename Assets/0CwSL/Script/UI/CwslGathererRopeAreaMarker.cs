using UnityEngine;
using UnityEngine.Rendering;

/// <summary>링거 W — 밧줄 범위 미리보기.</summary>
public static class CwslGathererRopeAreaMarker
{
    private static GameObject marker;
    private static Transform innerDisc;
    private static Material innerMaterial;

    public static void Show(Vector3 worldPoint, float radius)
    {
        Ensure();
        marker.SetActive(true);
        marker.transform.position = worldPoint + Vector3.up * 0.12f;
        var diameter = Mathf.Max(0.6f, radius * 2f);
        innerDisc.localScale = new Vector3(diameter, 0.1f, diameter);
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

        marker = new GameObject("CwslGathererRopeAreaMarker");
        Object.DontDestroyOnLoad(marker);

        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Inner";
        Object.Destroy(disc.GetComponent<Collider>());
        innerMaterial = CreateTransparentMaterial(new Color(0.45f, 0.35f, 0.95f, 0.38f));
        var renderer = disc.GetComponent<Renderer>();
        renderer.sharedMaterial = innerMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        innerDisc = disc.transform;
        innerDisc.SetParent(marker.transform, false);
        marker.SetActive(false);
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
        material.renderQueue = 3000;
        return material;
    }
}
