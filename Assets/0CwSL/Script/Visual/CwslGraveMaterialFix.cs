using UnityEngine;

public static class CwslGraveMaterialFix
{
    public static void Refresh(Transform root)
    {
        if (root == null)
            return;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            if (renderer.sharedMaterial != null && CwslMaterialUtil.IsMaterialValid(renderer.sharedMaterial))
                continue;

            CwslMaterialUtil.ApplyColor(renderer, ResolvePartColor(renderer.transform));
        }
    }

    private static Color ResolvePartColor(Transform part)
    {
        var y = part.localPosition.y;
        if (y < 0.15f)
            return new Color(0.28f, 0.38f, 0.3f);
        if (y < 0.45f)
            return new Color(0.34f, 0.36f, 0.4f);
        if (y < 1.1f)
            return new Color(0.5f, 0.52f, 0.56f);
        return new Color(0.34f, 0.36f, 0.4f);
    }
}
