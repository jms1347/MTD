using System.Collections.Generic;
using UnityEngine;

public class CwslGameObjectPool
{
    private readonly GameObject prefab;
    private readonly Transform poolRoot;
    private readonly Queue<GameObject> available = new();
    private readonly int expandSize;

    public CwslGameObjectPool(GameObject prefab, Transform poolRoot, int initialSize = 8, int expandSize = 4)
    {
        this.prefab = prefab;
        this.poolRoot = poolRoot;
        this.expandSize = Mathf.Max(1, expandSize);

        for (var i = 0; i < initialSize; i++)
            available.Enqueue(CreateInstance());
    }

    public GameObject Get()
    {
        if (available.Count == 0)
        {
            for (var i = 0; i < expandSize; i++)
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
        instance.transform.localScale = Vector3.one;
        available.Enqueue(instance);
    }

    public void Prewarm(int additionalCount)
    {
        if (additionalCount <= 0)
            return;

        for (var i = 0; i < additionalCount; i++)
            available.Enqueue(CreateInstance());
    }

    private GameObject CreateInstance()
    {
        var instance = Object.Instantiate(prefab, poolRoot);
        instance.SetActive(false);
        return instance;
    }
}
