using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class CwslRammerAudioFeedback
{
    private static AudioClip horseGallopClip;
    private static AudioClip stunClip;
    private static AudioClip brakeNeighClip;

    public static void Initialize(AudioClip gallop, AudioClip stun, AudioClip brakeNeigh = null)
    {
        if (gallop != null)
            horseGallopClip = gallop;
        if (stun != null)
            stunClip = stun;
        if (brakeNeigh != null)
            brakeNeighClip = brakeNeigh;
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

    public static void PlayBrakeNeigh(Vector3 worldPosition)
    {
        var clip = ResolveBrakeNeighClip();
        if (clip == null)
            return;

        var soundObject = new GameObject("CwslRammerBrakeNeigh");
        soundObject.transform.position = worldPosition + Vector3.up * 0.6f;
        var source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = 0.95f;
        source.spatialBlend = 0.65f;
        source.minDistance = 2f;
        source.maxDistance = 24f;
        source.priority = 24;
        source.Play();
        Object.Destroy(soundObject, clip.length + 0.05f);
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

    private static AudioClip ResolveBrakeNeighClip()
    {
        if (brakeNeighClip != null)
            return brakeNeighClip;

        brakeNeighClip = ResolveFromAssets(a => a.rammerBrakeNeighSound);
#if UNITY_EDITOR
        if (brakeNeighClip == null)
            brakeNeighClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.RammerBrakeNeighSound);
#endif
        return brakeNeighClip;
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
