using System.Collections.Generic;
using UnityEngine;

public class GameObjectPool
{
    private readonly GameObject prefab;
    private readonly Transform poolRoot;
    private readonly Queue<GameObject> available = new();
    private readonly int expandSize;

    public Transform PoolRoot => poolRoot;
    public int AvailableCount => available.Count;

    public GameObjectPool(GameObject prefab, Transform poolRoot, int initialSize = 8, int expandSize = 4)
    {
        this.prefab = prefab;
        this.poolRoot = poolRoot;
        this.expandSize = Mathf.Max(1, expandSize);

        for (int i = 0; i < initialSize; i++)
            available.Enqueue(CreateInstance());
    }

    public GameObject Get()
    {
        if (available.Count == 0)
        {
            for (int i = 0; i < expandSize; i++)
                available.Enqueue(CreateInstance());
        }

        var instance = available.Dequeue();
        instance.SetActive(true);
        return instance;
    }

    public void Release(GameObject instance)
    {
        if (instance == null)
            return;

        instance.SetActive(false);
        instance.transform.SetParent(poolRoot, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        available.Enqueue(instance);
    }

    private GameObject CreateInstance()
    {
        var instance = Object.Instantiate(prefab, poolRoot);
        instance.SetActive(false);
        return instance;
    }
}
