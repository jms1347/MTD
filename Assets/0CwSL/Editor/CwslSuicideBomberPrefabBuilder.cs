#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>자폭병 프리팹(돌격형·장착형) 생성·갱신.</summary>
public static class CwslSuicideBomberPrefabBuilder
{
    private const string AssetsPath = "Assets/0CwSL/Data/CwslGameAssets.asset";
    private const string LegacyStickyPrefabPath = "Assets/0CwSL/Prefabs/CwslMonster_KoreaUniversityStriker.prefab";

    [MenuItem("Tools/CwSL/Build Suicide Bomber Prefabs", false, 12)]
    public static void BuildFromMenu()
    {
        var rush = BuildPrefabsInternal();
        EditorUtility.DisplayDialog(
            "자폭병 프리팹",
            "생성 완료:\n• " + CwslPrefabPaths.Monsters.SuicideRush + " (돌격형)\n" +
            "• " + CwslPrefabPaths.Monsters.SuicideSticky + " (장착형)\n\n" +
            "Project 창에서 프리팹을 더블클릭해 미리보기할 수 있습니다.",
            "확인");

        Selection.activeObject = rush;
        EditorGUIUtility.PingObject(rush);
    }

    public static GameObject BuildPrefabsBatch() => BuildPrefabsInternal();

    private static GameObject BuildPrefabsInternal()
    {
        CwslPrefabPaths.EnsureFoldersExist();
        CwslSurfaceTextureAssetBuilder.EnsureGenerated();
        RemoveLegacyStickyPrefab();

        var rush = CwslGameSceneSetup.BuildMonsterPrefabForEditor(
            CwslMonsterType.Suicide,
            typeof(CwslSuicideMonster),
            0.5f);
        var sticky = CwslGameSceneSetup.BuildMonsterPrefabForEditor(
            CwslMonsterType.StickySuicide,
            typeof(CwslStickySuicideMonster),
            0.45f);

        var assets = AssetDatabase.LoadAssetAtPath<CwslGameAssets>(AssetsPath);
        if (assets != null)
        {
            assets.suicideMonsterPrefab = rush;
            assets.stickySuicideMonsterPrefab = sticky;
            if (assets.bombFuseVfx == null)
                assets.bombFuseVfx = AssetDatabase.LoadAssetAtPath<GameObject>(CwslVfxPaths.BombFuse);
            EditorUtility.SetDirty(assets);
        }

        var networkPrefabs = AssetDatabase.LoadAssetAtPath<Unity.Netcode.NetworkPrefabsList>(
            "Assets/0CwSL/Data/CwslNetworkPrefabs.asset");
        if (networkPrefabs != null)
        {
            CwslGameSceneSetup.RegisterNetworkPrefab(networkPrefabs, rush);
            CwslGameSceneSetup.RegisterNetworkPrefab(networkPrefabs, sticky);
            EditorUtility.SetDirty(networkPrefabs);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return rush;
    }

    private static void RemoveLegacyStickyPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(LegacyStickyPrefabPath) == null)
            return;

        AssetDatabase.DeleteAsset(LegacyStickyPrefabPath);
    }
}
#endif
