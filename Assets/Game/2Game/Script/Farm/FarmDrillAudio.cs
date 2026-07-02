using UnityEngine;

/// <summary>
/// 드릴 중 루프 사운드를 재생합니다.
/// </summary>
public static class FarmDrillAudio
{
    private static AudioClip drillClip;
    private static AudioSource activeSource;

    public static void Initialize(AudioClip clip)
    {
        drillClip = clip;
    }

    public static bool HasClip => drillClip != null;

    public static void PlayLoop(Transform followTarget)
    {
        if (drillClip == null || followTarget == null)
            return;

        Stop();

        var soundObject = new GameObject("FarmDrillSound");
        soundObject.transform.SetParent(followTarget, false);
        activeSource = soundObject.AddComponent<AudioSource>();
        activeSource.clip = drillClip;
        activeSource.loop = true;
        activeSource.spatialBlend = 0.55f;
        activeSource.minDistance = 1f;
        activeSource.maxDistance = 18f;
        activeSource.volume = 0.85f;
        activeSource.Play();
    }

    public static void Stop()
    {
        if (activeSource == null)
            return;

        Object.Destroy(activeSource.gameObject);
        activeSource = null;
    }
}
