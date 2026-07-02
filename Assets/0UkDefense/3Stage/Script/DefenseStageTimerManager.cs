using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 스테이지별 준비 시간 → 전투 → 클리어 후 바로 다음 스테이지 준비 루프를 관리합니다.
/// </summary>
public class DefenseStageTimerManager : MonoBehaviour
{
    public static DefenseStageTimerManager Instance { get; private set; }

    [Header("타이밍")]
    [SerializeField] private float preBattleCountdown = 60f;
    [SerializeField] private bool allowTestSkipWithMKey = true;

    [Header("스테이지")]
    [Tooltip("스테이지별 몬스터 구성. 비워두면 아래 임시 공식으로 총 마릿수만 사용합니다.")]
    [SerializeField] private StageDataSo stageDataSo;

    [Header("스테이지 (임시 폴백)")]
    [Tooltip("StageDataSo에 해당 stageId가 없을 때 사용")]
    [SerializeField] private int baseEnemyQuota = 24;
    [SerializeField] private int quotaPerStage = 12;
    [SerializeField] private int quotaRandomBonusMax = 10;

    [Header("전투 중 플레이어 행동")]
    [SerializeField] private DefensePlayerBattleRules battlePlayerRules = new();

    private int currentStage = 1;
    private DefenseStagePhase currentPhase;
    private float secondsRemaining;
    private string currentMessage = string.Empty;
    private Coroutine gameLoopCoroutine;
    private bool isWaitingStageClear;
    private bool skipPreBattleRequested;

    public int CurrentStage => currentStage;
    public DefenseStagePhase CurrentPhase => currentPhase;
    public float SecondsRemaining => secondsRemaining;
    public string CurrentMessage => currentMessage;
    public bool IsBattlePhase => currentPhase == DefenseStagePhase.Battle;
    public DefensePlayerBattleRules BattlePlayerRules => battlePlayerRules;

    public event Action<int, DefenseStagePhase, float, string> OnStageStateChanged;

    public void ConfigureBattlePlayerRules(DefensePlayerBattleRules rules)
    {
        battlePlayerRules = rules ?? new DefensePlayerBattleRules();
    }

    public bool CanPlayerMove()
    {
        return !IsBattlePhase || battlePlayerRules.allowMoveDuringBattle;
    }

    public bool CanPlayerBuild()
    {
        return !IsBattlePhase || battlePlayerRules.allowBuildDuringBattle;
    }

    public bool CanPlayerGather()
    {
        return !IsBattlePhase || battlePlayerRules.allowGatherDuringBattle;
    }

    public bool ShouldLockFarmBoundary()
    {
        return IsBattlePhase && battlePlayerRules.lockFarmBoundaryDuringBattle;
    }

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

        if (StageManager.Instance != null)
            StageManager.Instance.OnStageCleared -= HandleStageCleared;
    }

    private void Update()
    {
        if (!allowTestSkipWithMKey || !Input.GetKeyDown(KeyCode.M))
            return;

        RequestSkipPreBattleCountdown();
    }

    /// <summary>테스트용 — 준비 카운트다운을 건너뛰고 바로 전투 시작.</summary>
    public void RequestSkipPreBattleCountdown()
    {
        if (currentPhase != DefenseStagePhase.PreBattleCountdown)
            return;

        skipPreBattleRequested = true;
    }

    public void BeginGame()
    {
        if (gameLoopCoroutine != null)
            StopCoroutine(gameLoopCoroutine);

        isWaitingStageClear = false;
        skipPreBattleRequested = false;

        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnStageCleared -= HandleStageCleared;
            StageManager.Instance.OnStageCleared += HandleStageCleared;
        }

        currentStage = 1;
        gameLoopCoroutine = StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        yield return null;

        while (true)
        {
            StageData stageData = null;
            ResolveStageTable()?.TryGetStage(currentStage, out stageData);

            float preBattle = ResolvePreBattleSeconds(stageData);
            yield return RunPreBattleCountdown(preBattle, currentStage);

            BeginBattlePhase(stageData);
            yield return WaitForStageClear();

            StageManager.Instance?.EndStageBattle();

            if (RoguelikeCardManager.Instance != null)
                yield return RoguelikeCardManager.Instance.TryRunCardOfferAfterStageClear(currentStage);

            currentStage++;
        }
    }

    private float ResolvePreBattleSeconds(StageData stageData)
    {
        if (stageData != null && stageData.preBattleSeconds >= 0f)
            return stageData.preBattleSeconds;

        return preBattleCountdown;
    }

    private IEnumerator RunPreBattleCountdown(float duration, int displayStage)
    {
        float remaining = duration;
        while (remaining > 0f)
        {
            if (skipPreBattleRequested)
            {
                skipPreBattleRequested = false;
                break;
            }

            secondsRemaining = remaining;
            int secondsCeil = Mathf.Max(1, Mathf.CeilToInt(remaining));
            string message = $"{displayStage}스테이지 시작 {secondsCeil}초 전";
            SetPhase(DefenseStagePhase.PreBattleCountdown, remaining, message);
            yield return null;
            remaining -= Time.deltaTime;
        }

        secondsRemaining = 0f;
        SetPhase(DefenseStagePhase.PreBattleCountdown, 0f, $"{displayStage}스테이지 시작");
    }

    private void BeginBattlePhase(StageData stageData)
    {
        isWaitingStageClear = true;
        var stageLabel = stageData != null && !string.IsNullOrWhiteSpace(stageData.displayName)
            ? stageData.displayName
            : $"{currentStage}스테이지";
        SetPhase(DefenseStagePhase.Battle, 0f, $"{stageLabel} 시작");

        var stageManager = ResolveStageManager();
        if (stageManager == null)
        {
            Debug.LogError("[DefenseStageTimerManager] StageManager가 없어 몬스터 웨이브를 시작할 수 없습니다.");
            return;
        }

        try
        {
            if (stageData != null && stageData.TotalSpawnCount > 0)
                stageManager.BeginStageBattle(stageData);
            else
                stageManager.BeginStageBattle(RollStageQuota(currentStage));
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[DefenseStageTimerManager] 전투 시작 중 오류가 발생했습니다.");
        }
    }

    private static StageManager ResolveStageManager()
    {
        if (StageManager.Instance != null)
            return StageManager.Instance;

        return UnityEngine.Object.FindFirstObjectByType<StageManager>();
    }

    private IEnumerator WaitForStageClear()
    {
        while (isWaitingStageClear)
            yield return null;
    }

    private void HandleStageCleared()
    {
        isWaitingStageClear = false;
    }

    private int RollStageQuota(int stage)
    {
        int min = baseEnemyQuota + (stage - 1) * quotaPerStage;
        int max = min + quotaRandomBonusMax;
        return UnityEngine.Random.Range(min, max + 1);
    }

    private StageDataSo ResolveStageTable()
    {
        if (stageDataSo != null)
            return stageDataSo;

        return DataManager.Instance?.Stages;
    }

    private void SetPhase(DefenseStagePhase phase, float seconds, string message)
    {
        currentPhase = phase;
        secondsRemaining = seconds;
        currentMessage = message;
        NotifyState();
    }

    private void NotifyState()
    {
        OnStageStateChanged?.Invoke(currentStage, currentPhase, secondsRemaining, currentMessage);
        FarmGateController.SyncAllFromStageTimer();
    }
}
