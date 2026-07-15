using Unity.Netcode;
using UnityEngine;

public class StllPlayerHealth : NetworkBehaviour
{
    private readonly NetworkVariable<float> health = new(
        StllEaConstants.PlayerBaseHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> maxHealth = new(
        StllEaConstants.PlayerBaseHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private StllBrotherhoodRoleState roleState;
    private StllPlayerCardInventory cardInventory;

    public float CurrentHealth => health.Value;
    public float MaxHealth => maxHealth.Value;
    public bool IsAlive => health.Value > 0f;

    private void Awake()
    {
        roleState = GetComponent<StllBrotherhoodRoleState>();
        cardInventory = GetComponent<StllPlayerCardInventory>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        RecalculateMaxHealthServer();
        health.Value = maxHealth.Value;
    }

    public void RecalculateMaxHealthServer()
    {
        if (!IsServer)
            return;

        var baseHp = StllEaConstants.PlayerBaseHealth;
        if (roleState != null && roleState.Role == StllBrotherhoodRole.ZhangFei)
            baseHp *= 1.35f;

        if (cardInventory != null)
            baseHp *= 1f + cardInventory.GetPassiveBonus(StllPassiveBonusType.MaxHealth);

        maxHealth.Value = baseHp;
        health.Value = Mathf.Min(health.Value, maxHealth.Value);
    }

    public void DamageServer(float amount)
    {
        if (!IsServer || !IsAlive)
            return;

        var reduced = amount;
        if (cardInventory != null)
        {
            if (health.Value / maxHealth.Value <= 0.3f)
                reduced *= 1f - cardInventory.GetPassiveBonus(StllPassiveBonusType.LowHpDamageReduction);

            if (cardInventory.IsIronWallActive)
                reduced *= 0.3f;
        }

        if (roleState != null && roleState.Role == StllBrotherhoodRole.ZhangFei)
            reduced *= 0.85f;

        health.Value = Mathf.Max(0f, health.Value - reduced);
        if (health.Value <= 0f)
            HandleDeathServer();
    }

    public void HealServer(float amount)
    {
        if (!IsServer || !IsAlive)
            return;

        health.Value = Mathf.Min(maxHealth.Value, health.Value + amount);
    }

    public void HealPercentServer(float ratio)
    {
        HealServer(maxHealth.Value * ratio);
    }

    private void HandleDeathServer()
    {
        if (StllRunController.Instance != null)
            StllRunController.Instance.ServerNotifyPlayerDied(this);
    }
}
