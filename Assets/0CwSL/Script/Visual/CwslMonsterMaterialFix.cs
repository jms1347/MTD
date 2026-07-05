using UnityEngine;

public static class CwslMonsterMaterialFix
{
    public static void Refresh(Transform root, CwslMonsterType type)
    {
        if (root == null)
            return;

        var fallback = CwslMonsterVisualPalette.GetPalette(type).Primary;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            if (renderer.sharedMaterial != null && CwslMaterialUtil.IsMaterialValid(renderer.sharedMaterial))
                continue;

            CwslMaterialUtil.ApplyColor(renderer, fallback);
        }
    }
}
