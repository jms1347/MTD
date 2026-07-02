using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monster 테이블의 prefabKey → Addressables 로드 후 code별 오브젝트 풀을 관리합니다.
/// </summary>
public class MonsterEnemyPool
{
    private readonly Func<Transform> poolRootProvider;
    private readonly int initialPoolSize;
    private readonly int expandSize;
    private readonly RuntimeAnimatorController animatorController;
    private readonly Face faceAsset;
    private readonly Avatar avatar;

    private readonly Dictionary<string, GameObjectPool> poolsByCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> failedCodes = new(StringComparer.OrdinalIgnoreCase);

    public MonsterEnemyPool(
        Func<Transform> poolRootProvider,
        int initialPoolSize,
        int expandSize,
        RuntimeAnimatorController animatorController,
        Face faceAsset,
        Avatar avatar)
    {
        this.poolRootProvider = poolRootProvider;
        this.initialPoolSize = Mathf.Max(1, initialPoolSize);
        this.expandSize = Mathf.Max(1, expandSize);
        this.animatorController = animatorController;
        this.faceAsset = faceAsset;
        this.avatar = avatar;
    }

    public bool HasAnyPool => poolsByCode.Count > 0;

    public void PrepareForStage(StageData stage)
    {
        if (stage?.spawns == null)
            return;

        for (int i = 0; i < stage.spawns.Count; i++)
        {
            var spawn = stage.spawns[i];
            if (spawn == null || string.IsNullOrWhiteSpace(spawn.monsterCode))
                continue;

            EnsurePoolForSpawnCode(spawn.monsterCode.Trim());
        }
    }

    public void PrepareFromMonsterTable()
    {
        var monsters = DataManager.Instance?.Monsters;
        if (monsters?.All == null)
            return;

        for (int i = 0; i < monsters.All.Count; i++)
        {
            var monster = monsters.All[i];
            if (monster != null && !string.IsNullOrWhiteSpace(monster.code))
                EnsurePoolForData(monster);
        }
    }

    public bool TryGet(string monsterCode, out GameObject instance)
    {
        instance = null;
        if (string.IsNullOrWhiteSpace(monsterCode))
            return false;

        if (!EnsurePoolForDataCode(monsterCode.Trim(), out var poolKey))
            return false;

        instance = poolsByCode[poolKey].Get();
        return instance != null;
    }

    public void Release(string monsterCode, GameObject instance)
    {
        if (instance == null || string.IsNullOrWhiteSpace(monsterCode))
            return;

        var code = monsterCode.Trim();
        if (poolsByCode.TryGetValue(code, out var pool))
            pool.Release(instance);
    }

    private bool EnsurePoolForSpawnCode(string spawnCode)
    {
        if (!TryResolveMonsterData(spawnCode, out var monsterData))
        {
            failedCodes.Add(spawnCode);
            return false;
        }

        return EnsurePoolForData(monsterData);
    }

    private bool EnsurePoolForDataCode(string monsterCode, out string poolKey)
    {
        poolKey = monsterCode;
        if (poolsByCode.ContainsKey(poolKey))
            return true;

        if (failedCodes.Contains(poolKey))
            return false;

        if (!TryResolveMonsterData(monsterCode, out var monsterData))
        {
            failedCodes.Add(poolKey);
            return false;
        }

        poolKey = monsterData.code;
        return EnsurePoolForData(monsterData);
    }

    private bool EnsurePoolForData(MonsterData monsterData)
    {
        if (monsterData == null || string.IsNullOrWhiteSpace(monsterData.code))
            return false;

        var poolKey = monsterData.code.Trim();
        if (poolsByCode.ContainsKey(poolKey))
            return true;

        if (failedCodes.Contains(poolKey))
            return false;

        if (!TryLoadModelPrefab(monsterData, out var modelPrefab))
        {
            failedCodes.Add(poolKey);
            return false;
        }

        var poolRoot = poolRootProvider?.Invoke();
        if (poolRoot == null)
        {
            Debug.LogError("[MonsterEnemyPool] pool root가 없습니다.");
            failedCodes.Add(poolKey);
            return false;
        }

        if (!MonsterRuntimeFactory.TryCreatePoolTemplate(
                monsterData,
                modelPrefab,
                poolRoot,
                animatorController,
                faceAsset,
                avatar,
                out var template))
        {
            Debug.LogError($"[MonsterEnemyPool] '{poolKey}' 템플릿 생성 실패");
            failedCodes.Add(poolKey);
            return false;
        }

        var codePoolRoot = GetOrCreateCodePoolRoot(poolRoot, poolKey);
        poolsByCode[poolKey] = new GameObjectPool(
            template,
            codePoolRoot,
            initialPoolSize,
            expandSize);

        return true;
    }

    private static bool TryResolveMonsterData(string spawnCode, out MonsterData monsterData)
    {
        monsterData = null;
        var dataManager = DataManager.Instance;
        if (dataManager == null)
            return false;

        if (BossSpawnResolver.TryResolve(spawnCode, out var bossMonsterData, out _))
        {
            monsterData = bossMonsterData;
            return monsterData != null;
        }

        return dataManager.TryGetMonster(spawnCode, out monsterData);
    }

    private static bool TryLoadModelPrefab(MonsterData monsterData, out GameObject prefab)
    {
        prefab = null;
        if (monsterData == null || string.IsNullOrWhiteSpace(monsterData.prefabKey))
            return false;

        var prefabKey = monsterData.prefabKey.Trim();
        if (DefenseAddressableLoader.TryLoadPrefab(prefabKey, out prefab) && prefab != null)
            return true;

        Debug.LogError(
            $"[MonsterEnemyPool] prefabKey '{prefabKey}' Addressables 로드 실패 (monster={monsterData.code}). " +
            "AddressableKey.tsv 등록 및 UkDefense → Register Monster Model Addressables를 확인하세요.");
        return false;
    }

    private static Transform GetOrCreateCodePoolRoot(Transform poolRoot, string monsterCode)
    {
        var containerName = $"{monsterCode}_Pool";
        var existing = poolRoot.Find(containerName);
        if (existing != null)
            return existing;

        var container = new GameObject(containerName);
        container.transform.SetParent(poolRoot, false);
        return container.transform;
    }
}
