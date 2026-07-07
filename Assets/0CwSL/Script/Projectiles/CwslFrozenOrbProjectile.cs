using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>디아 오브 본체·얼음 파편 네트워크 투사체 (UkDefense FrozenOrbOBJ / FrostMissileOBJ).</summary>
public class CwslFrozenOrbProjectile : NetworkBehaviour, ICwslPooledNetworkObject
{
    private static int monsterLayerMask = -1;
    private static int playerLayerMask = -1;

    private readonly HashSet<ulong> hitMonsterIds = new();
    private readonly HashSet<ulong> hitPlayerIds = new();
    private readonly NetworkVariable<bool> networkIsShard = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Vector3 direction;
    private Vector3 spawnOrigin;
    private float speed;
    private float lifetime;
    private float spawnTime;
    private ulong ownerClientId;
    private NetworkObject ownerNetworkObject;
    private float damage;
    private bool configured;
    private bool pierce;
    private float frostDuration;
    private int frostStacks;
    private float flightScaleMultiplier = 1f;
    private Vector3 defaultLocalScale = Vector3.one;
    private CwslFrozenOrbEmitter emitter;

    public bool IsShard => networkIsShard.Value;
    public bool IsFlightActive => configured && IsSpawned;
    public Vector3 FlightVelocity => configured ? direction * speed : Vector3.zero;
    public ulong OwnerClientId => ownerClientId;
    public NetworkObject OwnerNetworkObject => ownerNetworkObject;
    public float OrbDamage => damage;
    public float FrostDuration => frostDuration;
    public int FrostStacks => frostStacks;

    private void Awake()
    {
        defaultLocalScale = transform.localScale;
        emitter = GetComponent<CwslFrozenOrbEmitter>();
    }

    public void ConfigureAsOrb(
        Vector3 fireDirection,
        float projectileSpeed,
        float maxLifetime,
        ulong attackerClientId,
        float projectileDamage,
        NetworkObject owner,
        float frostSeconds,
        int frostStackCount)
    {
        networkIsShard.Value = false;
        pierce = true;
        frostDuration = frostSeconds;
        frostStacks = frostStackCount;
        flightScaleMultiplier = 1f;
        ApplyCommonConfig(fireDirection, projectileSpeed, maxLifetime, attackerClientId, projectileDamage, owner);
        transform.localScale = defaultLocalScale;
        if (IsServer)
            GetComponent<CwslFrozenOrbVisual>()?.EnsureBuilt();

        emitter?.OnOrbLaunched(this);
    }

    public void ConfigureAsShard(
        Vector3 fireDirection,
        float projectileSpeed,
        float maxLifetime,
        ulong attackerClientId,
        float projectileDamage,
        NetworkObject owner,
        float visualScale,
        float frostSeconds,
        int frostStackCount)
    {
        networkIsShard.Value = true;
        pierce = false;
        frostDuration = frostSeconds;
        frostStacks = frostStackCount;
        flightScaleMultiplier = Mathf.Clamp(visualScale, 0.4f, 0.85f);
        ApplyCommonConfig(fireDirection, projectileSpeed, maxLifetime, attackerClientId, projectileDamage, owner);
        transform.localScale = defaultLocalScale * flightScaleMultiplier;
        if (IsServer)
            GetComponent<CwslFrozenOrbVisual>()?.EnsureBuilt();
    }

    private void ApplyCommonConfig(
        Vector3 fireDirection,
        float projectileSpeed,
        float maxLifetime,
        ulong attackerClientId,
        float projectileDamage,
        NetworkObject owner)
    {
        direction = fireDirection.sqrMagnitude < 0.0001f ? Vector3.forward : fireDirection.normalized;
        speed = projectileSpeed;
        lifetime = maxLifetime;
        spawnTime = Time.time;
        spawnOrigin = transform.position;
        ownerClientId = attackerClientId;
        ownerNetworkObject = owner;
        damage = projectileDamage;
        configured = true;
        hitMonsterIds.Clear();
        hitPlayerIds.Clear();
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    public void SetFlightScaleMultiplier(float scaleMultiplier)
    {
        flightScaleMultiplier = Mathf.Clamp(scaleMultiplier, 0.35f, 1f);
        transform.localScale = defaultLocalScale * flightScaleMultiplier;
    }

    public void OnSpawnedFromPool()
    {
        configured = false;
        networkIsShard.Value = false;
        pierce = false;
        flightScaleMultiplier = 1f;
        ownerNetworkObject = null;
        spawnTime = 0f;
        hitMonsterIds.Clear();
        hitPlayerIds.Clear();
        transform.localScale = defaultLocalScale;
        emitter?.OnOrbEnded();
    }

    public void OnReturnedToPool()
    {
        configured = false;
        networkIsShard.Value = false;
        pierce = false;
        ownerNetworkObject = null;
        hitMonsterIds.Clear();
        hitPlayerIds.Clear();
        transform.localScale = defaultLocalScale;
        emitter?.OnOrbEnded();
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

        var step = speed * Time.deltaTime;
        var from = transform.position;
        var to = from + direction * step;

        if (CanHitNow())
            TryHitAlongPath(from, to);

        transform.position = to;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !configured || !CanHitNow() || ShouldIgnoreCollider(other))
            return;

        if (TryHitShieldBubble(other))
            return;

        TryDamageCollider(other);
    }

