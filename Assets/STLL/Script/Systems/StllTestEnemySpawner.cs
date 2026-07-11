using Unity.Netcode;
using UnityEngine;

/// <summary>테스트용 졸개 스폰.</summary>
public class StllTestEnemySpawner : NetworkBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int ringCount = 16;
    [SerializeField] private float ringRadius = 14f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer || enemyPrefab == null)
            return;

        for (var i = 0; i < ringCount; i++)
        {
            var angle = i / (float)ringCount * Mathf.PI * 2f;
            var pos = new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius);
            var instance = Instantiate(enemyPrefab, pos, Quaternion.LookRotation(-pos.normalized, Vector3.up));
            var netObj = instance.GetComponent<NetworkObject>();
            if (netObj == null)
                continue;

            netObj.Spawn(true);
        }
    }
}
