using Unity.Netcode;
using UnityEngine;

public static class CwslGoldDropService
{
    public static void SpawnDrop(Vector3 position, int totalGold = CwslGameConstants.GoldDropNormal)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        var session = CwslGameSession.Instance;
        if (session == null || session.Assets == null || session.Assets.goldPickupPrefab == null)
            return;

        if (!NavMeshUtility.TryProject(position, out var grounded))
            grounded = position;

        totalGold = Mathf.Max(1, totalGold);
        for (var i = 0; i < totalGold; i++)
            SpawnSingleCoin(session.Assets.goldPickupPrefab, grounded);
    }

    public static bool TrySpawnFakeGoldNearPlayer(Vector3 playerPosition)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return false;

        var session = CwslGameSession.Instance;
        if (session == null || session.Assets == null || session.Assets.goldPickupPrefab == null)
            return false;

        var angle = Random.Range(0f, Mathf.PI * 2f);
        var distance = Random.Range(
            CwslGameConstants.FakeGoldSpawnMinDistance,
            CwslGameConstants.FakeGoldSpawnMaxDistance);
        var offset = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        var target = CwslArenaUtility.ClampToArena(playerPosition + offset);
        target.y = CwslGameConstants.SpawnHeight;

        if (!NavMeshUtility.TryProject(target, out target))
            target = playerPosition + offset;

        SpawnSingleCoin(session.Assets.goldPickupPrefab, playerPosition, target, CwslGoldPickupKind.Fake);
        return true;
    }

    public static void ScatterPlayerGoldAcrossArena(CwslPlayerGold playerGold, Vector3 origin)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || playerGold == null)
            return;

        var session = CwslGameSession.Instance;
        if (session == null || session.Assets == null || session.Assets.goldPickupPrefab == null)
            return;

        var scatterAmount = playerGold.Gold / 2;
        if (scatterAmount <= 0)
            return;

        if (!playerGold.TrySpendGoldServer(scatterAmount, playSpendEffect: false))
            return;

        origin.y = CwslGameConstants.SpawnHeight;
        for (var i = 0; i < scatterAmount; i++)
        {
            var randomCenter = CwslArenaUtility.GetRandomSpawnPosition();
            SpawnSingleCoin(session.Assets.goldPickupPrefab, origin, randomCenter, CwslGoldPickupKind.Normal);
        }
    }

    private static void SpawnSingleCoin(
        GameObject prefab,
        Vector3 center,
        Vector3 finalPosition,
        CwslGoldPickupKind kind)
    {
        if (!NavMeshUtility.TryProject(finalPosition, out finalPosition))
            finalPosition = center;

        var networkObject = CwslNetworkPoolService.Instance?.Get(
            prefab,
            center,
            Quaternion.identity);

        if (networkObject == null)
        {
            Debug.LogError($"[CwSL] 골드 픽업 풀 스폰 실패: {prefab.name}");
            return;
        }

        var goldPickup = networkObject?.GetComponent<CwslGoldPickup>();
        if (goldPickup == null)
            return;

        if (kind == CwslGoldPickupKind.Fake)
            goldPickup.ConfigureFakeServer(center, finalPosition);
        else
            goldPickup.ConfigureServer(center, finalPosition);
    }

    private static void SpawnSingleCoin(GameObject prefab, Vector3 center)
    {
        var direction = Random.insideUnitCircle;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;
        direction.Normalize();

        var radius = Random.Range(
            CwslGameConstants.GoldDropSpreadRadius * 0.45f,
            CwslGameConstants.GoldDropSpreadRadius);
        var offset = direction * radius;
        var spawnPosition = center + new Vector3(offset.x, 0f, offset.y);

        if (!NavMeshUtility.TryProject(spawnPosition, out spawnPosition))
            spawnPosition = center + new Vector3(offset.x, 0f, offset.y);

        SpawnSingleCoin(prefab, center, spawnPosition, CwslGoldPickupKind.Normal);
    }
}

internal static class NavMeshUtility
{
    public static bool TryProject(Vector3 position, out Vector3 grounded)
    {
        grounded = position;
        if (UnityEngine.AI.NavMesh.SamplePosition(position, out var hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            grounded = hit.position;
            return true;
        }

        return false;
    }
}
