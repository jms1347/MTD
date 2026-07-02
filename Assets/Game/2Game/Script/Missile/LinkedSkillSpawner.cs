using UnityEngine;

/// <summary>
/// 미사일 명중 후 M열 소환 프리팹 스폰.
/// </summary>
public static class LinkedSkillSpawner
{
    public const string StormCloudPrefabKey = "CloudBlack";

#if UNITY_EDITOR
    public const string StormCloudPrefabPath =
        "Assets/JMO Assets/Cartoon FX/CFX3 Prefabs (Mobile)/Misc/CloudBlack.prefab";
    public const string BlizzardSnowZonePrefabPath =
        "Assets/Game/2Game/Prefab/Defense/Combat/Zones/Zone_BlizzardSnow.prefab";
#endif

    private static Transform summonRoot;

    public static bool CanSpawn(DefenseSkillData skill)
    {
        return skill != null && skill.HasSummonPrefab;
    }

    public static bool TrySpawn(LinkedSkillSpawnContext context)
    {
        var prefabKey = ResolveSummonPrefabKey(context);
        if (string.IsNullOrWhiteSpace(prefabKey))
            return false;

        if (!TryLoadBehaviorPrefab(prefabKey, out var prefab) || prefab == null)
        {
            Debug.LogWarning(
                $"[LinkedSkillSpawner] 소환 프리팹을 찾을 수 없습니다: skill={context.sourceSkill?.skillCode} key={prefabKey}");
            return false;
        }

        var instance = Object.Instantiate(
            prefab,
            ResolveSpawnPosition(context),
            Quaternion.identity,
            EnsureSummonRoot());
        instance.name = $"{prefab.name}_{Time.frameCount}";
        if (!instance.TryGetComponent<ILinkedSkillSpawn>(out var spawnable))
            spawnable = instance.GetComponentInChildren<ILinkedSkillSpawn>();

        if (spawnable == null)
        {
            Debug.LogWarning(
                $"[LinkedSkillSpawner] ILinkedSkillSpawn이 없습니다: {prefab.name}. DefenseStormCloud를 프리팹에 추가해 주세요.");
            Object.Destroy(instance);
            return false;
        }

        spawnable.Initialize(context);
        return true;
    }

    public static string ResolveSummonPrefabKey(LinkedSkillSpawnContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.summonPrefabKey))
            return context.summonPrefabKey.Trim();

        return context.sourceSkill != null && context.sourceSkill.HasSummonPrefab
            ? context.sourceSkill.summonPrefabKey.Trim()
            : string.Empty;
    }

    private static bool TryLoadBehaviorPrefab(string key, out GameObject prefab)
    {
        prefab = null;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (DefenseAddressableLoader.TryLoadPrefab(key, out prefab) && prefab != null)
            return true;

        if (DefenseAddressableLoader.TryLoadEffect(key, out prefab) && prefab != null)
            return true;

#if UNITY_EDITOR
        if (string.Equals(key, StormCloudPrefabKey, System.StringComparison.OrdinalIgnoreCase))
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(StormCloudPrefabPath);
        else if (string.Equals(
                     key,
                     DefenseSkillCombatTable.BlizzardSnowZonePrefabKey,
                     System.StringComparison.OrdinalIgnoreCase))
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BlizzardSnowZonePrefabPath);
#endif

        return prefab != null;
    }

    private static Vector3 ResolveSpawnPosition(LinkedSkillSpawnContext context)
    {
        return context.spawnOrigin;
    }

    private static Transform EnsureSummonRoot()
    {
        if (summonRoot != null)
            return summonRoot;

        var root = new GameObject("LinkedSkillSummons");
        summonRoot = root.transform;
        return summonRoot;
    }
}
