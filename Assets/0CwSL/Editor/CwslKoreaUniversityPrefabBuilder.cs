#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>고려대 일반 병사 프리팹만 빠르게 생성·갱신.</summary>
public static class CwslKoreaUniversityPrefabBuilder
{
    private const string AssetsPath = "Assets/0CwSL/Data/CwslGameAssets.asset";

    [MenuItem("Tools/CwSL/Build Korea University Soldier Prefab", false, 11)]
    public static void BuildFromMenu()
    {
        var soldier = BuildPrefabsInternal();
        EditorUtility.DisplayDialog(
            "고려대 병사 프리팹",
            "생성 완료:\n• " + CwslPrefabPaths.Monsters.KoreaUniversitySoldier + "\n\n" +
            "자폭병은 Tools → CwSL → Build Suicide Bomber Prefabs 메뉴를 사용하세요.",
            "확인");

        Selection.activeObject = soldier;
        EditorGUIUtility.PingObject(soldier);
    }

    public static GameObject BuildPrefabsBatch() => BuildPrefabsInternal();

    private static GameObject BuildPrefabsInternal()
    {
        CwslPrefabPaths.EnsureFoldersExist();

        var soldier = CwslGameSceneSetup.BuildMonsterPrefabForEditor(
            CwslMonsterType.KoreaUniversitySoldier,
            typeof(CwslMeleeMonster),
            0.6f);

        var assets = AssetDatabase.LoadAssetAtPath<CwslGameAssets>(AssetsPath);
        if (assets != null)
        {
            assets.koreaUniversitySoldierPrefab = soldier;
            EditorUtility.SetDirty(assets);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return soldier;
    }
}
#endif
