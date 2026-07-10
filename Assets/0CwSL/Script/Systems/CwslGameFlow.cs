using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>전원 사망 판정 및 다시하기.</summary>
public class CwslGameFlow : NetworkBehaviour
{
    public static CwslGameFlow Instance { get; private set; }

    private readonly NetworkVariable<bool> allPlayersDefeated = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public bool AllPlayersDefeated => allPlayersDefeated.Value;

    public static event System.Action<bool> OnAllPlayersDefeatedChanged;

    private readonly NetworkVariable<bool> defenseEnded = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> defenseVictory = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public bool DefenseEnded => defenseEnded.Value;
    public bool DefenseVictory => defenseVictory.Value;

    private bool restartInProgress;

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
        allPlayersDefeated.OnValueChanged += HandleDefeatStateChanged;
        defenseEnded.OnValueChanged += HandleDefenseEndedChanged;
        NotifyDefeatStateChanged(allPlayersDefeated.Value);
        if (defenseEnded.Value)
            NotifyDefenseResultChanged(defenseVictory.Value);
    }

    public override void OnNetworkDespawn()
    {
        allPlayersDefeated.OnValueChanged -= HandleDefeatStateChanged;
        defenseEnded.OnValueChanged -= HandleDefenseEndedChanged;

        if (Instance == this)
            Instance = null;
    }

    public void NotifyDefenseEndedServer(bool victory)
    {
        if (!IsServer || restartInProgress)
            return;

        defenseVictory.Value = victory;
        defenseEnded.Value = true;

        var spawner = CwslGameSession.Instance?.MonsterSpawner;
        if (spawner != null)
            spawner.SpawningEnabled = false;

        NotifyDefenseResultChanged(victory);
    }

    public void NotifyPlayerStateChangedServer()
    {
        if (!IsServer || restartInProgress)
            return;

        var defeated = AreAllPlayersDead();
        if (allPlayersDefeated.Value == defeated)
            return;

        allPlayersDefeated.Value = defeated;
        if (defeated)
            OnAllPlayersDefeatedServer();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestRestartServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer || restartInProgress)
            return;

        if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
            return;

        restartInProgress = true;
        ReloadGameSceneClientRpc();
        StartCoroutine(RestartAfterBroadcast());
    }

    private IEnumerator RestartAfterBroadcast()
    {
        yield return null;
        yield return ReloadGameSceneLocal();
        restartInProgress = false;
    }

    [ClientRpc]
    private void ReloadGameSceneClientRpc()
    {
        if (IsServer)
            return;

        StartCoroutine(ReloadGameSceneLocal());
    }

    private static IEnumerator ReloadGameSceneLocal()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager != null && networkManager.IsListening)
            networkManager.Shutdown();

        yield return null;
        SceneManager.LoadScene(CwslGameConstants.GameSceneName);
    }

    public static void RestartFallback()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager != null && networkManager.IsListening)
            networkManager.Shutdown();

        SceneManager.LoadScene(CwslGameConstants.GameSceneName);
    }

    private void OnAllPlayersDefeatedServer()
    {
        var spawner = CwslGameSession.Instance?.MonsterSpawner;
        if (spawner != null)
            spawner.SpawningEnabled = false;
    }

    private static bool AreAllPlayersDead()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening)
            return false;

        var hasPlayer = false;
        foreach (var clientId in networkManager.ConnectedClientsIds)
        {
            if (!networkManager.ConnectedClients.TryGetValue(clientId, out var client))
                continue;

            var playerObject = client.PlayerObject;
            if (playerObject == null || !playerObject.IsSpawned)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health == null)
                continue;

            hasPlayer = true;
            if (health.IsAlive)
                return false;
        }

        return hasPlayer;
    }

    private static void NotifyDefenseResultChanged(bool victory)
    {
        CwslGameOverHud.SetDefenseResult(victory);
    }

    private void HandleDefenseEndedChanged(bool previous, bool current)
    {
        if (!current)
            return;

        NotifyDefenseResultChanged(defenseVictory.Value);
    }

    private void HandleDefeatStateChanged(bool previous, bool current)
    {
        NotifyDefeatStateChanged(current);
    }

    private static void NotifyDefeatStateChanged(bool defeated)
    {
        OnAllPlayersDefeatedChanged?.Invoke(defeated);
        CwslGameOverHud.SetVisible(defeated);
    }
}
