using UnityEngine;
using UnityEngine.Rendering;

public static class CwslMaterialUtil
{
    private static Material sharedFallback;
    private static Shader cachedShader;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void WarmShaderCache()
    {
        _ = ResolveShader();
    }

    public static Material CreateColored(Color color)
    {
        var material = new Material(ResolveShader());
        ApplyColorProperties(material, color);
        ApplyMatteProperties(material);
        return material;
    }

    public static Material CreateMatteColored(Color color)
    {
        var material = CreateColored(color);
        ApplyMatteProperties(material);
        return material;
    }

    public static void ApplyColor(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;

        renderer.material = CreateColored(color);
    }

    public static void ApplyMatteToRenderer(Renderer renderer)
    {
        if (renderer == null)
            return;

        var materials = renderer.materials;
        for (var i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null)
                ApplyMatteProperties(materials[i]);
        }

        renderer.materials = materials;
    }

    public static void ApplyMatteProperties(Material material)
    {
        if (material == null)
            return;

        // Standard
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", 0f);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0f);

        // URP Lit
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0f);
        if (material.HasProperty("_MetallicGlossMap"))
            material.SetFloat("_Metallic", 0f);
    }

    public static bool IsMaterialValid(Material material)
    {
        if (material == null || material.shader == null)
            return false;

        var shaderName = material.shader.name;
        return !shaderName.Contains("InternalError") && shaderName != "Hidden/InternalErrorShader";
    }

    public static Material GetFallbackMaterial()
    {
        if (sharedFallback != null)
            return sharedFallback;

        sharedFallback = CreateMatteColored(new Color(0.75f, 0.75f, 0.75f));
        return sharedFallback;
    }

    private static void ApplyColorProperties(Material material, Color color)
    {
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private static Shader ResolveShader()
    {
        if (cachedShader != null)
            return cachedShader;

        if (GraphicsSettings.currentRenderPipeline != null)
        {
            cachedShader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                ?? Shader.Find("Sprites/Default");
            if (cachedShader != null)
                return cachedShader;
        }

        cachedShader = Shader.Find("Standard")
            ?? Shader.Find("Legacy Shaders/Diffuse")
            ?? Shader.Find("Sprites/Default");

        return cachedShader;
    }
}
