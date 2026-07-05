using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class CwslPlayerSpawnOffset : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        var angle = OwnerClientId * 55f * Mathf.Deg2Rad;
        var radius = 3f + OwnerClientId * 0.35f;
        var spawnPosition = new Vector3(Mathf.Cos(angle) * radius, 1f, Mathf.Sin(angle) * radius);

        if (NavMesh.SamplePosition(spawnPosition, out var hit, 4f, NavMesh.AllAreas))
            spawnPosition = hit.position;

        transform.position = spawnPosition;

        var agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.Warp(spawnPosition);
    }
}
