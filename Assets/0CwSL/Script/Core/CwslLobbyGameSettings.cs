using UnityEngine;

/// <summary>
/// 로비에서 선택한 게임 옵션. 게임 씬 진입 시 적용됩니다.
/// </summary>
public static class CwslLobbyGameSettings
{
    public const string PrefEnableDevCheats = "Cwsl_EnableDevCheats";
    public const string PrefShowTrapGuideText = "Cwsl_ShowTrapGuideText";

    public static bool EnableDevCheats { get; private set; }
    public static bool ShowTrapGuideText { get; private set; } = true;

    private static bool appliedFromLobbyBroadcast;

    public static void LoadFromPlayerPrefs()
    {
        EnableDevCheats = PlayerPrefs.GetInt(PrefEnableDevCheats, 0) == 1;
        ShowTrapGuideText = PlayerPrefs.GetInt(PrefShowTrapGuideText, 1) == 1;
    }

    public static void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetInt(PrefEnableDevCheats, EnableDevCheats ? 1 : 0);
        PlayerPrefs.SetInt(PrefShowTrapGuideText, ShowTrapGuideText ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SetFromLobbyUi(bool enableDevCheats, bool showTrapGuideText)
    {
        EnableDevCheats = enableDevCheats;
        ShowTrapGuideText = showTrapGuideText;
        SaveToPlayerPrefs();
    }

    public static void ApplyFromLobbyBroadcast(bool enableDevCheats, bool showTrapGuideText)
    {
        EnableDevCheats = enableDevCheats;
        ShowTrapGuideText = showTrapGuideText;
        appliedFromLobbyBroadcast = true;
    }

    public static void EnsureLoaded()
    {
        if (appliedFromLobbyBroadcast)
            return;

        LoadFromPlayerPrefs();
    }
}
