using Unity.Netcode;
using UnityEngine;

public class CwslKarmaSystem : NetworkBehaviour
{
    public static CwslKarmaSystem Instance { get; private set; }

    private readonly NetworkVariable<long> karma = new(
        0L,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public long Karma => karma.Value;
    public bool IsBossThresholdReached =>
        karma.Value >= CwslGameConstants.BossKarmaThreshold;

    public event System.Action<long> OnKarmaChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        karma.OnValueChanged += HandleKarmaChanged;
        HandleKarmaChanged(0L, karma.Value);
    }

    public override void OnNetworkDespawn()
    {
        karma.OnValueChanged -= HandleKarmaChanged;
        if (Instance == this)
            Instance = null;
    }

    public void AddKarmaServer(long amount)
    {
        if (!IsServer || amount <= 0L)
            return;

        karma.Value += amount;
    }

    private void HandleKarmaChanged(long previous, long current)
    {
        OnKarmaChanged?.Invoke(current);
    }
}
