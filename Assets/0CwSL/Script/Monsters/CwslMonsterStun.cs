using Unity.Netcode;
using UnityEngine;

/// <summary>몬스터 스턴 — 서버에서 AI 정지 + 클라이언트 스턴 VFX.</summary>
public class CwslMonsterStun : MonoBehaviour
{
    private float stunEndTime;
    private CwslMonsterHealth health;

    public bool IsStunned => Time.time < stunEndTime;

    private void Awake()
    {
        health = GetComponent<CwslMonsterHealth>();
    }

    public void ApplyStunServer(float durationSeconds, Vector3 impactPosition = default)
    {
        var network = NetworkManager.Singleton;
        if (network != null && !network.IsServer)
            return;

        if (durationSeconds <= 0f)
            return;

        if (health != null && !health.IsAlive)
            return;

        var wasStunned = IsStunned;
        stunEndTime = Mathf.Max(stunEndTime, Time.time + durationSeconds);
        if (wasStunned)
            return;

        var position = impactPosition == default ? transform.position : impactPosition;
        if (health != null && health.IsSpawned)
        {
            health.NotifyMonsterStunVisualServer(position, durationSeconds);
            return;
        }

        CwslMonsterStunVisual.Ensure(gameObject).PlayStun(position, durationSeconds);
    }

    public void ClearStunServer()
    {
        stunEndTime = 0f;
        CwslMonsterStunVisual.Ensure(gameObject)?.EndStun();
    }
}
