using Unity.Netcode;
using UnityEngine;

/// <summary>개별 몬스터의 강제 추적 대상 — 홍명보 「싸워」 소환 등 (서버 전용).</summary>
public class CwslMonsterForcedTarget : MonoBehaviour
{
    private NetworkObject forcedTarget;
    private float expireTime = float.MaxValue;

    public void SetTargetServer(NetworkObject target, float durationSeconds = float.MaxValue)
    {
        if (Unity.Netcode.NetworkManager.Singleton == null || !Unity.Netcode.NetworkManager.Singleton.IsServer)
            return;

        forcedTarget = target;
        expireTime = durationSeconds >= float.MaxValue * 0.5f
            ? float.MaxValue
            : Time.time + Mathf.Max(0.1f, durationSeconds);
    }

    public bool TryGetTarget(out NetworkObject target)
    {
        target = null;
        if (forcedTarget == null || !forcedTarget.IsSpawned)
            return false;

        if (Time.time >= expireTime)
        {
            forcedTarget = null;
            return false;
        }

        var health = forcedTarget.GetComponent<CwslPlayerHealth>();
        if (health != null && !health.IsAlive)
            return false;

        target = forcedTarget;
        return true;
    }
}
