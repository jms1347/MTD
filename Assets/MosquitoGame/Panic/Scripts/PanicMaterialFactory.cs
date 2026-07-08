using UnityEngine;

public static class PanicMaterialFactory
{
    private static Shader cachedShader;

    public static Material Create(Color color, bool transparent = false)
    {
        var shader = GetLitShader();
        var material = new Material(shader) { color = color };

        if (!transparent)
            return material;

        ConfigureTransparent(material, color);
        return material;
    }

    public static void ApplyColor(Renderer renderer, Color color, bool transparent = false)
    {
        if (renderer == null)
            return;

        renderer.sharedMaterial = Create(color, transparent);
    }

    private static Shader GetLitShader()
    {
        if (cachedShader != null)
            return cachedShader;

        cachedShader = Shader.Find("Universal Render Pipeline/Lit");
        if (cachedShader == null)
            cachedShader = Shader.Find("Standard");
        return cachedShader;
    }

    private static void ConfigureTransparent(Material material, Color color)
    {
        var c = color;
        c.a = Mathf.Clamp01(color.a);
        material.color = c;
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }
}
