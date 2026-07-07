#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CwslMonsterVisualMaterialBake
{
    public static void Bake(GameObject root)
    {
        if (root == null)
            return;

        foreach (var colored in root.GetComponentsInChildren<CwslColoredRenderer>(true))
        {
            var renderer = colored.GetComponent<Renderer>();
            if (renderer == null)
                continue;

            renderer.sharedMaterial = CwslMaterialUtil.CreateStyled(colored.StoredColor, colored.StoredStyle);
        }

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!ShouldBakeRenderer(renderer))
                continue;

            if (renderer.sharedMaterial != null && CwslMaterialUtil.IsMaterialValid(renderer.sharedMaterial))
                continue;

            var color = ReadRendererColor(renderer);
            renderer.sharedMaterial = CwslMaterialUtil.CreateColored(color);
        }
    }

    /// <summary>저장된 프리팹에 머티리얼을 서브에셋으로 박아 넣는다 (fileID: 0 방지).</summary>
    public static void EmbedIntoSavedPrefab(string prefabPath)
    {
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null)
            return;

        var contents = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            EmbedMaterials(contents, prefabAsset);
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
    }

    private static void EmbedMaterials(GameObject contents, GameObject prefabAsset)
    {
        foreach (var colored in contents.GetComponentsInChildren<CwslColoredRenderer>(true))
        {
            var renderer = colored.GetComponent<Renderer>();
            if (renderer == null)
                continue;

            renderer.sharedMaterial = CreateEmbeddedMaterial(prefabAsset, colored.StoredColor, colored.StoredStyle);
        }

        foreach (var renderer in contents.GetComponentsInChildren<Renderer>(true))
        {
            if (!ShouldBakeRenderer(renderer))
                continue;

            if (renderer.sharedMaterial != null && CwslMaterialUtil.IsMaterialValid(renderer.sharedMaterial))
                continue;

            renderer.sharedMaterial = CreateEmbeddedMaterial(prefabAsset, ReadRendererColor(renderer));
        }

        EditorUtility.SetDirty(prefabAsset);
    }

    private static Material CreateEmbeddedMaterial(GameObject prefabAsset, Color color, CwslMaterialStyle style = CwslMaterialStyle.Matte)
    {
        var material = CwslMaterialUtil.CreateStyled(color, style);
        material.name = "CwslMat_" + style + "_" + ColorUtility.ToHtmlStringRGB(color);
        AssetDatabase.AddObjectToAsset(material, prefabAsset);
        return material;
    }

    private static bool ShouldBakeRenderer(Renderer renderer)
    {
        if (renderer == null)
            return false;

        if (renderer.GetComponent<CwslColoredRenderer>() != null)
            return false;

        if (renderer is ParticleSystemRenderer or TrailRenderer or LineRenderer)
            return false;

        return true;
    }

    private static Color ReadRendererColor(Renderer renderer)
    {
        if (renderer == null || renderer.sharedMaterial == null)
            return Color.white;

        var material = renderer.sharedMaterial;
        if (material.HasProperty("_BaseColor"))
            return material.GetColor("_BaseColor");
        if (material.HasProperty("_Color"))
            return material.GetColor("_Color");

        return material.color;
    }
}
#endif
