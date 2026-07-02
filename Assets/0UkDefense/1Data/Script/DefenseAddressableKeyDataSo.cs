using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DefenseAddressableKeyEntry
{
    public string key;
    public DefenseAddressableKeyType assetType;
    public string description;
    /// <summary>Addressables Address(파일명 컬럼).</summary>
    public string addressKey;
    public string note;
}

[CreateAssetMenu(fileName = "DefenseAddressableKeyDataSo", menuName = "UkDefense/Addressable Key Data")]
public class DefenseAddressableKeyDataSo : ScriptableObject
{
    public List<DefenseAddressableKeyEntry> list = new();

    private Dictionary<string, DefenseAddressableKeyEntry> lookupByKey;

    public IReadOnlyList<DefenseAddressableKeyEntry> All => list;

    public void SetData(List<DefenseAddressableKeyEntry> entries)
    {
        list = entries ?? new List<DefenseAddressableKeyEntry>();
        RebuildLookup();
    }

    public void ImportFromTsv(string tsv)
    {
        SetData(ParseTsv(tsv));
    }

    public void RebuildLookup()
    {
        lookupByKey = new Dictionary<string, DefenseAddressableKeyEntry>(StringComparer.OrdinalIgnoreCase);
        if (list == null)
            return;

        for (int i = 0; i < list.Count; i++)
        {
            var entry = list[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                continue;

            lookupByKey[entry.key.Trim()] = entry;
        }
    }

    public bool TryGet(string key, out DefenseAddressableKeyEntry entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        lookupByKey ??= new Dictionary<string, DefenseAddressableKeyEntry>(StringComparer.OrdinalIgnoreCase);
        return lookupByKey.TryGetValue(key.Trim(), out entry) && entry != null;
    }

    public bool TryGetAddress(string key, DefenseAddressableKeyType requiredType, out string addressKey)
    {
        addressKey = null;
        if (!TryGet(key, out var entry))
            return false;

        if (requiredType != DefenseAddressableKeyType.Unknown && entry.assetType != requiredType)
            return false;

        if (string.IsNullOrWhiteSpace(entry.addressKey))
            return false;

        addressKey = entry.addressKey.Trim();
        return true;
    }

    public static List<DefenseAddressableKeyEntry> ParseTsv(string tsv)
    {
        var result = new List<DefenseAddressableKeyEntry>();
        if (string.IsNullOrWhiteSpace(tsv))
            return result;

        var rows = tsv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < rows.Length; i++)
        {
            var cells = rows[i].Split('\t');
            if (cells.Length < 4)
                continue;

            var key = cells[0].Trim();
            if (string.IsNullOrEmpty(key) || IsHeaderRow(key))
                continue;

            var type = ParseType(cells[1]);
            if (type == DefenseAddressableKeyType.Unknown)
            {
                Debug.LogWarning($"[AddressableKeyData] 알 수 없는 Type '{cells[1]}' (key={key}) — 행을 건너뜁니다.");
                continue;
            }

            var addressKey = cells[3].Trim();
            if (string.IsNullOrEmpty(addressKey))
            {
                Debug.LogWarning($"[AddressableKeyData] 파일명(Address)이 비어 있습니다 (key={key}) — 행을 건너뜁니다.");
                continue;
            }

            result.Add(new DefenseAddressableKeyEntry
            {
                key = key,
                assetType = type,
                description = cells.Length > 2 ? cells[2].Trim() : string.Empty,
                addressKey = addressKey,
                note = cells.Length > 4 ? cells[4].Trim() : string.Empty,
            });
        }

        return result;
    }

    private static bool IsHeaderRow(string key)
    {
        return key.Equals("Key", StringComparison.OrdinalIgnoreCase)
            || key.Equals("키", StringComparison.OrdinalIgnoreCase);
    }

    private static DefenseAddressableKeyType ParseType(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DefenseAddressableKeyType.Unknown;

        var value = raw.Trim();
        if (value.Equals("Prefab", StringComparison.OrdinalIgnoreCase)
            || value.Equals("프리팹", StringComparison.OrdinalIgnoreCase))
            return DefenseAddressableKeyType.Prefab;

        if (value.Equals("Missile", StringComparison.OrdinalIgnoreCase)
            || value.Equals("미사일", StringComparison.OrdinalIgnoreCase))
            return DefenseAddressableKeyType.Missile;

        if (value.Equals("Effect", StringComparison.OrdinalIgnoreCase)
            || value.Equals("이펙트", StringComparison.OrdinalIgnoreCase))
            return DefenseAddressableKeyType.Effect;

        return DefenseAddressableKeyType.Unknown;
    }
}
