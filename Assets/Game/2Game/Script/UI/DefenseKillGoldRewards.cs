using UnityEngine;

/// <summary>
/// 적 처치 시 1~10 골드를 드롭하고 UI로 흡수 애니메이션을 재생합니다.
/// </summary>
public static class DefenseKillGoldRewards
{
    public const int MinGold = 1;
    public const int MaxGold = 10;

    public static void TryGrant(Vector3 worldPosition)
    {
        if (GameManager.Instance == null)
            return;

        long amount = Random.Range(MinGold, MaxGold + 1);
        DefenseGoldFlyReward.Play(worldPosition, amount);
    }
}
