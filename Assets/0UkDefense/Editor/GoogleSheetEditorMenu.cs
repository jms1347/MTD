#if UNITY_EDITOR
using System;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 에디터에서 구글 시트 TSV를 ScriptableObject에 일괄 반영합니다.
/// </summary>
public static class GoogleSheetEditorMenu
{
    private const string MenuRoot = "GoogleSheet/";

    [MenuItem(MenuRoot + "Import All Sheets From Google", false, 0)]
    public static void ImportAllSheetsFromGoogle()
    {
        if (!EditorUtility.DisplayDialog(
                "Google Sheet Import",
                "연동된 구글 시트 9종(몬스터·타워·스킬·이펙트·이펙트그룹·스테이지·스폰·어드레서블키·로그라이크카드)을 " +
                "모두 다운로드해 SO asset에 저장합니다.\n\n계속할까요?",
                "가져오기",
                "취소"))
        {
            return;
        }

        try
        {
            ImportAllSheetsFromGoogleSilent();
            EditorUtility.DisplayDialog("Google Sheet Import", "모든 시트 import가 완료되었습니다.", "확인");
        }
        catch (Exception ex)
        {
            Debug.LogError("[GoogleSheet] 전체 import 실패: " + ex.Message);
            EditorUtility.DisplayDialog("Google Sheet Import", "import 중 오류가 발생했습니다.\nConsole 로그를 확인하세요.", "확인");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    public static void ImportAllSheetsFromGoogleSilent()
    {
        Debug.Log("[GoogleSheet] 구글 시트 전체 import 시작...");

        using var client = new WebClient { Encoding = Encoding.UTF8 };

        ImportMonsterSheet(client);
        ImportTowerSheet(client);
        ImportSkillSheet(client);
        ImportEffectSheet(client);
        ImportEffectGroupSheet(client);
        ImportAddressableKeySheet(client);
        ImportStageSheets(client);
        ImportRoguelikeCardSheet(client);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        TowerStatsManager.RefreshFromSheetIfExists();
        DefenseCombatResourceSync.SyncFromCombatData(exportTsv: true);

        AssetDatabase.SaveAssets();

        Debug.Log("[GoogleSheet] 모든 시트 import 완료");
    }

    private static void ImportMonsterSheet(WebClient client)
    {
        ReportProgress("몬스터 DB", 0.1f);
        var tsv = Download(client, GoogleSheetDefinitions.MonsterDataUrl, "몬스터 DB");
        var parsed = GoogleSheetManager.ParseMonsterData(tsv);
        if (!TryApplyParsedRows("몬스터 DB", tsv, parsed.Count))
            return;

        var asset = LoadAsset<MonsterDataSo>(GoogleSheetDefinitions.MonsterDataAssetPath, "몬스터 DB");
        asset.SetData(parsed);
        asset.RebuildLookup();
        MarkDirty(asset, $"몬스터 DB ({asset.list.Count} rows)");
    }

    private static void ImportTowerSheet(WebClient client)
    {
        ReportProgress("타워 DB", 0.25f);
        var tsv = Download(client, GoogleSheetDefinitions.TowerDataUrl, "타워 DB");
        var parsed = TowerDataSo.ParseTsv(tsv);
        if (!TryApplyParsedRows("타워 DB", tsv, parsed.Count))
            return;

        var asset = LoadAsset<TowerDataSo>(GoogleSheetDefinitions.TowerDataAssetPath, "타워 DB");
        asset.SetData(parsed);
        asset.RebuildLookup();
        MarkDirty(asset, $"타워 DB ({asset.list.Count} rows)");
    }

    private static void ImportSkillSheet(WebClient client)
    {
        ReportProgress("스킬 DB", 0.4f);
        var tsv = Download(client, GoogleSheetDefinitions.SkillDataUrl, "스킬 DB");
        var parsed = DefenseSkillDataSo.ParseTsv(tsv);
        if (!TryApplyParsedRows("스킬 DB", tsv, parsed.Count))
            return;

        var asset = LoadAsset<DefenseSkillDataSo>(GoogleSheetDefinitions.SkillDataAssetPath, "스킬 DB");
        asset.SetData(parsed);
        asset.RebuildLookup();
        MarkDirty(asset, $"스킬 DB ({asset.list.Count} rows)");
    }

    private static void ImportEffectSheet(WebClient client)
    {
        ReportProgress("이펙트 DB", 0.55f);
        var tsv = Download(client, GoogleSheetDefinitions.EffectDataUrl, "이펙트 DB");
        var parsed = DefenseEffectDataSo.ParseTsv(tsv);
        if (!TryApplyParsedRows("이펙트 DB", tsv, parsed.Count))
            return;

        var asset = LoadAsset<DefenseEffectDataSo>(GoogleSheetDefinitions.EffectDataAssetPath, "이펙트 DB");
        asset.SetData(parsed);
        asset.RebuildLookup();
        MarkDirty(asset, $"이펙트 DB ({asset.list.Count} rows)");
    }

    private static void ImportEffectGroupSheet(WebClient client)
    {
        ReportProgress("이펙트 그룹 DB", 0.7f);
        var tsv = Download(client, GoogleSheetDefinitions.EffectGroupDataUrl, "이펙트 그룹 DB");
        var parsed = DefenseEffectGroupDataSo.ParseTsv(tsv);
        if (!TryApplyParsedRows("이펙트 그룹 DB", tsv, parsed.Count))
            return;

        var asset = LoadAsset<DefenseEffectGroupDataSo>(GoogleSheetDefinitions.EffectGroupDataAssetPath, "이펙트 그룹 DB");
        asset.SetData(parsed);
        asset.RebuildLookup();
        MarkDirty(asset, $"이펙트 그룹 DB ({asset.list.Count} rows)");
    }

    private static void ImportAddressableKeySheet(WebClient client)
    {
        ReportProgress("어드레서블 키 DB", 0.78f);
        var tsv = Download(client, GoogleSheetDefinitions.AddressableKeyDataUrl, "어드레서블 키 DB");
        var parsed = DefenseAddressableKeyDataSo.ParseTsv(tsv);
        if (!TryApplyParsedRows("어드레서블 키 DB", tsv, parsed.Count))
            return;

        var asset = LoadAsset<DefenseAddressableKeyDataSo>(GoogleSheetDefinitions.AddressableKeyDataAssetPath, "어드레서블 키 DB");
        asset.SetData(parsed);
        asset.RebuildLookup();
        MarkDirty(asset, $"어드레서블 키 DB ({asset.list.Count} rows)");
    }

    private static void ImportStageSheets(WebClient client)
    {
        ReportProgress("스테이지 DB", 0.85f);
        var metaTsv = Download(client, GoogleSheetDefinitions.StageMetaDataUrl, "스테이지 메타 DB");
        var spawnTsv = Download(client, GoogleSheetDefinitions.StageSpawnDataUrl, "스테이지 스폰 DB");
        var parsed = StageDataSo.ParseSheets(metaTsv ?? string.Empty, spawnTsv ?? string.Empty);
        var sourceLines = CountNonEmptyLines(metaTsv) + CountNonEmptyLines(spawnTsv);
        if (sourceLines <= 0)
        {
            Debug.LogWarning("[GoogleSheet] 스테이지 DB TSV가 비어 있습니다. 기존 SO를 유지합니다.");
            return;
        }

        if (parsed.Count <= 0)
        {
            Debug.LogError(
                $"[GoogleSheet] 스테이지 DB 파싱 결과 0건 (시트 {sourceLines}행). 기존 SO를 유지합니다.");
            return;
        }

        var asset = LoadAsset<StageDataSo>(GoogleSheetDefinitions.StageDataAssetPath, "스테이지 DB");
        asset.SetData(parsed);
        asset.RebuildLookup();
        MarkDirty(asset, $"스테이지 DB ({asset.stages.Count} stages)");
    }

    [MenuItem(MenuRoot + "Import Roguelike Card From Google", false, 12)]
    public static void ImportRoguelikeCardFromGoogle()
    {
        try
        {
            using var client = new WebClient { Encoding = Encoding.UTF8 };
            ImportRoguelikeCardSheet(client);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Google Sheet Import", "로그라이크 카드 시트 import가 완료되었습니다.", "확인");
        }
        catch (Exception ex)
        {
            Debug.LogError("[GoogleSheet] 로그라이크 카드 import 실패: " + ex.Message);
            EditorUtility.DisplayDialog("Google Sheet Import", "import 중 오류가 발생했습니다.\nConsole 로그를 확인하세요.", "확인");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static void ImportRoguelikeCardSheet(WebClient client)
    {
        ReportProgress("로그라이크 카드 DB", 0.92f);
        var tsv = Download(client, GoogleSheetDefinitions.RoguelikeCardDataUrl, "로그라이크 카드 DB");
        var parsed = RoguelikeCardDataSo.ParseTsv(tsv);
        if (!TryApplyParsedRows("로그라이크 카드 DB", tsv, parsed.Count))
            return;

        var asset = LoadOrCreateRoguelikeCardAsset();
        asset.SetData(parsed);
        asset.RebuildLookup();
        MarkDirty(asset, $"로그라이크 카드 DB ({asset.list.Count} rows)");

        DefenseRoguelikeSetupMenu.WriteSheetExportTsv(tsv);
        DefenseRoguelikeSetupMenu.WireDataManagerAndVisuals(asset);
    }

    private static RoguelikeCardDataSo LoadOrCreateRoguelikeCardAsset()
    {
        var asset = AssetDatabase.LoadAssetAtPath<RoguelikeCardDataSo>(GoogleSheetDefinitions.RoguelikeCardDataAssetPath);
        if (asset != null)
            return asset;

        DefenseSheetTsvUtility.EnsureFolder(GoogleSheetDefinitions.SoDirectory);
        asset = ScriptableObject.CreateInstance<RoguelikeCardDataSo>();
        AssetDatabase.CreateAsset(asset, GoogleSheetDefinitions.RoguelikeCardDataAssetPath);
        return asset;
    }

    private static bool TryApplyParsedRows(string label, string tsv, int parsedCount)
    {
        if (string.IsNullOrWhiteSpace(tsv))
        {
            Debug.LogWarning($"[GoogleSheet] {label} TSV가 비어 있습니다. 기존 SO를 유지합니다.");
            return false;
        }

        var sourceLines = CountNonEmptyLines(tsv);
        if (parsedCount <= 0)
        {
            Debug.LogError(
                $"[GoogleSheet] {label} 파싱 결과 0건 (시트 {sourceLines}행). " +
                "컬럼 형식이 맞지 않아 기존 SO를 유지합니다.");
            return false;
        }

        if (parsedCount < sourceLines)
        {
            Debug.LogWarning(
                $"[GoogleSheet] {label} 일부 행만 반영됨: {parsedCount}/{sourceLines}. " +
                "빈 행·필수 컬럼 누락 행은 건너뛰었습니다.");
        }

        return true;
    }

    private static int CountNonEmptyLines(string tsv)
    {
        if (string.IsNullOrWhiteSpace(tsv))
            return 0;

        var rows = tsv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        int count = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(rows[i]))
                count++;
        }

        return count;
    }

    private static string Download(WebClient client, string url, string label)
    {
        try
        {
            return client.DownloadString(url);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{label} 다운로드 실패: {ex.Message}", ex);
        }
    }

    private static T LoadAsset<T>(string assetPath, string label) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset == null)
            throw new InvalidOperationException($"{label} SO를 찾을 수 없습니다: {assetPath}");

        return asset;
    }

    private static void MarkDirty(ScriptableObject asset, string message)
    {
        EditorUtility.SetDirty(asset);
        Debug.Log($"[GoogleSheet] {message} → {AssetDatabase.GetAssetPath(asset)}");
    }

    private static void ReportProgress(string step, float progress)
    {
        EditorUtility.DisplayProgressBar("Google Sheet Import", step, progress);
    }
}
#endif
