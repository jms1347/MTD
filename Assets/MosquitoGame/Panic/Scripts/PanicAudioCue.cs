using UnityEngine;

public static class PanicAudioCue
{
    public static void PlayHeartbeat(float intensity)
    {
        // 프로토타입: AudioClip 없이 Debug 로그. SETUP_GUIDE에서 클립 연결 안내.
        if (intensity > 0.85f)
            Debug.Log("[PanicAudio] 두근두근!");
    }

    public static void PlayDecoyAlarm()
    {
        Debug.Log("[PanicAudio] 가짜 미끼 알람!");
    }

    public static void PlayGunShot()
    {
        Debug.Log("[PanicAudio] 에프킬라 발사");
    }
}
