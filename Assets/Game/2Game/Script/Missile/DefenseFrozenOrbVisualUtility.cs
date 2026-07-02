using UnityEngine;

/// <summary>
/// 디아 오브 본체·파편 공통 얼음 구체 머티리얼.
/// </summary>
public static class DefenseFrozenOrbVisualUtility
{
    private static Material iceOrbMaterial;

    public static Material GetIceOrbMaterial()
    {
        if (iceOrbMaterial != null)
            return iceOrbMaterial;

        var shader = Shader.Find("Standard");
        if (shader == null)
            return null;

        iceOrbMaterial = new Material(shader)
        {
            color = new Color(0.55f, 0.88f, 1f, 0.92f)
        };
        iceOrbMaterial.EnableKeyword("_EMISSION");
        iceOrbMaterial.SetColor("_EmissionColor", new Color(0.25f, 0.65f, 1f) * 1.35f);
        return iceOrbMaterial;
    }

    public static void ApplyIceOrbMaterial(Renderer renderer)
    {
        if (renderer == null)
            return;

        var material = GetIceOrbMaterial();
        if (material != null)
            renderer.sharedMaterial = material;
    }

    public static void EnsureOrbCoreVisual(Transform orbVisual)
    {
        if (orbVisual == null)
            return;

        var core = orbVisual.Find("OrbCore");
        if (core == null)
            return;

        ApplyIceOrbMaterial(core.GetComponent<Renderer>());
    }
}
