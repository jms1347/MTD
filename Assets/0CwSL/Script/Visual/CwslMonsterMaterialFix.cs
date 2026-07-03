using UnityEngine;

public static class CwslMonsterMaterialFix
{
    public static void Refresh(Transform root, CwslMonsterType type)
    {
        var fallback = GetTypeColor(type);
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            var color = fallback;
            if (renderer.sharedMaterial != null && CwslMaterialUtil.IsMaterialValid(renderer.sharedMaterial))
                color = renderer.sharedMaterial.color;

            CwslMaterialUtil.ApplyColor(renderer, color);
        }
    }

    private static Color GetTypeColor(CwslMonsterType type)
    {
        return type switch
        {
            CwslMonsterType.Ranged => new Color(0.55f, 0.25f, 0.95f),
            CwslMonsterType.Suicide => new Color(1f, 0.45f, 0.1f),
            CwslMonsterType.Melee => new Color(0.2f, 0.85f, 0.35f),
            CwslMonsterType.BossHongmyeongbo => new Color(0.9f, 0.12f, 0.1f),
            _ => new Color(0.7f, 0.7f, 0.7f)
        };
    }
}
