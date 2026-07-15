using Unity.Netcode;
using UnityEngine;

/// <summary>군영 허브 상점 — 무기·말·소모품·팀 아이템.</summary>
public class StllHubShopController : NetworkBehaviour
{
    [ServerRpc(RequireOwnership = false)]
    public void BuyWeaponUpgradeServerRpc(ServerRpcParams rpcParams = default)
    {
        TryBuyForPlayer(rpcParams.Receive.SenderClientId, player =>
        {
            var gold = player.GetComponent<StllPlayerGold>();
            var loadout = player.GetComponent<StllPlayerLoadout>();
            return loadout != null && loadout.TryUpgradeWeaponServer(gold);
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void BuyFastHorseServerRpc(ServerRpcParams rpcParams = default)
    {
        TryBuyForPlayer(rpcParams.Receive.SenderClientId, player =>
        {
            var gold = player.GetComponent<StllPlayerGold>();
            var loadout = player.GetComponent<StllPlayerLoadout>();
            return loadout != null && loadout.TryBuyHorseServer(StllHorseUpgrade.Fast, gold);
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void BuyHeavyHorseServerRpc(ServerRpcParams rpcParams = default)
    {
        TryBuyForPlayer(rpcParams.Receive.SenderClientId, player =>
        {
            var gold = player.GetComponent<StllPlayerGold>();
            var loadout = player.GetComponent<StllPlayerLoadout>();
            return loadout != null && loadout.TryBuyHorseServer(StllHorseUpgrade.Heavy, gold);
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void BuyHealPotionServerRpc(ServerRpcParams rpcParams = default)
    {
        TryBuyForPlayer(rpcParams.Receive.SenderClientId, player =>
        {
            var gold = player.GetComponent<StllPlayerGold>();
            if (gold == null || !gold.TrySpendGoldServer(StllEaConstants.HealPotionCost))
                return false;

            player.GetComponent<StllPlayerHealth>()?.HealPercentServer(0.35f);
            return true;
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void BuyTeamBannerServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        StllTeamGold.Instance?.TryBuyTeamBannerServer();
    }

    private void TryBuyForPlayer(ulong clientId, System.Func<NetworkObject, bool> buyAction)
    {
        if (!IsServer)
            return;

        if (StllRunController.Instance == null || StllRunController.Instance.Phase != StllRunPhase.Hub)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            return;

        if (client.PlayerObject == null)
            return;

        buyAction(client.PlayerObject);
    }
}
