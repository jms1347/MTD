using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MonsterPrefabEntry
{
    public string code;
    public GameObject prefab;
}

[CreateAssetMenu(fileName = "MonsterPrefabCatalog", menuName = "UkDefense/Monster Prefab Catalog")]
public class MonsterPrefabCatalog : ScriptableObject
{
    public List<MonsterPrefabEntry> entries = new();

    [Tooltip("code 매칭 실패 시 사용할 공용 프리팹")]
    public GameObject defaultPrefab;

    private Dictionary<string, GameObject> lookupByCode;

    public bool TryGetPrefab(string code, out GameObject prefab)
    {
        EnsureLookup();
        if (string.IsNullOrWhiteSpace(code))
        {
            prefab = defaultPrefab;
            return prefab != null;
        }

        if (lookupByCode.TryGetValue(code.Trim(), out prefab))
            return true;

        prefab = defaultPrefab;
        return prefab != null;
    }

    public IEnumerable<(string code, GameObject prefab)> GetAllEntries()
    {
        EnsureLookup();
        foreach (var pair in lookupByCode)
            yield return (pair.Key, pair.Value);
    }

    public void RebuildLookup()
    {
        lookupByCode = new Dictionary<string, GameObject>(entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.code) || entry.prefab == null)
                continue;

            var code = entry.code.Trim();
            if (!lookupByCode.ContainsKey(code))
                lookupByCode.Add(code, entry.prefab);
        }
    }

    private void EnsureLookup()
    {
        if (lookupByCode == null)
            RebuildLookup();
    }
}
