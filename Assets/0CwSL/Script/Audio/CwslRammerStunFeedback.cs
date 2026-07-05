using UnityEngine;

public static class CwslRammerStunFeedback
{
    private const float Volume = 1.1f;

    public static void PlaySound(Vector3 worldPosition)
    {
        var clip = CwslRammerAudioFeedback.ResolveStunClip();
        if (clip == null)
            return;

        var soundObject = new GameObject("CwslRammerStunSound");
        soundObject.transform.position = worldPosition + Vector3.up * 0.5f;

        var source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = Volume;
        source.spatialBlend = 1f;
        source.minDistance = 2f;
        source.maxDistance = 24f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.dopplerLevel = 0f;
        source.priority = 16;
        source.Play();

        Object.Destroy(soundObject, clip.length + 0.05f);
    }
}
