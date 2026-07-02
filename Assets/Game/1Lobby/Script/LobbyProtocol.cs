using System;

[Serializable]
public class LobbyPlayerData
{
    public string playerId;
    public string playerName;
    public bool isHost;
    public bool isReady;
}

[Serializable]
public class LobbyMessage
{
    public string type;
    public string playerId;
    public string playerName;
    public bool isReady;
    public LobbyPlayerData[] players;
    public string error;

    public const string JoinRequest = "join_request";
    public const string JoinAccepted = "join_accepted";
    public const string PlayerList = "player_list";
    public const string ReadyChanged = "ready_changed";
    public const string StartGame = "start_game";
    public const string Disconnect = "disconnect";
    public const string Error = "error";

    public static LobbyMessage Join(string playerId, string playerName) => new()
    {
        type = JoinRequest,
        playerId = playerId,
        playerName = playerName
    };

    public static LobbyMessage Ready(string playerId, bool ready) => new()
    {
        type = ReadyChanged,
        playerId = playerId,
        isReady = ready
    };

    public static LobbyMessage Players(LobbyPlayerData[] players) => new()
    {
        type = PlayerList,
        players = players
    };

    public static LobbyMessage Start() => new() { type = StartGame };

    public static LobbyMessage Leave(string playerId) => new()
    {
        type = Disconnect,
        playerId = playerId
    };

    public static LobbyMessage Accepted() => new() { type = JoinAccepted };
}
