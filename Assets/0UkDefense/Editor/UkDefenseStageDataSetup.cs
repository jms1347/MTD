#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class UkDefenseStageDataSetup
{
    public const string StageMetaTsvPath = "Assets/0UkDefense/1Data/SheetExport/StageMeta.tsv";
    public const string StageSpawnTsvPath = "Assets/0UkDefense/1Data/SheetExport/StageSpawn.tsv";
    public const string TowerTsvPath = "Assets/0UkDefense/1Data/SheetExport/Tower.tsv";

    [MenuItem(UkDefenseSetupMenu.DataRoot + "Import Stage TSVs", false, 7)]
    public static void ImportStageTsvsFromSheetExport()
    {
        if (!File.Exists(StageMetaTsvPath) || !File.Exists(StageSpawnTsvPath))
        {
            Debug.LogError("[UkDefense] StageMeta.tsv 또는 StageSpawn.tsv가 없습니다.");
            return;
        }

        var asset = AssetDatabase.LoadAssetAtPath<StageDataSo>(GoogleSheetDefinitions.StageDataAssetPath);
        if (asset == null)
        {
            Debug.LogError("[UkDefense] StageDataSo 없음");
            return;
        }

        var metaTsv = File.ReadAllText(StageMetaTsvPath);
        var spawnTsv = File.ReadAllText(StageSpawnTsvPath);
        asset.ImportFromSheets(metaTsv, spawnTsv);
        asset.RebuildLookup();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Debug.Log($"[UkDefense] Stage TSV import 완료 ({asset.stages.Count} stages)");
    }

    [MenuItem(UkDefenseSetupMenu.DataRoot + "Import Tower TSV", false, 8)]
    public static void ImportTowerTsvFromSheetExport()
    {
        if (!File.Exists(TowerTsvPath))
        {
            Debug.LogError($"[UkDefense] Tower.tsv 없음: {TowerTsvPath}");
            return;
        }

        var asset = AssetDatabase.LoadAssetAtPath<TowerDataSo>(GoogleSheetDefinitions.TowerDataAssetPath);
        if (asset == null)
        {
            Debug.LogError("[UkDefense] TowerDataSo 없음");
            return;
        }

        asset.ImportFromTsv(File.ReadAllText(TowerTsvPath));
        asset.RebuildLookup();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        TowerStatsManager.RefreshFromSheetIfExists();
        Debug.Log($"[UkDefense] Tower.tsv → TowerDataSo 적용 ({asset.list.Count} towers)");
    }
}
#endif
