using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시트 스킬·어드레서블 키 기반 미사일 프리팹 해석 (Catalog 미러 없음).
/// </summary>
public static class DefenseMissileResolver
{
    private const string MissilePrefabDir = "Assets/Game/2Game/Prefab/Defense/Combat/Missiles";

    private static readonly (DefenseMissileId id, string key)[] DefaultMissileKeys =
    {
        (DefenseMissileId.Physical, "MissilePhysicalOBJ"),
        (DefenseMissileId.Water, "MissileWaterOBJ"),
        (DefenseMissileId.Poison, "MissilePoisonOBJ"),
        (DefenseMissileId.Fire, "MissileFireOBJ"),
        (DefenseMissileId.Ice, "MissileIceOBJ"),
        (DefenseMissileId.Lightning, "MissileLightningOBJ"),
    };

    public static GameObject GetPrefab(DefenseMissileId missileId)
    {
        foreach (var (id, key) in DefaultMissileKeys)
        {
            if (id != missileId)
                continue;

            if (DefenseAddressableLoader.TryLoadMissile(key, out var prefab) && prefab != null)
                return prefab;

#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(GetEditorMissilePath(missileId));
#else
            return null;
#endif
        }

        return null;
    }

    public static bool TryResolveId(GameObject prefab, out DefenseMissileId missileId)
    {
        missileId = DefenseMissileId.Physical;
        if (prefab == null)
            return false;

        foreach (var (id, _) in DefaultMissileKeys)
        {
            var resolved = GetPrefab(id);
            if (resolved == prefab)
            {
                missileId = id;
                return true;
            }
        }

        return false;
    }

    public static DamageElement GetElement(DefenseMissileId missileId)
    {
        return DefenseTowerCombatTable.MissileIdToElement(missileId);
    }

    public static IEnumerable<GameObject> CollectPoolPrefabs()
    {
        var seen = new HashSet<int>();

        if (DataManager.Instance?.Skills != null)
        {
            var skills = DataManager.Instance.Skills.All;
            for (int i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];
                if (skill == null)
                    continue;

                var prefab = DefenseSkillCombatTable.GetMissilePrefabForSkill(skill);
                if (prefab != null && seen.Add(prefab.GetInstanceID()))
                    yield return prefab;
            }
        }

        foreach (var (id, _) in DefaultMissileKeys)
        {
            var prefab = GetPrefab(id);
            if (prefab != null && seen.Add(prefab.GetInstanceID()))
                yield return prefab;
        }

        if (DefenseAddressableLoader.TryLoadMissile(DefenseFrozenOrbEmitter.DefaultShardMissileKey, out var frozenOrbShard)
            && frozenOrbShard != null
            && seen.Add(frozenOrbShard.GetInstanceID()))
            yield return frozenOrbShard;
    }

    private static string GetEditorMissilePath(DefenseMissileId id)
    {
        return id switch
        {
            DefenseMissileId.Physical => $"{MissilePrefabDir}/MissilePhysicalOBJ.prefab",
            DefenseMissileId.Water => $"{MissilePrefabDir}/MissileWaterOBJ.prefab",
            DefenseMissileId.Poison => $"{MissilePrefabDir}/MissilePoisonOBJ.prefab",
            DefenseMissileId.Fire => $"{MissilePrefabDir}/MissileFireOBJ.prefab",
            DefenseMissileId.Ice => $"{MissilePrefabDir}/MissileIceOBJ.prefab",
            DefenseMissileId.Lightning => $"{MissilePrefabDir}/MissileLightningOBJ.prefab",
            _ => $"{MissilePrefabDir}/MissilePhysicalOBJ.prefab"
        };
    }
}
