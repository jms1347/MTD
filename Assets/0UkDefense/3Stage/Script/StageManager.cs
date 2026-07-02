using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 스폰·풀링·웨이브를 관리하는 싱글톤 매니저.
/// 하이어라키의 '적군POOL' 아래에 적 오브젝트가 생성됩니다.
/// </summary>
public class StageManager : Singleton<StageManager>
{
    public const string PoolRootName = "적군POOL";

    [Header("스폰 타이밍")]
    [Tooltip("한 번의 스폰(웨이브)이 끝난 뒤, 다음 스폰까지 대기하는 시간(초)입니다.\n값이 작을수록 적이 더 자주 나옵니다.")]
    [SerializeField] private float spawnInterval = 1f;

    [Tooltip("스폰 한 번(한 틱)마다 동시에 생성할 적의 마릿수입니다.\n1이면 한 마리씩, 4면 한 번에 네 마리가 나옵니다.")]
    [SerializeField] private int spawnCountPerInterval = 10;

    [Tooltip("한 틱 안에서 여러 마리를 띄울 때, 마리 사이 간격(초)입니다.\n0이면 완전 동시에 생성됩니다.")]
    [SerializeField] private float spawnStaggerDelay = 0.1f;

    [Header("스폰 위치")]
    [Tooltip("맵 중심(넥서스) 기준으로 적이 나타나는 원형 반경입니다.\n값이 클수록 맵 가장자리에서 스폰됩니다.")]
    [SerializeField] private float spawnRadius = 35f;

    [Tooltip("스폰 반경에 더해지는 랜덤 오차(±)입니다.\n같은 방향에서도 위치가 조금씩 흩어집니다.")]
    [SerializeField] private float spawnScatter = 2.5f;

    [Tooltip("적이 하늘에서 떨어지기 시작하는 높이(Y)입니다.\n값이 클수록 낙하 시간이 길어집니다.")]
    [SerializeField] private float dropHeight = 14f;

    [Tooltip("공중 몬스터 스폰 높이 배율 (dropHeight × 이 값) — 호버 고도보다 높을 때만 사용")]
    [SerializeField] private float airSpawnHeightMultiplier = 9f;

    [Tooltip("넥서스 꼭대기보다 공중 유닛이 얼마나 더 높게 비행할지(Y).")]
    [SerializeField] private float airHoverAboveNexusTop = 2.5f;

    [Tooltip("공중 유닝 낙하 시작 추가 높이(호버 고도 기준).")]
    [SerializeField] private float airSpawnDropOffset = 12f;

    [Header("적 수·풀링")]
    [Tooltip("필드에 동시에 존재할 수 있는 적의 최대 수입니다.\n이 수에 도달하면 스폰이 잠시 멈춥니다.")]
    [SerializeField] private int maxAliveEnemies = 50;

    [Tooltip("게임 시작 시 미리 만들어 둘 적 오브젝트 풀 개수입니다.\n스폰 수가 많으면 값을 키우세요.")]
    [SerializeField] private int initialEnemyPoolSize = 50;

    [Tooltip("풀이 부족할 때 한 번에 추가로 생성하는 적 개수입니다.")]
    [SerializeField] private int enemyPoolExpandSize = 1;

    [Header("방향 웨이브 스폰")]
    [Tooltip("켜면 동·서·남·북 중 한 방향으로만 몰려오는 웨이브 스폰을 사용합니다.\n끄면 맵 사방 랜덤 방향에서 스폰됩니다.")]
    [SerializeField] private bool useDirectionalWaves = true;

    [Tooltip("웨이브가 시작되기 전, 해당 방향 화면 테두리 경고가 표시되는 시간(초)입니다.")]
    [SerializeField] private float warningLeadTime = 1.8f;

    [Tooltip("방향 웨이브일 때, 선택된 방향을 중심으로 스폰되는 각도 범위(°)입니다.\n값이 클수록 해당 방향 호가 넓어집니다.")]
    [SerializeField] private float waveArcDegrees = 55f;

    [Header("적 스탯 (시트 미로드 시 폴백)")]
    [Tooltip("MonsterDataSo가 비어 있을 때만 사용하는 기본 체력입니다.")]
    [SerializeField] private float fallbackHealth = 100f;

