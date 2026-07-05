using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class CwslGatherAudioFeedback
{
    private static AudioClip gatherCastClip;
    private static AudioClip gatherChargeLoopClip;
    private static AudioClip gatherChargeEndClip;
    private static AudioClip skillGoldFailClip;
    private static AudioSource chargeLoopSource;

    public static void Initialize(AudioClip castClip, AudioClip loopClip, AudioClip endClip, AudioClip failClip)
    {
        if (castClip != null)
            gatherCastClip = castClip;
        if (loopClip != null)
            gatherChargeLoopClip = loopClip;
        if (endClip != null)
            gatherChargeEndClip = endClip;
        if (failClip != null)
            skillGoldFailClip = failClip;
    }

    public static void PlayGatherCast(Vector3 position) =>
        PlayOneShot(position, ResolveGatherCastClip(), 0.95f);

    public static void StartChargeLoop(Vector3 center)
    {
        StopChargeLoop();

        var clip = ResolveGatherChargeLoopClip();
        if (clip == null)
            return;

        var soundObject = new GameObject("CwslGatherChargeLoop");
        soundObject.transform.position = center + Vector3.up * 0.35f;

        chargeLoopSource = soundObject.AddComponent<AudioSource>();
        chargeLoopSource.clip = clip;
        chargeLoopSource.volume = 0.72f;
        chargeLoopSource.loop = true;
        chargeLoopSource.spatialBlend = 0.45f;
        chargeLoopSource.minDistance = 2f;
        chargeLoopSource.maxDistance = 28f;
        chargeLoopSource.priority = 28;
        chargeLoopSource.Play();
    }

    public static void UpdateChargeLoopPosition(Vector3 center)
    {
        if (chargeLoopSource == null)
            return;

        chargeLoopSource.transform.position = center + Vector3.up * 0.35f;
    }

    public static void StopChargeLoop()
    {
        if (chargeLoopSource == null)
            return;

        chargeLoopSource.Stop();
        Object.Destroy(chargeLoopSource.gameObject);
        chargeLoopSource = null;
    }

    public static void PlayChargeEnd(Vector3 center)
    {
        StopChargeLoop();
        PlaySpatialOneShot(center, ResolveGatherChargeEndClip(), 0.88f);
    }

    public static void PlaySkillGoldFail(Vector3 position) =>
        PlayOneShot(position, ResolveSkillGoldFailClip(), 1f);

    private static AudioClip ResolveGatherCastClip()
    {
        if (gatherCastClip != null)
            return gatherCastClip;

        gatherCastClip = ResolveFromAssets(a => a.gatherChargeCastSound);
#if UNITY_EDITOR
        if (gatherCastClip == null)
            gatherCastClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.GatherChargeCastSound);
#endif
        return gatherCastClip;
    }

    private static AudioClip ResolveGatherChargeLoopClip()
    {
        if (gatherChargeLoopClip != null)
            return gatherChargeLoopClip;

        gatherChargeLoopClip = ResolveFromAssets(a => a.gatherChargeLoopSound);
#if UNITY_EDITOR
        if (gatherChargeLoopClip == null)
            gatherChargeLoopClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.GatherChargeLoopSound);
#endif
        return gatherChargeLoopClip;
    }

    private static AudioClip ResolveGatherChargeEndClip()
    {
        if (gatherChargeEndClip != null)
            return gatherChargeEndClip;

        gatherChargeEndClip = ResolveFromAssets(a => a.gatherChargeEndSound);
#if UNITY_EDITOR
        if (gatherChargeEndClip == null)
            gatherChargeEndClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.GatherChargeEndSound);
#endif
        return gatherChargeEndClip;
    }

    private static AudioClip ResolveSkillGoldFailClip()
    {
        if (skillGoldFailClip != null)
            return skillGoldFailClip;

        skillGoldFailClip = ResolveFromAssets(a => a.skillGoldFailSound);
#if UNITY_EDITOR
        if (skillGoldFailClip == null)
            skillGoldFailClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CwslVfxPaths.SkillGoldFailSound);
#endif
        return skillGoldFailClip;
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

    private static void PlayOneShot(Vector3 worldPosition, AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        var soundObject = new GameObject("CwslGatherSound");
        soundObject.transform.position = worldPosition + Vector3.up * 0.5f;

        var source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0f;
        source.priority = 24;
        source.Play();

        Object.Destroy(soundObject, clip.length + 0.05f);
    }

    private static void PlaySpatialOneShot(Vector3 worldPosition, AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        var soundObject = new GameObject("CwslGatherSpatialSound");
        soundObject.transform.position = worldPosition + Vector3.up * 0.35f;

        var source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0.45f;
        source.minDistance = 2f;
        source.maxDistance = 28f;
        source.priority = 24;
        source.Play();

        Object.Destroy(soundObject, clip.length + 0.05f);
    }
}
