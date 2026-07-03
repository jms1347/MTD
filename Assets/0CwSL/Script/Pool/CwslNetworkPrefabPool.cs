using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CwslNetworkPrefabPool
{
    private static readonly Vector3 HiddenPosition = new(0f, -500f, 0f);

    private readonly GameObject prefab;
    private readonly Queue<NetworkObject> available = new();
    private readonly int expandSize;

    public CwslNetworkPrefabPool(
        GameObject sourcePrefab,
        Transform inactiveRoot,
        Transform activeRoot,
        int initialSize,
        int expandSize)
    {
        prefab = sourcePrefab;
        // inactiveRoot/activeRoot는 폴더 표시용. NetworkObject는 비스폰 상태에서 부모 변경 불가.
        this.expandSize = Mathf.Max(1, expandSize);

        for (var i = 0; i < initialSize; i++)
            available.Enqueue(CreateInstance());
    }

    public NetworkObject Get(Vector3 position, Quaternion rotation)
    {
        if (available.Count == 0)
        {
            for (var i = 0; i < expandSize; i++)
                available.Enqueue(CreateInstance());
        }

        var networkObject = available.Dequeue();
        var instance = networkObject.gameObject;
        instance.name = prefab.name;
        instance.SetActive(true);
        instance.transform.SetPositionAndRotation(position, rotation);
        NotifyPoolLifecycle(instance, spawned: true);

        if (!networkObject.IsSpawned)
            networkObject.Spawn(true);

        instance.transform.SetPositionAndRotation(position, rotation);
        return networkObject;
    }

    public void Release(NetworkObject networkObject)
    {
        if (networkObject == null)
            return;

        NotifyPoolLifecycle(networkObject.gameObject, spawned: false);

        if (networkObject.IsSpawned)
        {
            // 부모가 있으면 디스폰 전에 해제 (비스폰 후 SetParent 금지)
            if (networkObject.transform.parent != null)
                networkObject.TrySetParent((NetworkObject)null, worldPositionStays: true);

            networkObject.Despawn(false);
        }

        networkObject.transform.SetPositionAndRotation(HiddenPosition, Quaternion.identity);
        networkObject.gameObject.name = $"{prefab.name}_Pooled";
        networkObject.gameObject.SetActive(false);
        available.Enqueue(networkObject);
    }

    private NetworkObject CreateInstance()
    {
        var instance = Object.Instantiate(prefab, HiddenPosition, Quaternion.identity);
        instance.name = $"{prefab.name}_Pooled";

        var identity = instance.GetComponent<CwslPooledNetworkIdentity>();
        if (identity == null)
            identity = instance.AddComponent<CwslPooledNetworkIdentity>();
        identity.SourcePrefab = prefab;

        var networkObject = instance.GetComponent<NetworkObject>();
        instance.SetActive(false);
        return networkObject;
    }

    private static void NotifyPoolLifecycle(GameObject instance, bool spawned)
    {
        var poolables = instance.GetComponentsInChildren<ICwslPooledNetworkObject>(true);
        foreach (var poolable in poolables)
        {
            if (spawned)
                poolable.OnSpawnedFromPool();
            else
                poolable.OnReturnedToPool();
        }
    }
}
