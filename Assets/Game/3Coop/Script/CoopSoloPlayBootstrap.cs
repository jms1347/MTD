using UnityEngine;

/// <summary>
/// 협동 씬 진입 시 로비/호스트 상태를 보장합니다.
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

        LobbyNetworkManager.Instance?.EnsureCoopHostAuthority();
    }
}
