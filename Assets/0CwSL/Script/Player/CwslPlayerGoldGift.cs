using Unity.Netcode;
using UnityEngine;

public class CwslPlayerGoldGift : NetworkBehaviour
{
    private CwslPlayerGold playerGold;
    private CwslPlayerSelection selection;
    private CwslPlayerHealth playerHealth;

    private float holdTime;
    private float giftTimer;
    private bool holding;

    public override void OnNetworkSpawn()
    {
        playerGold = GetComponent<CwslPlayerGold>();
        selection = GetComponent<CwslPlayerSelection>();
        playerHealth = GetComponent<CwslPlayerHealth>();
    }

    public void BeginHold()
    {
        if (!IsOwner || playerHealth != null && !playerHealth.IsAlive)
            return;

        holding = true;
        holdTime = 0f;
        giftTimer = 0f;
        TryGiftServerRpc();
    }

    public void TickHold()
    {
        if (!IsOwner || !holding)
            return;

        if (playerHealth != null && !playerHealth.IsAlive)
        {
            holding = false;
            return;
        }

        holdTime += Time.deltaTime;
        giftTimer += Time.deltaTime;

        var interval = Mathf.Lerp(
            CwslGameConstants.GiftGoldStartInterval,
            CwslGameConstants.GiftGoldMinIntervalSeconds,
            Mathf.Clamp01(holdTime / CwslGameConstants.GiftGoldAccelDuration));

        while (giftTimer >= interval)
        {
            giftTimer -= interval;
            TryGiftServerRpc();
        }
    }

    public void EndHold()
    {
        holding = false;
    }

    [ServerRpc]
    private void TryGiftServerRpc()
    {
        if (!IsServer || playerGold == null || selection == null)
            return;

        if (!selection.TryGetSelectedTarget(out var targetObject) || targetObject == null)
            return;

        if (targetObject.OwnerClientId == OwnerClientId)
            return;

        var amount = CwslGameConstants.GiftGoldMinInterval;
        var targetHealth = targetObject.GetComponent<CwslPlayerHealth>();
        var targetGrave = targetObject.GetComponent<CwslPlayerGrave>();

        if (targetHealth != null && targetHealth.IsDead && targetGrave != null)
        {
            if (!playerGold.TrySpendGoldServer(amount))
                return;

            targetGrave.TryReceiveRevivePaymentServer(amount);
            return;
        }

        var recipient = targetObject.GetComponent<CwslPlayerGold>();
        if (recipient == null || recipient == playerGold)
            return;

        playerGold.TryTransferGoldServer(recipient, amount);
    }
}
