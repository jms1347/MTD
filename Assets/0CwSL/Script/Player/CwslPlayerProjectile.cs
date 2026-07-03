using Unity.Netcode;
using UnityEngine;

public class CwslPlayerProjectile : NetworkBehaviour, ICwslPooledNetworkObject
{
    private const float HitRadius = 0.42f;

    private static int monsterLayerMask = -1;

    private Vector3 direction;
    private float speed;
    private float lifetime;
    private float spawnTime;
    private ulong ownerClientId;
    private float damage;
    private bool configured;

    public void Configure(
        Vector3 fireDirection,
        float projectileSpeed,
        float maxLifetime,
        ulong attackerClientId,
        float projectileDamage)
    {
        direction = fireDirection.sqrMagnitude < 0.0001f ? Vector3.forward : fireDirection.normalized;
        speed = projectileSpeed;
        lifetime = maxLifetime;
        spawnTime = Time.time;
        ownerClientId = attackerClientId;
        damage = projectileDamage;
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
        TryHitMonsters();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !configured)
            return;

        TryDamageCollider(other);
    }

    private void TryHitMonsters()
    {
        if (monsterLayerMask < 0)
            monsterLayerMask = LayerMask.GetMask(CwslGameConstants.LayerMonster);

        var hits = Physics.OverlapSphere(transform.position, HitRadius, monsterLayerMask, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
            TryDamageCollider(hit);
    }

    private void TryDamageCollider(Component collider)
    {
        if (!configured)
            return;

        var monsterHealth = collider.GetComponentInParent<CwslMonsterHealth>();
        if (monsterHealth == null || !monsterHealth.IsAlive)
            return;

        configured = false;
        monsterHealth.DamageFromPlayer(ownerClientId, damage);
        DespawnSelf();
    }

    private void DespawnSelf()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            CwslNetworkPoolService.Instance?.Release(NetworkObject);
    }
}
