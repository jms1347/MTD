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

        RegisterPool(assets.projectilePrefab, 24, 8);
        RegisterPool(assets.playerMissilePrefab, 20, 6);
        RegisterPool(assets.rangedMonsterPrefab, 12, 4);
        RegisterPool(assets.suicideMonsterPrefab, 12, 4);
        RegisterPool(assets.meleeMonsterPrefab, 12, 4);
        RegisterPool(assets.bossPrefab, 2, 1);
        RegisterPool(assets.goldPickupPrefab, 48, 12);
    }

    public NetworkObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!IsServerActive() || prefab == null)
            return null;

        if (!pools.TryGetValue(prefab, out var pool))
            pool = RegisterPool(prefab, 4, 2);

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
