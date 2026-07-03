using System;
using Unity.Netcode;
using UnityEngine;

public class CwslPlayerGold : NetworkBehaviour
{
    private readonly NetworkVariable<int> gold = new(
        CwslGameConstants.StartingGold,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public int Gold => gold.Value;
    public event Action<int> OnGoldChanged;

    public override void OnNetworkSpawn()
    {
        gold.OnValueChanged += HandleGoldChanged;
        if (IsServer)
            gold.Value = CwslGameConstants.StartingGold;
        HandleGoldChanged(0, gold.Value);
    }

    public override void OnNetworkDespawn()
    {
        gold.OnValueChanged -= HandleGoldChanged;
    }

    public bool TrySpendGoldServer(int amount, bool playSpendEffect = true)
    {
        if (!IsServer || amount <= 0 || gold.Value < amount)
            return false;

        gold.Value -= amount;
        if (playSpendEffect)
            PlaySpendEffectClientRpc(amount);
        return true;
    }

    public void AddGoldServer(int amount)
    {
        if (!IsServer || amount <= 0)
            return;

        gold.Value += amount;
    }

    public void SetGoldServer(int amount)
    {
        if (!IsServer)
            return;

        gold.Value = Mathf.Max(0, amount);
    }

    public bool TryTransferGoldServer(CwslPlayerGold recipient, int amount)
    {
        if (!IsServer || recipient == null || amount <= 0 || gold.Value < amount)
            return false;

        gold.Value -= amount;
        recipient.AddGoldServer(amount);
        PlaySpendEffectClientRpc(amount);
        return true;
    }

    [ClientRpc]
    private void PlaySpendEffectClientRpc(int amount)
    {
        CwslBlockCoinPop.SpawnSpend(transform.position, amount);
    }

    private void HandleGoldChanged(int previous, int current)
    {
        OnGoldChanged?.Invoke(current);
    }
}
