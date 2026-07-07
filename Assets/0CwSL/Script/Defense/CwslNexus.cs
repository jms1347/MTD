using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>맵 중앙 넥서스 — 파괴되면 패배.</summary>
public class CwslNexus : NetworkBehaviour
{
    public static CwslNexus Instance { get; private set; }

    private readonly NetworkVariable<float> health = new(
        CwslGameConstants.NexusDefaultHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> syncedMaxHealth = new(
        CwslGameConstants.NexusDefaultHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private CwslWorldHealthBar worldBar;

    public float MaxHealth => syncedMaxHealth.Value;
    public float CurrentHealth => health.Value;
    public bool IsAlive => health.Value > 0f;

    public static event Action<float, float> OnHealthChanged;
    public static event Action OnDestroyed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CwslNexusVisual.ApplyTo(transform);
    }

    public override void OnNetworkSpawn()
    {
        CwslNexusVisual.ApplyTo(transform);
        EnsureHealthBar();
        health.OnValueChanged += HandleHealthChanged;
        syncedMaxHealth.OnValueChanged += HandleMaxHealthChanged;
        NotifyHealthChanged(health.Value, MaxHealth);
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= HandleHealthChanged;
        syncedMaxHealth.OnValueChanged -= HandleMaxHealthChanged;
        if (Instance == this)
            Instance = null;
    }

    public void ConfigureServer(float maxHealthValue)
    {
        if (!IsServer)
            return;

        var max = Mathf.Max(1f, maxHealthValue);
        syncedMaxHealth.Value = max;
        health.Value = max;
        RefreshHealthBar();
        NotifyHealthChanged(health.Value, MaxHealth);
    }

    public void DamageServer(float amount)
    {
        if (!IsServer || !IsAlive || amount <= 0f)
            return;

        if (CwslGameConstants.UseDefenseMode &&
            (CwslDefenseModeController.Instance == null || !CwslDefenseModeController.Instance.IsDefenseActive))
            return;

        health.Value = Mathf.Max(0f, health.Value - amount);
        CwslDamageFeedback.PlayFromServer(GetDamagePopupAnchor(), amount, CwslDamagePopupKind.Structure);
        if (health.Value <= 0f)
            OnDestroyed?.Invoke();
    }

    public float GetOuterRadius()
    {
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
            return capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

        return CwslNexusVisualBuilder.HitRadius;
    }

    public Vector3 GetMeleeApproachPoint(Vector3 attackerWorldPosition, float attackerRadius)
    {
        var flat = attackerWorldPosition - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            flat = Vector3.forward;

        var standRadius = GetOuterRadius() + attackerRadius + 0.5f;
        var point = transform.position + flat.normalized * standRadius;
        point.y = attackerWorldPosition.y;
        return CwslArenaUtility.ClampToPlayArea(point, attackerRadius);
    }

    public Vector3 GetAimPoint()
    {
        return transform.position + Vector3.up * CwslNexusVisualBuilder.AimPointHeight;
    }

    public Vector3 GetDamagePopupAnchor()
    {
        return GetAimPoint() + Vector3.up * 0.35f;
    }

    private void HandleHealthChanged(float previous, float current)
    {
        RefreshHealthBar();
        NotifyHealthChanged(current, MaxHealth);
    }

    private void HandleMaxHealthChanged(float previous, float current) => RefreshHealthBar();

    private void EnsureHealthBar()
    {
        if (worldBar != null)
            return;

        worldBar = gameObject.AddComponent<CwslWorldHealthBar>();
        worldBar.Configure(
            3.2f,
            0.16f,
            8.2f,
            new Color(1f, 0.84f, 0.22f),
            new Color(0.1f, 0.1f, 0.12f, 0.92f));
    }

    private void RefreshHealthBar()
    {
        if (worldBar == null)
            return;

        var ratio = MaxHealth > 0f ? health.Value / MaxHealth : 0f;
        worldBar.Refresh(ratio);
        worldBar.SetVisible(IsAlive);
    }

    private static void NotifyHealthChanged(float current, float max)
    {
        OnHealthChanged?.Invoke(current, max);
    }
}
