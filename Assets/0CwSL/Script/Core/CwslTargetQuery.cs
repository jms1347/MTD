using Unity.Netcode;
using UnityEngine;

public static class CwslTargetQuery
{
    public static bool TryGetNearestLivingPlayer(Vector3 from, out NetworkObject target, out float distance)
    {
        target = null;
        distance = float.MaxValue;

        if (NetworkManager.Singleton == null)
            return false;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null || !playerObject.IsSpawned)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health != null && !health.IsAlive)
                continue;

            var flat = playerObject.transform.position - from;
            flat.y = 0f;
            var dist = flat.magnitude;
            if (dist >= distance)
                continue;

            distance = dist;
            target = playerObject;
        }

        return target != null;
    }

    public static Vector3 GetFlatDirection(Vector3 from, Vector3 to)
    {
        var flat = to - from;
        flat.y = 0f;
        return flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
    }
}
