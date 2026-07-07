using Unity.Netcode;
using UnityEngine;

public class CwslSuicideMonster : CwslMonsterBase, ICwslPooledNetworkObject
{
    private const float RushSpeedMultiplier = 1.05f;
    private const float DetonateRadius = CwslMonsterStatCatalog.SuicideExplosionRadius;
    private const float ExplosionDamage = CwslMonsterStatCatalog.SuicideExplosionDamage;
    private bool detonated;

    public override void Initialize(CwslMonsterType type)
    {
        base.Initialize(type);
        detonated = false;
    }

    public void OnSpawnedFromPool()
    {
        detonated = false;
    }

    public void OnReturnedToPool()
    {
        detonated = false;
    }

    protected override void TickServerAI()
    {
        if (detonated)
            return;

        MoveToward(currentTarget.transform.position, RushSpeedMultiplier);

        if (GetFlatDistanceTo(currentTarget) <= DetonateRadius)
            DetonateServer();
    }

    private void DetonateServer()
    {
        if (detonated)
            return;

        detonated = true;
        var position = transform.position;
        PlayExplosionClientRpc(position);

        var damage = GetScaledDamage(ExplosionDamage);
        var nexus = currentTarget != null ? currentTarget.GetComponent<CwslNexus>() : null;
        if (nexus != null && nexus.IsAlive)
        {
            nexus.DamageServer(GetScaledDamage(CwslMonsterStatCatalog.SuicideNexusExplosionDamage));
        }
        else
        {
            var playerHealth = currentTarget != null
                ? currentTarget.GetComponent<CwslPlayerHealth>()
                : null;
            if (playerHealth != null)
            {
                var hitPoint = currentTarget.transform.position + Vector3.up * 0.9f;
                playerHealth.TryReceiveExplosionHitServer(damage, hitPoint);
            }
        }

        // 발밑이 아니라 바깥에서 드롭해 플레이어에게 날아오는 연출이 보이게 함
        var goldPosition = ResolveGoldDropPosition(position);
        health?.ForceKillWithGoldAtServer(goldPosition);
    }

    private Vector3 ResolveGoldDropPosition(Vector3 explosionPosition)
    {
        var playerPosition = currentTarget != null
            ? currentTarget.transform.position
            : explosionPosition;

        var away = explosionPosition - playerPosition;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
            away = currentTarget != null ? currentTarget.transform.forward : Vector3.forward;

        var side = Vector3.Cross(Vector3.up, away.normalized);
        return explosionPosition
               + away.normalized * 2.4f
               + side * Random.Range(-0.7f, 0.7f)
               + Vector3.up * 0.7f;
    }

    [ClientRpc]
    private void PlayExplosionClientRpc(Vector3 position)
    {
        CwslVfxSpawner.SpawnSuicideExplosion(position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || detonated)
            return;

        var nexus = other.GetComponentInParent<CwslNexus>();
        if (nexus != null && nexus.IsAlive)
        {
            currentTarget = nexus.GetComponent<NetworkObject>();
            DetonateServer();
            return;
        }

        var playerHealth = other.GetComponentInParent<CwslPlayerHealth>();
        if (playerHealth == null || !playerHealth.IsAlive)
            return;

        currentTarget = playerHealth.GetComponent<NetworkObject>();
        DetonateServer();
    }
}
