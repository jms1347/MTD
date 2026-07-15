using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;



/// <summary>EA 런 상태 머신 — ACT1 전체 (허브→사수관→카드→허브→호로관→카드→클리어).</summary>

public class StllRunController : NetworkBehaviour

{

    public static StllRunController Instance { get; private set; }



    private readonly NetworkVariable<byte> syncedPhase = new(

        (byte)StllRunPhase.BrotherhoodAssign,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private readonly NetworkVariable<float> phaseSecondsRemaining = new(

        0f,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private readonly NetworkVariable<int> destroyedDepotCount = new(

        0,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private readonly NetworkVariable<int> cardPickGeneration = new(

        0,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    private readonly NetworkVariable<int> hubVisitIndex = new(

        0,

        NetworkVariableReadPermission.Everyone,

        NetworkVariableWritePermission.Server);



    [SerializeField] private GameObject depotPrefab;

    [SerializeField] private GameObject bossPrefab;

    [SerializeField] private StllStageWaveSpawner waveSpawner;



    private readonly List<StllSupplyDepot> activeDepots = new();

    private readonly HashSet<ulong> playersPickedCard = new();

    private float phaseTimer;

    private float stageStartedAt;

    private int deadPlayers;



    public StllRunPhase Phase => (StllRunPhase)syncedPhase.Value;

    public float PhaseSecondsRemaining => phaseSecondsRemaining.Value;

    public int DestroyedDepotCount => destroyedDepotCount.Value;

    public GameObject BossPrefab => bossPrefab;



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

        StllPrimitiveMapBuilder.BuildAll();

        syncedPhase.OnValueChanged += HandlePhaseChanged;

        ApplyPhaseVisuals(Phase);



        if (!IsServer)

            return;



        StllSupplyDepot.OnAnyDestroyed += HandleDepotDestroyed;

        EnterPhase(StllRunPhase.BrotherhoodAssign, StllEaConstants.BrotherhoodAssignSeconds);

    }



    public override void OnNetworkDespawn()

    {

        syncedPhase.OnValueChanged -= HandlePhaseChanged;

        StllSupplyDepot.OnAnyDestroyed -= HandleDepotDestroyed;

        if (Instance == this)

            Instance = null;

    }



    private void HandlePhaseChanged(byte previous, byte current)

    {

        ApplyPhaseVisuals((StllRunPhase)current);

    }



    private static void ApplyPhaseVisuals(StllRunPhase phase)

    {

        switch (phase)

        {

            case StllRunPhase.StageSashuguan:

                StllPrimitiveMapBuilder.SetAreaActive(false, true, false);

                break;

            case StllRunPhase.StageHulao:

                StllPrimitiveMapBuilder.SetAreaActive(false, false, true);

                break;

            default:

                StllPrimitiveMapBuilder.SetAreaActive(true, false, false);

                break;

        }

    }



    private void Update()

    {

        if (!IsServer)

            return;



        if (phaseTimer > 0f && Phase != StllRunPhase.StageSashuguan && Phase != StllRunPhase.StageHulao)

        {

            phaseTimer -= Time.deltaTime;

            phaseSecondsRemaining.Value = Mathf.Max(0f, phaseTimer);

            if (phaseTimer <= 0f)

                OnPhaseTimerExpired();

        }



        if (Phase == StllRunPhase.StageSashuguan)

            phaseSecondsRemaining.Value = Mathf.Max(0f, StllSashuguanWaveTable.StageDurationSeconds - GetStageElapsed());



        if (Phase == StllRunPhase.Hub && phaseSecondsRemaining.Value <= StllEaConstants.HubDrumWarningSeconds)

        {

            // 북소리 구간 — 추후 SFX 연결

        }

    }



    private float GetStageElapsed() => Time.time - stageStartedAt;



    public void ServerForceStartHub()

    {

        if (!IsServer)

            return;



        EnterPhase(StllRunPhase.Hub, hubVisitIndex.Value == 0 ? StllEaConstants.HubSeconds : StllEaConstants.HubShortSeconds);

    }



    private void OnPhaseTimerExpired()

    {

        switch (Phase)

        {

            case StllRunPhase.BrotherhoodAssign:

                EnterPhase(StllRunPhase.Hub, StllEaConstants.HubSeconds);

                break;

            case StllRunPhase.Hub:

                if (hubVisitIndex.Value == 0)

                    EnterPhase(StllRunPhase.StageSashuguan, StllSashuguanWaveTable.StageDurationSeconds);

                else

                    EnterPhase(StllRunPhase.StageHulao, 600f);

                break;

            case StllRunPhase.CardPick:

                AdvanceAfterCardPick();

                break;

        }

    }



    private void AdvanceAfterCardPick()

    {

        if (hubVisitIndex.Value == 0)

        {

            hubVisitIndex.Value = 1;

            EnterPhase(StllRunPhase.Hub, StllEaConstants.HubShortSeconds);

        }

        else

        {

            EnterPhase(StllRunPhase.RunComplete, 0f);

        }

    }



    private void EnterPhase(StllRunPhase phase, float duration)

    {

        syncedPhase.Value = (byte)phase;

        phaseTimer = duration;

        phaseSecondsRemaining.Value = duration;

        playersPickedCard.Clear();

        deadPlayers = 0;



        switch (phase)

        {

            case StllRunPhase.Hub:

                SetupHub();

                break;

            case StllRunPhase.StageSashuguan:

                SetupSashuguan();

                break;

            case StllRunPhase.StageHulao:

                SetupHulao();

                break;

            case StllRunPhase.CardPick:

                SetupCardPick();

                break;

            case StllRunPhase.RunComplete:

            case StllRunPhase.RunFailed:

                waveSpawner?.StopStageServer();

                ClearDepots();

                break;

        }

    }



    private void SetupHub()

    {

        waveSpawner?.StopStageServer();

        ClearDepots();

        TeleportPlayersToHub();

    }



    private void SetupSashuguan()

    {

        ClearDepots();

        destroyedDepotCount.Value = 0;

        SpawnDepots();

        TeleportPlayersToStage();

        stageStartedAt = Time.time;

        waveSpawner?.BeginSashuguanServer();

    }



    private void SetupHulao()

    {

        ClearDepots();

        TeleportPlayersToHulao();

        stageStartedAt = Time.time;

        waveSpawner?.BeginHulaoServer();

    }



    private void SetupCardPick()

    {

        waveSpawner?.StopStageServer();

        cardPickGeneration.Value += 1;

        StllCardPickerController.Instance?.BeginPickForAllPlayersServer(cardPickGeneration.Value - 1);

    }



    private void SpawnDepots()

    {

        if (depotPrefab == null)

            return;



        SpawnDepot('A');

        SpawnDepot('B');

        SpawnDepot('C');

    }



    private void SpawnDepot(char label)

    {

        var pos = StllPrimitiveMapBuilder.GetDepotPosition(label);

        var instance = Instantiate(depotPrefab, pos, Quaternion.identity);

        var netObj = instance.GetComponent<NetworkObject>();

        if (netObj == null)

        {

            Destroy(instance);

            return;

        }



        netObj.Spawn(true);

        var depot = instance.GetComponent<StllSupplyDepot>();

        if (depot != null)

        {

            depot.ConfigureServer(label, StllSupplyDepot.DefaultMaxHealth, pos);

            activeDepots.Add(depot);

        }

    }



    private void ClearDepots()

    {

        for (var i = activeDepots.Count - 1; i >= 0; i--)

        {

            var depot = activeDepots[i];

            if (depot != null && depot.IsSpawned)

                depot.NetworkObject.Despawn(true);

        }



        activeDepots.Clear();

    }



    private void HandleDepotDestroyed(StllSupplyDepot depot)

    {

        if (!IsServer || Phase != StllRunPhase.StageSashuguan)

            return;



        activeDepots.Remove(depot);

        destroyedDepotCount.Value += 1;



        if (destroyedDepotCount.Value >= 2)

            EnterPhase(StllRunPhase.RunFailed, 0f);

    }



    private void TeleportPlayersToHub()

    {

        var index = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)

        {

            if (client.PlayerObject == null)

                continue;



            client.PlayerObject.transform.position = StllPrimitiveMapBuilder.GetPlayerSpawnPoint(index);

            index++;

        }

    }



    private void TeleportPlayersToStage()

    {

        var index = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)

        {

            if (client.PlayerObject == null)

                continue;



            client.PlayerObject.transform.position = StllPrimitiveMapBuilder.GetStagePlayerSpawnPoint(index);

            index++;

        }

    }



    private void TeleportPlayersToHulao()

    {

        var index = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)

        {

            if (client.PlayerObject == null)

                continue;



            client.PlayerObject.transform.position = StllPrimitiveMapBuilder.GetHulaoPlayerSpawn(index);

            index++;

        }

    }



