#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityEngine;

/// <summary>
/// 구글 시트 TSV 다운로드·병합·보내기. 컬럼 형식은 GoogleSheetDefinitions + 각 SO.ParseTsv와 동일합니다.
/// </summary>
public static class DefenseSheetTsvUtility
{
    public static string DownloadUtf8(string url)
    {
        using var client = new WebClient { Encoding = Encoding.UTF8 };
        return client.DownloadString(url);
    }

    /// <summary>첫 번째 탭 컬럼을 키로 시드 행을 기존 TSV에 upsert. 빈 키 행은 제외합니다.</summary>
    public static string MergeByFirstColumn(string baseTsv, string seedTsv)
    {
        var orderedKeys = new List<string>();
        var rows = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void ingest(string tsv, bool appendNewKeys)
        {
            if (string.IsNullOrWhiteSpace(tsv))
                return;

            var lines = tsv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var key = GetFirstColumn(line);
                if (string.IsNullOrEmpty(key))
                    continue;

                var isNew = !rows.ContainsKey(key);
                rows[key] = line.TrimEnd();

                if (appendNewKeys && isNew)
                    orderedKeys.Add(key);
            }
        }

        ingest(baseTsv, appendNewKeys: true);

        var seedKeys = new List<string>();
        if (!string.IsNullOrWhiteSpace(seedTsv))
        {
            var seedLines = seedTsv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < seedLines.Length; i++)
            {
                var line = seedLines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var key = GetFirstColumn(line);
                if (string.IsNullOrEmpty(key))
                    continue;

                var isNew = !rows.ContainsKey(key);
                rows[key] = line.TrimEnd();
                if (isNew)
                    seedKeys.Add(key);
            }
        }

        for (int i = 0; i < seedKeys.Count; i++)
            orderedKeys.Add(seedKeys[i]);

        var builder = new StringBuilder(orderedKeys.Count * 64);
        for (int i = 0; i < orderedKeys.Count; i++)
        {
            if (builder.Length > 0)
                builder.Append('\n');
            builder.Append(rows[orderedKeys[i]]);
        }

        return builder.ToString();
    }

    public static string GetFirstColumn(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return string.Empty;

        var tab = line.IndexOf('\t');
        return tab < 0 ? line.Trim() : line.Substring(0, tab).Trim();
    }

    public static void EnsureFolder(string path)
    {
#if UNITY_EDITOR
        if (UnityEditor.AssetDatabase.IsValidFolder(path))
            return;

        var parts = path.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!UnityEditor.AssetDatabase.IsValidFolder(next))
                UnityEditor.AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
#endif
    }
}
#endif
