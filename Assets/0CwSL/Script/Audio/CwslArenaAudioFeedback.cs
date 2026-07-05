using UnityEngine;

public static class CwslArenaAudioFeedback
{
    private static AudioClip bossTeleportCastClip;
    private static AudioClip bossTeleportArriveClip;
    private static AudioClip bossPhaseShiftClip;
    private static AudioClip teamBallRollClip;
    private static AudioClip teamBallHitClip;
    private static AudioClip cornerStoneBreakClip;
    private static AudioClip bossWatchStartClip;

    public static void Initialize(CwslGameAssets assets)
    {
        if (assets == null)
            return;

        bossTeleportCastClip = assets.bossTeleportCastSound;
        bossTeleportArriveClip = assets.bossTeleportArriveSound;
        bossPhaseShiftClip = assets.bossPhaseShiftSound;
        teamBallRollClip = assets.teamBallRollSound;
        teamBallHitClip = assets.teamBallHitSound;
        cornerStoneBreakClip = assets.cornerStoneBreakSound;
        bossWatchStartClip = assets.bossWatchStartSound;
    }

    public static AudioClip ResolveTeamBallRoll() => teamBallRollClip;

    public static void PlayBossTeleportCast(Vector3 position) =>
        PlayOneShot(position, bossTeleportCastClip, 1f);

    public static void PlayBossTeleportArrive(Vector3 position) =>
        PlayOneShot(position, bossTeleportArriveClip, 1f);

    public static void PlayBossPhaseShift(Vector3 position) =>
        PlayOneShot(position, bossPhaseShiftClip, 1.05f);

    public static void PlayTeamBallHit(Vector3 position) =>
        PlayOneShot(position, teamBallHitClip, 1.1f);

    public static void PlayCornerStoneBreak(Vector3 position) =>
        PlayOneShot(position, cornerStoneBreakClip, 1f);

    public static void PlayBossWatchStart(Vector3 position) =>
        PlayOneShot(position, bossWatchStartClip, 1f);

    public static AudioSource StartLoop(Transform followTarget, AudioClip clip, float volume = 0.85f)
    {
        if (clip == null || followTarget == null)
            return null;

        var soundObject = new GameObject("CwslLoopSound");
        soundObject.transform.SetParent(followTarget, false);
        soundObject.transform.localPosition = Vector3.up * 0.35f;

        var source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.loop = true;
        source.spatialBlend = 0f;
        source.priority = 32;
        source.Play();
        return source;
    }

    public static void StopLoop(AudioSource source)
    {
        if (source == null)
            return;

        source.Stop();
        Object.Destroy(source.gameObject);
    }

    private static void PlayOneShot(Vector3 worldPosition, AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        var soundObject = new GameObject("CwslArenaSound");
        soundObject.transform.position = worldPosition + Vector3.up * 0.5f;

        var source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0f;
        source.priority = 24;
        source.Play();

        Object.Destroy(soundObject, clip.length + 0.05f);
    }
}
