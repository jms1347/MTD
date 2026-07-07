using Unity.Netcode;
using UnityEngine;

/// <summary>서버에서 몬스터를 밀쳐내는 넉백. NetworkTransform으로 위치 동기화.</summary>
public class CwslMonsterKnockback : MonoBehaviour
{
    private Vector3 velocity;
    private float endTime;
    private CwslMonsterHealth health;

    public bool IsKnockedBack => Time.time < endTime && velocity.sqrMagnitude > 0.0004f;

    private void Awake()
    {
        health = GetComponent<CwslMonsterHealth>();
    }

    public void ApplyKnockbackServer(Vector3 worldDirection, float distance, float duration)
    {
        var network = NetworkManager.Singleton;
        if (network == null || !network.IsServer)
            return;

        if (health != null && !health.IsAlive)
            return;

        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude < 0.0001f)
            return;

        worldDirection.Normalize();
        var safeDuration = Mathf.Max(0.08f, duration);
        velocity = worldDirection * (distance / safeDuration);
        endTime = Time.time + safeDuration;
    }

    private void LateUpdate()
    {
        var network = NetworkManager.Singleton;
        if (network == null || !network.IsServer || !IsKnockedBack)
            return;

        if (health != null && !health.IsAlive)
        {
            velocity = Vector3.zero;
            return;
        }

        var delta = velocity * Time.deltaTime;
        var next = transform.position + delta;
        next = CwslArenaUtility.ClampToPlayArea(next, ResolveClampRadius());
        transform.position = next;

        var remaining = endTime - Time.time;
        if (remaining <= 0f)
        {
            velocity = Vector3.zero;
            return;
        }

        var decay = Mathf.Clamp01(Time.deltaTime / Mathf.Max(0.05f, remaining));
        velocity = Vector3.Lerp(velocity, Vector3.zero, decay * 1.35f);
    }

    private float ResolveClampRadius()
    {
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
            return capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

        return CwslGameConstants.MonsterHitMinRadius;
    }
}
