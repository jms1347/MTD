using Unity.Netcode;
using UnityEngine;

public static class CwslPillDropService
{
    public static void RollMonsterLoot(Vector3 position)
    {
        if (!IsServerActive())
            return;

        if (!NavMeshUtility.TryProject(position, out var grounded))
            grounded = position;

        var goldCount = Random.Range(
            CwslGameConstants.MonsterGoldDropMin,
            CwslGameConstants.MonsterGoldDropMax + 1);
        if (goldCount > 0)
            CwslGoldDropService.SpawnDrop(grounded, goldCount);

        if (Random.value > CwslGameConstants.PillDropChance)
            return;

        SpawnPill(grounded, (CwslPillType)Random.Range(0, 3));
    }

    public static void SpawnPill(Vector3 center, CwslPillType pillType)
    {
        if (!IsServerActive())
            return;

        var session = CwslGameSession.Instance;
        if (session == null || session.Assets == null || session.Assets.pillPickupPrefab == null)
            return;

        var direction = Random.insideUnitCircle;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;
        direction.Normalize();

        var radius = Random.Range(
            CwslGameConstants.GoldDropSpreadRadius * 0.35f,
            CwslGameConstants.GoldDropSpreadRadius * 0.85f);
        var offset = direction * radius;
        var finalPosition = center + new Vector3(offset.x, 0f, offset.y);
        if (!NavMeshUtility.TryProject(finalPosition, out finalPosition))
            finalPosition = center + new Vector3(offset.x, 0f, offset.y);

        var networkObject = CwslNetworkPoolService.Instance?.Get(
            session.Assets.pillPickupPrefab,
            center,
            Quaternion.identity);

        if (networkObject == null)
        {
            Debug.LogError($"[CwSL] 알약 픽업 풀 스폰 실패: {session.Assets.pillPickupPrefab.name}");
            return;
        }

        networkObject?.GetComponent<CwslPillPickup>()?.ConfigureServer(center, finalPosition, pillType);
    }

    private static bool IsServerActive()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
    }
}
