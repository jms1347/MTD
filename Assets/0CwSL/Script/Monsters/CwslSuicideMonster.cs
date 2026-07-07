using Unity.Netcode;
using UnityEngine;

public class CwslSuicideMonster : CwslMonsterBase, ICwslPooledNetworkObject
{
    protected const float RushSpeedMultiplier = 1.12f;
    protected const float DetonateRadius = CwslMonsterStatCatalog.SuicideExplosionRadius;
    protected const float ExplosionDamage = CwslMonsterStatCatalog.SuicideExplosionDamage;

    protected bool detonated;

    public override void Initialize(CwslMonsterType type)
    {
        base.Initialize(type);
        targetingMode = CwslMonsterTargetingMode.Nearest;
        detonated = false;
    }

    public void OnSpawnedFromPool()
    {
        detonated = false;
        ResetSuicideState();
    }

    public void OnReturnedToPool()
    {
        detonated = false;
        ResetSuicideState();
    }

    protected virtual void ResetSuicideState()
    {
    }

    protected override void TickServerAI()
    {
        if (detonated)
            return;

        if (!IsValidTarget(currentTarget))
        {
            RefreshTarget();
            if (!IsValidTarget(currentTarget))
                return;
        }

        var destination = currentTarget.GetComponent<CwslNexus>() != null
            ? GetTargetMovePosition()
            : currentTarget.transform.position;
        MoveToward(destination, RushSpeedMultiplier);

        if (GetFlatDistanceTo(currentTarget) <= DetonateRadius)
            DetonateServer();
    }

    protected void DetonateServer()
    {
        if (detonated)
            return;

        detonated = true;
        var position = transform.position;
        PlayExplosionClientRpc(position);

        var damage = GetScaledDamage(ExplosionDamage);
        DamagePlayersInRadius(position, damage, DetonateRadius);

        var nexus = CwslNexus.Instance;
        if (nexus != null && nexus.IsAlive)
        {
            var flat = nexus.transform.position - position;
            flat.y = 0f;
            if (flat.magnitude <= DetonateRadius + 1.2f)
                nexus.DamageServer(GetScaledDamage(CwslMonsterStatCatalog.SuicideNexusExplosionDamage));
        }

        var goldPosition = ResolveGoldDropPosition(position);
        health?.ForceKillWithGoldAtServer(goldPosition);
    }

    protected static void DamagePlayersInRadius(Vector3 center, float damage, float radius)
    {
        foreach (var playerHealth in Object.FindObjectsByType<CwslPlayerHealth>(FindObjectsSortMode.None))
        {
            if (playerHealth == null || !playerHealth.IsAlive)
                continue;

            var flat = playerHealth.transform.position - center;
            flat.y = 0f;
            if (flat.magnitude > radius)
                continue;

            playerHealth.TryReceiveExplosionHitServer(damage, playerHealth.transform.position + Vector3.up * 0.9f);
        }
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
