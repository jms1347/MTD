#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 변경된 Monster/Boss 테이블 기준으로 StageSpawn.tsv를 자동 생성합니다.
/// </summary>
public static class DefenseStageSpawnGenerator
{
    private static readonly Dictionary<int, string> BossByStage = new()
    {
        { 10, "BG-0001" },
        { 20, "BG-0002" },
        { 30, "BG-0003" },
        { 40, "BG-0004" },
        { 50, "BG-0005" },
        { 60, "BG-0006" },
        { 70, "BG-0001" },
        { 80, "BG-0002" },
        { 90, "BG-0003" },
        { 100, "BG-0004" },
    };

    [MenuItem(UkDefenseSetupMenu.DataRoot + "Regenerate Stage Spawn TSV", false, 6)]
    public static void RegenerateAndImport()
    {
        WriteStageSpawnTsv(UkDefenseStageDataSetup.StageSpawnTsvPath);
        UkDefenseStageDataSetup.ImportStageTsvsFromSheetExport();
        AssetDatabase.Refresh();
        Debug.Log("[UkDefense] StageSpawn.tsv 재생성 및 StageDataSo 반영 완료");
    }

    public static void WriteStageSpawnTsv(string path)
    {
        var builder = new StringBuilder();
        builder.AppendLine("stageId\tmonsterCode\tcount");

        for (int stage = 1; stage <= 100; stage++)
        {
            foreach (var entry in BuildStageSpawns(stage))
                builder.AppendLine($"{stage}\t{entry.code}\t{entry.count}");
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
    }

    private static List<(string code, int count)> BuildStageSpawns(int stage)
    {
        var weights = new Dictionary<string, float>();
        float t = (stage - 1) / 99f;

        weights["MG-0002"] = Clamp(1f - t * 0.85f, 0.08f, 1f);
        weights["MG-0004"] = Clamp(0.9f - t * 0.8f, 0.06f, 0.9f);
        if (stage >= 11) weights["MG-0003"] = Clamp((stage - 11) / 25f, 0f, 0.35f);
        if (stage >= 24) weights["MG-0001"] = Clamp((stage - 24) / 30f, 0f, 0.28f);
        if (stage >= 22) weights["MG-0007"] = Clamp((stage - 22) / 28f, 0f, 0.32f);
        if (stage >= 33) weights["MG-0005"] = Clamp((stage - 33) / 35f, 0f, 0.22f);
        if (stage >= 35) weights["MG-0006"] = Clamp((stage - 35) / 38f, 0f, 0.20f);
        if (stage >= 55) weights["MG-0008"] = Clamp((stage - 55) / 30f, 0f, 0.18f);
        if (stage >= 36) weights["MB-0003"] = Clamp((stage - 36) / 40f, 0f, 0.14f);
        if (stage >= 40) weights["MB-0004"] = Clamp((stage - 40) / 42f, 0f, 0.14f);
        if (stage >= 44) weights["MB-0005"] = Clamp((stage - 44) / 45f, 0f, 0.12f);
        if (stage >= 47) weights["MB-0006"] = Clamp((stage - 47) / 48f, 0f, 0.12f);

        int total = ResolveTotalMobCount(stage);
        var result = AllocateCounts(weights, total);

        if (BossByStage.TryGetValue(stage, out var bossCode))
            result.Insert(0, (bossCode, 1));

        return result;
    }

    private static int ResolveTotalMobCount(int stage)
    {
        int total;
        if (stage <= 9)
            total = 8 + stage;
        else if (stage <= 20)
            total = 17 + (stage - 9);
        else if (stage <= 40)
            total = 28 + Mathf.RoundToInt((stage - 20) * 1.1f);
        else if (stage <= 60)
            total = 50 + (stage - 40);
        else
            total = 70 + Mathf.RoundToInt((stage - 60) * 0.55f);

        if (stage % 10 == 0)
            total = Mathf.Max(total - 3, 12);

        return total;
    }

    private static List<(string code, int count)> AllocateCounts(Dictionary<string, float> weights, int total)
    {
        var active = new List<string>();
        float sum = 0f;
        foreach (var pair in weights)
        {
            if (pair.Value <= 0.001f)
                continue;

            active.Add(pair.Key);
            sum += pair.Value;
        }

        if (active.Count == 0)
            return new List<(string code, int count)> { ("MG-0002", total) };

        active.Sort();
        var result = new List<(string code, int count)>(active.Count);
        int remaining = total;

        for (int i = 0; i < active.Count; i++)
        {
            string code = active[i];
            int count;
            if (i == active.Count - 1)
            {
                count = Mathf.Max(1, remaining);
            }
            else
            {
                count = Mathf.Max(1, Mathf.RoundToInt(total * (weights[code] / sum)));
                count = Mathf.Min(count, remaining - (active.Count - i - 1));
            }

            result.Add((code, count));
            remaining -= count;
        }

        result.Sort((a, b) => string.CompareOrdinal(a.code, b.code));
        return result;
    }

    private static float Clamp(float value, float min, float max)
    {
        return Mathf.Clamp(value, min, max);
    }
}
#endif
