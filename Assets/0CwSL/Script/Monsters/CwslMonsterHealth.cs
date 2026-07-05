using System;
using Unity.Netcode;
using UnityEngine;

public class CwslMonsterHealth : NetworkBehaviour, ICwslPooledNetworkObject
{
    private readonly NetworkVariable<float> health = new(
        CwslGameConstants.MonsterMaxHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private int dropGoldAmount = CwslGameConstants.GoldDropNormal;
    private float maxHealth = CwslGameConstants.MonsterMaxHealth;
    private bool isExecutive;

    public CwslMonsterType MonsterType { get; private set; }
    public bool IsAlive => health.Value > 0f;
    public float CurrentHealth => health.Value;
    public float MaxHealth => maxHealth;
    public bool IsExecutive => isExecutive;
    public bool IsBoss => MonsterType == CwslMonsterType.BossHongmyeongbo;

    public Vector3 GetAimPoint()
    {
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
            return transform.TransformPoint(capsule.center);

        return transform.position + Vector3.up * CwslGameConstants.MonsterHitCenterY;
    }

    public event Action<CwslMonsterHealth, ulong> OnKilled;
    public event Action<CwslMonsterHealth, float, ulong> OnDamaged;

    public void Configure(CwslMonsterType type, int goldDrop = -1, bool executive = false)
    {
        MonsterType = type;
        isExecutive = executive;
        dropGoldAmount = goldDrop >= 0
            ? goldDrop
            : executive
                ? CwslGameConstants.GoldDropExecutive
                : CwslGameConstants.GoldDropNormal;
        maxHealth = CwslGameConstants.MonsterMaxHealth;
    }

    public void ConfigureBoss(float bossMaxHealth)
    {
        MonsterType = CwslMonsterType.BossHongmyeongbo;
        isExecutive = false;
        dropGoldAmount = 0;
        maxHealth = bossMaxHealth;
        if (IsServer)
            health.Value = bossMaxHealth;
    }

    public override void OnNetworkSpawn()
    {
        EnsureCombatHitCollider();
        if (IsServer && !IsBoss)
            health.Value = maxHealth;
    }

    public void OnSpawnedFromPool()
    {
        EnsureCombatHitCollider();
        if (IsServer && !IsBoss)
            health.Value = maxHealth;

        CwslMonsterMaterialFix.Refresh(transform, MonsterType);
        if (isExecutive)
            CwslMonsterExecutiveVisual.Apply(transform);
    }

    public void OnReturnedToPool()
    {
        isExecutive = false;
        dropGoldAmount = CwslGameConstants.GoldDropNormal;
        maxHealth = CwslGameConstants.MonsterMaxHealth;
    }

    private void EnsureCombatHitCollider()
    {
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule == null)
            return;

        capsule.isTrigger = true;
        capsule.direction = 1;
        capsule.center = new Vector3(0f, CwslGameConstants.MonsterHitCenterY, 0f);
        capsule.height = CwslGameConstants.MonsterHitHeight;
        if (capsule.radius < CwslGameConstants.MonsterHitMinRadius)
            capsule.radius = CwslGameConstants.MonsterHitMinRadius;

        if (isExecutive && capsule.radius < 0.72f)
            capsule.radius = 0.72f;
    }

    public void DamageFromPlayer(ulong attackerClientId, float amount)
    {
        if (!IsServer || !IsAlive || amount <= 0f)
            return;

        if (IsBoss)
        {
            if (!CwslBossHongmyeongbo.CanReceiveDamageFrom(attackerClientId))
                return;

            health.Value = Mathf.Max(0f, health.Value - amount);
            ShowDamagePopupClientRpc(transform.position + Vector3.up * 1.2f, amount, (int)CwslDamagePopupKind.Monster);
            OnDamaged?.Invoke(this, health.Value, attackerClientId);
            GetComponent<CwslBossHongmyeongbo>()?.NotifyDamagedServer(health.Value);

            if (health.Value <= 0f)
                Die(attackerClientId, dropGold: false);

            return;
        }

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
        if (dropGoldAmount > 0)
            CwslGoldDropService.SpawnDrop(goldPosition, dropGoldAmount);
        OnKilled?.Invoke(this, attackerClientId);
        PlayDeathClientRpc(transform.position, (int)MonsterType);

        if (NetworkObject != null && NetworkObject.IsSpawned)
            CwslNetworkPoolService.Instance?.Release(NetworkObject);
    }

    private void Die(ulong attackerClientId, bool dropGold = true)
    {
        health.Value = 0f;
        if (dropGold && dropGoldAmount > 0)
            CwslGoldDropService.SpawnDrop(transform.position, dropGoldAmount);
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
