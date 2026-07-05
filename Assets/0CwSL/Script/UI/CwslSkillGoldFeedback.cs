using TMPro;
using UnityEngine;

public static class CwslSkillGoldFeedback
{
    private const float ToastDurationSeconds = 2.4f;

    private static TextMeshProUGUI toastLabel;
    private static float toastHideTime;

    public static void BindToast(TextMeshProUGUI label)
    {
        toastLabel = label;
    }

    public static void ShowInsufficientGold(string message = "골드가 부족해 스킬을 시전할 수 없습니다.")
    {
        PlayFailSound();
        ShowToast(message);
    }

    public static void PlayFailSound()
    {
        CwslGatherAudioFeedback.PlaySkillGoldFail(Vector3.zero);
    }

    public static void Tick()
    {
        if (toastLabel == null || Time.unscaledTime >= toastHideTime)
        {
            if (toastLabel != null)
                toastLabel.gameObject.SetActive(false);
        }
    }

    private static void ShowToast(string message)
    {
        if (toastLabel == null)
        {
            Debug.Log($"[CwSL] {message}");
            return;
        }

        toastLabel.text = message;
        toastLabel.gameObject.SetActive(true);
        toastHideTime = Time.unscaledTime + ToastDurationSeconds;
    }
}
