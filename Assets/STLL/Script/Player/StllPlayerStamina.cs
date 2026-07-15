using Unity.Netcode;
using UnityEngine;

public class StllPlayerStamina : NetworkBehaviour
{
    private readonly NetworkVariable<float> stamina = new(
        StllGlaiveConstants.MaxStamina,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private StllPlayerCardInventory cardInventory;

    public float Current => stamina.Value;
    public float Max => StllGlaiveConstants.MaxStamina;

    private void Awake()
    {
        cardInventory = GetComponent<StllPlayerCardInventory>();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (stamina.Value >= StllGlaiveConstants.MaxStamina)
            return;

        var regen = StllGlaiveConstants.StaminaRegenPerSecond;
        if (cardInventory != null)
            regen *= 1f + cardInventory.GetPassiveBonus(StllPassiveBonusType.StaminaRegen);

        stamina.Value = Mathf.Min(StllGlaiveConstants.MaxStamina, stamina.Value + regen * Time.deltaTime);
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
