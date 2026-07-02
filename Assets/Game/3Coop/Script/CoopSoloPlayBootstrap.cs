using UnityEngine;

/// <summary>
/// 로비를 거치지 않고 협동 씬에 바로 들어온 경우 솔로 호스트 세션을 만듭니다.
/// </summary>
public static class CoopSoloPlayBootstrap
{
    public static void Ensure()
    {
        if (LobbyNetworkManager.Instance == null)
        {
            var lobbyObject = new GameObject("LobbyNetworkManager");
            lobbyObject.AddComponent<LobbyNetworkManager>();
        }

        var lobby = LobbyNetworkManager.Instance;
        if (lobby != null && !lobby.IsInRoom)
            lobby.BeginSoloSession();
    }
}
