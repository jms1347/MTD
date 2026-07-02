using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterDataSo", menuName = "UkDefense/Monster Data")]
public class MonsterDataSo : ScriptableObject
{
    public List<MonsterData> list = new();

    private Dictionary<string, MonsterData> lookupByCode;

    public IReadOnlyList<MonsterData> All => list;

    public void SetData(IEnumerable<MonsterData> source)
    {
        list.Clear();
        lookupByCode = null;

        if (source == null)
            return;

        foreach (var monster in source)
        {
            if (monster == null || string.IsNullOrWhiteSpace(monster.code))
                continue;

            var code = monster.code.Trim();
            if (ContainsCode(code))
            {
                Debug.LogWarning($"[MonsterDataSo] duplicate monster code ignored: {code}");
                continue;
            }

            monster.code = code;
            list.Add(monster);
        }
    }

    public void RebuildLookup()
    {
        lookupByCode = new Dictionary<string, MonsterData>(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            var monster = list[i];
            if (monster == null || string.IsNullOrWhiteSpace(monster.code))
                continue;

            var code = monster.code.Trim();
            if (!lookupByCode.ContainsKey(code))
                lookupByCode.Add(code, monster);
        }
    }

    public bool TryGet(string code, out MonsterData monster)
    {
        EnsureLookup();
        if (string.IsNullOrWhiteSpace(code))
        {
            monster = null;
            return false;
        }

        return lookupByCode.TryGetValue(code.Trim(), out monster);
    }

    private bool ContainsCode(string code)
    {
        EnsureLookup();
        return lookupByCode.ContainsKey(code);
    }

    private void EnsureLookup()
    {
        if (lookupByCode == null)
            RebuildLookup();
    }
}
