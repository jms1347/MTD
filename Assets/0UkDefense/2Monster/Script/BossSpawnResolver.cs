/// <summary>
/// 스테이지 스폰 코드(BG/MB)를 Monster + Boss 스탯으로 해석합니다.
/// </summary>
public static class BossSpawnResolver
{
    public static bool TryResolve(string spawnCode, out MonsterData monsterData, out BossData bossData)
    {
        monsterData = null;
        bossData = null;

        if (string.IsNullOrWhiteSpace(spawnCode))
            return false;

        var dataManager = DataManager.Instance;
        if (dataManager == null)
            return false;

        var code = spawnCode.Trim();
        if (!TryGetBossData(dataManager, code, out bossData))
            return false;

        if (!dataManager.TryGetMonster(bossData.monsterCode, out var baseMonster) || baseMonster == null)
            return false;

        monsterData = baseMonster.WithBossStats(bossData);
        return true;
    }

    private static bool TryGetBossData(DataManager dataManager, string code, out BossData bossData)
    {
        if (code.StartsWith("BG-", System.StringComparison.OrdinalIgnoreCase))
            return dataManager.TryGetBossByCode(code, out bossData);

        if (code.StartsWith("MB-", System.StringComparison.OrdinalIgnoreCase))
            return dataManager.TryGetBossByMonsterCode(code, out bossData);

        bossData = null;
        return false;
    }
}
