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

    private void SpawnRandomMonster()
    {
        var session = CwslGameSession.Instance;
        if (session == null)
            return;

        var roll = Random.Range(0, 3);
        var type = roll switch
        {
            0 => CwslMonsterType.Ranged,
            1 => CwslMonsterType.Suicide,
            _ => CwslMonsterType.Melee
        };

        var prefab = session.GetMonsterPrefab(type);
        if (prefab == null)
            return;

        var position = CwslArenaUtility.GetRandomSpawnPosition();
        var networkObject = CwslNetworkPoolService.Instance?.Get(prefab, position, Quaternion.identity);
        if (networkObject == null)
            return;

        var instance = networkObject.gameObject;
        var monsterBase = instance.GetComponent<CwslMonsterBase>();
        monsterBase?.Initialize(type);

        var health = instance.GetComponent<CwslMonsterHealth>();
        if (health != null)
            health.OnKilled += HandleMonsterKilled;

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
