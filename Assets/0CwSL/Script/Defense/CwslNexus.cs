using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>맵 중앙 넥서스 — 파괴되면 패배.</summary>
public class CwslNexus : NetworkBehaviour
{
    public static CwslNexus Instance { get; private set; }

    private readonly NetworkVariable<float> health = new(
        500f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public float MaxHealth { get; private set; } = 500f;
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
        health.OnValueChanged += HandleHealthChanged;
        NotifyHealthChanged(health.Value, MaxHealth);
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= HandleHealthChanged;
        if (Instance == this)
            Instance = null;
    }

    public void ConfigureServer(float maxHealthValue)
    {
        if (!IsServer)
            return;

        MaxHealth = Mathf.Max(1f, maxHealthValue);
        health.Value = MaxHealth;
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
        if (health.Value <= 0f)
            OnDestroyed?.Invoke();
    }

    public Vector3 GetAimPoint()
    {
        return transform.position + Vector3.up * CwslNexusVisualBuilder.AimPointHeight;
    }

    private void HandleHealthChanged(float previous, float current)
    {
        NotifyHealthChanged(current, MaxHealth);
    }

    private static void NotifyHealthChanged(float current, float max)
    {
        OnHealthChanged?.Invoke(current, max);
    }
}
