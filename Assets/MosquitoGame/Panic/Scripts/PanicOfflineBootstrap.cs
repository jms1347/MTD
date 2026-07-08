using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>네트워크 없이 에디터에서 빠르게 테스트할 때 Host를 자동 시작합니다.</summary>
public class PanicOfflineBootstrap : MonoBehaviour
{
    [SerializeField] private bool autoStartHost = true;
    [SerializeField] private GameObject humanPrefab;
    [SerializeField] private GameObject mosquitoPrefab;
    [SerializeField] private Transform humanSpawn;
    [SerializeField] private Transform[] mosquitoSpawns;

    private void Start()
    {
        if (!autoStartHost || NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.IsListening)
            return;

        NetworkManager.Singleton.StartHost();
        if (!NetworkManager.Singleton.IsServer)
            return;

        SpawnParticipants();
    }

    private void SpawnParticipants()
    {
        if (humanPrefab != null && humanSpawn != null)
            Instantiate(humanPrefab, humanSpawn.position, humanSpawn.rotation).GetComponent<NetworkObject>()?.SpawnAsPlayerObject(0);

        if (mosquitoPrefab == null || mosquitoSpawns == null)
            return;

        for (var i = 0; i < mosquitoSpawns.Length; i++)
        {
            if (mosquitoSpawns[i] == null)
                continue;

            var mosquito = Instantiate(mosquitoPrefab, mosquitoSpawns[i].position, mosquitoSpawns[i].rotation);
            mosquito.GetComponent<NetworkObject>()?.SpawnWithOwnership((ulong)(i + 1));
        }
    }
}
