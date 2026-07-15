using Unity.Netcode;

/// <summary>팀 골드 — 연합 깃발 등.</summary>
public class StllTeamGold : NetworkBehaviour
{
    public static StllTeamGold Instance { get; private set; }

    private readonly NetworkVariable<int> teamGold = new(
        StllEaConstants.StartingTeamGold,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> teamBannerPurchased = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public int Gold => teamGold.Value;
    public bool HasTeamBanner => teamBannerPurchased.Value;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddGoldServer(int amount)
    {
        if (!IsServer || amount <= 0)
            return;

        teamGold.Value += amount;
    }

    public bool TryBuyTeamBannerServer()
    {
        if (!IsServer || teamBannerPurchased.Value || teamGold.Value < StllEaConstants.TeamBannerCost)
            return false;

        teamGold.Value -= StllEaConstants.TeamBannerCost;
        teamBannerPurchased.Value = true;
        return true;
    }

    public float GetTeamAttackBonus()
    {
        return HasTeamBanner ? 0.08f : 0f;
    }
}
