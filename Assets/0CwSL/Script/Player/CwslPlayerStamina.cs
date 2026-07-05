using System;
using Unity.Netcode;
using UnityEngine;

public class CwslPlayerStamina : NetworkBehaviour
{
    private readonly NetworkVariable<float> stamina = new(
        CwslGameConstants.PlayerMaxStamina,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public float Current => stamina.Value;
    public float Max => CwslGameConstants.PlayerMaxStamina;
    public float Normalized => Max > 0f ? Mathf.Clamp01(Current / Max) : 0f;

    public event Action<float, float> OnStaminaChanged;

    public override void OnNetworkSpawn()
    {
        stamina.OnValueChanged += HandleStaminaChanged;

        if (IsServer)
            stamina.Value = CwslGameConstants.PlayerMaxStamina;

        NotifyStaminaChanged(stamina.Value, Max);
    }

    public override void OnNetworkDespawn()
    {
        stamina.OnValueChanged -= HandleStaminaChanged;
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned)
            return;

        var health = GetComponent<CwslPlayerHealth>();
        if (health != null && !health.IsAlive)
            return;

        if (stamina.Value >= Max)
            return;

        stamina.Value = Mathf.Min(
            Max,
            stamina.Value + CwslGameConstants.PlayerStaminaRegenPerSecond * Time.deltaTime);
    }

    public bool TrySpendServer(float amount)
    {
        if (!IsServer || amount <= 0f)
            return true;

        if (!CwslGameConstants.SkillsUseStamina)
            return true;

        if (stamina.Value + 0.001f < amount)
            return false;

        stamina.Value -= amount;
        return true;
    }

    public void RestoreFullServer()
    {
        if (!IsServer)
            return;

        stamina.Value = Max;
    }

    private void HandleStaminaChanged(float previous, float current)
    {
        NotifyStaminaChanged(current, Max);
    }

    private void NotifyStaminaChanged(float current, float max)
    {
        OnStaminaChanged?.Invoke(current, max);
    }
}
