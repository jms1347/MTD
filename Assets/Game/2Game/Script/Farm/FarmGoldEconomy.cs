using UnityEngine;

/// <summary>
/// 농장 드릴 골드 — 타워 시트 비용·준비 시간(약 60초) 대비 채굴 보상.
/// </summary>
public static class FarmGoldEconomy
{
    public const float DrillDurationSeconds = 5f;
    public const float TileCooldownSeconds = 3f;

    /// <summary>스테이지 준비 구간에서 기본 타워(100G) 1~2기 분량이 나오도록 맞춤.</summary>
    public static long RollDrillReward()
    {
        int stage = DefenseStageTimerManager.Instance != null
            ? Mathf.Max(1, DefenseStageTimerManager.Instance.CurrentStage)
            : 1;

        int min = 15 + stage * 2;
        int max = 24 + stage * 3;
        return Random.Range(min, max + 1);
    }
}
