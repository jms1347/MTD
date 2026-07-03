using Unity.Netcode;
using UnityEngine;

public class CwslPlayerSpawnOffset : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        var angle = OwnerClientId * 55f * Mathf.Deg2Rad;
        var radius = 3f + OwnerClientId * 0.35f;
        transform.position = new Vector3(Mathf.Cos(angle) * radius, 1f, Mathf.Sin(angle) * radius);
    }
}
