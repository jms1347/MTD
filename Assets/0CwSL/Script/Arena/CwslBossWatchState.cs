using Unity.Netcode;
using UnityEngine;

/// <summary>홍명보 「지켜보고 있다» — 감시 대상은 스킬 사용 불가.</summary>
public class CwslBossWatchState : NetworkBehaviour
{
    public static CwslBossWatchState Instance { get; private set; }

    private readonly NetworkVariable<ulong> watchedClientId = new(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> watchEndTime = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public ulong WatchedClientId => watchedClientId.Value;
    public float WatchEndTime => watchEndTime.Value;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static bool IsWatching(ulong clientId)
    {
        if (Instance == null || clientId == ulong.MaxValue)
            return false;

        if (Instance.watchedClientId.Value != clientId)
            return false;

        return Time.time < Instance.watchEndTime.Value;
    }

    public static bool BlocksSkills(ulong clientId)
    {
        return IsWatching(clientId);
    }

    public bool TryStartWatchServer()
    {
        if (!IsServer)
            return false;

        if (IsWatching(watchedClientId.Value))
            return false;

        if (!TryPickRandomLivingClient(out var clientId))
            return false;

        watchedClientId.Value = clientId;
        watchEndTime.Value = Time.time + CwslGameConstants.BossWatchDuration;
        NotifyWatchStartedClientRpc(clientId, CwslGameConstants.BossWatchDuration);
        return true;
    }

    public void ClearWatchServer()
    {
        if (!IsServer)
            return;

        if (watchedClientId.Value == ulong.MaxValue)
            return;

        watchedClientId.Value = ulong.MaxValue;
        watchEndTime.Value = 0f;
        NotifyWatchEndedClientRpc();
    }

    private void Update()
    {
        if (!IsServer || watchedClientId.Value == ulong.MaxValue)
            return;

        if (Time.time >= watchEndTime.Value)
            ClearWatchServer();
    }

    private static bool TryPickRandomLivingClient(out ulong clientId)
    {
        clientId = ulong.MaxValue;
        if (NetworkManager.Singleton == null)
            return false;

        var candidates = new System.Collections.Generic.List<ulong>();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health != null && !health.IsAlive)
                continue;

            candidates.Add(client.ClientId);
        }

        if (candidates.Count == 0)
            return false;

        clientId = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    [ClientRpc]
    private void NotifyWatchStartedClientRpc(ulong clientId, float duration)
    {
        CwslBossWatchHud.NotifyWatchStarted(clientId, duration);
    }

    [ClientRpc]
    private void NotifyWatchEndedClientRpc()
    {
        CwslBossWatchHud.NotifyWatchEnded();
    }
}