    [Tooltip("적 구체의 크기 배율입니다.\n1에 가까울수록 기본 구 크기입니다.")]
    [SerializeField] private float enemyScale = 0.9f;

    [Tooltip("스폰 후 땅에 닿기 전까지 떨어지는 속도입니다.\n착지 전에는 타워가 공격하지 않습니다.")]
    [SerializeField] private float fallSpeed = 10f;

    [Tooltip("적이 죽은 뒤 오브젝트 풀로 돌아가기까지 대기 시간(초)입니다.\n사망 이펙트 재생 시간에 맞춰 조정하세요.")]
    [SerializeField] private float deathReturnDelay = 0.35f;

    [Header("몬스터 런타임 비주얼")]
    [SerializeField] private RuntimeAnimatorController slimeAnimatorController;
    [SerializeField] private Face slimeFaceAsset;
    [SerializeField] private Avatar slimeAvatar;

    /// <summary>스폰 경고 UI가 구독하는 이벤트 (방향, 경고 지속 시간)</summary>
    public event Action<SpawnDirection, float> OnSpawnDirectionWarning;

    /// <summary>스테이지 할당 수만큼 스폰 완료 후 필드가 비었을 때</summary>
    public event Action OnStageCleared;

    /// <summary>새 전투 라운드가 시작될 때 (필드 적 정리 직후)</summary>
    public event Action OnStageBattleBegan;

    private Transform enemyPoolRoot;
    private MonsterEnemyPool monsterEnemyPool;
    private StageSpawnQueue stageSpawnQueue;
    private Vector3 spawnCenter = Vector3.zero;
    private int aliveEnemyCount;
    private readonly HashSet<GameObject> pendingEnemyReturns = new();
    private int stageSpawnQuota = int.MaxValue;
    private int stageSpawnedTotal;
    private bool stageBattleActive;
    private bool stageClearNotified;
    private bool coopSpawnMode;
    private bool isPoolReady;
    private Coroutine spawnCoroutine;
    private int laneSpawnSpreadCounter;

    public Transform EnemyPoolRoot => enemyPoolRoot;
    public int AliveEnemyCount => aliveEnemyCount;
    public int StageSpawnedTotal => stageSpawnedTotal;
    public int StageSpawnQuota => stageSpawnQuota;
    public bool IsStageBattleActive => stageBattleActive;
    public Vector3 SpawnCenter => spawnCenter;
    public float SpawnRadius => spawnRadius;
    public float DropHeight => dropHeight;

    private float ResolveAirHoverY(float fallbackGroundY)
    {
        float cruise = NexusManager.GetAirCruiseAltitude(airHoverAboveNexusTop);
        return Mathf.Max(fallbackGroundY + 4f, cruise);
    }

    protected override void Awake()
    {
        base.Awake();
    }

    public void ConfigureScene(Vector3 center, float? spawnRadiusOverride = null)
    {
        spawnCenter = center;

        if (spawnRadiusOverride.HasValue)
            spawnRadius = spawnRadiusOverride.Value;

        isPoolReady = false;
        EnsurePoolReady();

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    public void BeginStageBattle(StageData stage)
    {
        stageSpawnQueue = StageSpawnQueue.Build(stage, DataManager.Instance?.Monsters);
        var quota = stageSpawnQueue.TotalCount;

        if (quota <= 0)
        {
            Debug.LogWarning("[StageManager] StageData spawn list is empty. Falling back to random quota.");
            BeginStageBattle(24);
            return;
        }

        EnsureMonsterEnemyPool().PrepareForStage(stage);
        BeginStageBattleInternal(quota);
    }

    public void BeginStageBattle(int quota)
    {
        stageSpawnQueue = null;
        BeginStageBattleInternal(Mathf.Max(1, quota));
    }

    private void BeginStageBattleInternal(int quota)
    {
        EnsurePoolReady();
        EnsureStageManagerActive();

        ClearAllFieldEnemies();

        stageSpawnQuota = quota;
        stageSpawnedTotal = 0;
        stageBattleActive = true;
        stageClearNotified = false;
        laneSpawnSpreadCounter = 0;

        var pool = EnsureMonsterEnemyPool();
        if (stageSpawnQueue == null)
            pool.PrepareFromMonsterTable();

        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        spawnCoroutine = StartCoroutine(useDirectionalWaves ? DirectionalWaveLoop() : RandomSpawnLoop());
        OnStageBattleBegan?.Invoke();
        SpawnDirectionWarningUI.RefreshAllSubscriptions();
    }

    private void EnsureStageManagerActive()
    {
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        if (!enabled)
            enabled = true;
    }

    public void EndStageBattle()
    {
        stageBattleActive = false;
        stageSpawnQuota = int.MaxValue;
        stageSpawnedTotal = 0;
        stageClearNotified = false;
        stageSpawnQueue = null;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        ClearAllFieldEnemies();
    }

    /// <summary>
    /// 필드에 남아 있는 적(사망 대기 포함)을 즉시 풀로 반환합니다.
    /// </summary>
    public void ClearAllFieldEnemies()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.activeInHierarchy)
                continue;

