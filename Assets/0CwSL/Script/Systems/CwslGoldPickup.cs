using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 월드 3D 골드 1개 — 몬스터 중심에서 퍼진 뒤, 근처 플레이어에게 회전하며 빨려 들어간다.
/// </summary>
public class CwslGoldPickup : NetworkBehaviour, ICwslPooledNetworkObject
{
    private readonly NetworkVariable<int> goldAmount = new(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> pickupKind = new(
        (int)CwslGoldPickupKind.Normal,
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

    public int GoldAmount => goldAmount.Value;
    public bool IsMagnetized => magnetTargetId.Value != 0;
    public bool IsFake => pickupKind.Value == (int)CwslGoldPickupKind.Fake;

    public void ConfigureServer(Vector3 center, Vector3 finalPosition)
    {
        pickupKind.Value = (int)CwslGoldPickupKind.Normal;
        goldAmount.Value = 1;
        claimed = false;
        magnetTargetId.Value = 0;
        dropCenter = center;
        spreadTarget = finalPosition;
        spreadStartTime = Time.time;
        claimableTime = Time.time + CwslGameConstants.GoldCoinSpreadDuration + 0.05f;
        transform.position = dropCenter;
        CwslCombatRegistry.RegisterGoldPickup(this);
    }

    public void ConfigureFakeServer(Vector3 center, Vector3 finalPosition)
    {
        pickupKind.Value = (int)CwslGoldPickupKind.Fake;
        goldAmount.Value = 0;
        claimed = false;
        magnetTargetId.Value = 0;
        dropCenter = center;
        spreadTarget = finalPosition;
        spreadStartTime = Time.time;
        claimableTime = Time.time + CwslGameConstants.GoldCoinSpreadDuration * 0.35f;
        transform.position = dropCenter;
        CwslCombatRegistry.RegisterGoldPickup(this);
    }

    public void OnSpawnedFromPool()
    {
        claimed = false;
        magnetTargetId.Value = 0;
        pickupKind.Value = (int)CwslGoldPickupKind.Normal;
    }

    public void OnReturnedToPool()
    {
        CwslCombatRegistry.UnregisterGoldPickup(this);
        claimed = false;
        magnetTargetId.Value = 0;
        pickupKind.Value = (int)CwslGoldPickupKind.Normal;
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
            CwslGameConstants.GoldMagnetSpeed * 1.2f,
            CwslGameConstants.GoldMagnetSpeed * 3.5f,
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

        if (IsFake)
        {
            CwslArenaTrapSystem.Instance?.TriggerFakeGoldServer(player, transform.position);
            PlayFakeGoldTrapClientRpc(transform.position);
            DespawnSelf();
            return;
        }

        Collect(player);
    }

    private void Collect(NetworkObject player)
    {
        var playerGold = player.GetComponent<CwslPlayerGold>();
        if (playerGold == null)
        {
            claimed = false;
            return;
        }

        playerGold.AddGoldServer(1);
        CwslTeamGoldCollectedSystem.Instance?.RegisterCollectedServer(1);
        var karmaGain = CwslArenaZones.ApplyKarmaMultiplier(
            player.transform.position,
            CwslGameConstants.KarmaPickupAmount);
        CwslKarmaSystem.Instance?.AddKarmaServer(karmaGain);
        PlayCollectClientRpc();

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

    [ClientRpc]
    private void PlayCollectClientRpc()
    {
        CwslGoldFeedback.PlayCoinSound(transform.position);
    }

    [ClientRpc]
    private void PlayFakeGoldTrapClientRpc(Vector3 trapPosition)
    {
        CwslSkillGoldFeedback.PlayFailSound();
        CwslVfxSpawner.SpawnFakeGoldExplosion(trapPosition);
    }

    private static bool TryGetPlayerFromCollider(Collider other, out NetworkObject player)
    {
        player = other.GetComponentInParent<NetworkObject>();
        if (player == null || !player.IsSpawned)
            return false;

        var health = player.GetComponent<CwslPlayerHealth>();
        if (health != null && !health.IsAlive)
            return false;

        return player.GetComponent<CwslPlayerGold>() != null;
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
