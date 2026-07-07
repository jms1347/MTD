using UnityEngine;

/// <summary>디아 오브 본체 — 비행 중 8방향 순차 얼음 파편 분사 (UkDefense DefenseFrozenOrbEmitter).</summary>
[DisallowMultipleComponent]
public class CwslFrozenOrbEmitter : MonoBehaviour
{
    public const string OrbVisualName = "OrbVisual";
    private const float ShardScaleRatio = 0.55f;
    private const float MinShardVisualScale = 0.45f;

    [SerializeField] private Transform orbVisual;

    private CwslFrozenOrbProjectile host;
    private float nextShardTime;
    private int directionCursor;
    private float orbBodyScale = 1f;

    public Transform OrbVisual => orbVisual;

    public void BindOrbVisual(Transform visual)
    {
        orbVisual = visual;
    }

    public void OnOrbLaunched(CwslFrozenOrbProjectile projectile)
    {
        if (projectile == null || projectile.IsShard)
            return;

        host = projectile;
        nextShardTime = Time.time;
        directionCursor = 0;
        orbBodyScale = 1f;

        if (orbVisual != null)
        {
            orbVisual.gameObject.SetActive(true);
            orbVisual.localScale = Vector3.one * 1.15f;

            var core = orbVisual.Find("OrbCore");
            if (core != null)
                core.localScale = Vector3.one * 0.95f;

            var aura = orbVisual.Find("OrbAura");
            if (aura != null)
                aura.localScale = Vector3.one * 0.42f;
        }

        ApplyOrbBodyScale();
    }

    public void OnOrbEnded()
    {
        host = null;
        directionCursor = 0;
        orbBodyScale = 1f;

        if (orbVisual != null)
        {
            orbVisual.localScale = Vector3.one;
            var aura = orbVisual.Find("OrbAura");
            if (aura != null)
                aura.localScale = Vector3.one * 0.42f;
            orbVisual.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (orbVisual != null && host != null && host.IsFlightActive && orbBodyScale > 0f)
        {
            orbVisual.Rotate(Vector3.up, 320f * Time.deltaTime, Space.Self);
            orbVisual.Rotate(Vector3.right, 95f * Time.deltaTime, Space.Self);
        }

        if (!IsServerActive() || host == null || !host.IsFlightActive || orbBodyScale <= 0f || Time.time < nextShardTime)
            return;

        nextShardTime = Time.time + CwslGameConstants.RedMageFrozenOrbShardEmitInterval;
        EmitNextShard();
    }

    private void EmitNextShard()
    {
        if (orbBodyScale <= 0f || host == null)
            return;

        var session = CwslGameSession.Instance;
        if (session == null || session.Assets.frozenOrbPrefab == null)
            return;

        var origin = orbVisual != null ? orbVisual.position : transform.position;
        var flightVelocity = host.FlightVelocity;
        flightVelocity.y = 0f;
        if (flightVelocity.sqrMagnitude > 0.25f)
            origin += flightVelocity.normalized * 0.35f;

        var slots = Mathf.Max(1, CwslGameConstants.RedMageFrozenOrbEmitDirections);
        var angleStep = 360f / slots;
        var slot = directionCursor;
        var orbYaw = orbVisual != null ? orbVisual.eulerAngles.y : 0f;
        var angle = slot * angleStep + orbYaw;
        directionCursor = (directionCursor + 1) % slots;

        var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;
        else
            direction.Normalize();

        var shardVisualScale = Mathf.Max(MinShardVisualScale, orbBodyScale * ShardScaleRatio);
        var shardDamage = host.OrbDamage * CwslGameConstants.RedMageFrozenOrbShardDamageRatio;
        var velocity = direction * CwslGameConstants.RedMageFrozenOrbShardSpeed;
        var rotation = Quaternion.LookRotation(direction, Vector3.up);

        var networkObject = CwslNetworkPoolService.Instance?.Get(
            session.Assets.frozenOrbPrefab,
            origin,
            rotation);
        if (networkObject == null)
            return;

        var shard = networkObject.GetComponent<CwslFrozenOrbProjectile>();
        shard?.ConfigureAsShard(
            direction,
            CwslGameConstants.RedMageFrozenOrbShardSpeed,
            CwslGameConstants.RedMageFrozenOrbShardLifetime,
            host.OwnerClientId,
            shardDamage,
            host.OwnerNetworkObject,
            shardVisualScale,
            host.FrostDuration,
            host.FrostStacks);

        orbBodyScale = Mathf.Max(0f, orbBodyScale - CwslGameConstants.RedMageFrozenOrbScaleDrainPerShot);
        ApplyOrbBodyScale();
    }

    private void ApplyOrbBodyScale()
    {
        host?.SetFlightScaleMultiplier(orbBodyScale);

        if (orbBodyScale <= 0f && orbVisual != null)
            orbVisual.gameObject.SetActive(false);
    }

    private static bool IsServerActive()
    {
        return Unity.Netcode.NetworkManager.Singleton != null
               && Unity.Netcode.NetworkManager.Singleton.IsServer;
    }
}
