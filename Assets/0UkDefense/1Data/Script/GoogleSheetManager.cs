using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GoogleSheetManager : Singleton<GoogleSheetManager>
{
    [SerializeField] private float requestTimeoutSeconds = 15f;

    private string pendingStageMetaTsv = string.Empty;

    public bool IsLoaded { get; private set; }

    // 레거시 참조 호환
    public const string TowerDataExportUrl = GoogleSheetDefinitions.TowerDataUrl;
    public const string SkillDataExportUrl = GoogleSheetDefinitions.SkillDataUrl;
    public const string EffectDataExportUrl = GoogleSheetDefinitions.EffectDataUrl;
    public const string EffectGroupDataExportUrl = GoogleSheetDefinitions.EffectGroupDataUrl;

    protected override void Awake()
    {
        base.Awake();
        StartCoroutine(LoadAllDataRoutine());
    }

    private IEnumerator LoadAllDataRoutine()
    {
        IsLoaded = false;
        Debug.Log("[GoogleSheetManager] 구글 시트 데이터 다운로드 시작...");

        yield return DownloadAndApply(GoogleSheetDefinitions.MonsterDataUrl, ApplyMonsterData, "몬스터 DB");
        yield return DownloadAndApply(GoogleSheetDefinitions.TowerDataUrl, ApplyTowerData, "타워 DB");
        yield return DownloadAndApply(GoogleSheetDefinitions.SkillDataUrl, ApplySkillData, "스킬(미사일) DB");
        yield return DownloadAndApply(GoogleSheetDefinitions.EffectDataUrl, ApplyEffectData, "이펙트 DB");
        yield return DownloadAndApply(GoogleSheetDefinitions.EffectGroupDataUrl, ApplyEffectGroupData, "이펙트 그룹 DB");
        yield return DownloadAndApply(GoogleSheetDefinitions.AddressableKeyDataUrl, ApplyAddressableKeyData, "어드레서블 키 DB");
        yield return DownloadAndApply(GoogleSheetDefinitions.StageMetaDataUrl, ApplyStageMetaData, "스테이지 DB");
        yield return DownloadAndApply(GoogleSheetDefinitions.StageSpawnDataUrl, ApplyStageSpawnData, "스테이지 몬스터 구성 DB");
        yield return DownloadAndApply(GoogleSheetDefinitions.RoguelikeCardDataUrl, ApplyRoguelikeCardData, "로그라이크 카드 DB");

        if (DataManager.Instance != null)
            DataManager.Instance.InitializeAllData();
        else
            Debug.LogError("[GoogleSheetManager] DataManager 인스턴스가 없습니다.");

        IsLoaded = true;
        Debug.Log("[GoogleSheetManager] 모든 시트 데이터 적용 완료");
    }

    private IEnumerator DownloadAndApply(string url, Action<string> applyAction, string label)
    {
        using var request = UnityWebRequest.Get(url);
        request.timeout = Mathf.CeilToInt(requestTimeoutSeconds);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[GoogleSheetManager] {label} 다운로드 실패: {request.error}");
            applyAction(string.Empty);
            yield break;
        }

        applyAction(request.downloadHandler.text);
    }

    private void ApplyMonsterData(string tsv)
    {
        if (string.IsNullOrWhiteSpace(tsv))
        {
            Debug.LogWarning("[GoogleSheetManager] 몬스터 DB TSV가 비어 있습니다. SO 폴백 데이터를 유지합니다.");
            return;
        }

        var parsed = ParseMonsterData(tsv);
        if (DataManager.Instance?.Monsters != null)
            DataManager.Instance.Monsters.SetData(parsed);
        else
            Debug.LogError("[GoogleSheetManager] MonsterDataSo 참조가 없습니다.");
    }

    private void ApplyTowerData(string tsv)
    {
        if (string.IsNullOrWhiteSpace(tsv))
        {
            Debug.LogWarning("[GoogleSheetManager] 타워 DB TSV가 비어 있습니다. SO 폴백 데이터를 유지합니다.");
            return;
        }

        var parsed = TowerDataSo.ParseTsv(tsv);
        if (parsed.Count <= 0)
        {
            Debug.LogError("[GoogleSheetManager] 타워 DB 파싱 결과 0건. SO 폴백 데이터를 유지합니다.");
            return;
        }

        if (DataManager.Instance?.Towers != null)
        {
            DataManager.Instance.Towers.SetData(parsed);
            DataManager.Instance.Towers.RebuildLookup();
            Debug.Log($"[GoogleSheetManager] 타워 DB 적용 완료 ({DataManager.Instance.Towers.list.Count} towers)");
            TowerStatsManager.RefreshFromSheetIfExists();
        }
        else
            Debug.LogError("[GoogleSheetManager] TowerDataSo 참조가 없습니다.");
    }

    private void ApplySkillData(string tsv)
    {
        if (string.IsNullOrWhiteSpace(tsv))
        {
            Debug.LogWarning("[GoogleSheetManager] 스킬(미사일) DB TSV가 비어 있습니다. SO 폴백 데이터를 유지합니다.");
            return;
        }

        var parsed = DefenseSkillDataSo.ParseTsv(tsv);
        if (parsed.Count <= 0)
        {
            Debug.LogError("[GoogleSheetManager] 스킬 DB 파싱 결과 0건. SO 폴백 데이터를 유지합니다.");
            return;
        }

        if (DataManager.Instance?.Skills != null)
        {
            DataManager.Instance.Skills.SetData(parsed);
            DataManager.Instance.Skills.RebuildLookup();
            Debug.Log($"[GoogleSheetManager] 스킬(미사일) DB 적용 완료 ({DataManager.Instance.Skills.list.Count} skills)");
            TowerStatsManager.RefreshFromSheetIfExists();
        }
        else
            Debug.LogError("[GoogleSheetManager] DefenseSkillDataSo 참조가 없습니다.");
    }

    private void ApplyEffectData(string tsv)
    {
        if (string.IsNullOrWhiteSpace(tsv))
        {
            Debug.LogWarning("[GoogleSheetManager] 이펙트 DB TSV가 비어 있습니다. SO 폴백 데이터를 유지합니다.");
            return;
        }

        var parsed = DefenseEffectDataSo.ParseTsv(tsv);
        if (parsed.Count <= 0)
        {
            Debug.LogError("[GoogleSheetManager] 이펙트 DB 파싱 결과 0건. SO 폴백 데이터를 유지합니다.");
            return;
        }

        if (DataManager.Instance?.Effects != null)
        {
            DataManager.Instance.Effects.SetData(parsed);
            DataManager.Instance.Effects.RebuildLookup();
            Debug.Log($"[GoogleSheetManager] 이펙트 DB 적용 완료 ({DataManager.Instance.Effects.list.Count} effects)");
        }
        else
            Debug.LogError("[GoogleSheetManager] DefenseEffectDataSo 참조가 없습니다.");
    }

    private void ApplyEffectGroupData(string tsv)
    {
        if (string.IsNullOrWhiteSpace(tsv))
        {
            Debug.LogWarning("[GoogleSheetManager] 이펙트 그룹 DB TSV가 비어 있습니다. SO 폴백 데이터를 유지합니다.");
            return;
        }

        var parsed = DefenseEffectGroupDataSo.ParseTsv(tsv);
        if (parsed.Count <= 0)
        {
            Debug.LogError("[GoogleSheetManager] 이펙트 그룹 DB 파싱 결과 0건. SO 폴백 데이터를 유지합니다.");
            return;
        }

        if (DataManager.Instance?.EffectGroups != null)
        {
            DataManager.Instance.EffectGroups.SetData(parsed);
            DataManager.Instance.EffectGroups.RebuildLookup();
            Debug.Log($"[GoogleSheetManager] 이펙트 그룹 DB 적용 완료 ({parsed.Count} rows, {CountEffectGroups()} groups)");
        }
        else
            Debug.LogError("[GoogleSheetManager] DefenseEffectGroupDataSo 참조가 없습니다.");
    }

    private void ApplyAddressableKeyData(string tsv)
    {
        if (string.IsNullOrWhiteSpace(tsv))
        {
            Debug.LogWarning("[GoogleSheetManager] 어드레서블 키 DB TSV가 비어 있습니다. SO 폴백 데이터를 유지합니다.");
            return;
        }

        var parsed = DefenseAddressableKeyDataSo.ParseTsv(tsv);
        if (parsed.Count <= 0)
        {
            Debug.LogError("[GoogleSheetManager] 어드레서블 키 DB 파싱 결과 0건. SO 폴백 데이터를 유지합니다.");
            return;
        }

        if (DataManager.Instance?.AddressableKeys != null)
        {
            DataManager.Instance.AddressableKeys.SetData(parsed);
            DataManager.Instance.AddressableKeys.RebuildLookup();
            DefenseAddressableLoader.ClearCache();
            Debug.Log($"[GoogleSheetManager] 어드레서블 키 DB 적용 완료 ({DataManager.Instance.AddressableKeys.list.Count} keys)");
        }
        else
            Debug.LogError("[GoogleSheetManager] DefenseAddressableKeyDataSo 참조가 없습니다.");
    }

    private static int CountEffectGroups()
    {
        if (DataManager.Instance?.EffectGroups == null)
            return 0;

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in DataManager.Instance.EffectGroups.All)
        {
            if (entry != null && !string.IsNullOrWhiteSpace(entry.effectGroupCode))
                ids.Add(entry.effectGroupCode.Trim());
        }

        return ids.Count;
    }

    private void ApplyStageMetaData(string tsv)
    {
        pendingStageMetaTsv = tsv ?? string.Empty;
    }

    private void ApplyStageSpawnData(string tsv)
    {
        if (DataManager.Instance?.Stages == null)
        {
            Debug.LogError("[GoogleSheetManager] StageDataSo 참조가 없습니다.");
            pendingStageMetaTsv = string.Empty;
            return;
        }

        if (string.IsNullOrWhiteSpace(pendingStageMetaTsv) && string.IsNullOrWhiteSpace(tsv))
        {
            Debug.LogWarning("[GoogleSheetManager] 스테이지 DB TSV가 비어 있습니다. SO 폴백 데이터를 유지합니다.");
            pendingStageMetaTsv = string.Empty;
            return;
        }

        DataManager.Instance.Stages.ImportFromSheets(pendingStageMetaTsv, tsv ?? string.Empty);
        Debug.Log($"[GoogleSheetManager] 스테이지 DB 적용 완료 ({DataManager.Instance.Stages.stages.Count} stages)");

        pendingStageMetaTsv = string.Empty;
    }

    private void ApplyRoguelikeCardData(string tsv)
    {
        if (string.IsNullOrWhiteSpace(tsv))
        {
            Debug.LogWarning("[GoogleSheetManager] 로그라이크 카드 DB TSV가 비어 있습니다. SO 폴백 데이터를 유지합니다.");
            return;
        }

        var parsed = RoguelikeCardDataSo.ParseTsv(tsv);
        if (parsed.Count <= 0)
        {
            Debug.LogError("[GoogleSheetManager] 로그라이크 카드 DB 파싱 결과 0건. SO 폴백 데이터를 유지합니다.");
            return;
        }

        if (DataManager.Instance?.RoguelikeCards != null)
        {
            DataManager.Instance.RoguelikeCards.SetData(parsed);
            DataManager.Instance.RoguelikeCards.RebuildLookup();
            Debug.Log($"[GoogleSheetManager] 로그라이크 카드 DB 적용 완료 ({DataManager.Instance.RoguelikeCards.list.Count} cards)");
        }
        else
            Debug.LogError("[GoogleSheetManager] RoguelikeCardDataSo 참조가 없습니다.");
    }

    public static List<MonsterData> ParseMonsterData(string tsv)
    {
        var result = new List<MonsterData>();
        if (string.IsNullOrWhiteSpace(tsv))
            return result;

        var rows = tsv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < rows.Length; i++)
        {
            var cells = rows[i].Split('\t');
            if (cells.Length < 7)
                continue;

            var code = cells[0].Trim();
            if (string.IsNullOrEmpty(code))
                continue;

            // code | monsterType | attackMethod | hp | attack | defense | attackSpeed | prefabKey | scale | moveSpeed
            var attackMethodIndex = IsLegacyMobilityColumn(cells) ? 3 : 2;
            var statIndex = attackMethodIndex + 1;

            if (cells.Length <= statIndex + 3)
                continue;

            if (!SheetParseUtility.TryParseInt(cells[statIndex], out var hp))
                continue;

            if (!SheetParseUtility.TryParseInt(cells[statIndex + 1], out var attack))
                continue;

            if (!SheetParseUtility.TryParseInt(cells[statIndex + 2], out var defense))
                continue;

            if (!SheetParseUtility.TryParseFloat(cells[statIndex + 3], out var attackSpeed))
                continue;

            var prefabKey = cells.Length > statIndex + 4
                ? cells[statIndex + 4].Trim()
                : code;

            float tableScale = 0f;
            if (cells.Length > statIndex + 5)
                SheetParseUtility.TryParseFloat(cells[statIndex + 5], out tableScale);

            float tableMoveSpeed = 0f;
            if (cells.Length > statIndex + 6)
                SheetParseUtility.TryParseFloat(cells[statIndex + 6], out tableMoveSpeed);

            result.Add(new MonsterData
            {
                code = code,
                monsterType = cells[1].Trim(),
                attackMethod = cells[attackMethodIndex].Trim(),
                hp = hp,
                attack = attack,
                defense = defense,
                attackSpeed = attackSpeed,
                prefabKey = prefabKey,
                scale = tableScale,
                moveSpeed = tableMoveSpeed
            });
        }

        return result;
    }

    private static bool IsLegacyMobilityColumn(string[] cells)
    {
        if (cells.Length < 8)
            return false;

        var value = cells[2].Trim();
        return value.Contains("지상", StringComparison.Ordinal)
            || value.Contains("공중", StringComparison.Ordinal);
    }
}
