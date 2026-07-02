using System;

/// <summary>
/// 몬스터 비주얼·충돌 보조 (monsterType 기반).
/// </summary>
public static class MonsterVisualUtility
{
    public static float GetColliderRadius(MonsterData data)
    {
        if (data == null)
            return 0.42f;

        if (!string.IsNullOrEmpty(data.monsterType) && data.monsterType.Contains("날렵", StringComparison.Ordinal))
            return 0.32f;

        if (!string.IsNullOrEmpty(data.monsterType) &&
            (data.monsterType.Contains("뚱", StringComparison.Ordinal)
             || data.IsBoss))
            return 0.55f;

        return 0.42f;
    }
}
