#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// UkDefense 에디터 메뉴 — 데이터 갱신 전용.
/// 프리팹 생성 메뉴는 제거되었습니다 (이미 생성 완료 가정).
/// </summary>
public static class UkDefenseSetupMenu
{
    public const string Root = "UkDefense/";
    public const string DataRoot = Root + "Data/";

    public const string SoDir = "Assets/0UkDefense/1Data/SO";
    public const string MonsterPrefabDir = "Assets/0UkDefense/2Monster/Prefab";

    public const string DataManagerPrefabPath = "Assets/0UkDefense/1Data/Prefab/DataManager.prefab";
    public const string GoogleSheetManagerPrefabPath = "Assets/0UkDefense/1Data/Prefab/GoogleSheetManager.prefab";
    public const string GameManagerPrefabPath = "Assets/0UkDefense/5Manager/Prefab/GameManager.prefab";
    public const string StageManagerPrefabPath = "Assets/0UkDefense/3Stage/Prefab/StageManager.prefab";
    public const string SingletonLoaderPrefabPath = "Assets/0UkDefense/0Core/Prefab/SingletonLoader.prefab";
    public const string MonsterPrefabCatalogPath = SoDir + "/MonsterPrefabCatalog.asset";

    [MenuItem(DataRoot + "Import All Sheets From Google", false, 0)]
    public static void ImportAllFromGoogle()
    {
        GoogleSheetEditorMenu.ImportAllSheetsFromGoogle();
    }

    [MenuItem(DataRoot + "Merge Seed → SO + Export TSV", false, 1)]
    public static void MergeSeedToSoAndExport()
    {
        DefenseTowerContentBootstrap.RefreshFromSeed(exportTsv: true);
    }

    [MenuItem(DataRoot + "Export Merged TSV Only (no SO)", false, 2)]
    public static void ExportMergedTsvOnly()
    {
        DefenseTowerContentBootstrap.RefreshFromSeed(exportTsv: true, applyToAssets: false);
        EditorUtility.DisplayDialog(
            "TSV Export",
            "SheetExport 폴더에 병합 TSV를 저장했습니다.\n" +
            "Assets/0UkDefense/1Data/SheetExport\n\n구글 시트 각 탭 A2에 붙여넣으세요.",
            "확인");
    }

    [MenuItem(DataRoot + "Sync Combat Resource Keys", false, 3)]
    public static void SyncCombatResourceKeys()
    {
        DefenseCombatResourceSync.SyncFromMenu();
    }

    [MenuItem(DataRoot + "Register Monster Model Addressables", false, 7)]
    public static void RegisterMonsterModelAddressables()
    {
        DefenseCombatResourceSync.SyncMonsterModelAddressables(exportTsv: true);
        EditorUtility.DisplayDialog(
            "Monster Addressables",
            "Monster.tsv의 prefabKey(SLIME-*) 모델을 Addressables Prefab 그룹에 등록했습니다.",
            "확인");
    }
}
#endif
