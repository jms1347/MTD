using UnityEngine;

/// <summary>
/// 전투 번개 낙뢰 공용 사운드 (Addressables Effect 키).
/// </summary>
public static class DefenseCombatLightningSfx
{
    private const string DefaultStrikeSoundKey = "etfx_explosion_lightning";
    private const float DefaultVolume = 0.9f;

    public static void PlayStrike(Vector3 worldPosition)
    {
        if (!TryResolveStrikeClip(out var clip, out var volume))
            return;

        var soundObject = new GameObject("LightningStrikeSound");
        soundObject.transform.position = worldPosition;

        var source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.spatialBlend = 0f;
        source.priority = 48;
        source.pitch = Random.Range(0.94f, 1.06f);
        source.Play();

        Object.Destroy(soundObject, clip.length + 0.05f);
    }

    private static bool TryResolveStrikeClip(out AudioClip clip, out float volume)
    {
        clip = null;
        volume = DefaultVolume;

        if (!DefenseAddressableLoader.TryLoadEffect(DefaultStrikeSoundKey, out var prefab) || prefab == null)
            return false;

        var source = prefab.GetComponent<AudioSource>();
        if (source == null || source.clip == null)
            return false;

        clip = source.clip;
        volume = source.volume > 0.01f ? source.volume : DefaultVolume;
        return true;
    }
}
