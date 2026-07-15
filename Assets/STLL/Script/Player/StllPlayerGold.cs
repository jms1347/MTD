using Unity.Netcode;

public class StllPlayerGold : NetworkBehaviour
{
    private readonly NetworkVariable<int> personalGold = new(
        StllEaConstants.StartingPersonalGold,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public int PersonalGold => personalGold.Value;

    public void AddGoldServer(int amount)
    {
        if (!IsServer || amount <= 0)
            return;

        personalGold.Value += amount;
    }

    public bool TrySpendGoldServer(int amount)
    {
        if (!IsServer || amount <= 0 || personalGold.Value < amount)
            return false;

        personalGold.Value -= amount;
        return true;
    }
}
