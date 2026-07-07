using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 클라이언트 VFX 전용 오브젝트 풀. Instantiate/Destroy 스파이크를 줄인다.
/// </summary>
public static class CwslVfxPool
{
    private const string RootName = "VfxPOOL";
    private const int HighChurnWarmCount = CwslGameConstants.PoolHighChurnInitialSize;
    private const int MediumChurnWarmCount = 24;

    private static Transform poolRoot;
    private static Transform activeRoot;
    private static readonly Dictionary<int, CwslGameObjectPool> poolsByPrefabId = new();
    private static readonly Dictionary<int, Transform> bucketsByPrefabId = new();
    private static readonly HashSet<GameObject> warmedPrefabs = new();

    public static void WarmFromAssets(CwslGameAssets assets)
    {
        if (assets == null)
            return;

        EnsureHierarchy();

        WarmPrefab(assets.playerMissileVfx, HighChurnWarmCount);
        WarmPrefab(assets.gunMuzzleVfx, HighChurnWarmCount);
        WarmPrefab(assets.meleeHitVfx, HighChurnWarmCount);
        WarmPrefab(assets.enemyDeathVfx, HighChurnWarmCount);
        WarmPrefab(assets.suicideExplosionVfx, HighChurnWarmCount);
        WarmPrefab(assets.missileTankFireAmmoVfx, HighChurnWarmCount);
        WarmPrefab(assets.missileTankPoisonAmmoVfx, HighChurnWarmCount);
        WarmPrefab(assets.missileTankLightningAmmoVfx, HighChurnWarmCount);
        WarmPrefab(assets.missileTankSmokeBombVfx, HighChurnWarmCount);
        WarmPrefab(assets.missileTankSmokeZoneVfx, HighChurnWarmCount);
        WarmPrefab(assets.missileTankSmokeDashTrailVfx, HighChurnWarmCount);
        WarmPrefab(assets.meteorImpactVfx, HighChurnWarmCount);
        WarmPrefab(assets.meteorGroundFireSoftAbVfx, HighChurnWarmCount);
        WarmPrefab(assets.meteorGroundFireSoftBigVfx, HighChurnWarmCount);
        WarmPrefab(assets.meteorGroundFireAdditiveVfx, HighChurnWarmCount);
        WarmPrefab(assets.redMageLightningOrbVfx, HighChurnWarmCount);
        WarmPrefab(assets.redMageLightningOrbRadiusVfx, HighChurnWarmCount);
        WarmPrefab(assets.redMageLightningBoltVfx, HighChurnWarmCount);
        WarmPrefab(assets.redMageLightningStrikeVfx, HighChurnWarmCount);
        WarmPrefab(assets.redMageLightningStrikeTallVfx, HighChurnWarmCount);
        WarmPrefab(assets.redMageLightningExplosionVfx, HighChurnWarmCount);
        WarmPrefab(assets.redMageTeleportPortalVfx, HighChurnWarmCount);
        WarmPrefab(assets.frozenOrbIceBallVfx, HighChurnWarmCount);
        WarmPrefab(assets.frozenOrbHitAirVfx, HighChurnWarmCount);
        WarmPrefab(assets.frozenOrbGroundTrailVfx, HighChurnWarmCount);
        WarmPrefab(assets.darkMissileVfx, HighChurnWarmCount);
        WarmPrefab(assets.rangedTankProjectileVfx, HighChurnWarmCount);
        WarmPrefab(assets.rangedTankMuzzleVfx, HighChurnWarmCount);
        WarmPrefab(assets.rangedTankProjectileHitVfx, HighChurnWarmCount);
        WarmPrefab(assets.shadowProjectileHitVfx, HighChurnWarmCount);
        WarmPrefab(assets.shadowMuzzleVfx, HighChurnWarmCount);
        WarmPrefab(assets.monsterBurnStatusVfx, MediumChurnWarmCount);
        WarmPrefab(assets.monsterSlowStatusVfx, MediumChurnWarmCount);
        WarmPrefab(assets.monsterShockStatusVfx, MediumChurnWarmCount);
        WarmPrefab(assets.monsterPoisonStatusVfx, MediumChurnWarmCount);
        WarmPrefab(assets.playerDeathVfx, MediumChurnWarmCount);
        WarmPrefab(assets.bossDeathVfx, MediumChurnWarmCount);
        WarmPrefab(assets.fakeGoldExplosionVfx, MediumChurnWarmCount);
        WarmPrefab(assets.goldBurstVfx, MediumChurnWarmCount);
    }

