/// <summary>
/// 4원소 상태 이상 기획 상수.
/// </summary>
public static class MonsterElementStatusRules
{
    public const int MaxFrostStacks = 5;
    public const float FrostSlowPerStack = 0.10f;
    public const float MaxFrostSlow = 0.50f;

    /// <summary>duration &lt;= 0 이면 사실상 영구 지속.</summary>
    public const float PermanentDurationThreshold = 0.01f;

    public const float PoisonTickInterval = 0.5f;
    public const float DefaultPoisonArmorReductionPerStack = 5f;
    public const float MinBurnDuration = 0.1f;
}
