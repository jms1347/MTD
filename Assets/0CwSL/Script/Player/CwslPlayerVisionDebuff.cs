using Unity.Netcode;
using UnityEngine;

/// <summary>오프사이드 등으로 시야를 일시적으로 0(블라인드)으로 고정.</summary>
public class CwslPlayerVisionDebuff : NetworkBehaviour
{
    private readonly NetworkVariable<float> forcedBlindUntil = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public bool IsForcedBlind => forcedBlindUntil.Value > Time.time;

    public void ApplyForcedBlindServer(float durationSeconds)
    {
        if (!IsServer || durationSeconds <= 0f)
            return;

        forcedBlindUntil.Value = Time.time + durationSeconds;
    }

    public void ClearForcedBlindServer()
    {
        if (!IsServer)
            return;

        forcedBlindUntil.Value = 0f;
    }
}
