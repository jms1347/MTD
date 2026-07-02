using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(UnitCombatVFX))]
public class Monster : MonoBehaviour
{
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float windUpDistance = 0.45f;
    [SerializeField] private float lungeDistance = 1.1f;
    [SerializeField] private float suicideTouchRadius = 0.75f;
    [SerializeField] private float suicideExplosionRadius = 2.2f;

    private MonsterData data;
    private IMonsterMobility activeMobility;
    private MonsterAttack activeAttack;
    private MonsterStatusController statusController;
    private UnitGridNavigator navigator;
    private Renderer cachedRenderer;
    private Health health;
    private Vector3 defaultScale;

    public MonsterData Data => data;
    public bool IsLanded => activeMobility != null && activeMobility.IsLanded;
    public bool IsAirUnit => data != null && data.IsAirUnit;
    public bool IsGroundUnit => data != null && data.IsGroundUnit;
    public float MoveSpeed { get; private set; }
    public float AttackDamage { get; private set; }
    public float AttackCooldown { get; private set; }
    public float GroundY { get; private set; }
    public float AttackRange => attackRange;
    public float WindUpDistance => windUpDistance;
    public float LungeDistance => lungeDistance;
    public float SuicideTouchRadius => suicideTouchRadius;
    public float SuicideExplosionRadius => suicideExplosionRadius;
    public UnitGridNavigator Navigator => navigator;
    public Vector3 DefaultScale => defaultScale;

    private float EffectiveMoveSpeed =>
        MoveSpeed * (statusController != null ? statusController.GetMoveSpeedMultiplier() : 1f);

    public bool CanMove =>
        IsAlive
        && IsLanded
        && (statusController == null || !statusController.BlocksMovement);

    public bool CanAttack =>
        IsAlive
        && IsLanded
        && (statusController == null || !statusController.BlocksAction)
        && (activeAttack == null || !activeAttack.IsFinished);

    public bool IsAlive => health != null && health.IsAlive;

    public bool CanMoveAndAttack => CanMove && CanAttack;

    private void Awake()
    {
        defaultScale = transform.localScale;
        statusController = GetComponent<MonsterStatusController>();
        navigator = GetComponent<UnitGridNavigator>();
        cachedRenderer = GetComponent<Renderer>();
        health = GetComponent<Health>();
    }

    private void Update()
    {
        if (activeMobility == null || activeAttack == null)
            return;

        if (!IsAlive)
            return;

        if (!IsLanded)
        {
            activeMobility.Tick(this);
            return;
        }

        if (!CanMove && !CanAttack)
            return;

        activeAttack.Tick(this);
    }

    public void ResetForSpawn(
        MonsterData monsterData,
        Vector3 spawnPosition,
        float groundY,
        float fallSpeed,
        Color color,
        SpawnDirection spawnDirection = SpawnDirection.West)
    {
        data = monsterData;
        GroundY = groundY;
        MoveSpeed = monsterData.GetMoveSpeed();
        AttackDamage = monsterData.attack;
        AttackCooldown = monsterData.GetAttackCooldown();

        activeMobility = GetComponent<IMonsterMobility>();
        activeAttack = GetComponent<MonsterAttack>();

        if (activeMobility == null || activeAttack == null)
        {
            Debug.LogError(
                $"[Monster] {monsterData.code}: prefab에 IMonsterMobility·MonsterAttack이 필요합니다.",
                this);
            return;
        }

        ValidatePrefabMatchesData(monsterData);

        StopAllCoroutines();
        foreach (var attack in GetComponents<MonsterAttack>())
            attack.Interrupt(this);

        statusController?.ClearForRespawn();

        var context = new MonsterSpawnContext
        {
            spawnPosition = spawnPosition,
            groundY = groundY,
            fallSpeed = fallSpeed
        };

        activeMobility.Reset(this, in context);
        activeAttack.Reset(this);
        MonsterMovement.ConfigureLane(this, spawnDirection, spawnPosition);

        GetComponent<MonsterSlimeVisual>()?.ApplyForSpawn(monsterData);

        if (cachedRenderer != null)
            cachedRenderer.material.color = color;
    }

    public void InterruptForStun()
    {
        foreach (var attack in GetComponents<MonsterAttack>())
            attack.Interrupt(this);
    }

    public static Transform FindNexus()
    {
        if (Nexus.Target != null)
            return Nexus.Target;

        var nexus = GameObject.FindGameObjectWithTag("Nexus");
        return nexus != null ? nexus.transform : null;
    }

    public Vector3 MoveAlongPath(Vector3 targetPosition)
    {
        var laneFollower = GetComponent<MonsterLaneFollower>();
        if (laneFollower != null && laneFollower.IsActive)
            return MoveTowardsDirect(targetPosition);

        return MoveTowardsTarget(targetPosition);
    }

    public Vector3 MoveTowardsTarget(Vector3 targetPosition)
    {
        var current = transform.position;
        if (navigator != null)
        {
            float radius = transform.localScale.x * 0.5f;
            return navigator.MoveTowards(
                current,
                targetPosition,
                EffectiveMoveSpeed,
                radius,
                radius * 2f,
                GroundY);
        }

        return MoveTowardsDirect(targetPosition);
    }

    public Vector3 MoveTowardsDirect(Vector3 targetPosition)
    {
        var current = transform.position;
        var toTarget = targetPosition - current;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            current.y = GroundY;
            return current;
        }

        var step = toTarget.normalized * (EffectiveMoveSpeed * Time.deltaTime);
        if (step.sqrMagnitude > toTarget.sqrMagnitude)
            step = toTarget;

        var next = current + step;
        next.y = GroundY;
        return next;
    }

    private void ValidatePrefabMatchesData(MonsterData monsterData)
    {
        bool hasGround = GetComponent<GroundMonster>() != null;
        bool hasMelee = GetComponent<MeleeMonster>() != null;
        bool hasSuicide = GetComponent<SuicideMonster>() != null;

        if (monsterData.IsGroundUnit && !hasGround)
            Debug.LogWarning($"[Monster] {monsterData.code}: 지상 몬스터인데 GroundMonster가 없습니다.", this);

        if (monsterData.IsMeleeAttacker && !hasMelee)
            Debug.LogWarning($"[Monster] {monsterData.code}: 근접 데이터인데 MeleeMonster가 없습니다.", this);

        if (monsterData.IsSuicideAttacker && !hasSuicide)
            Debug.LogWarning($"[Monster] {monsterData.code}: 자폭 데이터인데 SuicideMonster가 없습니다.", this);

        if (hasMelee && hasSuicide)
            Debug.LogWarning($"[Monster] {monsterData.code}: Melee·Suicide 공격이 동시에 붙어 있습니다.", this);
    }
}
