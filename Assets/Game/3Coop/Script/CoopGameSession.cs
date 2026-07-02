using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class CoopGameSession : MonoBehaviour
{
    public static CoopGameSession Instance { get; private set; }

    public CoopSyncPayload LatestState { get; private set; } = new();
    public string LocalPlayerId => lobby != null ? lobby.LocalPlayerId : string.Empty;
    public bool IsHostAuthority => lobby != null && lobby.IsHost;
    public bool IsOfflineSolo => lobby != null && lobby.IsHost && lobby.Players.Count <= 1;
    public bool WaveActive => waveActive;
    public int CurrentWave => currentWave;

    public event Action<CoopSyncPayload> OnStateUpdated;
    public event Action<string> OnAnnouncement;
    public event Action<bool, string> OnGameEnded;

    private LobbyNetworkManager lobby;
    private Transform entityRoot;
    private CoopMonsterSpawner monsterSpawner;
    private readonly Dictionary<string, CoopPlayerState> players = new();
    private readonly Dictionary<string, CoopPlayerTowerUnit> liveTowers = new();
    private readonly Dictionary<int, CoopSyncedMonster> liveSyncedMonsters = new();
    private readonly Dictionary<int, CoopEnemyActor> liveEnemies = new();
    private readonly Dictionary<int, string> lastHitByPlayer = new();

    private int nextEnemyId = 1000;
    private int currentWave;
    private bool waveActive;
    private bool goldRush;
    private int enemiesToSpawn;
    private float spawnTimer;
    private float syncTimer;
    private float waveBreakTimer;
    private string announcement = string.Empty;
    private float announcementTimer;
    private bool gameOver;
    private bool useMapMode;
    private bool lastSyncedWaveActive;
    private bool farmGateOpen = true;
    private readonly System.Random random = new();

    private const float SyncInterval = 0.1f;
    private const float SpawnInterval = 0.85f;
    private const float WaveBreakDuration = 8f;
    private const float TowerRingRadius = 11f;
    private const float OrderArriveDistance = 0.35f;
    public const float MoveArriveDistance = OrderArriveDistance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        var rootObject = new GameObject("CoopEntities");
        entityRoot = rootObject.transform;
    }

    private void Start()
    {
        CoopSoloPlayBootstrap.Ensure();
        lobby = LobbyNetworkManager.Instance;
        if (lobby == null)
        {
            Debug.LogError("[CoopGameSession] LobbyNetworkManager가 없습니다.");
            return;
        }

        if (!lobby.IsInRoom)
        {
            Debug.LogWarning("[CoopGameSession] 방에 참여하지 않은 상태입니다. 솔로 호스트로 전환합니다.");
            lobby.BeginSoloSession();
        }

        if (!lobby.IsHost)
            SetAnnouncement("클라이언트 모드 — 이동·전투는 호스트가 처리합니다.");

        lobby.OnCoopMessage += HandleCoopMessage;
        StartCoroutine(BootstrapWhenReady());
    }

    private IEnumerator BootstrapWhenReady()
    {
        var timeout = 5f;
        while (CoopMapBootstrap.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (CoopMapBootstrap.Instance != null)
            yield return new WaitUntil(() => CoopMapBootstrap.Instance.IsReady);

        useMapMode = CoopMapBootstrap.Instance != null && CoopMapBootstrap.Instance.MapLayout != null;

        BootstrapPlayers();

        if (IsHostAuthority)
        {
            monsterSpawner = gameObject.AddComponent<CoopMonsterSpawner>();
            monsterSpawner.Initialize(this);
            SpawnRealTowers();
            RefreshFarmGates();
            BeginNextWave();
            BroadcastState();
        }
        else
        {
            SpawnVisualTowersForClients();
            SetAnnouncement("호스트가 웨이브를 준비 중...");
        }
    }

    private void SpawnVisualTowersForClients()
    {
        var index = 0;
        foreach (var player in players.Values)
        {
            var unit = CoopPlayerTowerFactory.CreatePlayerTank(entityRoot, player, index++);
            liveTowers[player.playerId] = unit;
            unit.ApplyState(player, snapPosition: true);
        }
    }

    private void OnDestroy()
    {
        if (lobby != null)
            lobby.OnCoopMessage -= HandleCoopMessage;

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (gameOver || lobby == null)
            return;

        if (IsHostAuthority)
            HostTick();

        if (IsHostAuthority)
            TickSkillCooldowns();

        syncTimer += Time.deltaTime;
        if (IsHostAuthority && syncTimer >= SyncInterval)
        {
            syncTimer = 0f;
            BroadcastState();
        }

        if (announcementTimer > 0f)
        {
            announcementTimer -= Time.deltaTime;
            if (announcementTimer <= 0f)
                announcement = string.Empty;
        }
    }

    public void RegisterSyncedMonster(int id, CoopSyncedMonster monster)
    {
        if (!IsHostAuthority || monster == null)
            return;

        liveSyncedMonsters[id] = monster;
    }

    public bool TryGetEnemyPosition(int enemyId, out Vector3 position)
    {
        if (liveSyncedMonsters.TryGetValue(enemyId, out var synced) && synced.TryGetWorldPosition(out position))
            return true;

        if (liveEnemies.TryGetValue(enemyId, out var actor) && actor != null)
        {
            position = actor.transform.position;
            return true;
        }

        position = default;
        return false;
    }

    public bool TryUpgrade(string upgradeKey)
    {
        if (gameOver || string.IsNullOrEmpty(LocalPlayerId))
            return false;

        if (IsHostAuthority)
            return HostTryUpgrade(LocalPlayerId, upgradeKey);

        lobby.SendCoopToHost(JsonConvert.SerializeObject(new CoopUpgradeRequest
        {
            playerId = LocalPlayerId,
            upgradeKey = upgradeKey
        }));
        return true;
    }

    public void RequestOrder(string playerId, int orderType, float x, float z, int attackTargetId = -1)
    {
        if (gameOver)
            return;

        var target = new Vector3(x, 0f, z);
        if (DefenseMapPathfinder.IsReady && !DefenseMapPathfinder.IsWorldWalkable(target))
            target = CoopMapSpawnUtility.SnapToWalkableWorld(target);

        if (IsHostAuthority)
        {
            ApplyOrder(playerId, orderType, target.x, target.z, attackTargetId);
            return;
        }

        if (playerId != LocalPlayerId)
            return;

        lobby.SendCoopToHost(JsonConvert.SerializeObject(new CoopOrderRequest
        {
            playerId = playerId,
            orderType = orderType,
            x = target.x,
            z = target.z,
            attackTargetId = attackTargetId
        }));
    }

    public void RequestSkill(string playerId, float x, float z)
    {
        if (gameOver)
            return;

        var target = new Vector3(x, 0f, z);
        if (DefenseMapPathfinder.IsReady && !DefenseMapPathfinder.IsWorldWalkable(target))
            target = CoopMapSpawnUtility.SnapToWalkableWorld(target);

        if (IsHostAuthority)
        {
            HostTryCastSkill(playerId, target.x, target.z);
            return;
        }

        if (playerId != LocalPlayerId)
            return;

        lobby.SendCoopToHost(JsonConvert.SerializeObject(new CoopSkillRequest
        {
            playerId = playerId,
            x = target.x,
            z = target.z
        }));
    }

    public bool TryGetLocalPlayer(out CoopPlayerState state) => players.TryGetValue(LocalPlayerId, out state);

    public IEnumerable<CoopPlayerState> EnumeratePlayerStates() => players.Values;

    public void TriggerAmbushBurst(Vector3 origin, int count)
    {
        if (!IsHostAuthority || !waveActive || monsterSpawner == null)
            return;

        monsterSpawner.TrySpawnAmbushBurst(origin, count, currentWave);
        SetAnnouncement("매복 구역에서 적이 기습합니다!");
    }

    public bool TryGetPlayer(string playerId, out CoopPlayerState state) => players.TryGetValue(playerId, out state);

    public bool TryGetLivingTower(string playerId, out CoopPlayerTowerUnit unit)
        => liveTowers.TryGetValue(playerId, out unit) && unit != null;

    public void OnEnemyKilled(int enemyId)
    {
        if (!IsHostAuthority)
            return;

        var reward = 10;
        if (liveSyncedMonsters.TryGetValue(enemyId, out var synced))
        {
            reward = synced.GoldReward;
            liveSyncedMonsters.Remove(enemyId);
        }
        else if (liveEnemies.TryGetValue(enemyId, out var actor))
        {
            reward = actor.GoldReward;
            liveEnemies.Remove(enemyId);
        }
        else
        {
            return;
        }

        lastHitByPlayer.TryGetValue(enemyId, out var killerId);
        lastHitByPlayer.Remove(enemyId);

        if (!players.TryGetValue(killerId, out var killer))
        {
            foreach (var pair in players)
            {
                killer = pair.Value;
                break;
            }
        }

        if (killer == null)
            return;

        if (goldRush)
            reward *= 2;

        killer.gold += reward;

        var now = Time.time;
        if (now - killer.lastKillTime < 8f)
            killer.killStreak++;
        else
            killer.killStreak = 1;

        killer.lastKillTime = now;

        if (killer.killStreak > 0 && killer.killStreak % 5 == 0)
        {
            var bonus = 25 + currentWave * 3;
            killer.gold += bonus;
            BroadcastEvent($"{killer.playerName} 연속 처치 x{killer.killStreak}! +{bonus}G", killer.playerId, bonus);
        }
    }

    public void UnregisterEnemyIfPresent(int enemyId)
    {
        if (!IsHostAuthority)
            return;

        liveSyncedMonsters.Remove(enemyId);
        liveEnemies.Remove(enemyId);
        lastHitByPlayer.Remove(enemyId);
    }

    public void RegisterEnemyDamaged(int enemyId, float hp, string attackerPlayerId)
    {
        if (!IsHostAuthority)
            return;

        lastHitByPlayer[enemyId] = attackerPlayerId;
    }

    public void RefreshFarmGates()
    {
        if (!IsHostAuthority)
            return;

        farmGateOpen = CoopFarmGateSync.ComputeShouldOpen(waveActive, CoopFarmGatePressurePlate.IsOccupied);
        CoopFarmGateSync.ApplyState(farmGateOpen);
    }

    public void DamagePlayerTower(string playerId, float amount)
    {
        if (!players.TryGetValue(playerId, out var player))
            return;

        player.towerHp = Mathf.Max(0f, player.towerHp - amount);
        if (player.towerHp <= 0f)
            SetAnnouncement($"{player.playerName}의 타워가 파괴되었습니다!");

        CheckAllTowersDestroyed();
    }

    private void CheckAllTowersDestroyed()
    {
        if (!IsHostAuthority || gameOver)
            return;

        foreach (var player in players.Values)
        {
            if (player.towerHp > 0f)
                return;
        }

        EndGame("모든 플레이어 탱크가 파괴되었습니다!");
    }

    public void DamageNexus(float amount)
    {
    }

    private void BootstrapPlayers()
    {
        players.Clear();
        var lobbyPlayers = lobby.Players;
        var count = Math.Max(1, lobbyPlayers.Count);
        var shuffledTanks = CoopTankCatalog.CreateShuffledCodes(random);
        var spawnCenter = ResolvePlayerSpawnCenter();

        for (var i = 0; i < count; i++)
        {
            var lobbyPlayer = lobbyPlayers[i];
            var slot = ResolveTowerSpawnSlot(spawnCenter, i, count);
            var tankCode = shuffledTanks[i % shuffledTanks.Length];
            CoopTankCatalog.TryGet(tankCode, out var tank);
            var state = new CoopPlayerState
            {
                playerId = lobbyPlayer.playerId,
                playerName = lobbyPlayer.playerName,
                gold = 0,
                towerX = slot.x,
                towerZ = slot.z
            };
            CoopTankCatalog.ApplyBaseStats(state, tank);
            state.skillId = CoopSkillCatalog.PickRandom(random);
            players[state.playerId] = state;
        }

        if (players.Count == 0)
        {
            var center = ResolvePlayerSpawnCenter();
            var slot = ResolveTowerSpawnSlot(center, 0, 1);
            var tank = CoopTankCatalog.GetRandom(random);
            var fallback = new CoopPlayerState
            {
                playerId = lobby.LocalPlayerId,
                playerName = lobby.LocalPlayerName,
                towerX = slot.x,
                towerZ = slot.z
            };
            CoopTankCatalog.ApplyBaseStats(fallback, tank);
            fallback.skillId = CoopSkillCatalog.PickRandom(random);
            players[lobby.LocalPlayerId] = fallback;
        }
    }

    private Vector3 ResolvePlayerSpawnCenter()
    {
        if (useMapMode && CoopMapBootstrap.Instance.MapLayout != null)
            return CoopMapBootstrap.Instance.MapLayout.GetPlayerSpawnWorld();

        return Vector3.zero;
    }

    private Vector3 ResolveTowerSpawnSlot(Vector3 spawnCenter, int index, int count)
    {
        return CoopMapSpawnUtility.ResolveTowerSpawn(index, count, spawnCenter, useMapMode);
    }

    private void SpawnRealTowers()
    {
        var index = 0;
        foreach (var player in players.Values)
        {
            if (string.IsNullOrWhiteSpace(player.towerCode))
            {
                var tank = CoopTankCatalog.GetRandom(random);
                CoopTankCatalog.ApplyBaseStats(player, tank);
            }

            var unit = CoopPlayerTowerFactory.CreatePlayerTank(entityRoot, player, index++);
            liveTowers[player.playerId] = unit;
            unit.ApplyState(player, snapPosition: true);
            Debug.Log($"[CoopGameSession] 탱크 스폰: {player.playerName} / {unit.TankDisplayName} ({player.towerCode}) / 스킬:{CoopSkillCatalog.ResolveDisplayName(player.skillId)} @ ({player.towerX:0.0}, {player.towerZ:0.0})");
        }
    }

    private void HostTick()
    {
        if (!waveActive)
        {
            waveBreakTimer -= Time.deltaTime;
            if (waveBreakTimer <= 0f)
                BeginNextWave();
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (enemiesToSpawn > 0 && spawnTimer <= 0f)
        {
            spawnTimer = SpawnInterval;
            SpawnWaveEnemy();
            enemiesToSpawn--;
        }

        if (enemiesToSpawn <= 0 && GetAliveEnemyCount() == 0)
            EndWave();

        SyncTowerTransformsToState();
    }

    private void TickSkillCooldowns()
    {
        foreach (var player in players.Values)
        {
            if (player.skillCooldown > 0f)
                player.skillCooldown = Mathf.Max(0f, player.skillCooldown - Time.deltaTime);
        }
    }

    private bool HostTryCastSkill(string playerId, float x, float z)
    {
        if (!players.TryGetValue(playerId, out var player))
            return false;

        if (!CoopSkillExecutor.TryCast(this, player, new Vector3(x, 0f, z), out var message))
        {
            if (!string.IsNullOrEmpty(message))
                SetAnnouncement(message);
            return false;
        }

        SetAnnouncement($"{player.playerName}: {message}");
        BroadcastState();
        return true;
    }

    private int GetAliveEnemyCount() => liveSyncedMonsters.Count + liveEnemies.Count;

    private void ApplyOrder(string playerId, int orderType, float x, float z, int attackTargetId)
    {
        if (!players.TryGetValue(playerId, out var player))
            return;

        player.orderType = orderType;
        player.orderX = x;
        player.orderZ = z;
        player.attackTargetId = attackTargetId;
        player.hasMoveTarget = orderType == CoopGameProtocol.OrderMove
            || orderType == CoopGameProtocol.OrderAttackMove;
        player.moveTargetX = x;
        player.moveTargetZ = z;
    }

    private Vector3 ResolveMoveTarget(string playerId, Vector3 target)
    {
        if (!players.TryGetValue(playerId, out var player))
            return target;

        if (DefenseMapPathfinder.IsReady && !DefenseMapPathfinder.IsWorldWalkable(target))
            target = CoopMapSpawnUtility.SnapToWalkableWorld(target);

        var current = new Vector3(player.towerX, 0f, player.towerZ);
        var flat = target - current;
        flat.y = 0f;
        if (flat.sqrMagnitude >= 0.5f * 0.5f)
            return target;

        if (flat.sqrMagnitude < 0.0001f)
            flat = Vector3.forward;

        var pushed = current + flat.normalized * 3f;
        if (DefenseMapPathfinder.IsReady && !DefenseMapPathfinder.IsWorldWalkable(pushed))
            pushed = CoopMapSpawnUtility.SnapToWalkableWorld(pushed);

        return pushed;
    }

    public bool TryGetLivingTower(string playerId, out CoopPlayerTowerUnit unit)
    {
        return TryFindEnemyInRange(origin, range, out enemyPosition);
    }

    private bool TryFindEnemyInRange(Vector3 origin, float range, out Vector3 enemyPosition)
    {
        enemyPosition = default;
        var best = range * range;
        var found = false;

        foreach (var synced in liveSyncedMonsters.Values)
        {
            if (synced == null || !synced.TryGetWorldPosition(out var pos))
                continue;

            var sqr = (pos - origin).sqrMagnitude;
            if (sqr > best)
                continue;

            best = sqr;
            enemyPosition = pos;
            found = true;
        }

        foreach (var actor in liveEnemies.Values)
        {
            if (actor == null)
                continue;

            var pos = actor.transform.position;
            var sqr = (pos - origin).sqrMagnitude;
            if (sqr > best)
                continue;

            best = sqr;
            enemyPosition = pos;
            found = true;
        }

        return found;
    }

    private void SyncTowerTransformsToState()
    {
        foreach (var pair in liveTowers)
        {
            if (!players.TryGetValue(pair.Key, out var player) || pair.Value == null)
                continue;

            var pos = pair.Value.transform.position;
            player.towerX = pos.x;
            player.towerZ = pos.z;

            var health = pair.Value.GetComponent<Health>();
            if (health != null)
            {
                player.towerHp = health.CurrentHealth;
                player.towerMaxHp = health.MaxHealth;
            }
        }

        CheckAllTowersDestroyed();
    }

    private void BeginNextWave()
    {
        currentWave++;
        waveActive = true;
        goldRush = currentWave % 4 == 0;
        enemiesToSpawn = useMapMode ? 10 + currentWave * 5 : 5 + currentWave * 3;
        spawnTimer = 0.25f;
        RefreshFarmGates();

        if (currentWave % 5 == 0)
        {
            enemiesToSpawn += 3;
            SetAnnouncement($"보스 웨이브 {currentWave}! 붉은 매복 구역과 비콘에서 대규모 기습!");
        }
        else if (goldRush)
        {
            SetAnnouncement($"골드 러시! 웨이브 {currentWave} — 처치 골드 2배! 보급 캐시를 함께 지키세요.");
        }
        else if (useMapMode)
        {
            SetAnnouncement($"웨이브 {currentWave}! 적이 맵 전역에서 랜덤·매복 출현 — 초록 발판으로 농장 문을 여세요.");
        }
        else
        {
            SetAnnouncement($"웨이브 {currentWave} 시작!");
        }
    }

    private void EndWave()
    {
        waveActive = false;
        waveBreakTimer = WaveBreakDuration;
        foreach (var player in players.Values)
        {
            player.killStreak = 0;
            player.orderType = CoopGameProtocol.OrderNone;
        }

        RefreshFarmGates();
        SetAnnouncement($"웨이브 {currentWave} 클리어! {WaveBreakDuration:0}초 휴식 — 농장 문이 열립니다.");
    }

    private void SpawnWaveEnemy()
    {
        var bossWave = currentWave % 5 == 0;
        if (monsterSpawner != null)
        {
            monsterSpawner.TrySpawnForWave(currentWave, bossWave);
            return;
        }

        var angle = (float)(random.NextDouble() * Math.PI * 2d);
        var radius = useMapMode && CoopMapBootstrap.Instance != null
            ? CoopMapBootstrap.Instance.MapHalfExtent * 0.85f
            : 20f;
        var pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        SpawnFallbackEnemyAt(pos, bossWave, currentWave);
    }

    public void SpawnFallbackEnemyAt(Vector3 position, bool boss, int wave)
    {
        var maxHp = boss ? 180f + wave * 40f : 35f + wave * 8f;
        var defense = boss ? 4 + wave / 2 : 1 + wave / 3;
        var speed = boss ? 2.2f : 3.2f + wave * 0.08f;
        var goldReward = boss ? 50 + wave * 8 : 10 + wave * 2;
        var monsterCode = CoopGameProtocol.EnemyVisualTypes[wave % CoopGameProtocol.EnemyVisualTypes.Length];
        var id = nextEnemyId++;

        position = CoopMapSpawnUtility.SnapToWalkableWorld(position);
        var enemyObject = new GameObject(boss ? $"Boss_{id}" : $"Enemy_{id}");
        enemyObject.transform.SetParent(entityRoot, false);
        var actor = enemyObject.AddComponent<CoopEnemyActor>();
        actor.Initialize(this, id, position, maxHp, defense, speed, boss, goldReward, monsterCode);
        liveEnemies[id] = actor;
    }

    private void EndGame(string message)
    {
        gameOver = true;
        lobby.SendCoopToAll(JsonConvert.SerializeObject(new CoopEventPayload
        {
            type = CoopGameProtocol.GameOver,
            message = message
        }));
        OnGameEnded?.Invoke(false, message);
    }

    private bool HostTryUpgrade(string playerId, string upgradeKey)
    {
        if (!players.TryGetValue(playerId, out var player))
            return false;

        var cost = CoopUpgradeRules.GetCost(upgradeKey, GetUpgradeLevel(player, upgradeKey));
        if (player.gold < cost)
        {
            SetAnnouncement("골드가 부족합니다.");
            return false;
        }

        player.gold -= cost;
        CoopUpgradeRules.Apply(player, upgradeKey);

        if (liveTowers.TryGetValue(playerId, out var unit) && unit != null)
            unit.ApplyState(player, snapPosition: false);

        SetAnnouncement($"{player.playerName} 업그레이드 완료!");
        BroadcastState();
        return true;
    }

    private static int GetUpgradeLevel(CoopPlayerState player, string upgradeKey)
    {
        return upgradeKey switch
        {
            CoopGameProtocol.UpgradeAttack => player.atkLevel,
            CoopGameProtocol.UpgradeHealth => player.hpLevel,
            CoopGameProtocol.UpgradeSpeed => player.spdLevel,
            CoopGameProtocol.UpgradePenetration => player.penLevel,
            _ => 0
        };
    }

    private void HandleCoopMessage(string senderPlayerId, string json)
    {
        if (string.IsNullOrEmpty(json))
            return;

        if (json.Contains(CoopGameProtocol.StateSync))
        {
            ApplyState(JsonConvert.DeserializeObject<CoopSyncPayload>(json));
            return;
        }

        if (json.Contains(CoopGameProtocol.UpgradeRequest) && IsHostAuthority)
        {
            var request = JsonConvert.DeserializeObject<CoopUpgradeRequest>(json);
            if (request != null)
                HostTryUpgrade(request.playerId, request.upgradeKey);
            return;
        }

        if (json.Contains(CoopGameProtocol.OrderRequest) && IsHostAuthority)
        {
            var request = JsonConvert.DeserializeObject<CoopOrderRequest>(json);
            if (request != null)
                ApplyOrder(request.playerId, request.orderType, request.x, request.z, request.attackTargetId);
            return;
        }

        if (json.Contains(CoopGameProtocol.SkillRequest) && IsHostAuthority)
        {
            var request = JsonConvert.DeserializeObject<CoopSkillRequest>(json);
            if (request != null)
                HostTryCastSkill(request.playerId, request.x, request.z);
            return;
        }

        if (json.Contains(CoopGameProtocol.MoveRequest) && IsHostAuthority)
        {
            var request = JsonConvert.DeserializeObject<CoopMoveRequest>(json);
            if (request != null)
                ApplyOrder(request.playerId, CoopGameProtocol.OrderMove, request.x, request.z, -1);
            return;
        }

        if (json.Contains(CoopGameProtocol.Event))
        {
            var evt = JsonConvert.DeserializeObject<CoopEventPayload>(json);
            if (evt != null && !string.IsNullOrEmpty(evt.message))
                SetAnnouncement(evt.message);
            return;
        }

        if (json.Contains(CoopGameProtocol.GameOver))
        {
            var evt = JsonConvert.DeserializeObject<CoopEventPayload>(json);
            gameOver = true;
            OnGameEnded?.Invoke(false, evt?.message ?? "게임 종료");
        }
    }

    private void ApplyState(CoopSyncPayload state)
    {
        if (state == null)
            return;

        LatestState = state;
        currentWave = state.wave;
        waveActive = state.waveActive;
        goldRush = state.goldRush;
        announcement = state.announcement;
        farmGateOpen = state.farmGateOpen;
        CoopFarmGateSync.ApplyState(farmGateOpen);
        lastSyncedWaveActive = waveActive;

        players.Clear();
        if (state.players != null)
        {
            foreach (var player in state.players)
            {
                players[player.playerId] = player;
                if (liveTowers.TryGetValue(player.playerId, out var unit) && unit != null)
                    unit.ApplyState(player, snapPosition: false);
            }
        }

        OnStateUpdated?.Invoke(state);
        if (!string.IsNullOrEmpty(state.announcement))
            OnAnnouncement?.Invoke(state.announcement);
    }

    private void BroadcastState()
    {
        var playerArray = new CoopPlayerState[players.Count];
        players.Values.CopyTo(playerArray, 0);

        var enemyList = new List<CoopEnemyState>();
        foreach (var synced in liveSyncedMonsters.Values)
        {
            if (synced != null)
                enemyList.Add(synced.ToState());
        }

        foreach (var actor in liveEnemies.Values)
        {
            if (actor != null)
                enemyList.Add(actor.ToState());
        }

        var payload = new CoopSyncPayload
        {
            nexusHp = 0f,
            nexusMaxHp = 0f,
            wave = currentWave,
            waveActive = waveActive,
            goldRush = goldRush,
            farmGateOpen = farmGateOpen,
            aliveEnemies = enemyList.Count,
            announcement = announcement,
            players = playerArray,
            enemies = enemyList.ToArray()
        };

        LatestState = payload;
        OnStateUpdated?.Invoke(payload);
        lobby.SendCoopToAll(JsonConvert.SerializeObject(payload));
    }

    private void BroadcastEvent(string message, string playerId, long goldDelta)
    {
        lobby.SendCoopToAll(JsonConvert.SerializeObject(new CoopEventPayload
        {
            type = CoopGameProtocol.Event,
            playerId = playerId,
            message = message,
            goldDelta = goldDelta,
            wave = currentWave
        }));
        SetAnnouncement(message);
    }

    public void SetAnnouncement(string message)
    {
        announcement = message;
        announcementTimer = 3f;
        OnAnnouncement?.Invoke(message);
    }
}