    public static GameObject Acquire(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        var pool = EnsurePool(prefab);
        if (pool == null)
            return null;

        var instance = pool.Get();
        instance.transform.SetParent(activeRoot, true);
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.transform.localScale = Vector3.one;

        var handle = instance.GetComponent<CwslPooledVfxHandle>();
        if (handle == null)
            handle = instance.AddComponent<CwslPooledVfxHandle>();
        handle.Bind(prefab);
        handle.CancelAutoRelease();
        CwslVfxSpawner.PrepareReusedEffect(instance);
        return instance;
    }

    public static void ScheduleRelease(GameObject instance, float lifetimeSeconds)
    {
        if (instance == null || lifetimeSeconds <= 0f)
            return;

        var handle = instance.GetComponent<CwslPooledVfxHandle>();
        if (handle == null)
            handle = instance.AddComponent<CwslPooledVfxHandle>();
        handle.ScheduleAutoRelease(lifetimeSeconds);
    }

    public static void Release(GameObject instance)
    {
        if (instance == null)
            return;

        var handle = instance.GetComponent<CwslPooledVfxHandle>();
        if (handle == null || handle.SourcePrefab == null)
        {
            Object.Destroy(instance);
            return;
        }

        handle.CancelAutoRelease();
        instance.transform.SetParent(GetBucket(handle.SourcePrefab), false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        if (poolsByPrefabId.TryGetValue(handle.SourcePrefab.GetInstanceID(), out var pool))
            pool.Release(instance);
        else
            Object.Destroy(instance);
    }

    private static void WarmPrefab(GameObject prefab, int count)
    {
        if (prefab == null || !warmedPrefabs.Add(prefab))
            return;

        var pool = EnsurePool(prefab);
        if (pool == null)
            return;

        if (count > CwslGameConstants.PoolHighChurnInitialSize)
            pool.Prewarm(count - CwslGameConstants.PoolHighChurnInitialSize);
    }

    private static CwslGameObjectPool EnsurePool(GameObject prefab)
    {
        if (prefab == null)
            return null;

        EnsureHierarchy();
        var prefabId = prefab.GetInstanceID();
        if (poolsByPrefabId.TryGetValue(prefabId, out var existing))
            return existing;

        var bucket = GetBucket(prefab);
        var pool = new CwslGameObjectPool(
            prefab,
            bucket,
            CwslGameConstants.PoolHighChurnInitialSize,
            CwslGameConstants.PoolHighChurnExpandSize);
        poolsByPrefabId[prefabId] = pool;
        return pool;
    }

    private static Transform GetBucket(GameObject prefab)
    {
        var prefabId = prefab.GetInstanceID();
        if (bucketsByPrefabId.TryGetValue(prefabId, out var bucket))
            return bucket;

        EnsureHierarchy();
        bucket = new GameObject(prefab.name).transform;
        bucket.SetParent(poolRoot, false);
        bucketsByPrefabId[prefabId] = bucket;
        return bucket;
    }

    private static void EnsureHierarchy()
    {
        if (poolRoot != null)
            return;

        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            poolRoot = existing.transform;
            activeRoot = poolRoot.Find("Active") ?? CreateChild(poolRoot, "Active");
            return;
        }

        poolRoot = new GameObject(RootName).transform;
        activeRoot = CreateChild(poolRoot, "Active");
        Object.DontDestroyOnLoad(poolRoot.gameObject);
    }

    private static Transform CreateChild(Transform parent, string childName)
    {
        var child = new GameObject(childName).transform;
        child.SetParent(parent, false);
        return child;
    }
}
