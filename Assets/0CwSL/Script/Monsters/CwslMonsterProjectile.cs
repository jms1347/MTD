using Unity.Netcode;
using UnityEngine;

public class CwslMonsterProjectile : NetworkBehaviour, ICwslPooledNetworkObject
{
    private const float ProjectileDamage = 10f;

    private static int playerLayerMask = -1;

    private Vector3 direction;
    private float speed;
    private float lifetime;
    private float spawnTime;
    private float damage = 10f;
    private bool configured;
    private CwslMonsterProjectileKind projectileKind = CwslMonsterProjectileKind.InkBolt;

    private readonly NetworkVariable<byte> syncedKind = new(
        (byte)CwslMonsterProjectileKind.InkBolt,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public bool IsActiveProjectile => configured && IsSpawned;
    public CwslMonsterProjectileKind ProjectileKind =>
        IsSpawned ? (CwslMonsterProjectileKind)syncedKind.Value : projectileKind;

    public void Configure(
        Vector3 fireDirection,
        float projectileSpeed,
        float maxLifetime,
        float projectileDamage = 10f,
        CwslMonsterProjectileKind kind = CwslMonsterProjectileKind.InkBolt)
    {
        direction = fireDirection.sqrMagnitude < 0.0001f ? Vector3.forward : fireDirection.normalized;

        speed = projectileSpeed;
        lifetime = maxLifetime;
        damage = projectileDamage;
        projectileKind = kind;
        if (IsServer)
            syncedKind.Value = (byte)kind;
        spawnTime = Time.time;
        configured = true;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        GetComponent<CwslProjectileVisual>()?.RefreshVisual();
        CwslCombatRegistry.RegisterMonsterProjectile(this);
    }

    public void OnSpawnedFromPool()
    {
        configured = false;
        spawnTime = 0f;
        projectileKind = CwslMonsterProjectileKind.InkBolt;
        if (IsServer)
            syncedKind.Value = (byte)CwslMonsterProjectileKind.InkBolt;
    }

    public void OnReturnedToPool()
    {
        CwslCombatRegistry.UnregisterMonsterProjectile(this);
        configured = false;
    }

    private void Update()
    {
        if (!IsServer || !configured)
            return;

        if (Time.time - spawnTime > lifetime)
        {
            configured = false;
            DespawnSelf();
            return;
        }

        transform.position += direction * (speed * (GetComponent<CwslSlowModifier>()?.SpeedMultiplier ?? 1f) * Time.deltaTime);
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
        var bodyQueryRadius = CwslGameConstants.PlayerBodyColliderRadiusDefault + CwslGameConstants.PlayerBodyHitSlop + 0.12f;
        var hits = Physics.OverlapSphere(
            transform.position,
            bodyQueryRadius,
            mask,
            QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            if (!configured)
                return;
            if (TryHitShieldBubble(hit))
                return;
        }

        TryHitShieldBubbleByProximity();

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

        if (!marker.Bubble.TryBlockProjectileServer(transform.position, damage))
            return false;

        DespawnWithHitFx(transform.position);
        return true;
    }

    private void TryHitShieldBubbleByProximity()
    {
        if (!configured)
            return;

        var markers = FindObjectsByType<CwslShieldBubbleMarker>(FindObjectsSortMode.None);
        foreach (var marker in markers)
        {
            if (marker == null || marker.Bubble == null || !marker.Bubble.IsBubbleActive)
                continue;

            var bubbleCenter = marker.transform.position;
            if ((transform.position - bubbleCenter).sqrMagnitude >
                CwslGameConstants.FortifyShieldBlockRadius * CwslGameConstants.FortifyShieldBlockRadius)
                continue;

            if (TryHitShieldBubble(marker))
                return;
        }
    }

    private void TryHitPlayersByDistance()
    {
        var players = CwslCombatRegistry.AlivePlayers;
        foreach (var playerHealth in players)
        {
            if (playerHealth == null || !playerHealth.IsAlive)
                continue;

            if (playerHealth.TryInterceptProjectileServer(transform.position, damage))
            {
                DespawnWithHitFx(transform.position);
                return;
            }

            var flat = playerHealth.transform.position - transform.position;
            flat.y = 0f;
            var bodyRadius = playerHealth.GetComponent<CwslPlayerBodyCollider>()?.Radius
                ?? CwslGameConstants.PlayerBodyColliderRadiusDefault;
            var hitReach = bodyRadius + CwslGameConstants.PlayerBodyHitSlop;
            if (flat.sqrMagnitude > hitReach * hitReach)
                continue;

            ApplyPlayerHitServer(playerHealth, transform.position);
            DespawnWithHitFx(transform.position);
            return;
        }
    }

    private void TryDamageCollider(Component collider)
    {
        if (!configured)
            return;

        if (collider.GetComponent<CwslShieldBubbleMarker>() != null
            || collider.GetComponentInParent<CwslShieldBubbleMarker>() != null)
            return;

        var nexus = collider.GetComponentInParent<CwslNexus>();
        if (nexus != null && nexus.IsAlive)
        {
            nexus.DamageServer(damage);
            DespawnWithHitFx(transform.position);
            return;
        }

        var playerHealth = collider.GetComponentInParent<CwslPlayerHealth>();
        if (playerHealth == null || !playerHealth.IsAlive)
            return;

        ApplyPlayerHitServer(playerHealth, transform.position);
        DespawnWithHitFx(transform.position);
    }

    private void ApplyPlayerHitServer(CwslPlayerHealth playerHealth, Vector3 hitPosition)
    {
        playerHealth.TryReceiveProjectileHitServer(damage, hitPosition);
        if (projectileKind != CwslMonsterProjectileKind.InkBolt)
            return;

        playerHealth.GetComponent<CwslPlayerVisionDebuff>()
            ?.ApplyInkBlindServer(CwslGameConstants.InkBlindDurationSeconds, hitPosition);
    }

    private void DespawnWithHitFx(Vector3 hitPosition)
    {
        if (!configured)
            return;

        configured = false;
        PlayHitFxClientRpc(hitPosition, direction, (byte)projectileKind);
        DespawnSelf();
    }

    private void DespawnSelf()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            CwslNetworkPoolService.Instance?.Release(NetworkObject);
    }

    [ClientRpc]
    private void PlayHitFxClientRpc(Vector3 hitPosition, Vector3 fireDirection, byte kindRaw)
    {
        var kind = (CwslMonsterProjectileKind)kindRaw;
        if (kind == CwslMonsterProjectileKind.TankBullet)
            CwslVfxSpawner.SpawnRangedTankProjectileHit(hitPosition, fireDirection);
        else
            CwslVfxSpawner.SpawnShadowProjectileHit(hitPosition, fireDirection);
    }
}
