using UnityEngine;

/// <summary>
/// 골드 획득 시 코인 사운드를 재생합니다.
/// </summary>
public static class FarmGoldAudio
{
    private static AudioClip coinClip;
    private const float Volume = 1.25f;

    public static void Initialize(AudioClip clip)
    {
        coinClip = clip;
    }

    public static void PlayCoin(Vector3 worldPosition)
    {
        if (coinClip == null)
            return;

        var soundObject = new GameObject("FarmGoldCoinSound");
        soundObject.transform.position = worldPosition + Vector3.up * 0.35f;

        var source = soundObject.AddComponent<AudioSource>();
        source.clip = coinClip;
        source.volume = Volume;
        source.spatialBlend = 0f;
        source.priority = 32;
        source.pitch = Random.Range(0.96f, 1.04f);
        source.Play();

        Object.Destroy(soundObject, coinClip.length + 0.05f);
    }
}
