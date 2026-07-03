using UnityEngine;
using UnityEngine.Rendering;

public static class CwslMaterialUtil
{
    private static Material sharedFallback;

    public static Material CreateColored(Color color)
    {
        var material = new Material(ResolveShader());
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        return material;
    }

    public static void ApplyColor(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;

        renderer.material = CreateColored(color);
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

        sharedFallback = CreateColored(new Color(0.75f, 0.75f, 0.75f));
        return sharedFallback;
    }

    private static Shader ResolveShader()
    {
        var standard = Shader.Find("Standard");
        if (standard != null)
            return standard;

        var diffuse = Shader.Find("Legacy Shaders/Diffuse");
        if (diffuse != null)
            return diffuse;

        if (GraphicsSettings.currentRenderPipeline != null)
        {
            var urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp != null)
                return urp;
        }

        return Shader.Find("Sprites/Default");
    }
}
