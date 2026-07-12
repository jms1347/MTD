using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class StllHorseGallopAudioUtil
{
    private static AudioClip cachedClip;

    public static AudioClip ResolveClip(StllGameAssets assets = null)
    {
        if (cachedClip != null)
            return cachedClip;

        if (assets != null && assets.horseGallopSound != null)
        {
            cachedClip = assets.horseGallopSound;
            return cachedClip;
        }

        assets ??= StllGameSession.Instance?.Assets;
        if (assets != null && assets.horseGallopSound != null)
        {
            cachedClip = assets.horseGallopSound;
            return cachedClip;
        }

#if UNITY_EDITOR
        cachedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(StllGameConstants.HorseGallopClipPath);
#endif
        return cachedClip;
    }
}
