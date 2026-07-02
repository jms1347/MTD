using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StageMonsterSpawn
{
    [Tooltip("MonsterDataSo code (MG-xxxx), BossDataSo code (BG-xxxx), or boss monster code (MB-xxxx)")]
    public string monsterCode;

    [Min(0)]
    [Tooltip("이 스테이지에서 스폰할 마릿수")]
    public int count;
}

[Serializable]
public class StageData
{
    [Tooltip("1부터 시작하는 스테이지 번호")]
    public int stageId;

    [Tooltip("UI 표시용 이름 (비워두면 stageId만 사용)")]
    public string displayName;

    [Tooltip("종류별 등장 마릿수. 합계가 스테이지 총 스폰 수")]
    public List<StageMonsterSpawn> spawns = new();

    [Tooltip("전투 시작 전 준비 시간(초). -1이면 DefenseStageTimerManager 기본값")]
    public float preBattleSeconds = -1f;

    public int TotalSpawnCount
    {
        get
        {
            int sum = 0;
            for (int i = 0; i < spawns.Count; i++)
            {
                if (spawns[i] == null)
                    continue;

                sum += Mathf.Max(0, spawns[i].count);
            }

            return sum;
        }
    }
}

[CreateAssetMenu(fileName = "StageDataSo", menuName = "UkDefense/Stage Data")]
public class StageDataSo : ScriptableObject
{
    public List<StageData> stages = new();

    private Dictionary<int, StageData> lookupByStageId;

    public void ImportFromSheets(string metaTsv, string spawnTsv)
    {
        SetData(ParseSheets(metaTsv, spawnTsv));
    }

    public static List<StageData> ParseSheets(string metaTsv, string spawnTsv)
    {
        var stageMap = new Dictionary<int, StageData>();
        ParseMetaTsv(metaTsv, stageMap);
        ParseSpawnTsv(spawnTsv, stageMap);

        var result = new List<StageData>(stageMap.Values);
        result.Sort((a, b) => a.stageId.CompareTo(b.stageId));
        return result;
    }

    public void SetData(IEnumerable<StageData> source)
    {
        stages.Clear();
        lookupByStageId = null;

        if (source == null)
            return;

        foreach (var stage in source)
        {
            if (stage == null || stage.stageId <= 0)
                continue;

            if (ContainsStage(stage.stageId))
            {
                Debug.LogWarning($"[StageDataSo] duplicate stageId ignored: {stage.stageId}");
                continue;
            }

            if (stage.spawns == null)
                stage.spawns = new List<StageMonsterSpawn>();

            stages.Add(stage);
        }
    }

    public void RebuildLookup()
    {
        lookupByStageId = new Dictionary<int, StageData>(stages.Count);
        for (int i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            if (stage == null || stage.stageId <= 0)
                continue;

            if (!lookupByStageId.ContainsKey(stage.stageId))
                lookupByStageId.Add(stage.stageId, stage);
        }
    }

    public bool TryGetStage(int stageId, out StageData stage)
    {
        EnsureLookup();
        return lookupByStageId.TryGetValue(stageId, out stage);
    }

    private static void ParseMetaTsv(string tsv, Dictionary<int, StageData> stageMap)
    {
        if (string.IsNullOrWhiteSpace(tsv))
            return;

        var rows = tsv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < rows.Length; i++)
        {
            var cells = rows[i].Split('\t');
            if (cells.Length < 1)
                continue;

            if (!SheetParseUtility.TryParseInt(cells[0], out var stageId) || stageId <= 0)
                continue;

            var displayName = cells.Length > 1 ? cells[1].Trim() : string.Empty;
            if (string.IsNullOrEmpty(displayName))
                displayName = $"{stageId}스테이지";

            var preBattleSeconds = -1f;
            if (cells.Length > 2)
                SheetParseUtility.TryParseFloat(cells[2], out preBattleSeconds);

            stageMap[stageId] = new StageData
            {
                stageId = stageId,
                displayName = displayName,
                preBattleSeconds = preBattleSeconds,
                spawns = new List<StageMonsterSpawn>()
            };
        }
    }

    private static void ParseSpawnTsv(string tsv, Dictionary<int, StageData> stageMap)
    {
        if (string.IsNullOrWhiteSpace(tsv))
            return;

        var rows = tsv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < rows.Length; i++)
        {
            var cells = rows[i].Split('\t');
            if (cells.Length < 3)
                continue;

            if (!SheetParseUtility.TryParseInt(cells[0], out var stageId) || stageId <= 0)
                continue;

            var monsterCode = cells[1].Trim();
            if (string.IsNullOrEmpty(monsterCode))
                continue;

            if (!SheetParseUtility.TryParseInt(cells[2], out var count) || count <= 0)
                continue;

            if (!stageMap.TryGetValue(stageId, out var stage))
            {
                stage = new StageData
                {
                    stageId = stageId,
                    displayName = $"{stageId}스테이지",
                    preBattleSeconds = -1f,
                    spawns = new List<StageMonsterSpawn>()
                };
                stageMap.Add(stageId, stage);
            }

            AddOrMergeSpawn(stage, monsterCode, count);
        }
    }

    private static void AddOrMergeSpawn(StageData stage, string monsterCode, int count)
    {
        for (int i = 0; i < stage.spawns.Count; i++)
        {
            var existing = stage.spawns[i];
            if (existing == null || !string.Equals(existing.monsterCode, monsterCode, StringComparison.Ordinal))
                continue;

            existing.count += count;
            return;
        }

        stage.spawns.Add(new StageMonsterSpawn
        {
            monsterCode = monsterCode,
            count = count
        });
    }

    private bool ContainsStage(int stageId)
    {
        EnsureLookup();
        return lookupByStageId.ContainsKey(stageId);
    }

    private void EnsureLookup()
    {
        if (lookupByStageId == null)
            RebuildLookup();
    }
}

public class StageSpawnQueue
{
    private readonly List<string> codes = new();
    private int index;

    public int TotalCount => codes.Count;
    public int Remaining => codes.Count - index;

    public static StageSpawnQueue Build(StageData stage, MonsterDataSo monsters)
    {
        var queue = new StageSpawnQueue();
        if (stage == null || stage.spawns == null)
            return queue;

        for (int i = 0; i < stage.spawns.Count; i++)
        {
            var entry = stage.spawns[i];
            if (entry == null || entry.count <= 0 || string.IsNullOrWhiteSpace(entry.monsterCode))
                continue;

            var code = entry.monsterCode.Trim();
            if (!IsResolvableSpawnCode(code, monsters))
            {
                Debug.LogWarning($"[StageSpawnQueue] unknown spawn code ignored: {code}");
                continue;
            }

            for (int c = 0; c < entry.count; c++)
                queue.codes.Add(code);
        }

        Shuffle(queue.codes);
        return queue;
    }

    public bool TryPeek(out string monsterCode)
    {
        if (index >= codes.Count)
        {
            monsterCode = null;
            return false;
        }

        monsterCode = codes[index];
        return true;
    }

    public void ConfirmSpawn()
    {
        if (index < codes.Count)
            index++;
    }

    public bool TryDequeue(out string monsterCode)
    {
        if (!TryPeek(out monsterCode))
            return false;

        ConfirmSpawn();
        return true;
    }

    private static void Shuffle(List<string> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static bool IsResolvableSpawnCode(string code, MonsterDataSo monsters)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        if (monsters != null && monsters.TryGet(code, out _))
            return true;

        return code.StartsWith("BG-", StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("MB-", StringComparison.OrdinalIgnoreCase);
    }
}
