#if UNITY_EDITOR
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

/// <summary>슬라임 근접 몬스터 프리팹(일반·넥서스 우선) 생성·갱신.</summary>
public static class CwslSlimeMeleePrefabBuilder
{
    private const string AssetsPath = "Assets/0CwSL/Data/CwslGameAssets.asset";

    [MenuItem("Tools/CwSL/Build Slime Melee Prefabs", false, 13)]
    public static void BuildFromMenu()
    {
        var melee = BuildPrefabsInternal();
        EditorUtility.DisplayDialog(
            "슬라임 근접 프리팹",
            "생성 완료:\n• " + CwslPrefabPaths.Monsters.Melee + " (Slime_01)\n" +
            "• " + CwslPrefabPaths.Monsters.NexusMelee + " (Slime_01_Viking x3)",
            "확인");

        Selection.activeObject = melee;
        EditorGUIUtility.PingObject(melee);
    }

    public static GameObject BuildPrefabsBatch() => BuildPrefabsInternal();

    private static GameObject BuildPrefabsInternal()
    {
        CwslPrefabPaths.EnsureFoldersExist();
        CwslSurfaceTextureAssetBuilder.EnsureGenerated();

        var melee = CwslGameSceneSetup.BuildMonsterPrefabForEditor(
            CwslMonsterType.Melee,
            typeof(CwslMeleeMonster),
            0.6f);
        var nexusMelee = CwslGameSceneSetup.BuildMonsterPrefabForEditor(
            CwslMonsterType.NexusMelee,
            typeof(CwslMeleeMonster),
            1.45f);

        var assets = AssetDatabase.LoadAssetAtPath<CwslGameAssets>(AssetsPath);
        if (assets != null)
        {
            assets.meleeMonsterPrefab = melee;
            assets.nexusMeleeMonsterPrefab = nexusMelee;
            assets.slimeMeleeModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CwslSlimeAssetPaths.Slime01);
            assets.slimeNexusMeleeModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CwslSlimeAssetPaths.SlimeViking);
            assets.slimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CwslSlimeAssetPaths.AnimatorController);
            EditorUtility.SetDirty(assets);
        }

        var networkPrefabs = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(
            "Assets/0CwSL/Data/CwslNetworkPrefabs.asset");
        if (networkPrefabs != null)
        {
            CwslGameSceneSetup.RegisterNetworkPrefab(networkPrefabs, melee);
            CwslGameSceneSetup.RegisterNetworkPrefab(networkPrefabs, nexusMelee);
            EditorUtility.SetDirty(networkPrefabs);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return melee;
    }
}
#endif
