#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CwslSurfaceTextureAssetBuilder
{
    [MenuItem("Tools/CwSL/Generate Surface Textures", false, 11)]
    public static void GenerateFromMenu()
    {
        EnsureGenerated(force: true);
        EditorUtility.DisplayDialog(
            "표면 텍스처",
            "폭탄 도색·낡은 금속 스크래치 텍스처를 생성했습니다.\n" +
            CwslSurfaceTexturePaths.Folder,
            "확인");
    }

    public static void EnsureGenerated(bool force = false)
    {
        if (!AssetDatabase.IsValidFolder("Assets/0CwSL/Data"))
            AssetDatabase.CreateFolder("Assets/0CwSL", "Data");
        if (!AssetDatabase.IsValidFolder(CwslSurfaceTexturePaths.Folder))
            AssetDatabase.CreateFolder("Assets/0CwSL/Data", "Textures");

        SaveTexture(
            CwslSurfaceTexturePaths.BombPaintAlbedo,
            CwslSurfaceTextureGenerator.CreateBombPaintAlbedo(),
            isNormal: false,
            force);

        SaveTexture(
            CwslSurfaceTexturePaths.BombPaintNormal,
            CwslSurfaceTextureGenerator.CreateBombPaintNormal(),
            isNormal: true,
            force);

        SaveTexture(
            CwslSurfaceTexturePaths.ScratchedMetalAlbedo,
            CwslSurfaceTextureGenerator.CreateScratchedMetalAlbedo(),
            isNormal: false,
            force);

        SaveTexture(
            CwslSurfaceTexturePaths.ScratchedMetalNormal,
            CwslSurfaceTextureGenerator.CreateScratchedMetalNormal(),
            isNormal: true,
            force);

        CwslSurfaceTextures.ClearCache();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void SaveTexture(string path, Texture2D source, bool isNormal, bool force)
    {
        if (!force && AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null)
            return;

        var bytes = source.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        Object.DestroyImmediate(source);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !isNormal;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = 256;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }
}
#endif
