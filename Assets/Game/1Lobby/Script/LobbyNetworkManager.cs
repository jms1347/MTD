using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// IP 직접 연결 TCP 로비. 호스트는 서버, 클라이언트는 호스트 IP로 접속합니다.
/// </summary>
public class LobbyNetworkManager : MonoBehaviour
{
    public const int DefaultPort = 7777;
    public const string GameSceneName = "CoopDefenseScene";

    public static LobbyNetworkManager Instance { get; private set; }

    public bool IsHost { get; private set; }
    public bool IsClient { get; private set; }
    public bool IsInRoom => IsHost || IsClient;
    public string LocalPlayerId { get; private set; }
    public string LocalPlayerName { get; set; } = "Player";
    public int Port { get; private set; } = DefaultPort;
    public string HostAddress { get; private set; }
    public IReadOnlyList<LobbyPlayerData> Players => players;

    public event Action OnPlayerListChanged;
    public event Action<string> OnStatusChanged;
    public event Action<string> OnError;
    public event Action OnLeftRoom;
    public event Action OnGameStarting;
    public event Action<string, string> OnCoopMessage;

    private readonly List<LobbyPlayerData> players = new();
    private readonly ConcurrentQueue<Action> mainThreadActions = new();
    private readonly Dictionary<string, ClientConnection> connections = new();

