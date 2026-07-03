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

        var amount = Random.Range(CwslGameConstants.GoldDropMin, CwslGameConstants.GoldDropMax + 1);

        if (!NavMeshUtility.TryProject(position, out var grounded))
            grounded = position;

        var networkObject = CwslNetworkPoolService.Instance?.Get(
            session.Assets.goldPickupPrefab,
            grounded,
            Quaternion.identity);

        if (networkObject == null)
        {
            var pickup = Object.Instantiate(session.Assets.goldPickupPrefab, grounded, Quaternion.identity);
            networkObject = pickup.GetComponent<NetworkObject>();
            networkObject?.Spawn(true);
        }

        networkObject?.GetComponent<CwslGoldPickup>()?.ConfigureServer(amount);
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
