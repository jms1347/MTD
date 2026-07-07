using Unity.Netcode;
using UnityEngine;

public class CwslPillPickup : NetworkBehaviour, ICwslPooledNetworkObject
{
    private readonly NetworkVariable<int> pillTypeValue = new(
        (int)CwslPillType.Blue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<ulong> magnetTargetId = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private bool claimed;
    private Vector3 dropCenter;
    private Vector3 spreadTarget;
    private float spreadStartTime;
    private float claimableTime;

    public CwslPillType PillType => (CwslPillType)pillTypeValue.Value;

    public void ConfigureServer(Vector3 center, Vector3 finalPosition, CwslPillType pillType)
    {
        pillTypeValue.Value = (int)pillType;
        claimed = false;
        magnetTargetId.Value = 0;
        dropCenter = center;
        spreadTarget = finalPosition;
        spreadStartTime = Time.time;
        claimableTime = Time.time + CwslGameConstants.GoldCoinSpreadDuration + 0.05f;
        transform.position = dropCenter;
        CwslCombatRegistry.RegisterPillPickup(this);
    }

    public void OnSpawnedFromPool()
    {
        claimed = false;
        magnetTargetId.Value = 0;
        pillTypeValue.Value = (int)CwslPillType.Blue;
    }

    public void OnReturnedToPool()
    {
        CwslCombatRegistry.UnregisterPillPickup(this);
        claimed = false;
        magnetTargetId.Value = 0;
        pillTypeValue.Value = (int)CwslPillType.Blue;
    }

    private void Update()
    {
        if (!IsServer || claimed)
            return;

        UpdateSpreadPosition();

        if (Time.time < claimableTime)
            return;

        UpdateMagnetAndCollect();
    }

    private void UpdateSpreadPosition()
    {
        var duration = CwslGameConstants.GoldCoinSpreadDuration;
        var elapsed = Time.time - spreadStartTime;
        if (elapsed >= duration)
            return;

        var t = Mathf.Clamp01(elapsed / duration);
        var eased = 1f - (1f - t) * (1f - t);
        transform.position = Vector3.Lerp(dropCenter, spreadTarget, eased);
    }

    private void UpdateMagnetAndCollect()
    {
        if (magnetTargetId.Value == 0)
        {
            if (!CwslTargetQuery.TryGetClosestPlayerInRadius(
                    transform.position,
                    CwslGameConstants.GoldMagnetRadius,
                    out var nearest))
                return;

            magnetTargetId.Value = nearest.NetworkObjectId;
            return;
        }

        if (!TryResolvePlayer(magnetTargetId.Value, out var player))
        {
            magnetTargetId.Value = 0;
            return;
        }

        var health = player.GetComponent<CwslPlayerHealth>();
        if (health != null && !health.IsAlive)
        {
            magnetTargetId.Value = 0;
            return;
        }

        var pickupPoint = player.transform.position + Vector3.up * 0.55f;
        var toTarget = pickupPoint - transform.position;
        var distance = toTarget.magnitude;

        if (distance <= CwslGameConstants.GoldCoinClaimRadius)
        {
            TryClaim(player);
            return;
        }

        if (distance > CwslGameConstants.GoldMagnetRadius * 1.6f)
        {
            magnetTargetId.Value = 0;
            return;
        }

        var speed = Mathf.Lerp(
            CwslGameConstants.GoldMagnetSpeed * 1.1f,
            CwslGameConstants.GoldMagnetSpeed * 3.2f,
            Mathf.Clamp01(1f - distance / (CwslGameConstants.GoldMagnetRadius * 1.4f)));

        var step = toTarget.normalized * (speed * Time.deltaTime);
        transform.position = step.sqrMagnitude >= toTarget.sqrMagnitude
            ? pickupPoint
            : transform.position + step;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || claimed || Time.time < claimableTime)
            return;

        if (!TryGetPlayerFromCollider(other, out var player))
            return;

        if (magnetTargetId.Value == 0)
            magnetTargetId.Value = player.NetworkObjectId;

        if (magnetTargetId.Value == player.NetworkObjectId)
            TryClaim(player);
    }

    private void TryClaim(NetworkObject player)
    {
        if (claimed || player == null || Time.time < claimableTime)
            return;

        if (magnetTargetId.Value != 0 && magnetTargetId.Value != player.NetworkObjectId)
            return;

        var pickupPoint = player.transform.position + Vector3.up * 0.55f;
        if (Vector3.Distance(transform.position, pickupPoint) > CwslGameConstants.GoldCoinClaimRadius)
            return;

        claimed = true;
        magnetTargetId.Value = 0;

        var pillBuff = player.GetComponent<CwslPlayerPillBuff>();
        if (pillBuff == null)
        {
            claimed = false;
            return;
        }

        pillBuff.ApplyPillServer(PillType);
        DespawnSelf();
    }

    private void DespawnSelf()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            if (CwslNetworkPoolService.Instance != null)
                CwslNetworkPoolService.Instance.Release(NetworkObject);
            else
                NetworkObject.Despawn(true);
        }
    }

    private static bool TryGetPlayerFromCollider(Collider other, out NetworkObject player)
    {
        player = other.GetComponentInParent<NetworkObject>();
        if (player == null || !player.IsSpawned)
            return false;

        var health = player.GetComponent<CwslPlayerHealth>();
        if (health != null && !health.IsAlive)
            return false;

        return player.GetComponent<CwslPlayerPillBuff>() != null;
    }

    private static bool TryResolvePlayer(ulong playerNetworkObjectId, out NetworkObject player)
    {
        player = null;
        if (NetworkManager.Singleton == null)
            return false;

        return NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
            playerNetworkObjectId,
            out player);
    }
}
