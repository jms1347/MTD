using UnityEngine;

public static class DefenseManagerLoader
{
    public static void LoadAll(
        GameObject missilePoolManagerPrefab,
        GameObject nexusManagerPrefab,
        GameObject towerStatsManagerPrefab,
        GameObject towerManagerPrefab,
        GameObject stageManagerPrefab)
    {
        MissilePoolManager.Load(missilePoolManagerPrefab);
        NexusManager.Load(nexusManagerPrefab);
        TowerStatsManager.Load(towerStatsManagerPrefab);
        TowerManager.Load(towerManagerPrefab);
        EnsureStageManager(stageManagerPrefab);
    }

    public static void EnsureStageManager(GameObject stageManagerPrefab)
    {
        if (StageManager.Instance != null)
            return;

        StageManager.Load(stageManagerPrefab);
    }

    public static bool AreAllLoaded()
    {
        return MissilePoolManager.Instance != null
            && NexusManager.Instance != null
            && TowerStatsManager.Instance != null
            && TowerManager.Instance != null
            && StageManager.Instance != null;
    }
}
