using Unity.Netcode;
using UnityEngine;

public static class StllGoldDropper
{
    public static void ServerAwardKillGold(ulong attackerClientId, GameObject enemy)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        var ai = enemy.GetComponent<StllEnemyGruntAI>();
        var amount = StllEaConstants.EnemyGoldDropGrunt;
        if (ai != null)
        {
            amount = ai.Kind switch
            {
                StllEnemyKind.Archer => StllEaConstants.EnemyGoldDropArcher,
                StllEnemyKind.Charger => StllEaConstants.EnemyGoldDropCharger,
                StllEnemyKind.EliteGuard => StllEaConstants.EnemyGoldDropElite,
                StllEnemyKind.Arsonist => StllEaConstants.EnemyGoldDropGrunt + 1,
                _ => StllEaConstants.EnemyGoldDropGrunt
            };
        }

        if (enemy.GetComponent<StllBossLuBu>() != null)
            amount = StllEaConstants.EnemyGoldDropBoss;

        if (enemy.GetComponent<StllMiniBossHuangYing>() != null)
            amount = StllEaConstants.EnemyGoldDropElite * 2;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerClientId, out var client)
            && client.PlayerObject != null)
        {
            client.PlayerObject.GetComponent<StllPlayerGold>()?.AddGoldServer(amount);
        }

        StllTeamGold.Instance?.AddGoldServer(Mathf.Max(1, amount / 2));
    }
}