    public void ServerNotifyStageSurvived()

    {

        if (!IsServer || Phase != StllRunPhase.StageSashuguan)

            return;



        if (destroyedDepotCount.Value >= 2)

            return;



        EnterPhase(StllRunPhase.CardPick, StllEaConstants.CardPickSeconds);

    }



    public void ServerNotifyBossDefeated()

    {

        if (!IsServer || Phase != StllRunPhase.StageHulao)

            return;



        EnterPhase(StllRunPhase.CardPick, StllEaConstants.CardPickSeconds);

    }



    public void ServerNotifyPlayerDied(StllPlayerHealth health)

    {

        if (!IsServer)

            return;



        deadPlayers++;

        if (deadPlayers >= NetworkManager.Singleton.ConnectedClientsList.Count)

            EnterPhase(StllRunPhase.RunFailed, 0f);

    }



    public void ServerNotifyPlayerPickedCard(ulong clientId)

    {

        if (!IsServer || Phase != StllRunPhase.CardPick)

            return;



        playersPickedCard.Add(clientId);

        if (playersPickedCard.Count >= NetworkManager.Singleton.ConnectedClientsList.Count)

            AdvanceAfterCardPick();

    }



    private void LateUpdate()

    {

        if (!IsServer || Phase != StllRunPhase.StageSashuguan)

            return;



        if (GetStageElapsed() >= StllSashuguanWaveTable.StageDurationSeconds)

            ServerNotifyStageSurvived();

    }

}


