using Unity.Netcode;
using UnityEngine;

public class StllPlayerStamina : NetworkBehaviour
{
    private readonly NetworkVariable<float> stamina = new(
        StllGlaiveConstants.MaxStamina,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public float Current => stamina.Value;
    public float Max => StllGlaiveConstants.MaxStamina;

    private void Update()
    {
        if (!IsServer)
            return;

        if (stamina.Value >= StllGlaiveConstants.MaxStamina)
            return;

        stamina.Value = Mathf.Min(
            StllGlaiveConstants.MaxStamina,
            stamina.Value + StllGlaiveConstants.StaminaRegenPerSecond * Time.deltaTime);
    }

    public bool TrySpendServer(float amount)
    {
        if (!IsServer || amount <= 0f)
            return false;

        if (stamina.Value + 0.001f < amount)
            return false;

        stamina.Value -= amount;
        return true;
    }
}
