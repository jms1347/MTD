using UnityEngine;

public static class UkDefenseStageLoader
{
    public static void LoadStageManager(GameObject stageManagerPrefab)
    {
        StageManager.Load(stageManagerPrefab);
    }

    public static bool IsStageManagerLoaded => StageManager.Instance != null;
}
