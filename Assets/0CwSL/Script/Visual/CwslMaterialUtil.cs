using UnityEngine;
using UnityEngine.Rendering;

public static class CwslMaterialUtil
{
    private static Material sharedFallback;
    private static Shader cachedShader;

    private static readonly Vector2 BombPaintTiling = new(2.2f, 2.2f);
    private static readonly Vector2 ScratchedMetalTiling = new(2.8f, 2.8f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void WarmShaderCache()
    {
        _ = ResolveShader();
    }

    public static Material CreateColored(Color color)
        => CreateStyled(color, CwslMaterialStyle.Matte);

    public static Material CreateMatteColored(Color color)
        => CreateStyled(color, CwslMaterialStyle.Matte);

    public static Material CreateStyled(Color color, CwslMaterialStyle style)
    {
        var material = new Material(ResolveShader());
        ApplyColorProperties(material, color);
        ApplyStyle(material, style);
        return material;
    }

    public static void ApplyColor(Renderer renderer, Color color)
        => ApplyStyled(renderer, color, CwslMaterialStyle.Matte);

    public static void ApplyStyled(Renderer renderer, Color color, CwslMaterialStyle style)
    {
        if (renderer == null)
            return;

        renderer.sharedMaterial = CreateStyled(color, style);
    }

    public static void ApplyMatteToRenderer(Renderer renderer)
    {
        if (renderer == null)
            return;

        var materials = renderer.materials;
        for (var i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null)
                ApplyStyle(materials[i], CwslMaterialStyle.Matte);
        }

        renderer.materials = materials;
    }

    public static void ApplyStyle(Material material, CwslMaterialStyle style)
    {
        if (material == null)
            return;

        ClearSurfaceMaps(material);

        switch (style)
        {
            case CwslMaterialStyle.Glossy:
                SetMetallic(material, 0.06f);
                SetSmoothness(material, 0.62f);
                break;

            case CwslMaterialStyle.Rubber:
                SetMetallic(material, 0f);
                SetSmoothness(material, 0.22f);
                break;

            case CwslMaterialStyle.Metal:
                SetMetallic(material, 0.88f);
                SetSmoothness(material, 0.58f);
                ApplyScratchedMetalMaps(material);
                break;

            case CwslMaterialStyle.Glass:
                SetMetallic(material, 0.12f);
                SetSmoothness(material, 0.94f);
                break;

            case CwslMaterialStyle.BombShell:
                SetMetallic(material, 0.1f);
                SetSmoothness(material, 0.78f);
                ApplyBombPaintMaps(material);
                break;

            case CwslMaterialStyle.FuseRope:
                SetMetallic(material, 0f);
                SetSmoothness(material, 0.08f);
                break;

            default:
                ApplyMatteProperties(material);
                break;
        }
    }

    public static void ApplyMatteProperties(Material material)
    {
        SetMetallic(material, 0f);
        SetSmoothness(material, 0f);
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

    private static void ApplyBombPaintMaps(Material material)
    {
        SetAlbedoMap(material, CwslSurfaceTextures.BombPaintAlbedo, BombPaintTiling);
        SetNormalMap(material, CwslSurfaceTextures.BombPaintNormal, BombPaintTiling, 0.85f);
    }

    private static void ApplyScratchedMetalMaps(Material material)
    {
        SetAlbedoMap(material, CwslSurfaceTextures.ScratchedMetalAlbedo, ScratchedMetalTiling);
        SetNormalMap(material, CwslSurfaceTextures.ScratchedMetalNormal, ScratchedMetalTiling, 1.1f);
    }

    private static void ClearSurfaceMaps(Material material)
    {
        SetTexture(material, "_BaseMap", null);
        SetTexture(material, "_MainTex", null);
        SetTexture(material, "_BumpMap", null);

        if (material.HasProperty("_BumpScale"))
            material.SetFloat("_BumpScale", 1f);
    }

    private static void SetAlbedoMap(Material material, Texture2D texture, Vector2 tiling)
    {
        if (texture == null)
            return;

        SetTexture(material, "_BaseMap", texture);
        SetTexture(material, "_MainTex", texture);
        SetTextureScale(material, "_BaseMap", tiling);
        SetTextureScale(material, "_MainTex", tiling);
    }

    private static void SetNormalMap(Material material, Texture2D texture, Vector2 tiling, float strength)
    {
        if (texture == null)
            return;

        SetTexture(material, "_BumpMap", texture);
        SetTextureScale(material, "_BumpMap", tiling);

        if (material.HasProperty("_BumpScale"))
            material.SetFloat("_BumpScale", strength);
    }

    private static void SetTexture(Material material, string property, Texture texture)
    {
        if (material.HasProperty(property))
            material.SetTexture(property, texture);
    }

    private static void SetTextureScale(Material material, string property, Vector2 scale)
    {
        if (material.HasProperty(property))
            material.SetTextureScale(property, scale);
    }

    private static void SetMetallic(Material material, float value)
    {
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", value);
    }

    private static void SetSmoothness(Material material, float value)
    {
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", value);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", value);
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
