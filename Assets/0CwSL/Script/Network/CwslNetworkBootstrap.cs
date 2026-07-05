using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        CwslLobbyGameSettings.EnsureLoaded();
        EnsureNetworkManager();
        StartCoroutine(BeginNetcodeSession());
    }

    private void EnsureNetworkManager()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[CwslNetworkBootstrap] NetworkManager가 씬에 없습니다. Tools → CwSL → Setup Game Scene을 실행하세요.");
            return;
        }

        var networkManager = NetworkManager.Singleton;
        networkManager.NetworkConfig.ConnectionApproval = true;
        networkManager.ConnectionApprovalCallback = ApproveConnection;

        if (gameAssets != null && gameAssets.playerPrefab != null)
            networkManager.NetworkConfig.PlayerPrefab = gameAssets.playerPrefab;
    }

    private static void ApproveConnection(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        var connectedCount = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.ConnectedClientsIds.Count
            : 0;

        if (connectedCount >= CwslGameConstants.MaxPlayers)
        {
            response.Approved = false;
            response.Reason = "방이 가득 찼습니다. (최대 5인)";
            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = true;
    }

    private IEnumerator BeginNetcodeSession()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            yield break;

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[CwslNetworkBootstrap] UnityTransport가 없습니다.");
            yield break;
        }

        var lobby = LobbyNetworkManager.Instance;
        var gamePort = CwslGameConstants.GameNetcodePort;

        if (CwslGameConstants.UseDefenseMode)
        {
            RemoveLegacySceneNexusObjects();
            yield return null;
        }

        if (lobby != null && (lobby.PendingNetcodeHost || lobby.PendingNetcodeClient))
        {
            if (lobby.PendingNetcodeHost)
            {
                transport.SetConnectionData("0.0.0.0", gamePort);
                if (!networkManager.StartHost())
                {
                    Debug.LogError("[CwslNetworkBootstrap] StartHost 실패");
                    FailAndReturnToLobby("Netcode 호스트 시작에 실패했습니다.");
                    yield break;
                }

                Debug.Log($"[CwslNetworkBootstrap] Netcode Host 시작 (UDP {gamePort})");
                lobby.ClearNetcodeTransition();
                yield break;
            }

            if (lobby.PendingNetcodeClient)
            {
                var hostAddress = string.IsNullOrWhiteSpace(lobby.PendingNetcodeHostAddress)
                    ? "127.0.0.1"
                    : lobby.PendingNetcodeHostAddress.Trim();
                transport.SetConnectionData(hostAddress, gamePort);

                yield return new WaitForSeconds(0.35f);

                var connected = false;
                for (var attempt = 0; attempt < 20; attempt++)
                {
                    if (networkManager.IsConnectedClient)
                    {
                        connected = true;
                        break;
                    }

                    if (networkManager.StartClient())
                    {
                        Debug.Log($"[CwslNetworkBootstrap] StartClient 성공 ({hostAddress}:{gamePort}, attempt {attempt + 1})");
                        connected = true;
                        break;
                    }

                    Debug.LogWarning($"[CwslNetworkBootstrap] StartClient 재시도 {attempt + 1}/20");
                    yield return new WaitForSeconds(0.4f);
                }

                lobby.ClearNetcodeTransition();

                if (!connected)
                {
                    FailAndReturnToLobby(
                        $"게임 서버({hostAddress}:{gamePort}) 연결 실패.\n" +
                        $"호스트 PC 방화벽에서 UDP {gamePort} 허용 여부를 확인하세요.");
                }

                yield break;
            }
        }

        if (lobby != null && lobby.IsInRoom)
        {
            if (lobby.IsHost)
            {
                transport.SetConnectionData("0.0.0.0", gamePort);
                if (!networkManager.StartHost())
                    Debug.LogError("[CwslNetworkBootstrap] StartHost 실패");
            }
            else if (lobby.IsClient)
            {
                transport.SetConnectionData(lobby.HostAddress, gamePort);
                yield return new WaitForSeconds(0.35f);
                yield return StartClientWithRetry(networkManager, lobby.HostAddress, gamePort);
            }

            yield break;
        }

        transport.SetConnectionData("127.0.0.1", gamePort);
        if (!networkManager.StartHost())
            Debug.LogError("[CwslNetworkBootstrap] 솔로 테스트 StartHost 실패");
    }

    private static IEnumerator StartClientWithRetry(NetworkManager networkManager, string hostAddress, int port)
    {
        const int maxAttempts = 20;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (networkManager.IsConnectedClient)
                yield break;

            if (networkManager.StartClient())
            {
                Debug.Log($"[CwslNetworkBootstrap] StartClient 성공 ({hostAddress}:{port}, attempt {attempt + 1})");
                yield break;
            }

            Debug.LogWarning($"[CwslNetworkBootstrap] StartClient 재시도 {attempt + 1}/{maxAttempts}");
            yield return new WaitForSeconds(0.4f);
        }

        FailAndReturnToLobby(
            $"게임 서버({hostAddress}:{port}) 연결 실패.\n" +
            $"호스트 PC 방화벽에서 UDP {port} 허용 여부를 확인하세요.");
    }

    private static void RemoveLegacySceneNexusObjects()
    {
        var nexuses = FindObjectsByType<CwslNexus>(FindObjectsSortMode.None);
        foreach (var nexus in nexuses)
        {
            if (nexus == null)
                continue;

            Debug.LogWarning("[CwSL] 씬에 배치된 CwslNexus를 제거합니다. 런타임 프리팹 스폰으로 대체됩니다.");
            Destroy(nexus.gameObject);
        }
    }

    private static void FailAndReturnToLobby(string message)
    {
        Debug.LogError($"[CwslNetworkBootstrap] {message}");

        var lobby = LobbyNetworkManager.Instance;
        if (lobby != null)
        {
            lobby.SetPendingBootstrapError(message);
            lobby.ClearNetcodeTransition();
        }

        SceneManager.LoadScene(CwslGameConstants.LobbySceneName);
    }
}
