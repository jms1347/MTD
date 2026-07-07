using Unity.Netcode;
using UnityEngine;

/// <summary>전역 몬스터 어그로 — 수석 코치 "네가 에이스냐?" 등.</summary>
public static class CwslMonsterGlobalAggro
{
    private static ulong forcedClientId = ulong.MaxValue;
    private static float expireTime;

    public static bool IsActive => Time.time < expireTime && forcedClientId != ulong.MaxValue;

    public static void SetForcedTargetServer(ulong clientId, float durationSeconds)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        forcedClientId = clientId;
        expireTime = Time.time + Mathf.Max(0.1f, durationSeconds);
    }

    public static void ClearServer()
    {
        forcedClientId = ulong.MaxValue;
        expireTime = 0f;
    }

    public static bool TryGetForcedPlayerTarget(out NetworkObject target)
    {
        target = null;
        if (!IsActive || NetworkManager.Singleton == null)
            return false;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != forcedClientId || client.PlayerObject == null || !client.PlayerObject.IsSpawned)
                continue;

            var health = client.PlayerObject.GetComponent<CwslPlayerHealth>();
            if (health != null && !health.IsAlive)
                return false;

            target = client.PlayerObject;
            return true;
        }

        return false;
    }
}