    private bool CanHitNow()
    {
        if (Time.time - spawnTime < CwslGameConstants.PlayerArrowMinHitDelay)
            return false;

        var traveled = Vector3.Distance(transform.position, spawnOrigin);
        return traveled >= CwslGameConstants.PlayerArrowMinHitDistance;
    }

    private bool ShouldIgnoreCollider(Component collider)
    {
        if (collider == null)
            return true;

        if (ownerNetworkObject != null)
        {
            var hitObject = collider.GetComponentInParent<NetworkObject>();
            if (hitObject != null && hitObject.NetworkObjectId == ownerNetworkObject.NetworkObjectId)
                return true;
        }

        var ownerHealth = collider.GetComponentInParent<CwslPlayerHealth>();
        return ownerHealth != null && ownerHealth.OwnerClientId == ownerClientId;
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

        configured = false;
        DespawnSelf();
        return true;
    }

    private void TryHitAlongPath(Vector3 from, Vector3 to)
    {
        if (monsterLayerMask < 0)
            monsterLayerMask = LayerMask.GetMask(CwslGameConstants.LayerMonster);

        var monsterMask = monsterLayerMask != 0 ? monsterLayerMask : ~0;
        var delta = to - from;
        var distance = delta.magnitude;
        if (distance > 0.0001f)
        {
            var hits = Physics.SphereCastAll(
                from,
                CwslGameConstants.PlayerBulletHitRadius,
                delta.normalized,
                distance,
                monsterMask,
                QueryTriggerInteraction.Collide);

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (ShouldIgnoreCollider(hit.collider))
                    continue;

                TryDamageCollider(hit.collider);
                if (!configured)
                    return;
            }
        }

        TryHitMonstersAt(to);
        TryHitPlayersAt(to);
    }

    private void TryHitMonstersAt(Vector3 position)
    {
        if (monsterLayerMask < 0)
            monsterLayerMask = LayerMask.GetMask(CwslGameConstants.LayerMonster);

        var monsterMask = monsterLayerMask != 0 ? monsterLayerMask : ~0;
        var hits = Physics.OverlapSphere(
            position,
            CwslGameConstants.PlayerBulletHitRadius,
            monsterMask,
            QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            if (ShouldIgnoreCollider(hit))
                continue;

            TryDamageCollider(hit);
            if (!configured)
                return;
        }
    }

    private void TryHitPlayersAt(Vector3 position)
    {
        if (playerLayerMask < 0)
            playerLayerMask = LayerMask.GetMask(CwslGameConstants.LayerPlayer);

        var mask = playerLayerMask != 0 ? playerLayerMask : ~0;
        var hits = Physics.OverlapSphere(
            position,
            CwslGameConstants.PlayerBulletHitRadius,
            mask,
            QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            if (ShouldIgnoreCollider(hit))
                continue;

            if (TryHitShieldBubble(hit))
                return;

            TryDamageCollider(hit);
            if (!configured)
                return;
        }
    }

    private void TryDamageCollider(Component collider)
    {
        if (collider == null)
            return;

        var monsterHealth = collider.GetComponentInParent<CwslMonsterHealth>();
        if (monsterHealth != null && monsterHealth.IsAlive)
        {
            TryDamageMonster(monsterHealth);
            return;
        }

        var playerHealth = collider.GetComponentInParent<CwslPlayerHealth>();
        if (playerHealth != null)
            TryDamagePlayer(playerHealth);
    }

    private void TryDamageMonster(CwslMonsterHealth monsterHealth)
    {
        if (monsterHealth == null || !monsterHealth.IsAlive)
            return;

        var networkObject = monsterHealth.NetworkObject;
        if (networkObject != null && !hitMonsterIds.Add(networkObject.NetworkObjectId))
            return;

        monsterHealth.DamageFromPlayer(ownerClientId, damage);
        CwslMonsterStatusController.Ensure(monsterHealth)?.ApplyFrostServer(
            ownerClientId,
            frostDuration,
            frostStacks);

        if (!pierce)
        {
            configured = false;
            DespawnSelf();
        }
    }

    private void TryDamagePlayer(CwslPlayerHealth playerHealth)
    {
        if (playerHealth == null || !playerHealth.IsAlive || playerHealth.OwnerClientId == ownerClientId)
            return;

        if (!hitPlayerIds.Add(playerHealth.OwnerClientId))
            return;

        playerHealth.TryReceiveProjectileHitServer(damage, transform.position);

        if (!pierce)
        {
            configured = false;
            DespawnSelf();
        }
    }

    private void DespawnSelf()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            CwslNetworkPoolService.Instance?.Release(NetworkObject);
    }
}
