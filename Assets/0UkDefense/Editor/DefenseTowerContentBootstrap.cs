#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 구글 시트 TSV 형식에 맞춘 타워 콘텐츠 시드.
/// 1) 시트 다운로드 → 시드 병합 → ParseTsv로 SO 반영
/// 2) 병합 TSV를 SheetExport에 저장 (구글 시트에 붙여넣기용)
/// </summary>
public static class DefenseTowerContentBootstrap
{
    private const string ExportDirectory = "Assets/0UkDefense/1Data/SheetExport";

    public static void RefreshFromSeed(bool exportTsv = true, bool applyToAssets = true)
    {
        try
        {
            BootstrapAll(exportForGoogleSheet: exportTsv, applyToAssets: applyToAssets);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[UkDefense] 타워 콘텐츠 데이터 갱신 완료 — 시드 병합" +
                (applyToAssets ? " · SO 반영" : string.Empty) +
                (exportTsv ? " · SheetExport TSV 저장" : string.Empty));
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[UkDefense] 타워 콘텐츠 데이터 갱신 실패: " + ex.Message);
            throw;
        }
    }

    [System.Obsolete("Use UkDefense/Data/Merge Seed → SO + Export TSV")]
    public static void BootstrapFromMenu() => RefreshFromSeed(exportTsv: true);

    public static void BootstrapAll(bool exportForGoogleSheet = false, bool applyToAssets = true)
    {
        var effectTsv = MergeSheet(GoogleSheetDefinitions.EffectDataUrl, DefenseTowerContentTsvSeed.EffectRows, DefenseSheetSoTsvExporter.LoadLocalEffectTsv);
        var effectGroupTsv = MergeSheet(GoogleSheetDefinitions.EffectGroupDataUrl, DefenseTowerContentTsvSeed.EffectGroupRows, DefenseSheetSoTsvExporter.LoadLocalEffectGroupTsv);
        var skillTsv = MergeSheet(GoogleSheetDefinitions.SkillDataUrl, DefenseTowerContentTsvSeed.SkillRows, DefenseSheetSoTsvExporter.LoadLocalSkillTsv);
        var towerTsv = MergeSheet(GoogleSheetDefinitions.TowerDataUrl, DefenseTowerContentTsvSeed.TowerRows, DefenseSheetSoTsvExporter.LoadLocalTowerTsv);

        if (exportForGoogleSheet)
            WriteExports(effectTsv, effectGroupTsv, skillTsv, towerTsv);

        if (!applyToAssets)
            return;

        ApplyEffectTsv(effectTsv);
        ApplyEffectGroupTsv(effectGroupTsv);
        ApplySkillTsv(skillTsv);
        ApplyTowerTsv(towerTsv);
    }

    private static string MergeSheet(string url, string seedRows, System.Func<string> localFallback)
    {
        string baseTsv = string.Empty;
        try
        {
            baseTsv = DefenseSheetTsvUtility.DownloadUtf8(url);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UkDefense] 시트 다운로드 실패: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(baseTsv))
        {
            baseTsv = localFallback?.Invoke() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(baseTsv))
                Debug.Log("[UkDefense] 로컬 SO를 TSV로 변환해 병합 베이스로 사용합니다.");
        }

        return DefenseSheetTsvUtility.MergeByFirstColumn(baseTsv, seedRows);
    }

    private static void ApplyEffectTsv(string tsv)
    {
        var parsed = DefenseEffectDataSo.ParseTsv(tsv);
        if (parsed.Count <= 0)
        {
            Debug.LogError("[UkDefense] 이펙트 TSV 파싱 0건 — SO 미반영");
            return;
        }

        var asset = Load<DefenseEffectDataSo>(GoogleSheetDefinitions.EffectDataAssetPath);
        asset.SetData(parsed);
        asset.RebuildLookup();
        EditorUtility.SetDirty(asset);
        Debug.Log($"[UkDefense] 이펙트 DB {parsed.Count}행 반영 (시트 형식)");
    }

    private static void ApplyEffectGroupTsv(string tsv)
    {
        var parsed = DefenseEffectGroupDataSo.ParseTsv(tsv);
        if (parsed.Count <= 0)
        {
            Debug.LogError("[UkDefense] 이펙트그룹 TSV 파싱 0건 — SO 미반영");
            return;
        }

        var asset = Load<DefenseEffectGroupDataSo>(GoogleSheetDefinitions.EffectGroupDataAssetPath);
        asset.SetData(parsed);
        asset.RebuildLookup();
        EditorUtility.SetDirty(asset);
        Debug.Log($"[UkDefense] 이펙트그룹 DB {parsed.Count}행 반영 (시트 형식)");
    }

    private static void ApplySkillTsv(string tsv)
    {
        var parsed = DefenseSkillDataSo.ParseTsv(tsv);
        if (parsed.Count <= 0)
        {
            Debug.LogError("[UkDefense] 스킬 TSV 파싱 0건 — SO 미반영");
            return;
        }

        var asset = Load<DefenseSkillDataSo>(GoogleSheetDefinitions.SkillDataAssetPath);
        asset.SetData(parsed);
        asset.RebuildLookup();
        EditorUtility.SetDirty(asset);
        Debug.Log($"[UkDefense] 스킬 DB {parsed.Count}행 반영 (시트 형식)");
    }

    private static void ApplyTowerTsv(string tsv)
    {
        var parsed = TowerDataSo.ParseTsv(tsv);
        if (parsed.Count <= 0)
        {
            Debug.LogError("[UkDefense] 타워 TSV 파싱 0건 — SO 미반영");
            return;
        }

        var asset = Load<TowerDataSo>(GoogleSheetDefinitions.TowerDataAssetPath);
        asset.SetData(parsed);
        asset.RebuildLookup();
        EditorUtility.SetDirty(asset);
        Debug.Log($"[UkDefense] 타워 DB {parsed.Count}행 반영 (시트 형식)");
    }

    private static void WriteExports(string effectTsv, string effectGroupTsv, string skillTsv, string towerTsv)
    {
        Directory.CreateDirectory(ExportDirectory);
        WriteUtf8(Path.Combine(ExportDirectory, "Effect.tsv"), effectTsv);
        WriteUtf8(Path.Combine(ExportDirectory, "EffectGroup.tsv"), effectGroupTsv);
        WriteUtf8(Path.Combine(ExportDirectory, "Skill.tsv"), skillTsv);
        WriteUtf8(Path.Combine(ExportDirectory, "Tower.tsv"), towerTsv);
        WriteUtf8(Path.Combine(ExportDirectory, "README.txt"), BuildReadme());
    }

    private static string BuildReadme()
    {
        return
            "구글 시트에 붙여넣기 (각 탭 데이터는 A2부터):\n\n" +
            "- Effect.tsv      → 이펙트 DB 탭 (gid=721846049) A2\n" +
            "- EffectGroup.tsv → 이펙트그룹 DB 탭 (gid=1627883599) A2\n" +
            "- Skill.tsv       → 스킬 DB 탭 (gid=900143476) A2\n" +
            "- Tower.tsv       → 타워 DB 탭 (gid=774552842) A2\n" +
            "- AddressableKey.tsv → 어드레서블 키 DB 탭 (gid=1338748644) A2\n\n" +
            "기존 행은 유지되고, 시드 행만 upsert 됩니다.\n" +
            "Unity에서 GoogleSheet/Import All Sheets From Google 으로 다시 가져올 수 있습니다.";
    }

    private static void WriteUtf8(string path, string content)
    {
        File.WriteAllText(path, content ?? string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Debug.Log($"[UkDefense] TSV 저장: {path}");
    }

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            throw new InvalidOperationException($"SO를 찾을 수 없습니다: {path}");
        return asset;
    }
}
#endif
