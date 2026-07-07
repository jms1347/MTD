using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CwslNetworkPoolService : MonoBehaviour
{
    public static CwslNetworkPoolService Instance { get; private set; }

    private readonly Dictionary<GameObject, CwslNetworkPrefabPool> pools = new();
    private Transform gamePoolRoot;
    private Transform inactiveRoot;
    private Transform activeRoot;

    public Transform GamePoolRoot => gamePoolRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureHierarchy();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Initialize(CwslGameAssets assets)
    {
        EnsureHierarchy();
        if (assets == null)
            return;

        var high = CwslGameConstants.PoolHighChurnInitialSize;
        var highExpand = CwslGameConstants.PoolHighChurnExpandSize;
        var boss = CwslGameConstants.PoolBossInitialSize;
        var bossExpand = CwslGameConstants.PoolBossExpandSize;

        RegisterPool(assets.projectilePrefab, high, highExpand);
        RegisterPool(assets.playerMissilePrefab, high, highExpand);
        RegisterPool(assets.rangedMonsterPrefab, high, highExpand);
        RegisterPool(assets.suicideMonsterPrefab, high, highExpand);
        RegisterPool(assets.meleeMonsterPrefab, high, highExpand);
        RegisterPool(assets.koreaUniversitySoldierPrefab, high, highExpand);
        RegisterPool(assets.stickySuicideMonsterPrefab, high, highExpand);
        RegisterPool(assets.midBossMonsterPrefab, high, highExpand);
        RegisterPool(assets.defenseBossMonsterPrefab, high, highExpand);
        RegisterPool(assets.goldPickupPrefab, high, highExpand);
        RegisterPool(assets.pillPickupPrefab, high, highExpand);
        RegisterPool(assets.bossPrefab, boss, bossExpand);
    }

    public NetworkObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!IsServerActive() || prefab == null)
            return null;

        if (!pools.TryGetValue(prefab, out var pool))
        {
            pool = RegisterPool(
                prefab,
                CwslGameConstants.PoolFallbackInitialSize,
                CwslGameConstants.PoolFallbackExpandSize);
        }

        return pool?.Get(position, rotation);
    }

    public void Release(NetworkObject networkObject)
    {
        if (!IsServerActive() || networkObject == null)
            return;

        var identity = networkObject.GetComponent<CwslPooledNetworkIdentity>();
        if (identity == null || identity.SourcePrefab == null)
        {
            if (networkObject.IsSpawned)
                networkObject.Despawn(true);
            return;
        }

        if (pools.TryGetValue(identity.SourcePrefab, out var pool))
            pool.Release(networkObject);
        else if (networkObject.IsSpawned)
            networkObject.Despawn(true);
    }

    private void EnsureHierarchy()
    {
        if (gamePoolRoot != null)
            return;

        var existing = GameObject.Find("GamePool");
        gamePoolRoot = existing != null
            ? existing.transform
            : new GameObject("GamePool").transform;

        inactiveRoot = EnsureChild(gamePoolRoot, "Inactive");
        activeRoot = EnsureChild(gamePoolRoot, "Active");
    }

    private CwslNetworkPrefabPool RegisterPool(GameObject prefab, int initialSize, int expandSize)
    {
        if (prefab == null)
            return null;

        if (pools.TryGetValue(prefab, out var existing))
            return existing;

        EnsureHierarchy();

        var inactiveBucket = EnsureChild(inactiveRoot, prefab.name);
        var activeBucket = EnsureChild(activeRoot, prefab.name);
        var pool = new CwslNetworkPrefabPool(prefab, inactiveBucket, activeBucket, initialSize, expandSize);
        pools[prefab] = pool;
        return pool;
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        var existing = parent.Find(childName);
        if (existing != null)
            return existing;

        var child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static bool IsServerActive()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
    }
}
