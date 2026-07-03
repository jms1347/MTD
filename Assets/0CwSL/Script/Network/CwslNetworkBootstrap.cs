using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// 로비(TCP)에서 게임 씬 진입 후 Netcode Host/Client를 시작합니다.
/// </summary>
public class CwslNetworkBootstrap : MonoBehaviour
{
    [SerializeField] private CwslGameAssets gameAssets;

    private bool started;

    private void Start()
    {
        if (started)
            return;

        started = true;
        EnsureNetworkManager();
        TryStartNetcode();
    }

    private void EnsureNetworkManager()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[CwslNetworkBootstrap] NetworkManager가 씬에 없습니다. Tools → CwSL → Setup Game Scene을 실행하세요.");
            return;
        }

        if (gameAssets != null && gameAssets.playerPrefab != null)
            NetworkManager.Singleton.NetworkConfig.PlayerPrefab = gameAssets.playerPrefab;
    }

    private void TryStartNetcode()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return;

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[CwslNetworkBootstrap] UnityTransport가 없습니다.");
            return;
        }

        var lobby = LobbyNetworkManager.Instance;
        if (lobby != null && lobby.IsInRoom)
        {
            if (lobby.IsHost)
            {
                transport.SetConnectionData("0.0.0.0", (ushort)lobby.Port);
                if (!networkManager.StartHost())
                    Debug.LogError("[CwslNetworkBootstrap] StartHost 실패");
            }
            else if (lobby.IsClient)
            {
                transport.SetConnectionData(lobby.HostAddress, (ushort)lobby.Port);
                if (!networkManager.StartClient())
                    Debug.LogError("[CwslNetworkBootstrap] StartClient 실패");
            }

            return;
        }

        transport.SetConnectionData("127.0.0.1", CwslGameConstants.GamePort);
        if (!networkManager.StartHost())
            Debug.LogError("[CwslNetworkBootstrap] 솔로 테스트 StartHost 실패");
    }
}
