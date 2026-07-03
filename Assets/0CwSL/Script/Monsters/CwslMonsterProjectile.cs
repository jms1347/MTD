using Unity.Netcode;
using UnityEngine;

public class CwslMonsterProjectile : NetworkBehaviour, ICwslPooledNetworkObject
{
    private const float ProjectileDamage = 10f;
    private const float HitRadius = 0.45f;

    private static int playerLayerMask = -1;

    private Vector3 direction;
    private float speed;
    private float lifetime;
    private float spawnTime;
    private bool configured;

    public void Configure(Vector3 fireDirection, float projectileSpeed, float maxLifetime)
    {
        direction = fireDirection.sqrMagnitude < 0.0001f ? Vector3.forward : fireDirection.normalized;

        speed = projectileSpeed;
        lifetime = maxLifetime;
        spawnTime = Time.time;
        configured = true;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    public void OnSpawnedFromPool()
    {
        configured = false;
        spawnTime = 0f;
    }

    public void OnReturnedToPool()
    {
        configured = false;
    }

    private void Update()
    {
        if (!IsServer || !configured)
            return;

        if (Time.time - spawnTime > lifetime)
        {
            DespawnSelf();
            return;
        }

        transform.position += direction * (speed * Time.deltaTime);
        TryHitPlayers();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !configured)
            return;

        if (TryHitShieldBubble(other))
            return;

        TryDamageCollider(other);
    }

    private void TryHitPlayers()
    {
        if (playerLayerMask < 0)
            playerLayerMask = LayerMask.GetMask(CwslGameConstants.LayerPlayer);

        var mask = playerLayerMask != 0 ? playerLayerMask : ~0;
        var hits = Physics.OverlapSphere(
            transform.position,
            Mathf.Max(HitRadius, CwslGameConstants.FortifyShieldBlockRadius),
            mask,
            QueryTriggerInteraction.Collide);

        // 방패 버블을 몸통보다 먼저 처리
        foreach (var hit in hits)
        {
            if (!configured)
                return;
            if (TryHitShieldBubble(hit))
                return;
        }

        foreach (var hit in hits)
        {
            if (!configured)
                return;
            TryDamageCollider(hit);
        }

        if (configured)
            TryHitPlayersByDistance();
    }

    private bool TryHitShieldBubble(Component collider)
    {
        if (!configured || collider == null)
            return false;

        var marker = collider.GetComponent<CwslShieldBubbleMarker>()
                     ?? collider.GetComponentInParent<CwslShieldBubbleMarker>();
        if (marker == null || marker.Bubble == null || !marker.Bubble.IsBubbleActive)
            return false;

        if (!marker.Bubble.TryBlockProjectileServer(transform.position, ProjectileDamage))
            return false;

        configured = false;
        DespawnSelf();
        return true;
    }

    private void TryHitPlayersByDistance()
    {
        var players = FindObjectsByType<CwslPlayerHealth>(FindObjectsSortMode.None);
        foreach (var playerHealth in players)
        {
            if (playerHealth == null || !playerHealth.IsAlive)
                continue;

            if (playerHealth.TryInterceptProjectileServer(transform.position, ProjectileDamage))
            {
                configured = false;
                DespawnSelf();
                return;
            }

            var flat = playerHealth.transform.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > (HitRadius + 0.55f) * (HitRadius + 0.55f))
                continue;

            configured = false;
            playerHealth.TryReceiveProjectileHitServer(ProjectileDamage, transform.position);
            DespawnSelf();
            return;
        }
    }

    private void TryDamageCollider(Component collider)
    {
        if (!configured)
            return;

        // 방패 버블은 위에서 처리
        if (collider.GetComponent<CwslShieldBubbleMarker>() != null
            || collider.GetComponentInParent<CwslShieldBubbleMarker>() != null)
            return;

        var playerHealth = collider.GetComponentInParent<CwslPlayerHealth>();
        if (playerHealth == null || !playerHealth.IsAlive)
            return;

        configured = false;
        playerHealth.TryReceiveProjectileHitServer(ProjectileDamage, transform.position);
        DespawnSelf();
    }

    private void DespawnSelf()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            CwslNetworkPoolService.Instance?.Release(NetworkObject);
    }
}
