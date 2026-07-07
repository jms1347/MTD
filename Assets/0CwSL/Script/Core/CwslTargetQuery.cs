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

    public static bool TryGetRichestLivingPlayer(out NetworkObject target, out int goldAmount)
    {
        target = null;
        goldAmount = int.MinValue;

        if (NetworkManager.Singleton == null)
            return false;

        var bestGold = int.MinValue;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null || !playerObject.IsSpawned)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health != null && !health.IsAlive)
                continue;

            var gold = playerObject.GetComponent<CwslPlayerGold>();
            var amount = gold != null ? gold.Gold : 0;
            if (amount < bestGold)
                continue;

            bestGold = amount;
            target = playerObject;
        }

        if (target == null)
            return false;

        goldAmount = bestGold;
        return true;
    }

    public static bool TryGetLowestHpLivingPlayer(out NetworkObject target, out float hpAmount)
    {
        target = null;
        hpAmount = float.MaxValue;

        if (NetworkManager.Singleton == null)
            return false;

        var bestHp = float.MaxValue;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null || !playerObject.IsSpawned)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health == null || !health.IsAlive)
                continue;

            var currentHp = health.CurrentHealth;
            if (currentHp >= bestHp)
                continue;

            bestHp = currentHp;
            target = playerObject;
        }

        if (target == null)
            return false;

        hpAmount = bestHp;
        return true;
    }

    public static bool TryGetRandomLivingPlayer(out NetworkObject target)
    {
        target = null;
        if (NetworkManager.Singleton == null)
            return false;

        var candidates = new System.Collections.Generic.List<NetworkObject>();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null || !playerObject.IsSpawned)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health != null && !health.IsAlive)
                continue;

            candidates.Add(playerObject);
        }

        if (candidates.Count == 0)
            return false;

        target = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    public static Vector3 GetFlatDirection(Vector3 from, Vector3 to)
    {
        var flat = to - from;
        flat.y = 0f;
        return flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
    }
}
