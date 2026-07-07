using Unity.Netcode;
using UnityEngine;

/// <summary>오프사이드·먹물 등으로 시야를 일시적으로 0(블라인드)으로 고정.</summary>
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

    public void ApplyInkBlindServer(float durationSeconds, Vector3 impactPosition)
    {
        if (!IsServer || durationSeconds <= 0f)
            return;

        forcedBlindUntil.Value = Time.time + durationSeconds;
        PlayInkBlindClientRpc(impactPosition, durationSeconds);
    }

    public void ClearForcedBlindServer()
    {
        if (!IsServer)
            return;

        forcedBlindUntil.Value = 0f;
    }

    [ClientRpc]
    private void PlayInkBlindClientRpc(Vector3 impactPosition, float durationSeconds)
    {
        var visual = GetComponent<CwslPlayerInkBlindVisual>();
        if (visual == null)
            visual = gameObject.AddComponent<CwslPlayerInkBlindVisual>();
        visual.Play(impactPosition, durationSeconds);
    }
}
