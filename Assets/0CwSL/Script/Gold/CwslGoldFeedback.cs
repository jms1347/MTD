using UnityEngine;

public static class CwslGoldFeedback
{
    private static GameObject goldBurstPrefab;
    private static AudioClip coinClip;
    private const float Volume = 1.25f;

    public static void Initialize(GameObject burstPrefab, AudioClip clip)
    {
        goldBurstPrefab = burstPrefab;
        coinClip = clip;
    }

    public static void PlayPickup(Vector3 worldPosition)
    {
        PlayBurst(worldPosition);
        PlayCoinSound(worldPosition);
    }

    private static void PlayBurst(Vector3 worldPosition)
    {
        if (goldBurstPrefab == null)
            return;

        var instance = CwslVfxSpawner.TryInstantiate(
            goldBurstPrefab,
            worldPosition + Vector3.up * 0.35f,
            Quaternion.identity);
        if (instance != null)
            Object.Destroy(instance, 4f);
    }

    public static void PlayCoinSound(Vector3 worldPosition)
    {
        if (coinClip == null)
            return;

        var soundObject = new GameObject("CwslGoldCoinSound");
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
