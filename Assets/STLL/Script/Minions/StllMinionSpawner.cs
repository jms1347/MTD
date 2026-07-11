using Unity.Netcode;
using UnityEngine;

public class StllMinionSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private int minionCount = StllGlaiveConstants.DefaultMinionCount;

    public override void OnNetworkSpawn()
    {
        if (!IsServer || minionPrefab == null)
            return;

        var commander = GetComponent<StllMinionCommander>();
        if (commander == null)
            return;

        for (var i = 0; i < minionCount; i++)
        {
            var offset = new Vector3((i % 2 == 0 ? -1f : 1f) * 1.2f, 0f, -2f - i * 0.6f);
            var spawnPos = transform.position + transform.TransformDirection(offset);
            var instance = Instantiate(minionPrefab, spawnPos, Quaternion.identity);
            var netObj = instance.GetComponent<NetworkObject>();
            if (netObj == null)
                continue;

            netObj.Spawn(true);
            commander.RegisterMinionServer(netObj);
        }
    }
}
