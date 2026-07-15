using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class StllStageWaveSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject miniBossPrefab;

    private readonly List<Coroutine> activeRoutines = new();
    private bool running;
    private bool isHulao;

    public void BeginSashuguanServer()
    {
        isHulao = false;
        BeginWaves(StllSashuguanWaveTable.Waves);
    }

    public void BeginHulaoServer()
    {
        isHulao = true;
        StopStageServer();
        running = true;
        StartCoroutine(SpawnLuBuRoutine());
    }

    public void StopStageServer()
    {
        running = false;
        StopAllRoutines();
    }

    private void BeginWaves(IReadOnlyList<StllWaveDefinition> waves)
    {
        if (!IsServer || enemyPrefab == null)
            return;

        StopStageServer();
        running = true;
        for (var i = 0; i < waves.Count; i++)
            activeRoutines.Add(StartCoroutine(RunWaveRoutine(waves[i])));
    }

    private void StopAllRoutines()
    {
        for (var i = 0; i < activeRoutines.Count; i++)
        {
            if (activeRoutines[i] != null)
                StopCoroutine(activeRoutines[i]);
        }

        activeRoutines.Clear();
    }

    private IEnumerator RunWaveRoutine(StllWaveDefinition wave)
    {
        yield return new WaitForSeconds(wave.StartSeconds);
        if (!running)
            yield break;

        for (var s = 0; s < wave.Spawns.Count; s++)
        {
            var spawn = wave.Spawns[s];
            if (spawn.SpawnMiniBoss)
            {
                SpawnMiniBossServer(spawn.SpawnDirection);
                continue;
            }

            for (var i = 0; i < spawn.Count; i++)
            {
                if (!running)
                    yield break;

                SpawnEnemyServer(spawn.Kind, spawn.SpawnDirection);
                yield return new WaitForSeconds(spawn.IntervalSeconds);
            }
        }
    }

    private IEnumerator SpawnLuBuRoutine()
    {
        yield return new WaitForSeconds(2f);
        if (!running)
            yield break;

        var bossPrefab = StllRunController.Instance?.BossPrefab;
        if (bossPrefab == null)
            yield break;

        var pos = StllPrimitiveMapBuilder.GetHulaoBossPosition();
        var instance = Instantiate(bossPrefab, pos, Quaternion.identity);
        var netObj = instance.GetComponent<NetworkObject>();
        if (netObj == null)
            yield break;

        netObj.Spawn(true);
        var boss = instance.GetComponent<StllBossLuBu>();
        boss?.ConfigureServer(StllEaConstants.BossLuBuHealth3P);
    }

    private void SpawnMiniBossServer(int direction)
    {
        if (miniBossPrefab == null)
            return;

        var spawnPos = StllPrimitiveMapBuilder.GetSpawnPoint(direction, true);
        var instance = Instantiate(miniBossPrefab, spawnPos, Quaternion.identity);
        var netObj = instance.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Destroy(instance);
            return;
        }

        netObj.Spawn(true);
        instance.GetComponent<StllMiniBossHuangYing>()
            ?.ConfigureServer(StllEaConstants.MiniBossHuangYingHealth);
    }

    private void SpawnEnemyServer(StllEnemyKind kind, int direction)
    {
        var spawnPos = StllPrimitiveMapBuilder.GetSpawnPoint(direction, !isHulao);
        var center = isHulao
            ? StllPrimitiveMapBuilder.HulaoRoot.position
            : StllPrimitiveMapBuilder.StageRoot.position;
        var look = center - spawnPos;
        look.y = 0f;
        if (look.sqrMagnitude < 0.01f)
            look = Vector3.forward;

        var instance = Instantiate(enemyPrefab, spawnPos, Quaternion.LookRotation(look.normalized, Vector3.up));
        var netObj = instance.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Destroy(instance);
            return;
        }

        netObj.Spawn(true);

        var ai = instance.GetComponent<StllEnemyGruntAI>();
        if (ai == null)
            return;

        var maxHp = kind switch
        {
            StllEnemyKind.Archer => 55f,
            StllEnemyKind.Charger => 120f,
            StllEnemyKind.Arsonist => 70f,
            StllEnemyKind.EliteGuard => 200f,
            _ => 80f
        };

        ai.ConfigureServer(kind, maxHp, Color.gray);
        instance.GetComponent<StllEnemyHealth>()?.ConfigureServer(maxHp);
    }
}
