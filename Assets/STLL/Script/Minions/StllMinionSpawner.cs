using Unity.Netcode;
using UnityEngine;

public class StllMinionSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private int minionCount = StllGlaiveConstants.DefaultMinionCount;

    private float hpBonus;
    private float attackBonus;

    public float HpBonus => hpBonus;
    public float AttackBonus => attackBonus;

    public void AddBonusMinionServer(int count)
    {
        if (!IsServer || minionPrefab == null || count <= 0)
            return;

        var commander = GetComponent<StllMinionCommander>();
        if (commander == null)
            return;

        for (var i = 0; i < count; i++)
        {
            var offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-2.5f, -0.5f));
            var spawnPos = transform.position + transform.TransformDirection(offset);
            var instance = Instantiate(minionPrefab, spawnPos, Quaternion.identity);
            var netObj = instance.GetComponent<NetworkObject>();
            if (netObj == null)
                continue;

            netObj.Spawn(true);
            commander.RegisterMinionServer(netObj);
        }
    }

    public void SetTrainingBonusServer(float hp, float attack)
    {
        if (!IsServer)
            return;

        hpBonus = hp;
        attackBonus = attack;
    }

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
