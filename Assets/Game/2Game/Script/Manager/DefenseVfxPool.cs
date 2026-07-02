using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미사일 임팩트·머즐·트레일 등 단발/부착 VFX 오브젝트 풀.
/// </summary>
public class DefenseVfxPool
{
    private readonly Dictionary<int, GameObjectPool> poolsByPrefab = new();
    private readonly Transform poolRoot;
    private readonly Transform activeRoot;
    private readonly MonoBehaviour coroutineHost;
    private readonly int initialSize;
    private readonly int expandSize;

    public DefenseVfxPool(
        Transform poolRoot,
        Transform activeRoot,
        MonoBehaviour coroutineHost,
        int initialSize = 12,
        int expandSize = 4)
    {
        this.poolRoot = poolRoot;
        this.activeRoot = activeRoot;
        this.coroutineHost = coroutineHost;
        this.initialSize = Mathf.Max(1, initialSize);
        this.expandSize = Mathf.Max(1, expandSize);
    }

    public GameObject PlayAttached(GameObject prefab, Transform parent)
    {
        if (prefab == null || parent == null)
            return null;

        var instance = GetInstance(prefab);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        RestartVisuals(instance);
        return instance;
    }

    public void PlayAt(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (prefab == null)
            return;

        var instance = GetInstance(prefab);
        instance.transform.SetParent(activeRoot, false);
        instance.transform.SetPositionAndRotation(position, rotation);
        RestartVisuals(instance);

        if (lifetime > 0f && coroutineHost != null && coroutineHost.isActiveAndEnabled)
            coroutineHost.StartCoroutine(ReleaseAfter(prefab, instance, lifetime));
    }

    public void Release(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null)
            return;

        StopVisuals(instance);

        if (!poolsByPrefab.TryGetValue(prefab.GetInstanceID(), out var pool))
        {
            Object.Destroy(instance);
            return;
        }

        pool.Release(instance);
    }

    public void ReleaseDetachedAfter(GameObject prefab, GameObject instance, float lifetime)
    {
        if (prefab == null || instance == null)
            return;

        if (lifetime <= 0f)
        {
            Release(prefab, instance);
            return;
        }

        instance.transform.SetParent(activeRoot, true);
        if (coroutineHost != null && coroutineHost.isActiveAndEnabled)
            coroutineHost.StartCoroutine(ReleaseAfter(prefab, instance, lifetime));
        else
            Release(prefab, instance);
    }

    private GameObject GetInstance(GameObject prefab)
    {
        var pool = GetOrCreatePool(prefab);
        var instance = pool.Get();
        instance.SetActive(true);
        return instance;
    }

    private GameObjectPool GetOrCreatePool(GameObject prefab)
    {
        int key = prefab.GetInstanceID();
        if (poolsByPrefab.TryGetValue(key, out var existing))
            return existing;

        var prefabPoolRoot = GetOrCreatePrefabPoolRoot(prefab.name);
        var template = Object.Instantiate(prefab, prefabPoolRoot);
        template.name = $"{prefab.name}_VfxPooled";
        template.SetActive(false);
        StopVisuals(template);

        var pool = new GameObjectPool(template, prefabPoolRoot, initialSize, expandSize);
        poolsByPrefab[key] = pool;
        return pool;
    }

    private Transform GetOrCreatePrefabPoolRoot(string prefabName)
    {
        string containerName = $"{prefabName}_VfxPool";
        var existing = poolRoot.Find(containerName);
        if (existing != null)
            return existing;

        var container = new GameObject(containerName);
        container.transform.SetParent(poolRoot, false);
        return container.transform;
    }

    private IEnumerator ReleaseAfter(GameObject prefab, GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        Release(prefab, instance);
    }

    private static void RestartVisuals(GameObject root)
    {
        if (root == null)
            return;

        root.SetActive(true);

        var systems = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            ps.Clear(true);
            ps.Play(true);
        }

        var animators = root.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            var animator = animators[i];
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private static void StopVisuals(GameObject root)
    {
        if (root == null)
            return;

        var systems = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
            systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        root.SetActive(false);
    }
}
