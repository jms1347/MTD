using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class CwslMonsterSpawner : NetworkBehaviour
{
    [SerializeField] private float spawnInterval = CwslGameConstants.SpawnIntervalSeconds;
    [SerializeField] private int maxAliveMonsters = CwslGameConstants.MaxAliveMonsters;

    private float spawnTimer;
    private int aliveCount;

    public bool SpawningEnabled { get; set; } = true;

    private void Update()
    {
        if (!IsServer || !SpawningEnabled)
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
        QueueSpawnWithWarning(CwslArenaUtility.GetRandomSpawnPosition(), CwslMonsterType.Melee);
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

        StartCoroutine(SpawnWithWarningRoutine(position, forcedType));
    }

    private IEnumerator SpawnWithWarningRoutine(Vector3 position, CwslMonsterType forcedType)
    {
        var resolvedType = ResolveSpawnType(forcedType);
        var isExecutive = forcedType == CwslMonsterType.Melee &&
                          resolvedType == CwslMonsterType.Melee &&
                          Random.value < CwslGameConstants.ExecutiveSpawnChance;
        ShowSpawnWarningClientRpc(position, (int)resolvedType, CwslGameConstants.MonsterSpawnWarningSeconds);

        yield return new WaitForSeconds(CwslGameConstants.MonsterSpawnWarningSeconds);

        if (!IsServer || !SpawningEnabled)
            yield break;

        if (CwslKarmaSystem.Instance != null && CwslKarmaSystem.Instance.IsBossThresholdReached)
            yield break;

        if (aliveCount >= maxAliveMonsters)
            yield break;

        SpawnMonsterAtServer(position, resolvedType, isExecutive);
    }

    private static CwslMonsterType ResolveSpawnType(CwslMonsterType forcedType)
    {
        if (forcedType != CwslMonsterType.Melee)
            return forcedType;

        var roll = Random.Range(0, 3);
        return roll switch
        {
            0 => CwslMonsterType.Ranged,
            1 => CwslMonsterType.Suicide,
            _ => CwslMonsterType.Melee
        };
    }

    private void SpawnMonsterAtServer(Vector3 position, CwslMonsterType type, bool isExecutive)
    {
        var session = CwslGameSession.Instance;
        if (session == null)
            return;

        var prefab = session.GetMonsterPrefab(type);
        if (prefab == null)
            return;

        var networkObject = CwslNetworkPoolService.Instance?.Get(prefab, position, Quaternion.identity);
        if (networkObject == null)
            return;

        var instance = networkObject.gameObject;
        var health = instance.GetComponent<CwslMonsterHealth>();
        var monsterBase = instance.GetComponent<CwslMonsterBase>();
        monsterBase?.Initialize(type);
        if (health != null)
        {
            health.Configure(
                type,
                isExecutive ? CwslGameConstants.GoldDropExecutive : CwslGameConstants.GoldDropNormal,
                isExecutive);
            health.OnKilled += HandleMonsterKilled;
        }

        aliveCount++;
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
