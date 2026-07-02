using UnityEngine;

/// <summary>
/// 건설 중 망치 사운드를 루프 재생합니다.
/// </summary>
public static class FarmBuildAudio
{
    private static AudioClip hammerClip;
    private static AudioSource activeSource;

    public static void Initialize(AudioClip clip)
    {
        hammerClip = clip;
    }

    public static void PlayLoop(Transform followTarget)
    {
        if (hammerClip == null || followTarget == null)
            return;

        Stop();

        var soundObject = new GameObject("FarmBuildHammerSound");
        soundObject.transform.SetParent(followTarget, false);
        activeSource = soundObject.AddComponent<AudioSource>();
        activeSource.clip = hammerClip;
        activeSource.loop = true;
        activeSource.spatialBlend = 0.55f;
        activeSource.minDistance = 1f;
        activeSource.maxDistance = 18f;
        activeSource.volume = 0.8f;
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
