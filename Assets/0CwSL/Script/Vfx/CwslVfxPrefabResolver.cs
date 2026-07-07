using System.Collections.Generic;
using UnityEngine;

/// <summary>에셋 SO에 VFX가 비어 있을 때 경로 기반으로 프리팹을 보강한다.</summary>
public static class CwslVfxPrefabResolver
{
    private static readonly Dictionary<string, GameObject> Cache = new();

    public static GameObject Resolve(GameObject assigned, string assetPath)
    {
        if (assigned != null)
            return assigned;

        if (string.IsNullOrWhiteSpace(assetPath))
            return null;

        if (Cache.TryGetValue(assetPath, out var cached))
            return cached;

#if UNITY_EDITOR
        var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (loaded != null)
            Cache[assetPath] = loaded;
        return loaded;
#else
        return null;
#endif
    }
}
