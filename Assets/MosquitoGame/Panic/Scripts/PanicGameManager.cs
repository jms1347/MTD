using System;
using Unity.Netcode;
using UnityEngine;

public class PanicGameManager : MonoBehaviour
{
    public static PanicGameManager Instance { get; private set; }

    private PanicGamePhase phase = PanicGamePhase.Lobby;
    private float phaseTimer;
    private int clearedMissionMask;
    private PanicWinReason winReason = PanicWinReason.None;
    private bool humanWon;

    public PanicGamePhase Phase => phase;
    public float PhaseTimer => phaseTimer;
    public int ClearedMissionCount => CountBits(clearedMissionMask);
    public bool IsPrep => phase == PanicGamePhase.Prep;
    public bool IsPlay => phase == PanicGamePhase.Play;
    public bool IsEnded => phase == PanicGamePhase.Ended;
    public bool HumanWon => humanWon;
    public PanicWinReason WinReason => winReason;

    public event Action<PanicGamePhase> OnPhaseChanged;
    public event Action<PanicWinReason, bool> OnMatchEnded;

    private HumanController human;
    private int aliveMosquitoes;
    private bool started;

    private bool IsServer => NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        if (IsServer && !started)
        {
            started = true;
            BeginPrepPhase();
        }
    }

    private void Update()
    {
        if (!IsServer || phase == PanicGamePhase.Lobby || phase == PanicGamePhase.Ended)
            return;

        phaseTimer = Mathf.Max(0f, phaseTimer - Time.deltaTime);
        if (phaseTimer > 0f)
            return;

        if (phase == PanicGamePhase.Prep)
            BeginPlayPhase();
        else if (phase == PanicGamePhase.Play)
            EndMatch(PanicWinReason.TimeExpiredHumanAhead, EvaluateHumanAhead());
    }

    public void RegisterHuman(HumanController controller) => human = controller;

    public void RegisterMosquitoSpawned()
    {
        if (IsServer)
            aliveMosquitoes++;
    }

    public void NotifyMosquitoEliminated()
    {
        if (!IsServer || phase != PanicGamePhase.Play)
            return;

        aliveMosquitoes = Mathf.Max(0, aliveMosquitoes - 1);
        if (aliveMosquitoes <= 0)
            EndMatch(PanicWinReason.MosquitoesEliminated, true);
    }

    public void NotifyHumanHealthDepleted()
    {
        if (!IsServer || phase != PanicGamePhase.Play)
            return;

        EndMatch(PanicWinReason.HumanEliminated, false);
    }

    public void NotifyMissionCleared(PanicMissionType mission)
    {
        if (!IsServer)
            return;

        var bit = 1 << (int)mission;
        if ((clearedMissionMask & bit) != 0)
            return;

        clearedMissionMask |= bit;
        if (ClearedMissionCount >= PanicGameConstants.RequiredMissionCount)
            EndMatch(PanicWinReason.HumanMissionsComplete, true);
    }

    private void BeginPrepPhase()
    {
        phase = PanicGamePhase.Prep;
        phaseTimer = PanicGameConstants.PrepDurationSeconds;
        winReason = PanicWinReason.None;
        humanWon = false;
        clearedMissionMask = 0;
        OnPhaseChanged?.Invoke(phase);
    }

    private void BeginPlayPhase()
    {
        phase = PanicGamePhase.Play;
        phaseTimer = PanicGameConstants.MatchDurationSeconds;
        TrapManager.Instance?.LockPlacement();
        OnPhaseChanged?.Invoke(phase);
    }

    private void EndMatch(PanicWinReason reason, bool humanVictory)
    {
        if (phase == PanicGamePhase.Ended)
            return;

        phase = PanicGamePhase.Ended;
        phaseTimer = 0f;
        winReason = reason;
        humanWon = humanVictory;

        if (ScoreManager.Instance != null)
        {
            if (humanVictory && human != null)
                ScoreManager.Instance.ApplyEndBonusHumanWin(human.CurrentHealth);
            else if (!humanVictory)
                ScoreManager.Instance.ApplyEndBonusMosquitoWin();
        }

        OnPhaseChanged?.Invoke(phase);
        OnMatchEnded?.Invoke(reason, humanVictory);
    }

    private static bool EvaluateHumanAhead()
    {
        if (ScoreManager.Instance == null)
            return true;
        return ScoreManager.Instance.HumanRp >= ScoreManager.Instance.MosquitoTeamRp;
    }

    private static int CountBits(int value)
    {
        var count = 0;
        while (value != 0)
        {
            count += value & 1;
            value >>= 1;
        }
        return count;
    }
}
