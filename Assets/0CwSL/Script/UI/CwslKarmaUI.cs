using TMPro;
using UnityEngine;

public class CwslKarmaUI : MonoBehaviour
{
    private TextMeshProUGUI karmaLabel;
    private CwslKarmaSystem karmaSystem;
    private CwslTeamGoldCollectedSystem teamGoldSystem;

    public void Bind(CwslKarmaSystem karma, CwslTeamGoldCollectedSystem teamGold, TextMeshProUGUI label)
    {
        if (karmaSystem != null)
            karmaSystem.OnKarmaChanged -= RefreshKarma;

        if (teamGoldSystem != null)
            teamGoldSystem.OnTotalCollectedChanged -= RefreshTeamGold;

        karmaSystem = karma;
        teamGoldSystem = teamGold;
        karmaLabel = label;

        if (karmaSystem != null)
        {
            karmaSystem.OnKarmaChanged += RefreshKarma;
            RefreshKarma(karmaSystem.Karma);
        }

        if (teamGoldSystem != null)
        {
            teamGoldSystem.OnTotalCollectedChanged += RefreshTeamGold;
            RefreshTeamGold(teamGoldSystem.TotalCollected);
        }
    }

    private void OnDestroy()
    {
        if (karmaSystem != null)
            karmaSystem.OnKarmaChanged -= RefreshKarma;
        if (teamGoldSystem != null)
            teamGoldSystem.OnTotalCollectedChanged -= RefreshTeamGold;
    }

    private void RefreshKarma(long karma)
    {
        RefreshLabels(karma, teamGoldSystem != null ? teamGoldSystem.TotalCollected : 0L);
    }

    private void RefreshTeamGold(long totalCollected)
    {
        RefreshLabels(karmaSystem != null ? karmaSystem.Karma : 0L, totalCollected);
    }

    private void RefreshLabels(long karma, long totalCollected)
    {
        if (karmaLabel == null)
            return;

        karmaLabel.text =
            $"업보: {CwslCurrencyDisplay.FormatKarma(karma)} / {CwslCurrencyDisplay.FormatKarma(CwslGameConstants.BossKarmaThreshold)} ({karma:N0}) | " +
            $"팀 골드: {CwslCurrencyDisplay.FormatGold((int)Mathf.Min(totalCollected, int.MaxValue))} ({totalCollected:N0})";
    }
}
