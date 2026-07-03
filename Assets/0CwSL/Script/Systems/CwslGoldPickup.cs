using Unity.Netcode;
using UnityEngine;

public class CwslGoldPickup : NetworkBehaviour, ICwslPooledNetworkObject
{
    private readonly NetworkVariable<int> goldAmount = new(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<ulong> magnetTargetId = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private bool magnetFlyActive;
    private Transform coinVisual;

    public int GoldAmount => goldAmount.Value;

    public void ConfigureServer(int amount)
    {
        goldAmount.Value = Mathf.Clamp(amount, CwslGameConstants.GoldDropMin, CwslGameConstants.GoldDropMax);
        magnetFlyActive = false;
        magnetTargetId.Value = 0;
        ApplyVisualScale();
        if (coinVisual != null)
            coinVisual.gameObject.SetActive(true);

        // 생성 즉시 가장 가까운 플레이어에게 붙기 시작
        TryCollectNearestPlayer();
    }

    public void OnSpawnedFromPool()
    {
        magnetFlyActive = false;
        magnetTargetId.Value = 0;
        CacheVisual();
        if (coinVisual != null)
            coinVisual.gameObject.SetActive(true);
    }

    public void OnReturnedToPool()
    {
        magnetFlyActive = false;
        magnetTargetId.Value = 0;
        CwslGoldFlyToPlayer.EndMagnetFly(NetworkObjectId);
    }

    public override void OnNetworkSpawn()
    {
        goldAmount.OnValueChanged += HandleAmountChanged;
        magnetTargetId.OnValueChanged += HandleMagnetTargetChanged;
        CacheVisual();
        ApplyVisualScale();

        if (magnetTargetId.Value != 0)
            BeginMagnetVisual(magnetTargetId.Value);
    }

    public override void OnNetworkDespawn()
    {
        goldAmount.OnValueChanged -= HandleAmountChanged;
        magnetTargetId.OnValueChanged -= HandleMagnetTargetChanged;
        CwslGoldFlyToPlayer.EndMagnetFly(NetworkObjectId);
    }

    private void Update()
    {
        if (!IsServer)
            return;

        TryCollectNearestPlayer();
    }

    private void TryCollectNearestPlayer()
    {
        if (!CwslTargetQuery.TryGetNearestLivingPlayer(transform.position, out var player, out var distance))
        {
            StopMagnetFlyIfNeeded();
            return;
        }

        var pickupPoint = player.transform.position + Vector3.up * 0.35f;

        // 맵 어디서든 가장 가까운 플레이어에게 항상 자석
        if (!magnetFlyActive || magnetTargetId.Value != player.NetworkObjectId)
        {
            magnetFlyActive = true;
            magnetTargetId.Value = player.NetworkObjectId;
        }

        if (distance <= CwslGameConstants.GoldPickupRadius)
        {
            Collect(player);
            return;
        }

        var speed = Mathf.Lerp(
            CwslGameConstants.GoldMagnetSpeed,
            CwslGameConstants.GoldMagnetSpeed * 3.2f,
            Mathf.Clamp01(distance / 12f));

        var toTarget = pickupPoint - transform.position;
        var step = toTarget.normalized * (speed * Time.deltaTime);
        if (step.sqrMagnitude > toTarget.sqrMagnitude)
            transform.position = pickupPoint;
        else
            transform.position += step;

        if (Vector3.Distance(transform.position, pickupPoint) <= CwslGameConstants.GoldPickupRadius)
            Collect(player);
    }

    private void Collect(NetworkObject player)
    {
        var playerGold = player.GetComponent<CwslPlayerGold>();
        if (playerGold == null)
            return;

        var amount = goldAmount.Value;
        var collectPosition = transform.position;
        playerGold.AddGoldServer(amount);
        CwslKarmaSystem.Instance?.AddKarmaServer(amount);

        magnetFlyActive = false;
        magnetTargetId.Value = 0;
        PlayCollectFlyClientRpc(player.NetworkObjectId, collectPosition, amount);

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            if (CwslNetworkPoolService.Instance != null)
                CwslNetworkPoolService.Instance.Release(NetworkObject);
            else
                NetworkObject.Despawn(true);
        }
    }

    private void StopMagnetFlyIfNeeded()
    {
        if (!magnetFlyActive && magnetTargetId.Value == 0)
            return;

        magnetFlyActive = false;
        magnetTargetId.Value = 0;
        CwslGoldFlyToPlayer.EndMagnetFly(NetworkObjectId);
    }

    private void HandleMagnetTargetChanged(ulong previous, ulong current)
    {
        CwslGoldFlyToPlayer.EndMagnetFly(NetworkObjectId);
        if (current == 0)
            return;

        BeginMagnetVisual(current);
    }

    private void BeginMagnetVisual(ulong playerNetworkObjectId)
    {
        if (!TryResolvePlayer(playerNetworkObjectId, out var playerTransform))
            return;

        HideCoinVisual();
        CwslGoldFlyToPlayer.BeginMagnetFly(
            NetworkObjectId,
            transform.position,
            playerTransform,
            goldAmount.Value);
    }

    [ClientRpc]
    private void PlayCollectFlyClientRpc(ulong playerNetworkObjectId, Vector3 position, int amount)
    {
        CwslGoldFlyToPlayer.EndMagnetFly(NetworkObjectId);

        if (!TryResolvePlayer(playerNetworkObjectId, out var playerTransform))
            return;

        var start = ResolveVisibleFlyStart(position, playerTransform.position);
        CwslGoldFlyToPlayer.Play(start, playerTransform, amount);
    }

    private static Vector3 ResolveVisibleFlyStart(Vector3 collectPosition, Vector3 playerPosition)
    {
        var flat = collectPosition - playerPosition;
        flat.y = 0f;

        if (flat.sqrMagnitude >= 1.5f * 1.5f)
            return collectPosition + Vector3.up * 0.6f;

        var dir = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.forward;
        var side = Vector3.Cross(Vector3.up, dir);
        if (side.sqrMagnitude < 0.01f)
            side = Vector3.right;
        side.Normalize();

        return playerPosition
               + dir * 1.8f
               + side * Random.Range(-0.6f, 0.6f)
               + Vector3.up * Random.Range(1.0f, 1.6f);
    }

    private static bool TryResolvePlayer(ulong playerNetworkObjectId, out Transform playerTransform)
    {
        playerTransform = null;
        if (NetworkManager.Singleton == null)
            return false;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                playerNetworkObjectId,
                out var playerObject))
            return false;

        playerTransform = playerObject.transform;
        return true;
    }

    private void HandleAmountChanged(int previous, int current)
    {
        ApplyVisualScale();
    }

    private void CacheVisual()
    {
        coinVisual = transform.childCount > 0 ? transform.GetChild(0) : transform;
    }

    private void HideCoinVisual()
    {
        CacheVisual();
        if (coinVisual != null)
            coinVisual.gameObject.SetActive(false);
    }

    private void ApplyVisualScale()
    {
        CacheVisual();
        if (coinVisual == null)
            return;

        var t = Mathf.InverseLerp(CwslGameConstants.GoldDropMin, CwslGameConstants.GoldDropMax, goldAmount.Value);
        var scale = Mathf.Lerp(0.4f, 0.75f, t);
        coinVisual.localScale = Vector3.one * scale;
    }
}
