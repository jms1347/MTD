using Unity.Netcode;
using UnityEngine;

public static class CwslGoldDropService
{
    public static void SpawnDrop(Vector3 position)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        var session = CwslGameSession.Instance;
        if (session == null || session.Assets == null || session.Assets.goldPickupPrefab == null)
            return;

        if (!NavMeshUtility.TryProject(position, out var grounded))
            grounded = position;

        var totalGold = Random.Range(CwslGameConstants.GoldDropMin, CwslGameConstants.GoldDropMax + 1);
        for (var i = 0; i < totalGold; i++)
            SpawnSingleCoin(session.Assets.goldPickupPrefab, grounded);
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

        var networkObject = CwslNetworkPoolService.Instance?.Get(
            prefab,
            spawnPosition,
            Quaternion.identity);

        if (networkObject == null)
        {
            var pickup = Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
            networkObject = pickup.GetComponent<NetworkObject>();
            networkObject?.Spawn(true);
        }

        networkObject?.GetComponent<CwslGoldPickup>()?.ConfigureServer(center, spawnPosition);
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
