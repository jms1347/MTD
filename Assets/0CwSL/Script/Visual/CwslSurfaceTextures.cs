using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>표면 텍스처 에셋 로드·런타임 폴백.</summary>
public static class CwslSurfaceTextures
{
    private static Texture2D bombPaintAlbedo;
    private static Texture2D bombPaintNormal;
    private static Texture2D scratchedMetalAlbedo;
    private static Texture2D scratchedMetalNormal;

    public static Texture2D BombPaintAlbedo => bombPaintAlbedo ??= LoadOrGenerate(
        CwslSurfaceTexturePaths.BombPaintAlbedo,
        CwslSurfaceTextureGenerator.CreateBombPaintAlbedo);

    public static Texture2D BombPaintNormal => bombPaintNormal ??= LoadOrGenerate(
        CwslSurfaceTexturePaths.BombPaintNormal,
        CwslSurfaceTextureGenerator.CreateBombPaintNormal);

    public static Texture2D ScratchedMetalAlbedo => scratchedMetalAlbedo ??= LoadOrGenerate(
        CwslSurfaceTexturePaths.ScratchedMetalAlbedo,
        CwslSurfaceTextureGenerator.CreateScratchedMetalAlbedo);

    public static Texture2D ScratchedMetalNormal => scratchedMetalNormal ??= LoadOrGenerate(
        CwslSurfaceTexturePaths.ScratchedMetalNormal,
        CwslSurfaceTextureGenerator.CreateScratchedMetalNormal);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void WarmCache()
    {
        _ = BombPaintAlbedo;
        _ = BombPaintNormal;
        _ = ScratchedMetalAlbedo;
        _ = ScratchedMetalNormal;
    }

    public static void ClearCache()
    {
        bombPaintAlbedo = null;
        bombPaintNormal = null;
        scratchedMetalAlbedo = null;
        scratchedMetalNormal = null;
    }

    private static Texture2D LoadOrGenerate(string assetPath, System.Func<Texture2D> generate)
    {
#if UNITY_EDITOR
        var fromAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (fromAsset != null)
            return fromAsset;
#endif

        var runtime = generate();
        runtime.hideFlags = HideFlags.HideAndDontSave;
        return runtime;
    }
}
