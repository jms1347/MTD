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
    private float autoReviveHoldTime;
    private float autoReviveTimer;

    public override void OnNetworkSpawn()
    {
        playerGold = GetComponent<CwslPlayerGold>();
        selection = GetComponent<CwslPlayerSelection>();
        playerHealth = GetComponent<CwslPlayerHealth>();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        TickAutoReviveServer();
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

        var interval = ResolveGiftInterval(holdTime);

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

    private void TickAutoReviveServer()
    {
        if (playerGold == null || playerHealth == null || !playerHealth.IsAlive)
        {
            ResetAutoReviveTimers();
            return;
        }

        var targetGrave = FindNearbyRevivableGrave();
        if (targetGrave == null)
        {
            ResetAutoReviveTimers();
            return;
        }

        autoReviveHoldTime += Time.deltaTime;
        autoReviveTimer += Time.deltaTime;

        var interval = ResolveGiftInterval(autoReviveHoldTime);
        while (autoReviveTimer >= interval)
        {
            autoReviveTimer -= interval;
            var amount = CwslGameConstants.GiftGoldMinInterval;
            if (!playerGold.TrySpendGoldServer(amount))
                return;

            targetGrave.TryReceiveRevivePaymentServer(amount);
            if (!targetGrave.IsTombstoneActive)
            {
                ResetAutoReviveTimers();
                return;
            }
        }
    }

    private CwslPlayerGrave FindNearbyRevivableGrave()
    {
        var radius = CwslGameConstants.ReviveProximityRadius;
        var radiusSqr = radius * radius;
        var position = transform.position;

        CwslPlayerGrave closest = null;
        var bestSqr = float.MaxValue;

        var graves = FindObjectsByType<CwslPlayerGrave>(FindObjectsSortMode.None);
        foreach (var grave in graves)
        {
            if (grave == null || grave.NetworkObjectId == NetworkObjectId || !grave.IsTombstoneActive)
                continue;

            var health = grave.GetComponent<CwslPlayerHealth>();
            if (health == null || !health.IsDead)
                continue;

            var flat = grave.transform.position - position;
            flat.y = 0f;
            var sqr = flat.sqrMagnitude;
            if (sqr > radiusSqr || sqr >= bestSqr)
                continue;

            bestSqr = sqr;
            closest = grave;
        }

        return closest;
    }

    private static float ResolveGiftInterval(float elapsedHoldTime)
    {
        return Mathf.Lerp(
            CwslGameConstants.GiftGoldStartInterval,
            CwslGameConstants.GiftGoldMinIntervalSeconds,
            Mathf.Clamp01(elapsedHoldTime / CwslGameConstants.GiftGoldAccelDuration));
    }

    private void ResetAutoReviveTimers()
    {
        autoReviveHoldTime = 0f;
        autoReviveTimer = 0f;
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

        var targetHealth = targetObject.GetComponent<CwslPlayerHealth>();
        if (targetHealth != null && targetHealth.IsDead)
            return;

        var amount = CwslGameConstants.GiftGoldMinInterval;
        var recipient = targetObject.GetComponent<CwslPlayerGold>();
        if (recipient == null || recipient == playerGold)
            return;

        playerGold.TryTransferGoldServer(recipient, amount);
    }
}
