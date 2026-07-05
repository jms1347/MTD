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

    public static bool TryGetNearestCombatTarget(
        Vector3 from,
        CwslMonsterTargetingMode mode,
        out NetworkObject target,
        out float distance)
    {
        target = null;
        distance = float.MaxValue;

        var hasPlayer = TryGetNearestLivingPlayer(from, out var player, out var playerDistance);
        var hasNexus = TryGetNexus(from, out var nexus, out var nexusDistance);

        if (mode == CwslMonsterTargetingMode.NexusFirst && hasNexus)
        {
            target = nexus;
            distance = nexusDistance;
            return true;
        }

        if (!hasPlayer && !hasNexus)
            return false;

        if (hasPlayer && (!hasNexus || playerDistance <= nexusDistance))
        {
            target = player;
            distance = playerDistance;
            return true;
        }

        target = nexus;
        distance = nexusDistance;
        return true;
    }

    public static bool TryGetNexus(Vector3 from, out NetworkObject nexusObject, out float distance)
    {
        nexusObject = null;
        distance = float.MaxValue;

        var nexus = CwslNexus.Instance;
        if (nexus == null || !nexus.IsAlive)
            return false;

        nexusObject = nexus.GetComponent<NetworkObject>();
        if (nexusObject == null || !nexusObject.IsSpawned)
            return false;

        var flat = nexus.transform.position - from;
        flat.y = 0f;
        distance = flat.magnitude;
        return true;
    }

    public static bool TryGetClosestPlayerInRadius(Vector3 from, float radius, out NetworkObject target)
    {
        target = null;
        var bestDistance = radius;

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

            var pickupPoint = playerObject.transform.position + Vector3.up * 0.35f;
            var flat = pickupPoint - from;
            flat.y = 0f;
            var dist = flat.magnitude;
            if (dist > radius || dist >= bestDistance)
                continue;

            bestDistance = dist;
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
