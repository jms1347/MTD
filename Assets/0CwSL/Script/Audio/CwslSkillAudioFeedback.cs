using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class CwslSkillAudioFeedback
{
    private static AudioClip frozenOrbCastClip;
    private static AudioClip frozenOrbTravelClip;
    private static AudioClip lightningOrbCastClip;
    private static AudioClip lightningOrbStrikeClip;
    private static AudioClip lightningOrbImpactClip;

    public static void Initialize(CwslGameAssets assets)
    {
        if (assets == null)
            return;

        frozenOrbCastClip = assets.redMageFrozenOrbCastSound;
        frozenOrbTravelClip = assets.redMageFrozenOrbTravelSound;
        lightningOrbCastClip = assets.redMageLightningOrbCastSound;
        lightningOrbStrikeClip = assets.redMageLightningOrbStrikeSound;
        lightningOrbImpactClip = assets.redMageLightningOrbImpactSound;
    }

    public static void PlayFrozenOrbCast(Vector3 position) =>
        PlaySpatial(position, ResolveFrozenOrbCast(), 0.92f);

    public static void PlayFrozenOrbTravel(Vector3 position) =>
        PlaySpatial(position, ResolveFrozenOrbTravel(), 0.42f);

    public static void PlayLightningOrbCast(Vector3 position) =>
        PlaySpatial(position, ResolveLightningOrbCast(), 0.88f);

    public static void PlayLightningOrbStrike(Vector3 position) =>
        PlaySpatial(position, ResolveLightningOrbStrike(), 0.72f);

    public static void PlayLightningOrbImpact(Vector3 position) =>
        PlaySpatial(position, ResolveLightningOrbImpact(), 0.95f);

    private static AudioClip ResolveFrozenOrbCast() =>
        ResolveClip(ref frozenOrbCastClip, a => a.redMageFrozenOrbCastSound, CwslVfxPaths.RedMageFrozenOrbCastSound);

    private static AudioClip ResolveFrozenOrbTravel() =>
        ResolveClip(ref frozenOrbTravelClip, a => a.redMageFrozenOrbTravelSound, CwslVfxPaths.RedMageFrozenOrbTravelSound);

    private static AudioClip ResolveLightningOrbCast() =>
        ResolveClip(ref lightningOrbCastClip, a => a.redMageLightningOrbCastSound, CwslVfxPaths.RedMageLightningOrbCastSound);

    private static AudioClip ResolveLightningOrbStrike() =>
        ResolveClip(ref lightningOrbStrikeClip, a => a.redMageLightningOrbStrikeSound, CwslVfxPaths.RedMageLightningOrbStrikeSound);

    private static AudioClip ResolveLightningOrbImpact() =>
        ResolveClip(ref lightningOrbImpactClip, a => a.redMageLightningOrbImpactSound, CwslVfxPaths.RedMageLightningOrbImpactSound);

    private static AudioClip ResolveClip(
        ref AudioClip cached,
        System.Func<CwslGameAssets, AudioClip> selector,
        string fallbackPath)
    {
        if (cached != null)
            return cached;

        cached = ResolveFromAssets(selector);
#if UNITY_EDITOR
        if (cached == null && !string.IsNullOrEmpty(fallbackPath))
            cached = AssetDatabase.LoadAssetAtPath<AudioClip>(fallbackPath);
#endif
        return cached;
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

    private static void PlaySpatial(Vector3 worldPosition, AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        var soundObject = new GameObject("CwslSkillSound");
        soundObject.transform.position = worldPosition + Vector3.up * 0.45f;

        var source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0.55f;
        source.minDistance = 2f;
        source.maxDistance = 24f;
        source.priority = 24;
        source.Play();

        Object.Destroy(soundObject, clip.length + 0.05f);
    }
}
