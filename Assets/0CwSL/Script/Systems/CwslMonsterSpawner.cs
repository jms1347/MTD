using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CwslMonsterSpawner : NetworkBehaviour
{
    [SerializeField] private float spawnInterval = CwslGameConstants.SpawnIntervalSeconds;
    [SerializeField] private int maxAliveMonsters = CwslGameConstants.MaxAliveMonsters;

    private float spawnTimer;
    private int aliveCount;
    private readonly List<float> baseSpawnTimers = new();

    public bool SpawningEnabled { get; set; } = true;

    private void Update()
    {
        if (!IsServer || !SpawningEnabled)
            return;

        if (CwslGameConstants.UseDefenseMode)
            return;

        if (CwslKarmaSystem.Instance != null && CwslKarmaSystem.Instance.IsBossThresholdReached)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
            return;

        if (aliveCount >= maxAliveMonsters)
        {
            spawnTimer = spawnInterval;
            return;
        }

        spawnTimer = spawnInterval;
        QueueSpawnWithWarning(CwslArenaUtility.GetRandomMonsterSpawnPosition(), CwslMonsterType.Melee);
    }

    public void TickDefenseSpawnsServer(float dt, IReadOnlyList<Vector3> basePositions, float intervalPerBase)
    {
        if (!IsServer || !SpawningEnabled || basePositions == null)
            return;

        while (baseSpawnTimers.Count < basePositions.Count)
            baseSpawnTimers.Add(Random.Range(0f, intervalPerBase));

        var manager = CwslMonsterManager.Instance;
        var maxAlive = manager != null ? manager.MaxAliveMonsters : maxAliveMonsters;
        var interval = manager != null ? manager.SpawnIntervalPerBase : intervalPerBase;

        for (var i = 0; i < basePositions.Count; i++)
        {
            baseSpawnTimers[i] -= dt;
            if (baseSpawnTimers[i] > 0f)
                continue;

            baseSpawnTimers[i] = interval;
            if (aliveCount >= maxAlive)
                continue;

            var type = RollDefenseMinionType();
            QueueDefenseSpawnServer(basePositions[i], type);
        }
    }

    public void QueueDefenseSpawnServer(Vector3 position, CwslMonsterType forcedType)
    {
        if (!IsServer)
            return;

        StartCoroutine(SpawnWithWarningRoutine(position, forcedType, useDefenseRules: true));
    }

    public void SpawnMonstersNearServer(Vector3 center, int count, float spreadRadius)
    {
        SpawnMonstersNearServer(center, count, spreadRadius, CwslMonsterType.Melee);
    }

    public void SpawnSuicidesNearServer(Vector3 center, int count, float spreadRadius)
    {
        SpawnMonstersNearServer(center, count, spreadRadius, CwslMonsterType.Suicide);
    }

    public void SpawnMonstersInRingServer(
        Vector3 center,
        int count,
        float minRadius,
        float maxRadius,
        CwslMonsterType forcedType)
    {
        if (!IsServer || count <= 0)
            return;

        minRadius = Mathf.Max(0f, minRadius);
        maxRadius = Mathf.Max(minRadius, maxRadius);

        for (var i = 0; i < count; i++)
        {
            if (aliveCount >= maxAliveMonsters)
                break;

            var angle = Random.Range(0f, Mathf.PI * 2f);
            var radius = Random.Range(minRadius, maxRadius);
            var offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            var position = new Vector3(center.x + offset.x, CwslGameConstants.SpawnHeight, center.z + offset.z);
            position = CwslArenaUtility.ClampToArena(position);
            QueueSpawnWithWarning(position, forcedType);
        }
    }

    public void SpawnMonstersNearServer(Vector3 center, int count, float spreadRadius, CwslMonsterType forcedType)
    {
        if (!IsServer || count <= 0)
            return;

        for (var i = 0; i < count; i++)
        {
            if (aliveCount >= maxAliveMonsters)
                break;

            var offset = Random.insideUnitCircle * spreadRadius;
            var position = new Vector3(center.x + offset.x, CwslGameConstants.SpawnHeight, center.z + offset.y);
            position = CwslArenaUtility.ClampToArena(position);
            QueueSpawnWithWarning(position, forcedType);
        }
    }

    private void QueueSpawnWithWarning(Vector3 position, CwslMonsterType forcedType)
    {
        if (!IsServer)
            return;

        StartCoroutine(SpawnWithWarningRoutine(position, forcedType, useDefenseRules: false));
    }

    private IEnumerator SpawnWithWarningRoutine(Vector3 position, CwslMonsterType forcedType, bool useDefenseRules)
    {
        var resolvedType = useDefenseRules ? forcedType : ResolveSpawnType(forcedType);
        var isExecutive = !useDefenseRules &&
                          forcedType == CwslMonsterType.Melee &&
                          resolvedType == CwslMonsterType.Melee &&
                          Random.value < CwslGameConstants.ExecutiveSpawnChance;

        var warningSeconds = useDefenseRules && CwslMonsterManager.Instance != null
            ? CwslMonsterManager.Instance.SpawnWarningSeconds
            : CwslGameConstants.MonsterSpawnWarningSeconds;

        ShowSpawnWarningClientRpc(position, (int)resolvedType, warningSeconds);
        yield return new WaitForSeconds(warningSeconds);

        if (!IsServer || !SpawningEnabled)
            yield break;

        if (!useDefenseRules &&
            CwslKarmaSystem.Instance != null &&
            CwslKarmaSystem.Instance.IsBossThresholdReached)
            yield break;

        var maxAlive = useDefenseRules && CwslMonsterManager.Instance != null
            ? CwslMonsterManager.Instance.MaxAliveMonsters
            : maxAliveMonsters;
        if (aliveCount >= maxAlive)
            yield break;

        SpawnMonsterAtServer(position, resolvedType, isExecutive);
    }

    private static CwslMonsterType RollDefenseMinionType()
    {
        if (Random.value < CwslGameConstants.InkSniperSpawnChance)
            return Random.value < 0.4f ? CwslMonsterType.NexusInkSniper : CwslMonsterType.InkSniper;

        return Random.Range(0, 7) switch
        {
            0 => CwslMonsterType.Melee,
            1 => CwslMonsterType.Ranged,
            2 => CwslMonsterType.Suicide,
            3 => CwslMonsterType.StickySuicide,
            4 => CwslMonsterType.NexusMelee,
            5 => CwslMonsterType.NexusRanged,
            _ => CwslMonsterType.NexusSuicide
        };
    }

    private static CwslMonsterType ResolveSpawnType(CwslMonsterType forcedType)
    {
        if (forcedType != CwslMonsterType.Melee)
            return forcedType;

        if (Random.value < CwslGameConstants.InkSniperSpawnChance)
            return CwslMonsterType.InkSniper;

        return Random.Range(0, 4) switch
        {
            0 => CwslMonsterType.Ranged,
            1 => CwslMonsterType.Suicide,
            2 => CwslMonsterType.StickySuicide,
            _ => CwslMonsterType.Melee
        };
    }

    private NetworkObject SpawnMonsterAtServer(Vector3 position, CwslMonsterType type, bool isExecutive)
    {
        var session = CwslGameSession.Instance;
        if (session == null)
            return null;

        var prefab = session.GetMonsterPrefab(type);
        if (prefab == null)
            return null;

        var networkObject = CwslNetworkPoolService.Instance?.Get(prefab, position, Quaternion.identity);
        if (networkObject == null)
            return null;

        var instance = networkObject.gameObject;
        instance.transform.localScale = Vector3.one;

        var health = instance.GetComponent<CwslMonsterHealth>();
        var monsterBase = instance.GetComponent<CwslMonsterBase>();
        monsterBase?.Initialize(type);
        if (health != null && monsterBase == null)
        {
            health.Configure(
                type,
                isExecutive ? CwslGameConstants.GoldDropExecutive : CwslGameConstants.GoldDropNormal,
                isExecutive);
        }

        if (health != null)
            health.OnKilled += HandleMonsterKilled;

        aliveCount++;
        return networkObject;
    }

    /// <summary>홍명보 「싸워」 — 맵 가장자리 엘리트 2 + 자폭 3, 최저 HP 플레이어 추적.</summary>
    public void SpawnBossElitePackAtEdgeServer(Vector3 edgeCenter, NetworkObject forcedPlayerTarget)
    {
        if (!IsServer || forcedPlayerTarget == null)
            return;

        var spread = 3.5f;
        SpawnBossSkillMonsterImmediate(edgeCenter, CwslMonsterType.KoreaUniversitySoldier, forcedPlayerTarget);
        SpawnBossSkillMonsterImmediate(
            edgeCenter + new Vector3(spread, 0f, 0f),
            CwslMonsterType.KoreaUniversitySoldier,
            forcedPlayerTarget);
        SpawnBossSkillMonsterImmediate(
            edgeCenter + new Vector3(-spread * 0.5f, 0f, spread),
            CwslMonsterType.Suicide,
            forcedPlayerTarget);
        SpawnBossSkillMonsterImmediate(
            edgeCenter + new Vector3(spread * 0.5f, 0f, -spread),
            CwslMonsterType.Suicide,
            forcedPlayerTarget);
        SpawnBossSkillMonsterImmediate(
            edgeCenter + new Vector3(0f, 0f, spread * 0.8f),
            CwslMonsterType.Suicide,
            forcedPlayerTarget);
    }

    private void SpawnBossSkillMonsterImmediate(
        Vector3 position,
        CwslMonsterType type,
        NetworkObject forcedPlayerTarget)
    {
        if (aliveCount >= maxAliveMonsters)
            return;

        position = CwslArenaUtility.ClampToArena(position);
        var spawned = SpawnMonsterAtServer(position, type, isExecutive: false);
        if (spawned == null)
            return;

        var forced = spawned.GetComponent<CwslMonsterForcedTarget>();
        if (forced == null)
            forced = spawned.gameObject.AddComponent<CwslMonsterForcedTarget>();
        forced.SetTargetServer(forcedPlayerTarget);
    }

    private void HandleMonsterKilled(CwslMonsterHealth monster, ulong attackerClientId)
    {
        if (!IsServer)
            return;

        aliveCount = Mathf.Max(0, aliveCount - 1);
        if (monster != null)
            monster.OnKilled -= HandleMonsterKilled;
    }

    [ClientRpc]
    private void ShowSpawnWarningClientRpc(Vector3 position, int monsterType, float durationSeconds)
    {
        CwslMonsterSpawnWarningVisual.Show(position, (CwslMonsterType)monsterType, durationSeconds);
    }
}
