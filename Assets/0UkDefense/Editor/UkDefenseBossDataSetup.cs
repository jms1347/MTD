#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class UkDefenseBossDataSetup
{
    public const string BossTsvPath = "Assets/0UkDefense/1Data/SheetExport/Boss.tsv";
    public const string BossElementGroupTsvPath = "Assets/0UkDefense/1Data/SheetExport/BossElementGroup.tsv";

    public const string BossDataAssetPath = GoogleSheetDefinitions.SoDirectory + "/BossDataSo.asset";
    public const string BossElementGroupDataAssetPath = GoogleSheetDefinitions.SoDirectory + "/BossElementGroupDataSo.asset";

    [MenuItem(UkDefenseSetupMenu.DataRoot + "Import Boss TSVs", false, 6)]
    public static void ImportAllBossTsvsFromSheetExport()
    {
        EnsureBossDataAssets();
        ImportBossElementGroupTsv();
        ImportBossTsv();
        WireBossDataToDataManager();
        AssetDatabase.SaveAssets();
        Debug.Log("[UkDefense] Boss TSV import 완료");
    }

    public static void EnsureBossDataAssets()
    {
        EnsureAsset<BossDataSo>(BossDataAssetPath);
        EnsureAsset<BossElementGroupDataSo>(BossElementGroupDataAssetPath);
    }

    public static void ImportBossTsv()
    {
        var asset = AssetDatabase.LoadAssetAtPath<BossDataSo>(BossDataAssetPath);
        if (asset == null || !File.Exists(BossTsvPath))
            return;

        asset.ImportFromTsv(File.ReadAllText(BossTsvPath));
        asset.RebuildLookup();
        EditorUtility.SetDirty(asset);
    }

    public static void ImportBossElementGroupTsv()
    {
        var asset = AssetDatabase.LoadAssetAtPath<BossElementGroupDataSo>(BossElementGroupDataAssetPath);
        if (asset == null || !File.Exists(BossElementGroupTsvPath))
            return;

        asset.ImportFromTsv(File.ReadAllText(BossElementGroupTsvPath));
        asset.RebuildLookup();
        EditorUtility.SetDirty(asset);
    }

    public static void WireBossDataToDataManager()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UkDefenseSetupMenu.DataManagerPrefabPath);
        if (prefab == null)
            return;

        var dataManager = prefab.GetComponent<DataManager>();
        if (dataManager == null)
            return;

        var so = new SerializedObject(dataManager);
        so.FindProperty("bossDataSo").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<BossDataSo>(BossDataAssetPath);
        so.FindProperty("bossElementGroupDataSo").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<BossElementGroupDataSo>(BossElementGroupDataAssetPath);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(prefab);
    }

    private static T EnsureAsset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        EnsureFolder(GoogleSheetDefinitions.SoDirectory);
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parts = path.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
