using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 협동 모드 전투 VFX — 타워와 동일한 미사일·Epic Toon FX 폭발을 사용합니다.
/// </summary>
public static class CoopCombatVfxCache
{
    private const string MissileVisualPath =
        "Assets/Epic Toon FX/Prefabs/Combat/Missiles/Grenade/GrenadeMissilePink.prefab";
    private const string ImpactPath =
        "Assets/Epic Toon FX/Prefabs/Combat/Explosions/GrenadeExplosion/GrenadeExplosionPink.prefab";
    private const string ExplosionPath =
        "Assets/Epic Toon FX/Prefabs/Combat/Explosions/GasExplosion/GasExplosionFire.prefab";
    private const string HeavyExplosionPath =
        "Assets/Epic Toon FX/Prefabs/Combat/Explosions/NukeExplosion/NukeExplosionFire.prefab";

    private static bool initialized;
    private static GameObject missileVisualPrefab;
    private static GameObject impactPrefab;
    private static GameObject explosionPrefab;
    private static GameObject heavyExplosionPrefab;

    public static void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        missileVisualPrefab = LoadPrefab(MissileVisualPath);
        impactPrefab = LoadPrefab(ImpactPath);
        explosionPrefab = LoadPrefab(ExplosionPath);
        heavyExplosionPrefab = LoadPrefab(HeavyExplosionPath);

        EnsureMissilePool();
    }

    public static void AttachMissileVisual(Transform parent)
    {
        EnsureInitialized();
        if (parent == null || missileVisualPrefab == null)
            return;

        var visual = Object.Instantiate(missileVisualPrefab, parent, false);
        visual.name = "MissileVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
    }

    public static void PlayImpact(Vector3 position, Quaternion rotation)
    {
        EnsureInitialized();
        PlayAt(impactPrefab, position, rotation, 2.5f);
    }

    public static void PlayExplosion(Vector3 position, bool heavy)
    {
        EnsureInitialized();
        var prefab = heavy && heavyExplosionPrefab != null ? heavyExplosionPrefab : explosionPrefab;
        PlayAt(prefab, position, Quaternion.identity, heavy ? 4f : 2.8f);
    }

    private static void PlayAt(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (prefab == null)
            return;

        if (MissilePoolManager.Instance != null)
        {
            MissilePoolManager.Instance.PlayVfxAt(prefab, position, rotation, lifetime);
            return;
        }

        var instance = Object.Instantiate(prefab, position, rotation);
        Object.Destroy(instance, lifetime);
    }

    private static void EnsureMissilePool()
    {
        if (MissilePoolManager.Instance != null)
        {
            var prefab = DefenseMissileResolver.GetPrefab(DefenseMissileId.Physical);
            if (prefab != null)
                MissilePoolManager.Instance.Initialize(new[] { prefab });
            return;
        }

        var poolObject = new GameObject("MissilePoolManager");
        Object.DontDestroyOnLoad(poolObject);
        var manager = poolObject.AddComponent<MissilePoolManager>();
        var missilePrefab = DefenseMissileResolver.GetPrefab(DefenseMissileId.Physical);
        if (missilePrefab != null)
            manager.Initialize(new[] { missilePrefab });
    }

    private static GameObject LoadPrefab(string assetPath)
    {
#if UNITY_EDITOR
        var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (editorPrefab != null)
            return editorPrefab;
#endif

        var fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (DefenseAddressableLoader.TryLoadByAddress(fileName, out var loaded) && loaded != null)
            return loaded;

        return null;
    }
}
