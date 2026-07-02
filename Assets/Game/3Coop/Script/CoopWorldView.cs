using System.Collections.Generic;
using UnityEngine;

public class CoopWorldView : MonoBehaviour
{
    public static CoopWorldView Instance { get; private set; }

    private readonly Dictionary<string, CoopPlayerTowerUnit> mirroredTowers = new();
    private readonly Dictionary<int, Transform> mirroredEnemies = new();
    private readonly Dictionary<int, CoopMirroredEnemyProxy> mirroredProxies = new();
    private CoopGameSession session;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (session != null)
            session.OnStateUpdated -= Refresh;
    }

    private void Start()
    {
        session = CoopGameSession.Instance;
        if (session == null)
        {
            Debug.LogWarning("[CoopWorldView] CoopGameSession이 없습니다. CoopSceneBootstrap을 확인하세요.");
            return;
        }

        session.OnStateUpdated += Refresh;
        if (session.LatestState != null)
            Refresh(session.LatestState);
    }

    public bool TryGetMirroredEnemy(int enemyId, out Transform enemyTransform)
    {
        enemyTransform = null;
        if (!mirroredEnemies.TryGetValue(enemyId, out var transform) || transform == null)
            return false;

        enemyTransform = transform;
        return true;
    }

    private void Refresh(CoopSyncPayload state)
    {
        if (state == null || session.IsHostAuthority)
            return;

        RefreshMirroredTowers(state);
        RefreshMirroredEnemies(state);
    }

    private void RefreshMirroredTowers(CoopSyncPayload state)
    {
        var activeIds = new HashSet<string>();
        if (state.players == null)
            return;

        for (var i = 0; i < state.players.Length; i++)
        {
            var player = state.players[i];
            activeIds.Add(player.playerId);

            if (!mirroredTowers.TryGetValue(player.playerId, out var unit) || unit == null)
            {
                if (session.TryGetLivingTower(player.playerId, out var existing) && existing != null)
                {
                    mirroredTowers[player.playerId] = existing;
                    existing.ApplyState(player, snapPosition: false);
                    continue;
                }

                unit = CoopPlayerTowerFactory.CreatePlayerTank(transform, player, i);
                mirroredTowers[player.playerId] = unit;
            }

            unit.ApplyState(player, snapPosition: false);
        }

        RemoveMissingTowers(activeIds);
    }

    private void RefreshMirroredEnemies(CoopSyncPayload state)
    {
        var activeIds = new HashSet<int>();
        if (state.enemies == null)
            return;

        foreach (var enemy in state.enemies)
        {
            if (enemy.hp <= 0f)
                continue;

            activeIds.Add(enemy.id);
            var view = GetOrCreateMirroredEnemy(enemy);
            var y = enemy.isBoss ? 0.2f : 0.08f;
            view.position = Vector3.Lerp(
                view.position,
                new Vector3(enemy.x, y, enemy.z),
                Time.deltaTime * 12f);

            view.localScale = Vector3.one;

            if (mirroredProxies.TryGetValue(enemy.id, out var proxy) && proxy != null)
                proxy.ApplySyncedState(enemy);
        }

        RemoveMissingEnemies(activeIds);
    }

    private Transform GetOrCreateMirroredEnemy(CoopEnemyState enemy)
    {
        if (mirroredEnemies.TryGetValue(enemy.id, out var existing))
            return existing;

        var root = new GameObject(enemy.isBoss ? $"MirroredBoss_{enemy.id}" : $"MirroredEnemy_{enemy.id}");
        root.transform.SetParent(transform, false);
        var code = string.IsNullOrEmpty(enemy.monsterCode)
            ? CoopGameProtocol.EnemyVisualTypes[enemy.id % CoopGameProtocol.EnemyVisualTypes.Length]
            : enemy.monsterCode;
        CoopSlimeVisualFactory.BuildMirrored(root.transform, code, enemy.archetype, enemy.speed, enemy.isBoss);

        var proxy = root.GetComponent<CoopMirroredEnemyProxy>();
        if (proxy == null)
            proxy = root.AddComponent<CoopMirroredEnemyProxy>();
        proxy.Initialize(enemy);

        mirroredEnemies[enemy.id] = root.transform;
        mirroredProxies[enemy.id] = proxy;
        return root.transform;
    }

    private void RemoveMissingTowers(HashSet<string> activeIds)
    {
        var remove = new List<string>();
        foreach (var pair in mirroredTowers)
        {
            if (activeIds.Contains(pair.Key))
                continue;

            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
            remove.Add(pair.Key);
        }

        foreach (var id in remove)
            mirroredTowers.Remove(id);
    }

    private void RemoveMissingEnemies(HashSet<int> activeIds)
    {
        var remove = new List<int>();
        foreach (var pair in mirroredEnemies)
        {
            if (activeIds.Contains(pair.Key))
                continue;

            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
            remove.Add(pair.Key);
        }

        foreach (var id in remove)
        {
            mirroredEnemies.Remove(id);
            mirroredProxies.Remove(id);
        }
    }
}
