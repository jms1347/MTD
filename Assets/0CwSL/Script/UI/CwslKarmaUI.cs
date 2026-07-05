using TMPro;
using UnityEngine;

public class CwslKarmaUI : MonoBehaviour
{
    private TextMeshProUGUI karmaLabel;
    private CwslKarmaSystem karmaSystem;

    public void Bind(CwslKarmaSystem system, TextMeshProUGUI label)
    {
        karmaSystem = system;
        karmaLabel = label;
        if (karmaSystem != null)
        {
            karmaSystem.OnKarmaChanged -= Refresh;
            karmaSystem.OnKarmaChanged += Refresh;
            Refresh(karmaSystem.Karma);
        }
    }

    private void OnDestroy()
    {
        if (karmaSystem != null)
            karmaSystem.OnKarmaChanged -= Refresh;
    }

    private void Refresh(long karma)
    {
        if (karmaLabel == null)
            return;

        karmaLabel.text =
            $"업보: {CwslCurrencyDisplay.FormatKarma(karma)} / {CwslCurrencyDisplay.FormatKarma(CwslGameConstants.BossKarmaThreshold)} ({karma:N0})";
    }
}
