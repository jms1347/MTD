using Unity.Netcode;
using UnityEngine;

/// <summary>3택1 카드 — 플레이어별 옵션.</summary>
public class StllCardPickerController : NetworkBehaviour
{
    public static StllCardPickerController Instance { get; private set; }

    private readonly System.Random rng = new(7714);

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

    public void BeginPickForAllPlayersServer(int pickIndex)
    {
        if (!IsServer)
            return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
                continue;

            var inventory = client.PlayerObject.GetComponent<StllPlayerCardInventory>();
            var role = client.PlayerObject.GetComponent<StllBrotherhoodRoleState>();
            inventory?.BeginPersonalPickServer(pickIndex, role != null ? role.Role : StllBrotherhoodRole.None, rng);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SelectCardServerRpc(byte cardId, ServerRpcParams rpcParams = default)
    {
        if (StllRunController.Instance == null || StllRunController.Instance.Phase != StllRunPhase.CardPick)
            return;

        var clientId = rpcParams.Receive.SenderClientId;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            return;

        var inventory = client.PlayerObject?.GetComponent<StllPlayerCardInventory>();
        if (inventory == null || !inventory.TrySelectPendingCardServer((StllCardId)cardId))
            return;

        StllRunController.Instance.ServerNotifyPlayerPickedCard(clientId);
    }
}
