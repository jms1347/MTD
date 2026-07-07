using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>맵 가장자리 적 기지 — 파괴 시 해당 기지 스폰 중단.</summary>
public class CwslEnemyBase : NetworkBehaviour
{
    public const float BarHeight = 3.6f;

    private readonly NetworkVariable<float> health = new(
        CwslGameConstants.EnemyBaseDefaultHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> syncedMaxHealth = new(
        CwslGameConstants.EnemyBaseDefaultHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private CwslWorldHealthBar worldBar;
    private CapsuleCollider hitCollider;

    public Vector3 SpawnPosition => transform.position;
    public float CurrentHealth => health.Value;
    public float MaxHealth => syncedMaxHealth.Value;
    public bool IsAlive => health.Value > 0f;

    public static event Action<float, float> OnAnyHealthChanged;

    private void Awake()
    {
        CwslEnemyBaseVisualBuilder.Build(transform);
        EnsureCollider();
    }

    public override void OnNetworkSpawn()
    {
        EnsureHealthBar();
        health.OnValueChanged += HandleHealthChanged;
        syncedMaxHealth.OnValueChanged += HandleMaxHealthChanged;
        RefreshHealthBar();
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= HandleHealthChanged;
        syncedMaxHealth.OnValueChanged -= HandleMaxHealthChanged;
    }

    public void ConfigureServer(float maxHealthValue)
    {
        if (!IsServer)
            return;

        var max = Mathf.Max(1f, maxHealthValue);
        syncedMaxHealth.Value = max;
        health.Value = max;
        RefreshHealthBar();
    }

    public void DamageFromPlayer(ulong attackerClientId, float amount)
    {
        if (!IsServer)
            return;

        DamageServer(amount, attackerClientId);
    }

    public void DamageServer(float amount, ulong attackerClientId = ulong.MaxValue)
    {
        if (!IsServer || !IsAlive || amount <= 0f)
            return;

        if (CwslDefenseModeController.Instance == null || !CwslDefenseModeController.Instance.IsDefenseActive)
            return;

        health.Value = Mathf.Max(0f, health.Value - amount);
        CwslDamageFeedback.PlayFromServer(GetAimPoint(), amount, CwslDamagePopupKind.Structure);

        if (health.Value <= 0f)
            HandleDestroyedServer();
    }

    public Vector3 GetAimPoint()
    {
        return transform.position + Vector3.up * (BarHeight - 0.4f);
    }

    private void EnsureCollider()
    {
        if (hitCollider != null)
            return;

        hitCollider = GetComponent<CapsuleCollider>();
        if (hitCollider == null)
            hitCollider = gameObject.AddComponent<CapsuleCollider>();
        hitCollider.isTrigger = true;
        hitCollider.direction = 1;
        hitCollider.center = new Vector3(0f, 1.35f, 0f);
        hitCollider.height = 2.7f;
        hitCollider.radius = 1.15f;
    }

    private void EnsureHealthBar()
    {
        if (worldBar != null)
            return;

        worldBar = gameObject.AddComponent<CwslWorldHealthBar>();
        worldBar.Configure(
            2.1f,
            0.14f,
            BarHeight,
            new Color(1f, 0.45f, 0.12f),
            new Color(0.14f, 0.08f, 0.06f, 0.92f));
    }

    private void HandleHealthChanged(float previous, float current)
    {
        RefreshHealthBar();
        OnAnyHealthChanged?.Invoke(current, MaxHealth);
    }

    private void HandleMaxHealthChanged(float previous, float current) => RefreshHealthBar();

    private void RefreshHealthBar()
    {
        if (worldBar == null)
            return;

        var ratio = MaxHealth > 0f ? health.Value / MaxHealth : 0f;
        worldBar.Refresh(ratio, MaxHealth);
        worldBar.SetVisible(IsAlive);
    }

    private void HandleDestroyedServer()
    {
        CwslDefenseModeController.Instance?.NotifyEnemyBaseDestroyedServer(this);
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }
}
