using Unity.Netcode;
using UnityEngine;

/// <summary>활성 방패 버블 내부 아군 무적 판정 (서버).</summary>
public static class CwslShieldBubbleProtection
{
    public static bool IsPlayerProtectedServer(CwslPlayerHealth health)
    {
        if (health == null || !health.IsAlive)
            return false;

        return IsPositionProtectedServer(health.transform.position);
    }

    public static bool IsPositionProtectedServer(Vector3 worldPosition)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return false;

        var markers = Object.FindObjectsByType<CwslShieldBubbleMarker>(FindObjectsSortMode.None);
        foreach (var marker in markers)
        {
            if (marker == null || marker.Bubble == null || !marker.Bubble.IsBubbleActive)
                continue;

            if (IsInsideBubble(worldPosition, marker.transform.position, marker.Bubble.Radius))
                return true;
        }

        return false;
    }

    private static bool IsInsideBubble(Vector3 worldPosition, Vector3 bubbleCenter, float radius)
    {
        var flat = worldPosition - bubbleCenter;
        flat.y = 0f;
        return flat.sqrMagnitude <= radius * radius;
    }
}
