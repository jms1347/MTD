using Unity.Netcode;
using UnityEngine;

/// <summary>홍명보 보스 스킬 투사체 — 탄막·감염 구체·감염 가시.</summary>
public class CwslBossSkillProjectile : NetworkBehaviour, ICwslPooledNetworkObject
{
    private static int playerLayerMask = -1;

    private Vector3 direction;
    private float speed;
    private float lifetime;
    private float spawnTime;
    private float damage;
    private bool configured;
    private CwslBossSkillProjectileKind kind;
    private ulong sourceClientId;
    private ulong ownerClientId;

    public void Configure(
        Vector3 fireDirection,
        float projectileSpeed,
        float maxLifetime,
        CwslBossSkillProjectileKind projectileKind,
        float projectileDamage,
        ulong infectedSourceClientId = ulong.MaxValue,
        ulong barrageOwnerClientId = ulong.MaxValue)
    {
        direction = fireDirection.sqrMagnitude < 0.0001f ? Vector3.forward : fireDirection.normalized;
        speed = projectileSpeed;
        lifetime = maxLifetime;
        damage = projectileDamage;
        kind = projectileKind;
        sourceClientId = infectedSourceClientId;
        ownerClientId = barrageOwnerClientId;
        spawnTime = Time.time;
        configured = true;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        ApplyVisualScale();
    }

    public static void SpawnServer(
        Vector3 origin,
        Vector3 fireDirection,
        CwslBossSkillProjectileKind projectileKind,
        ulong infectedSourceClientId = ulong.MaxValue,
        ulong barrageOwnerClientId = ulong.MaxValue)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        var session = CwslGameSession.Instance;
        var prefab = session?.Assets.bossSkillProjectilePrefab ?? session?.Assets.projectilePrefab;
        if (session == null || prefab == null)
            return;

        var rotation = fireDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(fireDirection.normalized, Vector3.up)
            : Quaternion.identity;
        var networkObject = CwslNetworkPoolService.Instance?.Get(
            prefab,
            origin,
            rotation);
        if (networkObject == null)
            return;

        var projectile = networkObject.GetComponent<CwslBossSkillProjectile>();
        if (projectile == null)
            return;

        var speed = projectileKind switch
        {
            CwslBossSkillProjectileKind.Barrage => CwslGameConstants.BossBarrageProjectileSpeed,
            CwslBossSkillProjectileKind.InfectionOrb => CwslGameConstants.BossInfectionOrbSpeed,
            _ => CwslGameConstants.BossInfectedSpikeSpeed
        };
        var lifetime = projectileKind switch
        {
            CwslBossSkillProjectileKind.Barrage => CwslGameConstants.BossBarrageProjectileLifetime,
            CwslBossSkillProjectileKind.InfectionOrb => CwslGameConstants.BossInfectionOrbLifetime,
            _ => CwslGameConstants.BossInfectedSpikeLifetime
        };
        var dmg = projectileKind switch
        {
            CwslBossSkillProjectileKind.Barrage => CwslGameConstants.BossBarrageDamage,
            CwslBossSkillProjectileKind.InfectionOrb => 0f,
            _ => CwslGameConstants.BossInfectedSpikeDamage
        };

        projectile.Configure(
            fireDirection,
            speed,
            lifetime,
            projectileKind,
            dmg,
            infectedSourceClientId,
            barrageOwnerClientId);
    }

    public void OnSpawnedFromPool()
    {
        configured = false;
        spawnTime = 0f;
        sourceClientId = ulong.MaxValue;
        ownerClientId = ulong.MaxValue;
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
            configured = false;
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

        TryDamageCollider(other);
    }

    private void TryHitPlayers()
    {
        if (playerLayerMask < 0)
            playerLayerMask = LayerMask.GetMask(CwslGameConstants.LayerPlayer);

        var mask = playerLayerMask != 0 ? playerLayerMask : ~0;
        var hits = Physics.OverlapSphere(
            transform.position,
            GetHitRadius(),
            mask,
            QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            if (!configured)
                return;
            TryDamageCollider(hit);
        }
    }

    private float GetHitRadius()
    {
        return kind switch
        {
            CwslBossSkillProjectileKind.InfectionOrb => CwslGameConstants.BossInfectionOrbRadius,
            CwslBossSkillProjectileKind.InfectedSpike => 0.35f,
            _ => 0.28f
        };
    }

    private void TryDamageCollider(Component collider)
    {
        if (!configured || collider == null)
            return;

        var playerHealth = collider.GetComponent<CwslPlayerHealth>()
                           ?? collider.GetComponentInParent<CwslPlayerHealth>();
        if (playerHealth == null || !playerHealth.IsAlive)
            return;

        var playerObject = playerHealth.GetComponent<NetworkObject>();
        if (playerObject == null)
            return;

        switch (kind)
        {
            case CwslBossSkillProjectileKind.Barrage:
                var barrageDebuff = playerHealth.GetComponent<CwslPlayerBossDebuff>();
                if (barrageDebuff != null && barrageDebuff.IsInvincibleToBossBarrage)
                    return;

                playerHealth.TryReceiveProjectileHitServer(damage, transform.position);
                DespawnWithFx();
                break;

            case CwslBossSkillProjectileKind.InfectionOrb:
                var infectionDebuff = playerHealth.GetComponent<CwslPlayerBossDebuff>();
                if (infectionDebuff == null)
                    return;

                infectionDebuff.ApplyInfectedServer(CwslGameConstants.BossInfectedDuration);
                DespawnWithFx();
                break;

            case CwslBossSkillProjectileKind.InfectedSpike:
                if (playerObject.OwnerClientId == sourceClientId)
                    return;

                playerHealth.TryReceiveProjectileHitServer(damage, transform.position);
                DespawnWithFx();
                break;
        }
    }

    private void DespawnWithFx()
    {
        var color = kind switch
        {
            CwslBossSkillProjectileKind.InfectionOrb => new Color(0.08f, 0.08f, 0.1f),
            CwslBossSkillProjectileKind.InfectedSpike => new Color(0.9f, 0.1f, 0.1f),
            _ => new Color(0.95f, 0.2f, 0.12f)
        };
        PlayHitFxClientRpc(transform.position, (byte)kind);
        configured = false;
        DespawnSelf();
    }

    private void DespawnSelf()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            CwslNetworkPoolService.Instance?.Release(NetworkObject);
    }

    private void ApplyVisualScale()
    {
        var scale = kind switch
        {
            CwslBossSkillProjectileKind.InfectionOrb => CwslGameConstants.BossInfectionOrbRadius * 2f,
            CwslBossSkillProjectileKind.InfectedSpike => 0.35f,
            _ => 0.3f
        };
        transform.localScale = Vector3.one * scale;
    }

    [ClientRpc]
    private void PlayHitFxClientRpc(Vector3 position, byte kindRaw)
    {
        var projectileKind = (CwslBossSkillProjectileKind)kindRaw;
        var color = projectileKind switch
        {
            CwslBossSkillProjectileKind.InfectionOrb => new Color(0.15f, 0.05f, 0.08f),
            CwslBossSkillProjectileKind.InfectedSpike => new Color(0.95f, 0.1f, 0.1f),
            _ => new Color(0.95f, 0.25f, 0.1f)
        };
        CwslSimpleVfx.SpawnBurst(position, color, 0.8f, 0.2f);
    }
}
