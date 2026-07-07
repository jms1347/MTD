using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
public class CwslGameSession : NetworkBehaviour
{
    public static CwslGameSession Instance { get; private set; }

    [SerializeField] private CwslGameAssets assets;
    [SerializeField] private CwslMonsterSpawner monsterSpawner;

    public CwslGameAssets Assets => assets;
    public CwslMonsterSpawner MonsterSpawner => monsterSpawner;

    private bool bossSpawned;
    private readonly Dictionary<ulong, CwslCharacterId> assignedCharacters = new();

    public static event Action OnCharacterAssignmentsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (assets != null)
        {
            CwslGoldFeedback.Initialize(assets.goldBurstVfx, assets.goldPickupSound);
            CwslRammerAudioFeedback.Initialize(assets.horseGallopSound, assets.rammerStunSound);
            CwslGatherAudioFeedback.Initialize(
                assets.gatherChargeCastSound,
                assets.gatherChargeLoopSound,
                assets.gatherChargeEndSound,
                assets.skillGoldFailSound);
            CwslArenaAudioFeedback.Initialize(assets);
        }

        CwslDamagePopupPool.EnsureReady();
        EnsureArenaSystems();
    }

    [ClientRpc]
    public void ReportDamagePopupClientRpc(Vector3 worldAnchor, float damage, int kind)
    {
        CwslDamagePopupPool.EnsureReady();
        CwslDamagePopupPool.Play(worldAnchor, damage, (CwslDamagePopupKind)kind);
    }

    private void EnsureArenaSystems()
    {
        if (CwslGameConstants.UseDefenseMode)
        {
            if (GetComponent<CwslMonsterManager>() == null)
                gameObject.AddComponent<CwslMonsterManager>();
            if (GetComponent<CwslDefenseModeController>() == null)
                gameObject.AddComponent<CwslDefenseModeController>();
            return;
        }

        if (GetComponent<CwslBossWatchState>() == null)
            gameObject.AddComponent<CwslBossWatchState>();
        if (GetComponent<CwslArenaGimmickSystem>() == null)
            gameObject.AddComponent<CwslArenaGimmickSystem>();
        if (GetComponent<CwslArenaTrapSystem>() == null)
            gameObject.AddComponent<CwslArenaTrapSystem>();
        if (GetComponent<CwslArenaHazardPadSystem>() == null)
            gameObject.AddComponent<CwslArenaHazardPadSystem>();
        if (GetComponent<CwslArenaBuffSystem>() == null)
            gameObject.AddComponent<CwslArenaBuffSystem>();
        if (GetComponent<CwslArenaDynamicZoneSystem>() == null)
            gameObject.AddComponent<CwslArenaDynamicZoneSystem>();
        if (GetComponent<CwslTeamGoldCollectedSystem>() == null)
            gameObject.AddComponent<CwslTeamGoldCollectedSystem>();
    }

    public override void OnNetworkSpawn()
    {
        CwslDamagePopupPool.EnsureReady();

        if (IsServer && assets != null)
            CwslNetworkPoolService.Instance?.Initialize(assets);

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                EnsureCharacterAssigned(clientId);
                ApplyAssignedCharacterToPlayer(clientId);
            }
        }

        if (!IsServer)
            return;

        if (CwslGameConstants.UseDefenseMode)
            return;

        if (CwslKarmaSystem.Instance != null)
        {
            CwslKarmaSystem.Instance.OnKarmaChanged += HandleKarmaChanged;
            HandleKarmaChanged(CwslKarmaSystem.Instance.Karma);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        if (CwslKarmaSystem.Instance != null)
            CwslKarmaSystem.Instance.OnKarmaChanged -= HandleKarmaChanged;

        if (IsServer)
            assignedCharacters.Clear();

        if (Instance == this)
            Instance = null;
    }

    private void HandleClientConnected(ulong clientId)
    {
        EnsureCharacterAssigned(clientId);
        ApplyAssignedCharacterToPlayer(clientId);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        ReleaseCharacter(clientId);
    }

    private void ApplyAssignedCharacterToPlayer(ulong clientId)
    {
        if (!TryGetAssignedCharacter(clientId, out var characterId))
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            return;

        var playerObject = client.PlayerObject;
        if (playerObject == null)
            return;

        var playerCharacter = playerObject.GetComponent<CwslPlayerCharacter>();
        playerCharacter?.ApplyAssignedCharacterServer(characterId);
    }

    private static bool IsServerActive()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
    }

    public bool TryAssignRandomCharacter(ulong clientId, out CwslCharacterId characterId)
    {
        characterId = default;
        if (!IsServerActive())
            return false;

        assignedCharacters.Remove(clientId);

        var available = new List<CwslCharacterId>();
        foreach (CwslCharacterId id in Enum.GetValues(typeof(CwslCharacterId)))
        {
            if (!IsCharacterTakenByOther(id, clientId))
                available.Add(id);
        }

        if (available.Count == 0)
            return false;

        characterId = available[UnityEngine.Random.Range(0, available.Count)];
        assignedCharacters[clientId] = characterId;
        NotifyAssignmentsChanged();
        return true;
    }

    public bool TryGetAssignedCharacter(ulong clientId, out CwslCharacterId characterId)
    {
        return assignedCharacters.TryGetValue(clientId, out characterId);
    }

    public void EnsureCharacterAssigned(ulong clientId)
    {
        if (!IsServerActive())
            return;

        if (assignedCharacters.ContainsKey(clientId))
            return;

        TryAssignRandomCharacter(clientId, out _);
    }

    public bool TryAssignCharacter(ulong clientId, CwslCharacterId characterId)
    {
        if (!IsServerActive() || !Enum.IsDefined(typeof(CwslCharacterId), characterId))
            return false;

        if (IsCharacterTakenByOther(characterId, clientId))
            return false;

        assignedCharacters[clientId] = characterId;
        NotifyAssignmentsChanged();
        return true;
    }

    public void CheatAssignCharacterServer(ulong clientId, CwslCharacterId characterId)
    {
        if (!IsServerActive() || !CwslLobbyGameSettings.EnableDevCheats)
            return;

        if (!Enum.IsDefined(typeof(CwslCharacterId), characterId))
            return;

        assignedCharacters[clientId] = characterId;
        NotifyAssignmentsChanged();
        ApplyAssignedCharacterToPlayer(clientId);
    }

    public void ReleaseCharacter(ulong clientId)
    {
        if (!IsServerActive())
            return;

        if (!assignedCharacters.Remove(clientId))
            return;

        NotifyAssignmentsChanged();
    }

    private bool IsCharacterTakenByOther(CwslCharacterId characterId, ulong exceptClientId)
    {
        foreach (var pair in assignedCharacters)
        {
            if (pair.Key == exceptClientId)
                continue;

            if (pair.Value == characterId)
                return true;
        }

        return false;
    }

    private static void NotifyAssignmentsChanged()
    {
        OnCharacterAssignmentsChanged?.Invoke();
    }

    public GameObject GetMonsterPrefab(CwslMonsterType type)
    {
        if (assets == null)
            return null;

        return type switch
        {
            CwslMonsterType.Ranged or CwslMonsterType.NexusRanged => assets.rangedMonsterPrefab,
            CwslMonsterType.Suicide or CwslMonsterType.NexusSuicide => assets.suicideMonsterPrefab,
            CwslMonsterType.Melee or CwslMonsterType.NexusMelee => assets.meleeMonsterPrefab,
            CwslMonsterType.KoreaUniversitySoldier =>
                assets.koreaUniversitySoldierPrefab != null
                    ? assets.koreaUniversitySoldierPrefab
                    : assets.meleeMonsterPrefab,
            CwslMonsterType.StickySuicide =>
                assets.stickySuicideMonsterPrefab != null
                    ? assets.stickySuicideMonsterPrefab
                    : assets.suicideMonsterPrefab,
            CwslMonsterType.MidBoss => assets.midBossMonsterPrefab != null ? assets.midBossMonsterPrefab : assets.meleeMonsterPrefab,
            CwslMonsterType.DefenseBoss => assets.defenseBossMonsterPrefab != null ? assets.defenseBossMonsterPrefab : assets.meleeMonsterPrefab,
            CwslMonsterType.BossHongmyeongbo => assets.bossPrefab,
            _ => null
        };
    }

    private void HandleKarmaChanged(long karma)
    {
        if (!IsServer || bossSpawned)
            return;

        if (karma < CwslGameConstants.BossKarmaThreshold)
            return;

        bossSpawned = true;
        if (monsterSpawner != null)
            monsterSpawner.SpawningEnabled = false;

        SpawnBoss();
    }

    private void SpawnBoss()
    {
        var prefab = GetMonsterPrefab(CwslMonsterType.BossHongmyeongbo);
        if (prefab == null)
            return;

        var boss = CwslNetworkPoolService.Instance?.Get(
            prefab,
            new Vector3(0f, 0f, 0f),
            Quaternion.identity);
        if (boss == null)
            return;

        var monster = boss.GetComponent<CwslMonsterBase>();
        monster?.Initialize(CwslMonsterType.BossHongmyeongbo);
    }
}
