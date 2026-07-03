using Unity.Netcode;
using UnityEngine;

public class CwslPlayerCombat : NetworkBehaviour
{
    private float nextAttackTime;
    private CwslPlayerController playerController;
    private CwslPlayerSelection selection;

    public override void OnNetworkSpawn()
    {
        playerController = GetComponent<CwslPlayerController>();
        selection = GetComponent<CwslPlayerSelection>();
    }

    public void AttackSelectedTarget()
    {
        if (!IsServer)
            return;

        if (Time.time < nextAttackTime)
            return;

        if (selection == null || !selection.TryGetSelectedTarget(out var target) || target == null)
            return;

        var monsterHealth = target.GetComponent<CwslMonsterHealth>();
        if (monsterHealth == null || !monsterHealth.IsAlive)
            return;

        var distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > CwslGameConstants.AttackRange + target.transform.localScale.x)
            return;

        nextAttackTime = Time.time + CwslGameConstants.AttackCooldown;
        PlayAttackClientRpc();
        monsterHealth.DamageFromPlayer(OwnerClientId, CwslGameConstants.AttackDamage);
    }

    [ClientRpc]
    private void PlayAttackClientRpc()
    {
        playerController?.PlayAttackPulse();
    }
}
