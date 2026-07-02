using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 초당 근접 아군 유닛을 소환해 적의 진격을 막는 타워.
/// </summary>
public class SummonTowerController : MonoBehaviour
{
    [Header("소환")]
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private int maxMinions = 14;
    [SerializeField] private float spawnRadius = 1.4f;
    [SerializeField] private float minionHealth = 4f;
    [SerializeField] private float minionSpeed = 3.8f;
    [SerializeField] private float minionDamage = 1f;
    [SerializeField] private float minionScale = 0.75f;
    [SerializeField] private Color minionColor = new Color(0.25f, 0.82f, 0.38f);

    private float nextSpawnTime;
    private Transform minionRoot;
    private readonly Queue<GameObject> minionPool = new();
    private readonly List<GameObject> activeMinions = new();
    private GameObject minionTemplate;
    private float groundY;

    public void ApplyStats(SummonTowerStats stats)
    {
        if (stats == null)
            return;

        spawnInterval = stats.spawnInterval;
        maxMinions = stats.maxMinions;
        minionHealth = stats.minionHealth;
        minionSpeed = stats.minionSpeed;
        minionDamage = stats.minionDamage;
        minionScale = stats.minionScale;
    }

    public void Initialize()
    {
        if (TowerStatsManager.Instance != null)
            TowerStatsManager.Instance.ApplyTo(this);
    }

    private void Start()
    {
        groundY = minionScale * 0.5f;
        EnsureMinionRoot();
        EnsureMinionTemplate();
        nextSpawnTime = Time.time + 0.4f;
    }

    private void Update()
    {
        CleanupInactiveMinions();

        if (!CanSummon())
            return;

        if (activeMinions.Count >= maxMinions || Time.time < nextSpawnTime)
            return;

        SpawnMinion();
        nextSpawnTime = Time.time + spawnInterval;
    }

    private static bool CanSummon()
    {
        if (DefenseStageTimerManager.Instance == null)
            return true;

        return DefenseStageTimerManager.Instance.IsBattlePhase;
    }

    public void ReleaseMinion(GameObject minion)
    {
        if (minion == null)
            return;

        activeMinions.Remove(minion);
        minion.SetActive(false);
        minion.transform.SetParent(minionRoot, false);
        minionPool.Enqueue(minion);
    }

    private void SpawnMinion()
    {
        EnsureMinionRoot();
        EnsureMinionTemplate();

        GameObject minion = minionPool.Count > 0 ? minionPool.Dequeue() : Instantiate(minionTemplate, minionRoot);
        minion.SetActive(true);

        Vector2 offset2D = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(offset2D.x, groundY, offset2D.y);
        minion.transform.position = spawnPos;
        minion.transform.localScale = Vector3.one * minionScale;

        var controller = minion.GetComponent<MinionController>();
        controller.Initialize(this, minionHealth, minionSpeed, minionDamage);
        activeMinions.Add(minion);
    }

    private void CleanupInactiveMinions()
    {
        for (int i = activeMinions.Count - 1; i >= 0; i--)
        {
            if (activeMinions[i] == null || !activeMinions[i].activeInHierarchy)
                activeMinions.RemoveAt(i);
        }
    }

    private void EnsureMinionRoot()
    {
        if (minionRoot != null)
            return;

        var existing = transform.Find("MinionPool");
        minionRoot = existing != null ? existing : new GameObject("MinionPool").transform;
        minionRoot.SetParent(transform, false);
    }

    private void EnsureMinionTemplate()
    {
        if (minionTemplate != null)
            return;

        minionTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        minionTemplate.name = "MinionTemplate";
        minionTemplate.tag = "AllyMinion";
        minionTemplate.SetActive(false);
        minionTemplate.transform.SetParent(minionRoot, false);

        var renderer = minionTemplate.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = minionColor;
            renderer.material = material;
        }

        var collider = minionTemplate.GetComponent<SphereCollider>();
        if (collider != null)
            collider.isTrigger = false;

        minionTemplate.AddComponent<Rigidbody>().isKinematic = true;
        minionTemplate.AddComponent<Health>();
        minionTemplate.AddComponent<MinionController>();
        minionTemplate.AddComponent<UnitGridNavigator>();
        minionTemplate.AddComponent<HealthBarUI>().ConfigureAsAlly();
    }
}
