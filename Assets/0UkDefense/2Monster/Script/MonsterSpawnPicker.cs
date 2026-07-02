using System.Collections.Generic;
using UnityEngine;

public static class MonsterVisuals
{
    public static Color GetColor(MonsterData data)
    {
        if (data == null)
            return new Color(0.12f, 0.18f, 0.58f);

        if (data.IsSuicideAttacker)
            return new Color(0.92f, 0.15f, 0.12f);

        if (!string.IsNullOrEmpty(data.monsterType) && data.monsterType.Contains("뚱"))
            return new Color(0.12f, 0.18f, 0.58f);

        return new Color(0.18f, 0.55f, 0.28f);
    }
}

public static class MonsterSpawnPicker
{
    private static readonly List<MonsterData> FallbackMonsters = new()
    {
        new MonsterData
        {
            code = "MG-0002",
            monsterType = "일반",
            attackMethod = "근접",
            hp = 100,
            attack = 50,
            defense = 20,
            attackSpeed = 1f
        }
    };

    public static MonsterData RollRandom()
    {
        var table = DataManager.Instance?.Monsters;
        if (table != null && table.All.Count > 0)
        {
            MonsterData picked = null;
            int eligibleCount = 0;

            for (int i = 0; i < table.All.Count; i++)
            {
                var monster = table.All[i];
                if (monster == null || monster.IsBoss)
                    continue;

                eligibleCount++;
                if (Random.Range(0, eligibleCount) == 0)
                    picked = monster;
            }

            if (picked != null)
                return picked;
        }

        return FallbackMonsters[Random.Range(0, FallbackMonsters.Count)];
    }
}
