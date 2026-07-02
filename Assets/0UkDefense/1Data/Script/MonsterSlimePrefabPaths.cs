using System.Collections.Generic;

/// <summary>
/// 몬스터 비주얼 프리팹 키(SLIME-*) → 프로젝트 에셋 경로.
/// </summary>
public static class MonsterSlimePrefabPaths
{
    private static readonly Dictionary<string, string> Paths = new()
    {
        ["SLIME-01"] = "Assets/Kawaii Slimes/Prefabs/Slime_01.prefab",
        ["SLIME-01-KING"] = "Assets/Kawaii Slimes/Prefabs/Slime_01_King.prefab",
        ["SLIME-01-VIKING"] = "Assets/Kawaii Slimes/Prefabs/Slime_01_Viking.prefab",
        ["SLIME-01-METAL"] = "Assets/Kawaii Slimes/Prefabs/Slime_01_MeltalHelmet.prefab",
        ["SLIME-02"] = "Assets/Kawaii Slimes/Prefabs/Slime_02.prefab",
        ["SLIME-03"] = "Assets/Kawaii Slimes/Prefabs/Slime_03.prefab",
        ["SLIME-03-KING"] = "Assets/Kawaii Slimes/Prefabs/Slime_03 King.prefab",
        ["SLIME-03-LEAF"] = "Assets/Kawaii Slimes/Prefabs/Slime_03 Leaf.prefab",
        ["SLIME-03-SPROUT"] = "Assets/Kawaii Slimes/Prefabs/Slime_03 Sprout.prefab",
    };

    public static bool TryGetAssetPath(string prefabKey, out string assetPath)
    {
        assetPath = null;
        if (string.IsNullOrWhiteSpace(prefabKey))
            return false;

        return Paths.TryGetValue(prefabKey.Trim(), out assetPath);
    }
}
