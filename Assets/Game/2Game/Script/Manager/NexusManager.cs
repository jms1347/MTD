using UnityEngine;

/// <summary>
/// 중앙 넥서스(기지) 생성·체력을 관리하는 싱글톤 매니저.
/// 적이 넥서스 HP를 0으로 만들면 게임 오버 처리의 기준이 됩니다.
/// </summary>
public class NexusManager : Singleton<NexusManager>
{
    public const string PoolRootName = "넥서스POOL";

    [Header("넥서스 설정")]
    [Tooltip("넥서스의 최대 체력(HP)입니다.\n적의 공격으로 0이 되면 패배 조건으로 사용할 수 있습니다.")]
    [SerializeField] private float nexusHealth = 10000f;

    [Tooltip("넥서스가 배치될 월드 좌표(맵 중심)입니다.\n카메라 추적·적 이동 목표의 기준점이 됩니다.")]
    [SerializeField] private Vector3 nexusCenter = Vector3.zero;

    private Transform nexusPoolRoot;
    private bool isSceneBuilt;

    public Transform NexusTransform { get; private set; }

    /// <summary>넥서스 비주얼 꼭대기(월드 Y).</summary>
    public float VisualTopWorldY =>
        (NexusTransform != null ? NexusTransform.position.y : nexusCenter.y) + 3.2f;

    /// <summary>공중 유닛이 비행하는 고도 — 넥서스 꼭대기 + 여유.</summary>
    public static float GetAirCruiseAltitude(float marginAboveNexusTop = 2.5f)
    {
        if (Instance != null)
            return Instance.VisualTopWorldY + marginAboveNexusTop;

        return 3.2f + marginAboveNexusTop;
    }

    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// 씬에 넥서스 오브젝트를 한 번만 생성합니다.
    /// </summary>
    public void BuildScene(Vector3 center, float health)
    {
        nexusCenter = center;
        nexusHealth = health;

        EnsurePoolRoot();

        if (!isSceneBuilt)
        {
            CreateNexus();
            isSceneBuilt = true;
            return;
        }

        RefreshExistingNexusHealth(health);
    }

    private void RefreshExistingNexusHealth(float health)
    {
        nexusHealth = health;
        var nexus = nexusPoolRoot != null ? nexusPoolRoot.Find("Nexus") : null;
        if (nexus == null)
        {
            CreateNexus();
            return;
        }

        NexusTransform = nexus;
        nexus.position = nexusCenter;
        EnsureNexusHealthBar(nexus.gameObject);
    }

    private void CreateNexus()
    {
        var existing = nexusPoolRoot.Find("Nexus");
        if (existing != null)
        {
            NexusTransform = existing;
            existing.position = nexusCenter;
            EnsureNexusHealthBar(existing.gameObject);
            return;
        }

        var nexus = new GameObject("Nexus");
        nexus.tag = "Nexus";
        nexus.transform.SetParent(nexusPoolRoot, false);
        nexus.transform.position = nexusCenter;

        nexus.AddComponent<Nexus>();
        BuildNexusVisuals(nexus.transform);

        var collider = nexus.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.center = new Vector3(0f, 1.1f, 0f);
        collider.size = new Vector3(2.8f, 2.4f, 2.8f);

        EnsureNexusHealthBar(nexus);
        NexusTransform = nexus.transform;
    }

    private void EnsureNexusHealthBar(GameObject nexus)
    {
        CombatDamagePopupPool.EnsureReady();

        var health = nexus.GetComponent<Health>();
        if (health == null)
            health = nexus.AddComponent<Health>();
        health.Initialize(nexusHealth);

        var healthBar = nexus.GetComponent<HealthBarUI>();
        if (healthBar == null)
            healthBar = nexus.AddComponent<HealthBarUI>();
        healthBar.RefreshForNexus();

        if (nexus.GetComponent<HealthDamagePopupBridge>() == null)
            nexus.AddComponent<HealthDamagePopupBridge>();
    }

    private static void BuildNexusVisuals(Transform root)
    {
        var stone = new Color(0.32f, 0.34f, 0.4f);
        var stoneDark = new Color(0.22f, 0.24f, 0.3f);
        var gold = new Color(0.95f, 0.78f, 0.28f);
        var crystal = new Color(0.35f, 0.85f, 1f);

        CreatePart(root, "Base_Outer", new Vector3(0f, 0.18f, 0f), new Vector3(3.4f, 0.36f, 3.4f), stoneDark);
        CreatePart(root, "Base_Inner", new Vector3(0f, 0.42f, 0f), new Vector3(2.6f, 0.28f, 2.6f), stone);
        CreatePart(root, "Ring_NE", new Vector3(1.05f, 0.55f, 1.05f), new Vector3(0.45f, 0.7f, 0.45f), stoneDark);
        CreatePart(root, "Ring_NW", new Vector3(-1.05f, 0.55f, 1.05f), new Vector3(0.45f, 0.7f, 0.45f), stoneDark);
        CreatePart(root, "Ring_SE", new Vector3(1.05f, 0.55f, -1.05f), new Vector3(0.45f, 0.7f, 0.45f), stoneDark);
        CreatePart(root, "Ring_SW", new Vector3(-1.05f, 0.55f, -1.05f), new Vector3(0.45f, 0.7f, 0.45f), stoneDark);
        CreatePart(root, "Core_Body", new Vector3(0f, 1.15f, 0f), new Vector3(1.35f, 1.5f, 1.35f), stone);
        CreatePart(root, "Core_Trim", new Vector3(0f, 1.15f, 0f), new Vector3(1.55f, 0.22f, 1.55f), gold);
        CreatePart(root, "Core_Top", new Vector3(0f, 2.05f, 0f), new Vector3(1.05f, 0.35f, 1.05f), gold);
        CreatePart(root, "Crystal", new Vector3(0f, 2.55f, 0f), new Vector3(0.75f, 1.1f, 0.75f), crystal, true);
        CreatePart(root, "Crystal_Outer", new Vector3(0f, 2.45f, 0f), new Vector3(1.05f, 0.25f, 1.05f), gold);
        CreatePart(root, "Spire_N", new Vector3(0f, 0.95f, 1.15f), new Vector3(0.35f, 1.1f, 0.35f), gold);
        CreatePart(root, "Spire_S", new Vector3(0f, 0.95f, -1.15f), new Vector3(0.35f, 1.1f, 0.35f), gold);
        CreatePart(root, "Spire_E", new Vector3(1.15f, 0.95f, 0f), new Vector3(0.35f, 1.1f, 0.35f), gold);
        CreatePart(root, "Spire_W", new Vector3(-1.15f, 0.95f, 0f), new Vector3(0.35f, 1.1f, 0.35f), gold);
    }

    private static void CreatePart(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color, bool emissive = false)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = scale;

        var partCollider = part.GetComponent<Collider>();
        if (partCollider != null)
            Object.Destroy(partCollider);

        var renderer = part.GetComponent<Renderer>();
        if (renderer == null)
            return;

        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.65f);
        }

        renderer.material = material;
    }

    private void EnsurePoolRoot()
    {
        if (nexusPoolRoot != null)
            return;

        var rootObject = transform.Find(PoolRootName);
        if (rootObject == null)
        {
            rootObject = new GameObject(PoolRootName).transform;
            rootObject.SetParent(transform, false);
        }

        nexusPoolRoot = rootObject;
    }
}
