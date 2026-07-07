using Unity.Netcode;
using UnityEngine;

/// <summary>몬스터 스턴 — 서버에서 AI 정지.</summary>
public class CwslMonsterStun : MonoBehaviour
{
    private float stunEndTime;
    private CwslMonsterHealth health;

    public bool IsStunned => Time.time < stunEndTime;

    private void Awake()
    {
        health = GetComponent<CwslMonsterHealth>();
    }

    public void ApplyStunServer(float durationSeconds)
    {
        var network = NetworkManager.Singleton;
        if (network == null || !network.IsServer || durationSeconds <= 0f)
            return;

        if (health != null && !health.IsAlive)
            return;

        stunEndTime = Mathf.Max(stunEndTime, Time.time + durationSeconds);
    }

    public void ClearStunServer()
    {
        stunEndTime = 0f;
    }
}
