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
        EnsureInitialized();
        return horseGallopClip;
    }

    public static AudioClip ResolveStunClip()
    {
        EnsureInitialized();
        return stunClip;
    }

    private static void EnsureInitialized()
    {
        if (horseGallopClip != null && stunClip != null)
            return;

        var assets = CwslGameSession.Instance?.Assets;
        if (assets == null)
        {
            var session = Object.FindFirstObjectByType<CwslGameSession>();
            assets = session?.Assets;
        }

        if (assets != null)
            Initialize(assets.horseGallopSound, assets.rammerStunSound);

#if UNITY_EDITOR
        if (horseGallopClip == null)
            horseGallopClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.HorseGallopSound);
        if (stunClip == null)
            stunClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.RammerStunSound);
#endif
    }
}