            pendingEnemyReturns.Remove(enemy);
            ReleaseEnemyToPool(enemy);
        }

        pendingEnemyReturns.Clear();
        aliveEnemyCount = 0;
    }

    private bool CanSpawnMoreForStage()
    {
        if (coopSpawnMode)
            return aliveEnemyCount < maxAliveEnemies;

        return stageBattleActive && stageSpawnedTotal < stageSpawnQuota;
    }

    public void EnterCoopSpawnMode()
    {
        EnsurePoolReady();
        EnsureStageManagerActive();
        coopSpawnMode = true;
        stageBattleActive = true;
        stageSpawnQuota = int.MaxValue;
        stageClearNotified = false;

        var pool = EnsureMonsterEnemyPool();
        pool.PrepareFromMonsterTable();

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    public bool TryCoopSpawnInDirection(SpawnDirection direction)
    {
        if (!coopSpawnMode)
            return false;

        return SpawnEnemyOnLane(direction);
    }

    public bool TryCoopSpawnAtWorld(Vector3 worldPoint)
    {
        if (!coopSpawnMode || !CanSpawnMoreForStage())
            return false;

        var direction = (SpawnDirection)UnityEngine.Random.Range(0, 2);
        return SpawnEnemyAtGroundPoint(worldPoint, direction);
    }

    private void TryNotifyStageCleared()
    {
        if (!stageBattleActive || stageClearNotified)
            return;

        if (stageSpawnedTotal >= stageSpawnQuota && aliveEnemyCount <= 0)
        {
            stageClearNotified = true;
            OnStageCleared?.Invoke();
        }
    }

    private IEnumerator DirectionalWaveLoop()
    {
        yield return null;

        while (true)
        {
            if (!stageBattleActive)
            {
                yield return null;
                continue;
            }

            if (!CanSpawnMoreForStage())
            {
                TryNotifyStageCleared();
                yield return new WaitForSeconds(0.25f);
                continue;
            }

            if (aliveEnemyCount >= maxAliveEnemies)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            var direction = (SpawnDirection)UnityEngine.Random.Range(0, 2);

            OnSpawnDirectionWarning?.Invoke(direction, warningLeadTime);
            yield return new WaitForSeconds(warningLeadTime);

            if (!CanSpawnMoreForStage())
                continue;

            yield return SpawnBatch(() => SpawnEnemyInDirection(direction));

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private IEnumerator RandomSpawnLoop()
    {
        yield return null;

        while (true)
        {
            if (!stageBattleActive)
            {
                yield return null;
                continue;
            }

            if (!CanSpawnMoreForStage())
            {
                TryNotifyStageCleared();
                yield return new WaitForSeconds(0.25f);
                continue;
            }

            if (aliveEnemyCount >= maxAliveEnemies)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            yield return SpawnBatch(SpawnEnemy);

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    /// <summary>
    /// spawnCountPerInterval 마리를 생성합니다. spawnStaggerDelay가 0이면 동시에 스폰합니다.
    /// </summary>
    private IEnumerator SpawnBatch(Action spawnAction)
    {
        int count = Mathf.Max(1, spawnCountPerInterval);

        for (int i = 0; i < count; i++)
        {
            if (!CanSpawnMoreForStage() || aliveEnemyCount >= maxAliveEnemies)
                break;

            try
            {
                spawnAction.Invoke();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }

            if (spawnStaggerDelay > 0f && i < count - 1)
                yield return new WaitForSeconds(spawnStaggerDelay);
            else if (spawnStaggerDelay <= 0f && i < count - 1)
                yield return null;
        }
    }

    public void SpawnEnemy()
    {
        var direction = (SpawnDirection)UnityEngine.Random.Range(0, 2);
        SpawnEnemyInDirection(direction);
    }

    private MonsterData ResolveMonsterDataForSpawn(out bool consumedStageEntry, out BossData bossData)
    {
        consumedStageEntry = false;
        bossData = null;

        if (stageSpawnQueue != null && stageSpawnQueue.TryPeek(out var code))
        {
            consumedStageEntry = true;
            if (BossSpawnResolver.TryResolve(code, out var bossMonsterData, out bossData))
                return bossMonsterData;

            if (DataManager.Instance?.Monsters != null &&
                DataManager.Instance.Monsters.TryGet(code, out var data))
                return data;

            Debug.LogWarning($"[StageManager] stage spawn code not found: {code}");
            stageSpawnQueue.ConfirmSpawn();
            consumedStageEntry = false;
        }

        return MonsterSpawnPicker.RollRandom();
    }

    private bool TryGetEnemyFromPool(string monsterCode, out GameObject enemy)
    {
        enemy = null;
        EnsurePoolReady();
        return monsterEnemyPool != null && monsterEnemyPool.TryGet(monsterCode, out enemy);
    }

    public void SpawnEnemyInDirection(SpawnDirection direction)
    {
        SpawnEnemyOnLane(direction);
    }

    private bool SpawnEnemyOnLane(SpawnDirection spawnDirection)
    {
        if (!CanSpawnMoreForStage())
            return false;

        int spreadIndex = laneSpawnSpreadCounter % 3;
        laneSpawnSpreadCounter++;

        Vector3 groundPoint;
        if (!DefenseMonsterLaneRegistry.TryGetLaneSpawnWorld(spawnDirection, spreadIndex, out groundPoint))
        {
            float angle = DirectionToRadians(spawnDirection);
            groundPoint = new Vector3(
                spawnCenter.x + Mathf.Cos(angle) * spawnRadius,
                0f,
                spawnCenter.z + Mathf.Sin(angle) * spawnRadius);
        }

        return SpawnEnemyAtGroundPoint(groundPoint, spawnDirection);
    }

    private bool SpawnEnemyAtGroundPoint(Vector3 groundPoint, SpawnDirection spawnDirection)
    {
        if (!CanSpawnMoreForStage())
            return false;

        float spawnX = groundPoint.x;
        float spawnZ = groundPoint.z;

        var monsterData = ResolveMonsterDataForSpawn(out var consumedStageEntry, out var bossData);
        if (!TryGetEnemyFromPool(monsterData.code, out var enemy))
        {
            if (consumedStageEntry)
                stageSpawnQueue?.ConfirmSpawn();
            return false;
        }

        float groundY = MonsterGroundPlacement.ResolveGroundY(groundPoint);
        float scale = enemyScale * monsterData.GetScaleMultiplier();
        float spawnY = groundY;
        var spawnPosition = new Vector3(spawnX, spawnY, spawnZ);

        enemy.name = $"{monsterData.code}_{aliveEnemyCount + 1:00}";
        enemy.transform.position = spawnPosition;
        enemy.transform.localScale = Vector3.one * scale;

        var health = enemy.GetComponent<Health>();
        if (health == null)
        {
            if (consumedStageEntry)
                stageSpawnQueue?.ConfirmSpawn();
            return false;
        }

        enemy.GetComponent<HealthBarUI>()?.RefreshForSpawn();

        health.Initialize(
            monsterData.hp > 0 ? monsterData.hp : fallbackHealth,
            deathReturnDelay,
            monsterData.defense);
        health.SetDestroyOnDeath(false);

        enemy.GetComponent<MonsterStatusOverlayUI>()?.RefreshForSpawn();

        var monster = enemy.GetComponent<Monster>();
        if (monster == null)
        {
            if (consumedStageEntry)
                stageSpawnQueue?.ConfirmSpawn();
            return false;
        }

        monster.ResetForSpawn(
            monsterData,
            spawnPosition,
            groundY,
            fallSpeed,
            MonsterVisuals.GetColor(monsterData),
            spawnDirection);

        ConfigureBossCombatProfile(enemy, bossData);

        var pooled = enemy.GetComponent<PooledEnemy>();
        if (pooled != null)
            pooled.PoolCode = monsterData.code;

        if (consumedStageEntry)
            stageSpawnQueue?.ConfirmSpawn();

        aliveEnemyCount++;
        stageSpawnedTotal++;
        OnEnemySpawned?.Invoke(enemy, monsterData);
        TryNotifyStageCleared();
        return true;
    }

    public static event System.Action<GameObject, MonsterData> OnEnemySpawned;

    public static float DirectionToRadians(SpawnDirection direction)
    {
        return direction switch
        {
            SpawnDirection.West => Mathf.PI * 0.75f,
            SpawnDirection.East => -Mathf.PI * 0.25f,
            _ => 0f
        };
    }

    private static void ConfigureBossCombatProfile(GameObject enemy, BossData bossData)
    {
        if (enemy == null)
            return;

        var profile = enemy.GetComponent<BossCombatProfile>();
        if (profile == null)
            profile = enemy.AddComponent<BossCombatProfile>();

        if (bossData != null)
            profile.Configure(bossData);
        else
            profile.Clear();
    }

    public void ScheduleReturnEnemy(GameObject enemy)
    {
        if (enemy == null || pendingEnemyReturns.Contains(enemy))
            return;

        pendingEnemyReturns.Add(enemy);
        StartCoroutine(ReturnEnemyAfterDelay(enemy));
    }

    public void ReturnEnemy(GameObject enemy)
    {
        if (enemy == null || !enemy.activeSelf)
            return;

        pendingEnemyReturns.Remove(enemy);
        aliveEnemyCount = Mathf.Max(0, aliveEnemyCount - 1);
        ReleaseEnemyToPool(enemy);
        TryNotifyStageCleared();
    }

    private void ReleaseEnemyToPool(GameObject enemy)
    {
        if (enemy == null || !enemy.activeSelf)
            return;

        var poolCode = enemy.GetComponent<PooledEnemy>()?.PoolCode;
        if (!string.IsNullOrWhiteSpace(poolCode))
            monsterEnemyPool?.Release(poolCode.Trim(), enemy);
    }

    private IEnumerator ReturnEnemyAfterDelay(GameObject enemy)
    {
        if (deathReturnDelay > 0f)
            yield return new WaitForSeconds(deathReturnDelay);

        pendingEnemyReturns.Remove(enemy);
        ReturnEnemy(enemy);
    }

    private void EnsurePoolReady()
    {
        if (isPoolReady)
            return;

        EnsurePoolRoot();
        EnsureMonsterEnemyPool();
        CombatDamagePopupPool.EnsureReady();
        isPoolReady = true;
    }

    private MonsterEnemyPool EnsureMonsterEnemyPool()
    {
        if (monsterEnemyPool != null)
            return monsterEnemyPool;

        monsterEnemyPool = new MonsterEnemyPool(
            () => enemyPoolRoot != null ? enemyPoolRoot : EnsurePoolRootAndReturn(),
            initialEnemyPoolSize,
            enemyPoolExpandSize,
            slimeAnimatorController,
            slimeFaceAsset,
            slimeAvatar);

        return monsterEnemyPool;
    }

    private Transform EnsurePoolRootAndReturn()
    {
        EnsurePoolRoot();
        return enemyPoolRoot;
    }

    private void EnsurePoolRoot()
    {
        if (enemyPoolRoot != null)
            return;

        var rootObject = transform.Find(PoolRootName);
        if (rootObject == null)
        {
            rootObject = new GameObject(PoolRootName).transform;
            rootObject.SetParent(transform, false);
        }

        enemyPoolRoot = rootObject;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawnCenter + Vector3.up * dropHeight, spawnRadius);
    }
}
