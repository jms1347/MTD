using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CwslPlayerProjectile : NetworkBehaviour, ICwslPooledNetworkObject
{
    private const float HitRadius = 0.42f;

    private static int monsterLayerMask = -1;

    private readonly HashSet<ulong> hitMonsterIds = new();

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

    public void Configure(
        Vector3 fireDirection,
        float projectileSpeed,
        float maxLifetime,
        ulong attackerClientId,
        float projectileDamage,
        bool piercing = false,
        NetworkObject owner = null)
    {
        direction = fireDirection.sqrMagnitude < 0.0001f ? Vector3.forward : fireDirection.normalized;
        speed = projectileSpeed;
        lifetime = maxLifetime;
        spawnTime = Time.time;
        spawnOrigin = transform.position;
        ownerClientId = attackerClientId;
        ownerNetworkObject = owner;
        damage = projectileDamage;
        pierce = piercing;
        configured = true;
        hitMonsterIds.Clear();
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    public void OnSpawnedFromPool()
    {
        configured = false;
        pierce = false;
        spawnTime = 0f;
        ownerNetworkObject = null;
        hitMonsterIds.Clear();
    }

    public void OnReturnedToPool()
    {
        configured = false;
        pierce = false;
        ownerNetworkObject = null;
        hitMonsterIds.Clear();
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

    private void TryHitAlongPath(Vector3 from, Vector3 to)
    {
        if (monsterLayerMask < 0)
            monsterLayerMask = LayerMask.GetMask(CwslGameConstants.LayerMonster);

        var delta = to - from;
        var distance = delta.magnitude;
        if (distance > 0.0001f)
        {
            var hits = Physics.SphereCastAll(
                from,
                HitRadius,
                delta.normalized,
                distance,
                monsterLayerMask,
                QueryTriggerInteraction.Collide);
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
    }

    private void TryHitMonstersAt(Vector3 position)
    {
        if (monsterLayerMask < 0)
            monsterLayerMask = LayerMask.GetMask(CwslGameConstants.LayerMonster);

        var hits = Physics.OverlapSphere(position, HitRadius, monsterLayerMask, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
        {
            if (ShouldIgnoreCollider(hit))
                continue;

            TryDamageCollider(hit);
            if (!configured)
                return;
        }
    }

    private void TryDamageCollider(Component collider)
    {
        if (!configured || !CanHitNow() || ShouldIgnoreCollider(collider))
            return;

        var monsterHealth = collider.GetComponentInParent<CwslMonsterHealth>();
        if (monsterHealth == null || !monsterHealth.IsAlive)
            return;

        var networkObject = monsterHealth.NetworkObject;
        if (networkObject != null && !hitMonsterIds.Add(networkObject.NetworkObjectId))
            return;

        monsterHealth.DamageFromPlayer(ownerClientId, damage);

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
