using Unity.Netcode;
using UnityEngine;

/// <summary>팀이 주운 골드 누적 — 보스 등장 조건.</summary>
public class CwslTeamGoldCollectedSystem : NetworkBehaviour
{
    public static CwslTeamGoldCollectedSystem Instance { get; private set; }

    private readonly NetworkVariable<long> totalCollected = new(
        0L,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public long TotalCollected => totalCollected.Value;

    public event System.Action<long> OnTotalCollectedChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        totalCollected.OnValueChanged += HandleTotalChanged;
        HandleTotalChanged(0L, totalCollected.Value);
    }

    public override void OnNetworkDespawn()
    {
        totalCollected.OnValueChanged -= HandleTotalChanged;
        if (Instance == this)
            Instance = null;
    }

    public void RegisterCollectedServer(int amount)
    {
        if (!IsServer || amount <= 0)
            return;

        totalCollected.Value += amount;
    }

    private void HandleTotalChanged(long previous, long current)
    {
        OnTotalCollectedChanged?.Invoke(current);
    }
}
