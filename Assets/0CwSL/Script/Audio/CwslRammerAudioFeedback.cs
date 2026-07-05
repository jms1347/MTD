using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class CwslRammerAudioFeedback
{
    private static AudioClip horseGallopClip;
    private static AudioClip stunClip;

    public static void Initialize(AudioClip gallop, AudioClip stun)
    {
        if (gallop != null)
            horseGallopClip = gallop;
        if (stun != null)
            stunClip = stun;
    }

    public static AudioClip ResolveHorseGallopClip()
    {
        EnsureHorseGallopClip();
        return horseGallopClip;
    }

    public static AudioClip ResolveStunClip()
    {
        EnsureStunClip();
        return stunClip;
    }

    private static void EnsureHorseGallopClip()
    {
        if (horseGallopClip != null)
            return;

        horseGallopClip = ResolveFromAssets(a => a.horseGallopSound);
#if UNITY_EDITOR
        if (horseGallopClip == null)
            horseGallopClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.HorseGallopSound);
#endif
    }

    private static void EnsureStunClip()
    {
        if (stunClip != null)
            return;

        stunClip = ResolveFromAssets(a => a.rammerStunSound);
#if UNITY_EDITOR
        if (stunClip == null)
            stunClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.RammerStunSound);
#endif
    }

    private static AudioClip ResolveFromAssets(System.Func<CwslGameAssets, AudioClip> selector)
    {
        var assets = CwslGameSession.Instance?.Assets;
        if (assets == null)
        {
            var session = Object.FindFirstObjectByType<CwslGameSession>();
            assets = session?.Assets;
        }

        if (assets == null)
        {
            var allAssets = Resources.FindObjectsOfTypeAll<CwslGameAssets>();
            if (allAssets.Length > 0)
                assets = allAssets[0];
        }

        return assets != null ? selector(assets) : null;
    }
}
