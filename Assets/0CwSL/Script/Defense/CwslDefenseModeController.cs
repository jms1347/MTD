using System;

using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;



/// <summary>5분 넥서스 방어 — 준비 발판 → 카운트다운 → 웨이브.</summary>

public class CwslDefenseModeController : NetworkBehaviour

{

    public static CwslDefenseModeController Instance { get; private set; }



    private readonly NetworkVariable<float> remainingSeconds = new(

        300f,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private readonly NetworkVariable<bool> defenseActive = new(

        false,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private readonly NetworkVariable<bool> defenseWon = new(

        false,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private readonly NetworkVariable<byte> matchPhase = new(

        (byte)CwslDefenseMatchPhase.PreMatch,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private readonly NetworkVariable<float> countdownSeconds = new(

        0f,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private readonly NetworkVariable<int> readyMask = new(

        0,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private readonly NetworkVariable<int> requiredPlayerCount = new(

        0,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private readonly List<CwslEnemyBase> enemyBases = new();

    private readonly List<ulong> sortedClientIds = new();



    private float minuteTimer;

    private float elapsed;

    private bool ended;



    public bool IsDefenseActive => defenseActive.Value;

    public bool DefenseWon => defenseWon.Value;

    public float RemainingSeconds => remainingSeconds.Value;

    public CwslDefenseMatchPhase MatchPhase => (CwslDefenseMatchPhase)matchPhase.Value;

    public float CountdownSeconds => countdownSeconds.Value;

    public int ReadyMask => readyMask.Value;

    public int RequiredPlayerCount => requiredPlayerCount.Value;

    public IReadOnlyList<Vector3> EnemyBasePositions
    {
        get
        {
            aliveBasePositions.Clear();
            for (var i = 0; i < enemyBases.Count; i++)
            {
                var enemyBase = enemyBases[i];
                if (enemyBase != null && enemyBase.IsAlive)
                    aliveBasePositions.Add(enemyBase.SpawnPosition);
            }

            return aliveBasePositions;
        }
    }

    public int EnemyBaseCount
    {
        get
        {
            var count = 0;
            for (var i = 0; i < enemyBases.Count; i++)
            {
                if (enemyBases[i] != null && enemyBases[i].IsAlive)
                    count++;
            }

            return count;
        }
    }

    private readonly List<Vector3> aliveBasePositions = new();



    public static event Action<float> OnTimerChanged;

    public static event Action OnPrepStateChanged;



    private void Awake()

    {

        if (Instance != null && Instance != this)

        {

            Destroy(this);

            return;

        }



        Instance = this;

    }



    public override void OnNetworkSpawn()

    {

        remainingSeconds.OnValueChanged += (_, current) => OnTimerChanged?.Invoke(current);

        matchPhase.OnValueChanged += (_, _) => NotifyPrepStateChanged();

        countdownSeconds.OnValueChanged += (_, _) => NotifyPrepStateChanged();

        readyMask.OnValueChanged += (_, _) => NotifyPrepStateChanged();

        requiredPlayerCount.OnValueChanged += (_, _) => NotifyPrepStateChanged();



        if (!IsServer)

        {

            NotifyPrepStateChanged();

            return;

        }



        if (!CwslGameConstants.UseDefenseMode)

            return;



        InitDefenseServer();

    }



    public override void OnNetworkDespawn()

    {

        if (Instance == this)

            Instance = null;



        CwslNexus.OnDestroyed -= HandleNexusDestroyed;

        SetPrepBarrierActive(false);

    }



    public bool IsMatchStarted => MatchPhase == CwslDefenseMatchPhase.Active && IsDefenseActive;



    public bool CanPlayerAct()

    {

        if (!CwslGameConstants.UseDefenseMode)

            return true;



        return IsMatchStarted;

    }



    public int GetLocalSlotIndex()

    {

        var localClient = NetworkManager.Singleton?.LocalClientId ?? ulong.MaxValue;

        CwslDefensePrepUtility.CollectSortedClientIds(sortedClientIds);

        return CwslDefensePrepUtility.GetSlotIndex(localClient, sortedClientIds);

    }



    public int GetReadyCount()

    {

        return CwslDefensePrepUtility.CountReadyMaskBits(readyMask.Value, requiredPlayerCount.Value);

    }



    private void Update()

    {

        if (!IsServer || !CwslGameConstants.UseDefenseMode)

            return;



        switch (MatchPhase)

        {

            case CwslDefenseMatchPhase.PreMatch:

                TickPreMatchServer();

                break;

            case CwslDefenseMatchPhase.Countdown:

                TickCountdownServer();

                break;

            case CwslDefenseMatchPhase.Active:

                if (!ended)

                    TickDefenseServer();

                break;

        }

    }



    private void InitDefenseServer()

    {

        var manager = CwslMonsterManager.Instance;

        if (manager == null)

            manager = gameObject.AddComponent<CwslMonsterManager>();



        manager.ResetForDefenseStart();



        var spawner = CwslGameSession.Instance?.MonsterSpawner;

        if (spawner != null)

            spawner.SpawningEnabled = false;



        DisableLegacyArenaSystems();

        SpawnNexusServer(CwslGameConstants.NexusDefaultHealth);



        defenseActive.Value = false;

        defenseWon.Value = false;

        ended = false;

        elapsed = 0f;

        minuteTimer = 0f;

        enemyBases.Clear();

        matchPhase.Value = (byte)CwslDefenseMatchPhase.PreMatch;

        countdownSeconds.Value = 0f;

        readyMask.Value = 0;

        UpdateRequiredPlayerCountServer();



        CwslNexus.OnDestroyed += HandleNexusDestroyed;

        SetPrepBarrierActive(true);

        SyncPrepBarrierClientRpc(true);

        NotifyPrepStateChangedClientRpc();

    }



    private void TickPreMatchServer()

    {

        EnforcePrepBoundariesServer();

        UpdateRequiredPlayerCountServer();

        readyMask.Value = ComputeReadyMaskServer();



        if (requiredPlayerCount.Value <= 0)

            return;



        if (GetReadyCount() < requiredPlayerCount.Value)

            return;



        matchPhase.Value = (byte)CwslDefenseMatchPhase.Countdown;

        countdownSeconds.Value = CwslGameConstants.DefenseStartCountdownSeconds;

        NotifyPrepStateChangedClientRpc();

    }



    private void TickCountdownServer()

    {

        EnforcePrepBoundariesServer();

        UpdateRequiredPlayerCountServer();

        readyMask.Value = ComputeReadyMaskServer();



        if (GetReadyCount() < requiredPlayerCount.Value)

        {

            matchPhase.Value = (byte)CwslDefenseMatchPhase.PreMatch;

            countdownSeconds.Value = 0f;

            NotifyPrepStateChangedClientRpc();

            return;

        }



        countdownSeconds.Value -= Time.deltaTime;

        if (countdownSeconds.Value > 0f)

            return;



        countdownSeconds.Value = 0f;

        BeginDefenseServer();

    }



    private void BeginDefenseServer()

    {

        var manager = CwslMonsterManager.Instance;

        if (manager == null)

            return;



        manager.ConfigureForPlayerCount(Mathf.Clamp(requiredPlayerCount.Value, 2, CwslGameConstants.MaxPlayers));

        var spawner = CwslGameSession.Instance?.MonsterSpawner;

        if (spawner != null)

            spawner.SpawningEnabled = true;



        var baseCount = UnityEngine.Random.Range(
            manager.GetScaledInitialBaseCountMin(),
            manager.GetScaledInitialBaseCountMax() + 1);

        for (var i = 0; i < baseCount; i++)

            TryAddEnemyBaseServer();



        defenseActive.Value = true;

        defenseWon.Value = false;

        remainingSeconds.Value = manager.DefenseDurationSeconds;

        elapsed = 0f;

        minuteTimer = 0f;

        matchPhase.Value = (byte)CwslDefenseMatchPhase.Active;

        SetPrepBarrierActive(false);

        SyncPrepBarrierClientRpc(false);

        NotifyPrepStateChangedClientRpc();

    }



    private void TickDefenseServer()

    {

        var manager = CwslMonsterManager.Instance;

        if (manager == null)

            return;



        var dt = Time.deltaTime;

        elapsed += dt;

        minuteTimer += dt;



        remainingSeconds.Value = Mathf.Max(0f, manager.DefenseDurationSeconds - elapsed);

        if (remainingSeconds.Value <= 0f)

        {

            WinDefenseServer();

            return;

        }



        if (minuteTimer >= manager.BaseSpawnIntervalSeconds)

        {

            minuteTimer = 0f;

            manager.ApplyMinuteEscalation();

            TryAddEnemyBaseServer();

            if (manager.SpawnMidBossEachMinute)

                SpawnFromRandomBase(CwslMonsterType.MidBoss);

            if (manager.SpawnDefenseBossEachMinute)

                SpawnFromRandomBase(CwslMonsterType.DefenseBoss);

            if (manager.SpawnSeniorCoachEachMinute)

                SpawnSeniorCoachAtMapEdge();

        }



        TickBaseSpawns(dt, manager);

    }



    private void UpdateRequiredPlayerCountServer()

    {

        CwslDefensePrepUtility.CollectSortedClientIds(sortedClientIds);

        requiredPlayerCount.Value = sortedClientIds.Count;

    }



    private int ComputeReadyMaskServer()

    {

        var network = NetworkManager.Singleton;

        if (network == null)

            return 0;



        CwslDefensePrepUtility.CollectSortedClientIds(sortedClientIds);

        var mask = 0;

        for (var i = 0; i < sortedClientIds.Count; i++)

        {

            var clientId = sortedClientIds[i];

            if (!network.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)

                continue;



            if (CwslDefensePrepUtility.IsOnStartPad(client.PlayerObject.transform.position))

                mask |= 1 << i;

        }



        return mask;

    }



    private static void EnforcePrepBoundariesServer()

    {

        var network = NetworkManager.Singleton;

        if (network == null)

            return;



        foreach (var client in network.ConnectedClientsList)

        {

            if (client.PlayerObject == null)

                continue;



            var playerObject = client.PlayerObject;

            var bodyRadius = playerObject.GetComponent<CwslPlayerBodyCollider>()?.Radius

                ?? CwslGameConstants.PlayerBodyColliderRadiusDefault;

            var position = playerObject.transform.position;

            var clamped = CwslDefensePrepUtility.ClampToPrepArea(position, bodyRadius);

            if ((clamped - position).sqrMagnitude < 0.0001f)

                continue;



            var movement = playerObject.GetComponent<CwslPlayerMovement>();

            movement?.StopMovement();



            var agent = playerObject.GetComponent<UnityEngine.AI.NavMeshAgent>();

            if (agent != null && agent.enabled && agent.isOnNavMesh)

                agent.Warp(clamped);

            else

                playerObject.transform.position = clamped;

        }

    }



    private static void DisableLegacyArenaSystems()

    {

        if (CwslKarmaSystem.Instance != null)

            CwslKarmaSystem.Instance.enabled = false;



        DisableIfPresent<CwslArenaGimmickSystem>();

        DisableIfPresent<CwslArenaTrapSystem>();

        DisableIfPresent<CwslArenaHazardPadSystem>();

        DisableIfPresent<CwslArenaBuffSystem>();

        DisableIfPresent<CwslArenaDynamicZoneSystem>();

        DisableIfPresent<CwslBossWatchState>();

    }



    private static void DisableIfPresent<T>() where T : Behaviour

    {

        var component = FindFirstObjectByType<T>();

        if (component != null)

            component.enabled = false;

    }



    private static void SpawnNexusServer(float maxHealth)

    {

        var existing = FindFirstObjectByType<CwslNexus>();

        if (existing != null)

        {

            existing.ConfigureServer(maxHealth);

            return;

        }



        var prefab = CwslGameSession.Instance?.Assets?.nexusPrefab;

        if (prefab == null)

        {

            Debug.LogWarning("[CwSL] nexusPrefab이 없습니다. Tools → CwSL → Setup Game Scene을 실행하세요.");

            return;

        }



        var instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);

        var networkObject = instance.GetComponent<NetworkObject>();

        if (networkObject == null)

        {

            Destroy(instance);

            Debug.LogError("[CwSL] CwslNexus 프리팹에 NetworkObject가 없습니다.");

            return;

        }



        networkObject.Spawn();

        instance.GetComponent<CwslNexus>()?.ConfigureServer(maxHealth);

    }



    private void TryAddEnemyBaseServer()

    {

        var manager = CwslMonsterManager.Instance;

        if (manager == null || EnemyBaseCount >= manager.MaxBases)

            return;



        var position = CwslArenaUtility.GetRandomMapEdgeSpawnPosition();

        for (var attempt = 0; attempt < 12; attempt++)

        {

            if (!HasNearbyBase(position, 8f))

                break;



            position = CwslArenaUtility.GetRandomMapEdgeSpawnPosition();

        }



        SpawnEnemyBaseServer(position, manager.EnemyBaseMaxHealth);

    }



    private bool HasNearbyBase(Vector3 position, float minDistance)

    {

        for (var i = 0; i < enemyBases.Count; i++)

        {

            var enemyBase = enemyBases[i];

            if (enemyBase == null || !enemyBase.IsAlive)

                continue;

            if (Vector3.Distance(enemyBase.SpawnPosition, position) < minDistance)

                return true;

        }

        return false;

    }



    private void SpawnEnemyBaseServer(Vector3 position, float maxHealth)

    {

        var prefab = CwslGameSession.Instance?.Assets?.enemyBasePrefab;

        if (prefab == null)

        {

            Debug.LogWarning("[CwSL] enemyBasePrefab이 없습니다. Tools → CwSL → Setup Game Scene을 실행하세요.");

            return;

        }



        var rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

        var networkObject = CwslNetworkPoolService.Instance?.Get(prefab, position, rotation);
        if (networkObject == null)
        {
            Debug.LogError("[CwSL] 적 기지 풀 스폰 실패.");
            return;
        }

        var enemyBase = networkObject.GetComponent<CwslEnemyBase>();
        enemyBase?.ConfigureServer(maxHealth);
        if (enemyBase != null)
            enemyBases.Add(enemyBase);
    }



    public void NotifyEnemyBaseDestroyedServer(CwslEnemyBase enemyBase)

    {

        if (!IsServer || enemyBase == null)

            return;

        enemyBases.Remove(enemyBase);

    }



    private void TickBaseSpawns(float dt, CwslMonsterManager manager)

    {

        var spawner = CwslGameSession.Instance?.MonsterSpawner;

        if (spawner == null || EnemyBaseCount == 0)

            return;



        spawner.TickDefenseSpawnsServer(dt, EnemyBasePositions, manager.SpawnIntervalPerBase);

    }



    private void SpawnFromRandomBase(CwslMonsterType type)

    {

        if (EnemyBaseCount == 0)

            return;



        CwslEnemyBase chosen = null;

        var aliveCount = 0;

        for (var i = 0; i < enemyBases.Count; i++)

        {

            var enemyBase = enemyBases[i];

            if (enemyBase == null || !enemyBase.IsAlive)

                continue;

            aliveCount++;

            if (UnityEngine.Random.Range(0, aliveCount) == aliveCount - 1)

                chosen = enemyBase;

        }



        if (chosen == null)

            return;



        CwslGameSession.Instance?.MonsterSpawner?.QueueDefenseSpawnServer(chosen.SpawnPosition, type);

    }



    private void SpawnSeniorCoachAtMapEdge()

    {

        var position = CwslArenaUtility.GetRandomMapEdgeSpawnPosition(CwslGameConstants.SeniorCoachOrbitInset);

        CwslGameSession.Instance?.MonsterSpawner?.QueueDefenseSpawnServer(position, CwslMonsterType.SeniorCoach);

    }



    private void HandleNexusDestroyed()

    {

        if (!IsServer || ended)

            return;



        LoseDefenseServer();

    }



    private void WinDefenseServer()

    {

        if (ended)

            return;



        ended = true;

        defenseWon.Value = true;

        defenseActive.Value = false;

        CwslGameFlow.Instance?.NotifyDefenseEndedServer(true);

    }



    private void LoseDefenseServer()

    {

        if (ended)

            return;



        ended = true;

        defenseWon.Value = false;

        defenseActive.Value = false;

        CwslGameFlow.Instance?.NotifyDefenseEndedServer(false);

    }



    private static void NotifyPrepStateChanged()

    {

        OnPrepStateChanged?.Invoke();

    }



    private static void SetPrepBarrierActive(bool active)

    {

        CwslDefensePrepBarrier.SetActive(active);

    }



    [ClientRpc]

    private void SyncPrepBarrierClientRpc(bool active)

    {

        if (IsServer)

            return;

        SetPrepBarrierActive(active);

    }



    [ClientRpc]

    private void NotifyPrepStateChangedClientRpc()

    {

        NotifyPrepStateChanged();

    }

}