    private TcpListener listener;
    private ClientConnection serverConnection;
    private Thread acceptThread;
    private volatile bool isRunning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LocalPlayerId = Guid.NewGuid().ToString("N");
    }

    private void Update()
    {
        while (mainThreadActions.TryDequeue(out var action))
            action?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        ShutdownNetwork();
    }

    private void OnApplicationQuit() => ShutdownNetwork();

    public void BeginSoloSession()
    {
        if (IsInRoom)
            return;

        if (string.IsNullOrWhiteSpace(LocalPlayerName))
            LocalPlayerName = "Solo";

        IsHost = true;
        IsClient = false;
        players.Clear();
        players.Add(CreateLocalPlayer(isHost: true, ready: true));
        SetStatus("솔로 플레이");
        OnPlayerListChanged?.Invoke();
    }

    public void HostRoom(int port, string playerName)
    {
        if (IsInRoom)
        {
            ReportError("이미 방에 있습니다.");
            return;
        }

        if (port <= 0 || port > 65535)
        {
            ReportError("포트 번호가 올바르지 않습니다.");
            return;
        }

        LocalPlayerName = string.IsNullOrWhiteSpace(playerName) ? "Host" : playerName.Trim();
        Port = port;
        HostAddress = LobbyNetworkAddress.GetLocalIPv4();

        try
        {
            listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            isRunning = true;
            IsHost = true;
            IsClient = false;

            players.Clear();
            players.Add(CreateLocalPlayer(isHost: true, ready: true));

            acceptThread = new Thread(AcceptClientsLoop) { IsBackground = true };
            acceptThread.Start();

            SetStatus($"방 생성 — {LobbyNetworkAddress.FormatEndpoint(HostAddress, Port)}");
            BroadcastPlayerList();
            OnPlayerListChanged?.Invoke();
        }
        catch (Exception exception)
        {
            ShutdownNetwork();
            ReportError($"방 생성 실패: {exception.Message}");
        }
    }

    public void JoinRoom(string hostIp, int port, string playerName)
    {
        if (IsInRoom)
        {
            ReportError("이미 방에 있습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(hostIp))
        {
            ReportError("호스트 IP를 입력해 주세요.");
            return;
        }

        if (port <= 0 || port > 65535)
        {
            ReportError("포트 번호가 올바르지 않습니다.");
            return;
        }

        LocalPlayerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();
        Port = port;
        HostAddress = hostIp.Trim();

        try
        {
            var client = new TcpClient();
            client.Connect(HostAddress, Port);

            IsHost = false;
            IsClient = true;
            isRunning = true;

            serverConnection = new ClientConnection(client);
            serverConnection.OnMessageReceived += HandleClientMessage;
            serverConnection.OnCoopMessageReceived += HandleClientCoopMessage;
            serverConnection.OnDisconnected += HandleServerDisconnected;
            serverConnection.StartReadLoop();

            SendToServer(LobbyMessage.Join(LocalPlayerId, LocalPlayerName));
            SetStatus($"{LobbyNetworkAddress.FormatEndpoint(HostAddress, Port)} 접속 중...");
        }
        catch (Exception exception)
        {
            ShutdownNetwork();
            ReportError($"접속 실패: {exception.Message}");
        }
    }

    public void LeaveRoom()
    {
        if (!IsInRoom)
            return;

        if (IsClient)
            SendToServer(LobbyMessage.Leave(LocalPlayerId));

        ShutdownNetwork();
        players.Clear();
        OnPlayerListChanged?.Invoke();
        OnLeftRoom?.Invoke();
        SetStatus("대기실");
    }

    public void SetReady(bool ready)
    {
        if (!IsInRoom)
            return;

        UpdateLocalReadyState(ready);

        if (IsHost)
        {
            BroadcastPlayerList();
            OnPlayerListChanged?.Invoke();
        }
        else
        {
            SendToServer(LobbyMessage.Ready(LocalPlayerId, ready));
        }
    }

    public void StartGame()
    {
        if (!IsHost)
        {
            ReportError("호스트만 게임을 시작할 수 있습니다.");
            return;
        }

        if (!CanStartGame())
        {
            ReportError("모든 플레이어가 준비해야 합니다.");
            return;
        }

        Broadcast(LobbyMessage.Start());
        BeginGameSceneLoad();
    }

    public bool CanStartGame()
    {
        if (!IsHost || players.Count == 0)
            return false;

        foreach (var player in players)
        {
            if (!player.isReady)
                return false;
        }

        return true;
    }

    public void SendCoopToAll(string json)
    {
        if (!IsHost || string.IsNullOrEmpty(json))
            return;

        foreach (var connection in connections.Values)
            connection.SendRaw(json);
    }

    public void SendCoopToHost(string json)
    {
        if (!IsClient || string.IsNullOrEmpty(json))
            return;

        serverConnection?.SendRaw(json);
    }

    private void AcceptClientsLoop()
    {
        while (isRunning && listener != null)
        {
            try
            {
                var client = listener.AcceptTcpClient();
                var connection = new ClientConnection(client);
                EnqueueMain(() => RegisterClient(connection));
            }
            catch (SocketException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception exception)
            {
                EnqueueMain(() => ReportError($"접속 처리 오류: {exception.Message}"));
            }
        }
    }

    private void RegisterClient(ClientConnection connection)
    {
        if (!IsHost)
        {
            connection.Dispose();
            return;
        }

        connection.OnMessageReceived += HandleHostSideClientMessage;
        connection.OnCoopMessageReceived += HandleHostSideCoopMessage;
        connection.OnDisconnected += () => HandleClientDisconnected(connection);
        connection.StartReadLoop();
    }

    private void HandleHostSideClientMessage(ClientConnection connection, LobbyMessage message)
    {
        if (!IsHost)
            return;

        switch (message.type)
        {
            case LobbyMessage.JoinRequest:
                if (string.IsNullOrEmpty(message.playerId))
                    message.playerId = Guid.NewGuid().ToString("N");

                connection.PlayerId = message.playerId;
                connections[message.playerId] = connection;

                players.Add(new LobbyPlayerData
                {
                    playerId = message.playerId,
                    playerName = string.IsNullOrWhiteSpace(message.playerName) ? "Player" : message.playerName,
                    isHost = false,
                    isReady = false
                });

                connection.Send(LobbyMessage.Accepted());
                BroadcastPlayerList();
                OnPlayerListChanged?.Invoke();
                SetStatus($"접속: {message.playerName}");
                break;

            case LobbyMessage.ReadyChanged:
                UpdatePlayerReady(message.playerId, message.isReady);
                BroadcastPlayerList();
                OnPlayerListChanged?.Invoke();
                break;

            case LobbyMessage.Disconnect:
                RemovePlayer(message.playerId);
                break;
        }
    }

    private void HandleClientMessage(ClientConnection connection, LobbyMessage message)
    {
        if (IsHost)
            return;

        switch (message.type)
        {
            case LobbyMessage.JoinAccepted:
                SetStatus("방에 입장했습니다.");
                break;
            case LobbyMessage.PlayerList:
                ApplyPlayerList(message.players);
                break;
            case LobbyMessage.StartGame:
                BeginGameSceneLoad();
                break;
            case LobbyMessage.Error:
                ReportError(message.error);
                break;
            case LobbyMessage.Disconnect:
                LeaveRoom();
                break;
        }
    }

    private void HandleHostSideCoopMessage(ClientConnection connection, string json)
    {
        if (!IsHost)
            return;

        var senderId = string.IsNullOrEmpty(connection.PlayerId) ? LocalPlayerId : connection.PlayerId;
        EnqueueMain(() => OnCoopMessage?.Invoke(senderId, json));
    }

    private void HandleClientCoopMessage(ClientConnection connection, string json)
    {
        if (IsHost)
            return;

        EnqueueMain(() => OnCoopMessage?.Invoke(string.Empty, json));
    }

    private void HandleClientDisconnected(ClientConnection connection)
    {
        if (!IsHost || string.IsNullOrEmpty(connection.PlayerId))
        {
            connection.Dispose();
            return;
        }

        RemovePlayer(connection.PlayerId);
    }

    private void HandleServerDisconnected()
    {
        EnqueueMain(() =>
        {
            if (!IsClient)
                return;

            ShutdownNetwork();
            players.Clear();
            OnPlayerListChanged?.Invoke();
            OnLeftRoom?.Invoke();
            ReportError("호스트와 연결이 끊어졌습니다.");
        });
    }

    private void RemovePlayer(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
            return;

        players.RemoveAll(player => player.playerId == playerId);

        if (connections.TryGetValue(playerId, out var connection))
        {
            connections.Remove(playerId);
            connection.Dispose();
        }

        BroadcastPlayerList();
        OnPlayerListChanged?.Invoke();
        SetStatus("플레이어가 나갔습니다.");
    }

    private void UpdateLocalReadyState(bool ready)
    {
        for (var i = 0; i < players.Count; i++)
        {
            if (players[i].playerId != LocalPlayerId)
                continue;

            players[i].isReady = ready;
            return;
        }
    }

    private void UpdatePlayerReady(string playerId, bool ready)
    {
        for (var i = 0; i < players.Count; i++)
        {
            if (players[i].playerId != playerId)
                continue;

            players[i].isReady = ready;
            return;
        }
    }

    private LobbyPlayerData CreateLocalPlayer(bool isHost, bool ready = false) => new()
    {
        playerId = LocalPlayerId,
        playerName = LocalPlayerName,
        isHost = isHost,
        isReady = ready
    };

    private void ApplyPlayerList(LobbyPlayerData[] remotePlayers)
    {
        players.Clear();
        if (remotePlayers != null)
            players.AddRange(remotePlayers);

        OnPlayerListChanged?.Invoke();
    }

    private void BroadcastPlayerList() => Broadcast(LobbyMessage.Players(players.ToArray()));

    private void Broadcast(LobbyMessage message)
    {
        if (!IsHost)
            return;

        var payload = JsonConvert.SerializeObject(message);
        foreach (var connection in connections.Values)
            connection.SendRaw(payload);
    }

    private void SendToServer(LobbyMessage message) => serverConnection?.Send(message);

    private void BeginGameSceneLoad()
    {
        OnGameStarting?.Invoke();
        SetStatus("게임 시작...");
        SceneManager.LoadScene(GameSceneName);
    }

    private void ShutdownNetwork()
    {
        isRunning = false;

        try { listener?.Stop(); } catch { }

        listener = null;

        foreach (var connection in connections.Values)
            connection.Dispose();

        connections.Clear();
        serverConnection?.Dispose();
        serverConnection = null;
        IsHost = false;
        IsClient = false;
    }

    private void SetStatus(string status) => EnqueueMain(() => OnStatusChanged?.Invoke(status));

    private void ReportError(string message)
    {
        Debug.LogWarning($"[LobbyNetworkManager] {message}");
        EnqueueMain(() => OnError?.Invoke(message));
    }

    private void EnqueueMain(Action action) => mainThreadActions.Enqueue(action);

    private sealed class ClientConnection : IDisposable
    {
        public string PlayerId { get; set; }
        public event Action<ClientConnection, LobbyMessage> OnMessageReceived;
        public event Action<ClientConnection, string> OnCoopMessageReceived;
        public event Action OnDisconnected;

        private readonly TcpClient client;
        private readonly NetworkStream stream;
        private readonly object sendLock = new();
        private volatile bool disposed;

        public ClientConnection(TcpClient client)
        {
            this.client = client;
            stream = client.GetStream();
        }

        public void StartReadLoop()
        {
            var readThread = new Thread(ReadLoop) { IsBackground = true };
            readThread.Start();
        }

        public void Send(LobbyMessage message) => SendRaw(JsonConvert.SerializeObject(message));

        public void SendRaw(string json)
        {
            if (disposed)
                return;

            var payload = Encoding.UTF8.GetBytes(json);
            var length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payload.Length));

            lock (sendLock)
            {
                stream.Write(length, 0, length.Length);
                stream.Write(payload, 0, payload.Length);
                stream.Flush();
            }
        }

        private void ReadLoop()
        {
            var lengthBuffer = new byte[4];

            try
            {
                while (!disposed)
                {
                    if (!ReadExact(lengthBuffer, 4))
                        break;

                    var length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBuffer, 0));
                    if (length <= 0 || length > 1024 * 1024)
                        break;

                    var payload = new byte[length];
                    if (!ReadExact(payload, length))
                        break;

                    var json = Encoding.UTF8.GetString(payload);
                    if (json.Contains("\"type\":\"coop_"))
                    {
                        OnCoopMessageReceived?.Invoke(this, json);
                        continue;
                    }

                    var message = JsonConvert.DeserializeObject<LobbyMessage>(json);
                    if (message != null)
                        OnMessageReceived?.Invoke(this, message);
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            catch (Exception exception)
            {
                Debug.LogWarning($"[LobbyNetworkManager] Read error: {exception.Message}");
            }
            finally
            {
                if (!disposed)
                    OnDisconnected?.Invoke();
            }
        }

        private bool ReadExact(byte[] buffer, int size)
        {
            var offset = 0;
            while (offset < size)
            {
                var read = stream.Read(buffer, offset, size - offset);
                if (read <= 0)
                    return false;

                offset += read;
            }

            return true;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            try { stream?.Close(); } catch { }
            try { client?.Close(); } catch { }
        }
    }
}

public static class LobbyNetworkAddress
{
    public static string GetLocalIPv4()
    {
        try
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint endPoint)
                    return endPoint.Address.ToString();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[LobbyNetworkAddress] {exception.Message}");
        }

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up)
                continue;

            if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            foreach (var address in networkInterface.GetIPProperties().UnicastAddresses)
            {
                if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                if (IPAddress.IsLoopback(address.Address))
                    continue;

                return address.Address.ToString();
            }
        }

        return "127.0.0.1";
    }

    public static string FormatEndpoint(string ip, int port) => $"{ip}:{port}";
}
