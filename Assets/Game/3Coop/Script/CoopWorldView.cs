using System.Collections.Generic;
using UnityEngine;

public class CoopWorldView : MonoBehaviour
{
    private readonly Dictionary<string, CoopPlayerTowerUnit> mirroredTowers = new();
    private readonly Dictionary<int, Transform> mirroredEnemies = new();
    private CoopGameSession session;

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

    private void OnDestroy()
    {
        if (session != null)
            session.OnStateUpdated -= Refresh;
    }

    private void BuildFallbackArena()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "CoopGround";
        ground.transform.SetParent(transform);
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
        ground.GetComponent<Renderer>().material.color = new Color(0.35f, 0.42f, 0.32f);
        ground.tag = "Ground";
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
            activeIds.Add(enemy.id);
            var view = GetOrCreateMirroredEnemy(enemy);
            var y = enemy.isBoss ? 0.2f : 0.08f;
            view.position = Vector3.Lerp(
                view.position,
                new Vector3(enemy.x, y, enemy.z),
                Time.deltaTime * 12f);

            view.localScale = Vector3.one;
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
        CoopSlimeVisualFactory.BuildMirrored(root.transform, code, enemy.speed, enemy.isBoss);
        mirroredEnemies[enemy.id] = root.transform;
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
            mirroredEnemies.Remove(id);
    }
}
