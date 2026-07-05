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

        spawnTimer = spawnInterval;
        if (aliveCount >= maxAliveMonsters)
            return;

        SpawnRandomMonster();
    }

    public void SpawnMonstersNearServer(Vector3 center, int count, float spreadRadius)
    {
        SpawnMonstersNearServer(center, count, spreadRadius, CwslMonsterType.Melee);
    }

    public void SpawnSuicidesNearServer(Vector3 center, int count, float spreadRadius)
    {
        SpawnMonstersNearServer(center, count, spreadRadius, CwslMonsterType.Suicide);
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
            SpawnMonsterAtServer(position, forcedType);
        }
    }

    private void SpawnRandomMonster()
    {
        SpawnMonsterAtServer(CwslArenaUtility.GetRandomSpawnPosition(), CwslMonsterType.Melee);
    }

    private void SpawnMonsterAtServer(Vector3 position, CwslMonsterType forcedType = CwslMonsterType.Melee)
    {
        var session = CwslGameSession.Instance;
        if (session == null)
            return;

        var isExecutive = forcedType == CwslMonsterType.Melee && Random.value < CwslGameConstants.ExecutiveSpawnChance;
        var type = forcedType;
        if (forcedType == CwslMonsterType.Melee)
        {
            var roll = Random.Range(0, 3);
            type = roll switch
            {
                0 => CwslMonsterType.Ranged,
                1 => CwslMonsterType.Suicide,
                _ => CwslMonsterType.Melee
            };
        }

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
}
