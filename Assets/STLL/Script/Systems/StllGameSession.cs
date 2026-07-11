using Unity.Netcode;
using UnityEngine;

/// <summary>
/// STLL 게임 세션 — 규칙·매치 상태는 여기서 확장합니다.
/// </summary>
public class StllGameSession : NetworkBehaviour
{
    public static StllGameSession Instance { get; private set; }

    [SerializeField] private StllGameAssets assets;

    public StllGameAssets Assets => assets;

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
        if (!IsServer)
            return;

        Debug.Log("[STLL] Game session started.");
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
            Instance = null;
    }
}
