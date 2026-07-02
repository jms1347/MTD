using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타워가 발사하는 미사일 오브젝트 풀을 관리하는 싱글톤 매니저.
/// 미사일 종류(색상)별로 별도 풀을 유지합니다.
/// </summary>
public class MissilePoolManager : Singleton<MissilePoolManager>
{
    public const string PoolRootName = "미사일POOL";
    public const string ActivePoolName = "Active";
    public const string VfxPoolName = "이펙트POOL";
    public const string VfxActivePoolName = "Active";

    [Header("오브젝트 풀")]
    [Tooltip("게임 시작 시 미사일 프리팹 종류마다 미리 생성해 둘 개수입니다.\n타워 수와 연사 속도에 맞게 넉넉히 설정하세요.")]
    [SerializeField] private int initialPoolSize = 50;

    [Tooltip("풀에 남은 미사일이 없을 때 한 번에 추가 생성하는 개수입니다.")]
    [SerializeField] private int expandSize = 1;

    [Header("이펙트 풀")]
    [SerializeField] private int initialVfxPoolSize = 12;
    [SerializeField] private int expandVfxPoolSize = 4;

    private readonly Dictionary<int, GameObjectPool> poolsByPrefab = new();
    private Transform poolRoot;
    private Transform activeRoot;
    private Transform vfxPoolRoot;
    private Transform vfxActiveRoot;
    private DefenseVfxPool vfxPool;
    private bool isInitialized;

    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// DefenseSceneSetup에서 로켓 미사일 프리팹 목록을 넘겨 풀을 초기화합니다.
    /// </summary>
    public void Initialize(IEnumerable<GameObject> missilePrefabs)
    {
        if (isInitialized)
            return;

        EnsurePoolRoot();

        foreach (var prefab in missilePrefabs)
        {
            if (prefab == null || poolsByPrefab.ContainsKey(prefab.GetInstanceID()))
                continue;

            var template = PrepareMissileTemplate(prefab);
            var prefabPoolRoot = GetOrCreatePrefabPoolRoot(prefab.name);
            poolsByPrefab[prefab.GetInstanceID()] = new GameObjectPool(template, prefabPoolRoot, initialPoolSize, expandSize);
        }

        isInitialized = true;
    }

    public DefenseProjectile Spawn(
        GameObject missilePrefab,
        Vector3 position,
        Quaternion rotation,
        float damage,
        Vector3 velocity,
        DamageElement element)
    {
        if (missilePrefab == null)
            return null;

        EnsurePoolRoot();

        if (!poolsByPrefab.TryGetValue(missilePrefab.GetInstanceID(), out var pool))
        {
            var template = PrepareMissileTemplate(missilePrefab);
            var prefabPoolRoot = GetOrCreatePrefabPoolRoot(missilePrefab.name);
            pool = new GameObjectPool(template, prefabPoolRoot, initialPoolSize, expandSize);
            poolsByPrefab[missilePrefab.GetInstanceID()] = pool;
        }

        var missileObject = pool.Get();
        missileObject.SetActive(false);
        missileObject.transform.SetParent(activeRoot, true);
        missileObject.transform.SetPositionAndRotation(position, rotation);
        missileObject.SetActive(true);

        var projectile = missileObject.GetComponent<DefenseProjectile>();
        projectile.BindSourcePrefab(missilePrefab);
        projectile.Launch(damage, velocity, element);
        return projectile;
    }

    public DefenseProjectile SpawnScatterRock(
        GameObject missilePrefab,
        Vector3 position,
        Quaternion rotation,
        float damage,
        Vector3 velocity,
        Vector3 landPoint,
        float splashRadius,
        float visualScale = 0.52f,
        DefenseSkillData strikeSkill = null,
        bool enableFallHoming = false,
        Vector3 homingSearchOrigin = default,
        float homingSearchRange = 0f,
        string targetMobility = null,
        float fallHomingSpeedMultiplier = 1f)
    {
        if (missilePrefab == null)
            return null;

        EnsurePoolRoot();

        if (!poolsByPrefab.TryGetValue(missilePrefab.GetInstanceID(), out var pool))
        {
            var template = PrepareMissileTemplate(missilePrefab);
            if (template == null)
                return null;

            var prefabPoolRoot = GetOrCreatePrefabPoolRoot(missilePrefab.name);
            pool = new GameObjectPool(template, prefabPoolRoot, initialPoolSize, expandSize);
            poolsByPrefab[missilePrefab.GetInstanceID()] = pool;
        }

        var missileObject = pool.Get();
        missileObject.SetActive(false);
        missileObject.transform.SetParent(activeRoot, true);
        missileObject.transform.SetPositionAndRotation(position, rotation);
        missileObject.SetActive(true);

        var projectile = missileObject.GetComponent<DefenseProjectile>();
        projectile.BindSourcePrefab(missilePrefab);
        projectile.LaunchScatterRock(
            damage,
            velocity,
            landPoint,
            splashRadius,
            visualScale,
            strikeSkill,
            enableFallHoming,
            homingSearchOrigin,
            homingSearchRange,
            targetMobility,
            fallHomingSpeedMultiplier);
        return projectile;
    }

