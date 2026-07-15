using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>참가 순서대로 도원결의 역할 배정.</summary>
public class StllRoleAssigner : NetworkBehaviour
{
    private readonly HashSet<ulong> assignedClients = new();

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        AssignExistingPlayers();
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (!IsServer)
            return;

        TryAssignClient(clientId);
    }

    private void AssignExistingPlayers()
    {
        if (NetworkManager.Singleton == null)
            return;

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            TryAssignClient(clientId);
    }

    private void TryAssignClient(ulong clientId)
    {
        if (assignedClients.Contains(clientId))
            return;

        StartCoroutine(AssignWhenReady(clientId));
    }

    private IEnumerator AssignWhenReady(ulong clientId)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if (assignedClients.Contains(clientId))
                yield break;

            if (NetworkManager.Singleton == null)
                yield break;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                yield return null;
                continue;
            }

            var player = client.PlayerObject;
            if (player == null)
            {
                yield return null;
                continue;
            }

            var roleState = player.GetComponent<StllBrotherhoodRoleState>();
            if (roleState == null)
                yield break;

            var order = assignedClients.Count;
            var role = StllBrotherhoodRoleUtil.FromJoinOrder(order);
            if (role == StllBrotherhoodRole.None)
                yield break;

            roleState.AssignRoleServer(role);
            assignedClients.Add(clientId);
            Debug.Log($"[STLL] Client {clientId} → {StllBrotherhoodRoleUtil.GetDisplayName(role)}");
            yield break;
        }
    }
}
