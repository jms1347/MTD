using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class CwslPlayerSpawnOffset : NetworkBehaviour
{
    private static readonly System.Collections.Generic.List<ulong> SortedClientIds = new();

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        Vector3 spawnPosition;
        if (CwslGameConstants.UseDefenseMode)
        {
            CwslDefensePrepUtility.CollectSortedClientIds(SortedClientIds);
            var slot = CwslDefensePrepUtility.GetSlotIndex(OwnerClientId, SortedClientIds);
            spawnPosition = CwslDefensePrepUtility.GetLineupWorldPosition(slot);
        }
        else
        {
            spawnPosition = GetLegacySpawnPosition();
        }

        if (NavMesh.SamplePosition(spawnPosition, out var hit, 4f, NavMesh.AllAreas))
            spawnPosition = hit.position;

        transform.position = spawnPosition;

        var agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.Warp(spawnPosition);
    }

    private Vector3 GetLegacySpawnPosition()
    {
        var angle = OwnerClientId * 55f * Mathf.Deg2Rad;
        var radius = 3f + OwnerClientId * 0.35f;
        return new Vector3(Mathf.Cos(angle) * radius, 1f, Mathf.Sin(angle) * radius);
    }
}