    public DefenseProjectile SpawnWithSkill(
        GameObject missilePrefab,
        Vector3 position,
        Quaternion rotation,
        float damage,
        Vector3 velocity,
        DamageElement element,
        DefenseSkillProjectileContext context)
    {
        if (missilePrefab == null)
            return null;

        EnsurePoolRoot();

        if (!poolsByPrefab.TryGetValue(missilePrefab.GetInstanceID(), out var pool))
        {
            var template = PrepareMissileTemplate(missilePrefab);
            if (template == null)
                return null;

            var prefabPoolRoot = GetOrCreatePrefabPoolRoot(missilePrefab.name);
            pool = new GameObjectPool(template, prefabPoolRoot, initialPoolSize, expandSize);
            poolsByPrefab[missilePrefab.GetInstanceID()] = pool;
        }

        var missileObject = pool.Get();
        missileObject.SetActive(false);
        missileObject.transform.SetParent(activeRoot, true);
        missileObject.transform.SetPositionAndRotation(position, rotation);
        missileObject.SetActive(true);

        var projectile = missileObject.GetComponent<DefenseProjectile>();
        projectile.BindSourcePrefab(missilePrefab);
        projectile.LaunchWithSkill(damage, velocity, element, context);
        return projectile;
    }

    public DefenseProjectile Spawn(
        DefenseMissileId missileId,
        Vector3 position,
        Quaternion rotation,
        float damage,
        Vector3 velocity)
    {
        var prefab = DefenseMissileResolver.GetPrefab(missileId);
        if (prefab == null)
            return null;

        return Spawn(prefab, position, rotation, damage, velocity, DefenseMissileResolver.GetElement(missileId));
    }

    [System.Obsolete("Use Spawn with DamageElement or DefenseMissileId")]
    public DefenseProjectile Spawn(
        GameObject missilePrefab,
        Vector3 position,
        Quaternion rotation,
        float damage,
        Vector3 velocity)
    {
        var element = DamageElement.Blue;
        if (DefenseMissileResolver.TryResolveId(missilePrefab, out var missileId))
            element = DefenseMissileResolver.GetElement(missileId);

        return Spawn(missilePrefab, position, rotation, damage, velocity, element);
    }

    public void Release(GameObject missilePrefab, GameObject missileInstance)
    {
        if (missilePrefab == null || missileInstance == null)
            return;

        if (!poolsByPrefab.TryGetValue(missilePrefab.GetInstanceID(), out var pool))
        {
            Destroy(missileInstance);
            return;
        }

        pool.Release(missileInstance);
    }

    public GameObject PlayVfxAttached(GameObject prefab, Transform parent)
    {
        return EnsureVfxPool().PlayAttached(prefab, parent);
    }

    public void PlayVfxAt(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        EnsureVfxPool().PlayAt(prefab, position, rotation, lifetime);
    }

    public void ReleaseVfx(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null)
            return;

        EnsureVfxPool().Release(prefab, instance);
    }

    public void ReleaseVfxAfter(GameObject prefab, GameObject instance, float lifetime)
    {
        if (prefab == null || instance == null)
            return;

        EnsureVfxPool().ReleaseDetachedAfter(prefab, instance, lifetime);
    }

    private DefenseVfxPool EnsureVfxPool()
    {
        EnsurePoolRoot();
        if (vfxPool != null)
            return vfxPool;

        vfxPool = new DefenseVfxPool(
            vfxPoolRoot,
            vfxActiveRoot,
            this,
            initialVfxPoolSize,
            expandVfxPoolSize);
        return vfxPool;
    }

    private void EnsurePoolRoot()
    {
        if (poolRoot != null && activeRoot != null && vfxPoolRoot != null && vfxActiveRoot != null)
            return;

        var existingRoot = transform.Find(PoolRootName);
        if (existingRoot != null)
        {
            poolRoot = existingRoot;
        }
        else
        {
            var rootObject = new GameObject(PoolRootName);
            rootObject.transform.SetParent(transform, false);
            poolRoot = rootObject.transform;
        }

        activeRoot = EnsureChild(poolRoot, ActivePoolName);
        vfxPoolRoot = EnsureChild(poolRoot, VfxPoolName);
        vfxActiveRoot = EnsureChild(vfxPoolRoot, VfxActivePoolName);
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        var existing = parent.Find(childName);
        if (existing != null)
            return existing;

        var childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private Transform GetOrCreatePrefabPoolRoot(string prefabName)
    {
        EnsurePoolRoot();

        string containerName = $"{prefabName}_Pool";
        var existing = poolRoot.Find(containerName);
        if (existing != null)
            return existing;

        var container = new GameObject(containerName);
        container.transform.SetParent(poolRoot, false);
        return container.transform;
    }

    private GameObject PrepareMissileTemplate(GameObject prefab)
    {
        EnsurePoolRoot();

        if (prefab == null)
            return null;

        var prefabPoolRoot = GetOrCreatePrefabPoolRoot(prefab.name);
        GameObject template;
        try
        {
            template = Instantiate(prefab, prefabPoolRoot);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MissilePoolManager] 미사일 프리팹 인스턴스화 실패 '{prefab.name}': {ex.Message}");
            return null;
        }

        template.name = $"{prefab.name}_Pooled";
        template.SetActive(false);

        var legacyProjectile = template.GetComponent<ETFXProjectileScript>();
        var defenseProjectile = template.GetComponent<DefenseProjectile>();
        if (defenseProjectile == null)
            defenseProjectile = template.AddComponent<DefenseProjectile>();

        if (legacyProjectile != null)
        {
            defenseProjectile.SetBaseConfig(
                legacyProjectile.impactParticle,
                legacyProjectile.projectileParticle,
                legacyProjectile.muzzleParticle,
                legacyProjectile.colliderRadius,
                legacyProjectile.collideOffset);
            Destroy(legacyProjectile);
        }

        return template;
    }
}
