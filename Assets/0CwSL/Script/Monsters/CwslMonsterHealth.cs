using System;
using Unity.Netcode;
using UnityEngine;

public class CwslMonsterHealth : NetworkBehaviour, ICwslPooledNetworkObject
{
    private readonly NetworkVariable<float> health = new(
        CwslGameConstants.MonsterMaxHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public CwslMonsterType MonsterType { get; private set; }
    public bool IsAlive => health.Value > 0f;

    public event Action<CwslMonsterHealth, ulong> OnKilled;

    public void Configure(CwslMonsterType type)
    {
        MonsterType = type;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            health.Value = CwslGameConstants.MonsterMaxHealth;
    }

    public void OnSpawnedFromPool()
    {
        if (IsServer)
            health.Value = CwslGameConstants.MonsterMaxHealth;

        CwslMonsterMaterialFix.Refresh(transform, MonsterType);
    }

    public void OnReturnedToPool()
    {
    }

    public void DamageFromPlayer(ulong attackerClientId, float amount)
    {
        if (!IsServer || !IsAlive || amount <= 0f)
            return;

        ShowDamagePopupClientRpc(transform.position + Vector3.up * 1.2f, amount, (int)CwslDamagePopupKind.Monster);
        Die(attackerClientId);
    }

    public void ForceKillServer(ulong attackerClientId = ulong.MaxValue, bool dropGold = true)
    {
        if (!IsServer || !IsAlive)
            return;

        Die(attackerClientId, dropGold);
    }

    public void ForceKillWithGoldAtServer(Vector3 goldPosition, ulong attackerClientId = ulong.MaxValue)
    {
        if (!IsServer || !IsAlive)
            return;

        health.Value = 0f;
        CwslGoldDropService.SpawnDrop(goldPosition);
        OnKilled?.Invoke(this, attackerClientId);
        PlayDeathClientRpc(transform.position, (int)MonsterType);

        if (NetworkObject != null && NetworkObject.IsSpawned)
            CwslNetworkPoolService.Instance?.Release(NetworkObject);
    }

    private void Die(ulong attackerClientId, bool dropGold = true)
    {
        health.Value = 0f;
        if (dropGold)
            CwslGoldDropService.SpawnDrop(transform.position);
        OnKilled?.Invoke(this, attackerClientId);
        PlayDeathClientRpc(transform.position, (int)MonsterType);

        if (NetworkObject != null && NetworkObject.IsSpawned)
            CwslNetworkPoolService.Instance?.Release(NetworkObject);
    }

    [ClientRpc]
    private void ShowDamagePopupClientRpc(Vector3 position, float amount, int kind)
    {
        CwslDamagePopupPool.Play(position, amount, (CwslDamagePopupKind)kind);
    }

    [ClientRpc]
    private void PlayDeathClientRpc(Vector3 position, int monsterType)
    {
        CwslVfxSpawner.SpawnEnemyDeath(position, (CwslMonsterType)monsterType);
    }
}
