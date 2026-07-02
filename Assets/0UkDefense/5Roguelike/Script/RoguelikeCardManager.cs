using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로그라이크 카드 런 상태·선택·조건 추적을 관리합니다.
/// </summary>
public class RoguelikeCardManager : MonoBehaviour
{
    public static RoguelikeCardManager Instance { get; private set; }

    [SerializeField] private RoguelikeOfferSettings offerSettings = new();
    [SerializeField] private RoguelikeCardVisualCatalog visualCatalog;

    private readonly RoguelikeRunState runState = new();
    private RoguelikeCardSelectUI selectUi;
    private RoguelikeMagicHandUI magicHandUi;
    private RoguelikeMagicTargetController magicTargetController;
    private bool isOfferingCards;

    public RoguelikeRunState RunState => runState;
    public RoguelikeRunModifiers Modifiers => runState.Modifiers;
    public RoguelikeOfferSettings OfferSettings => offerSettings;
    public RoguelikeCardVisualCatalog VisualCatalog => visualCatalog;
    public bool IsOfferingCards => isOfferingCards;

    public event Action OnRunStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (offerSettings == null)
            offerSettings = new RoguelikeOfferSettings();

        runState.ResetRun();

        if (visualCatalog == null)
            visualCatalog = Resources.Load<RoguelikeCardVisualCatalog>("RoguelikeCardVisualCatalog");
    }

    private void OnEnable()
    {
        RoguelikeRunEvents.OnEnemyKilled += HandleEnemyKilled;
        RoguelikeRunEvents.OnGoldSpent += HandleGoldSpent;
        RoguelikeRunEvents.OnTowerBuilt += HandleTowerBuilt;
    }

    private void OnDisable()
    {
        RoguelikeRunEvents.OnEnemyKilled -= HandleEnemyKilled;
        RoguelikeRunEvents.OnGoldSpent -= HandleGoldSpent;
        RoguelikeRunEvents.OnTowerBuilt -= HandleTowerBuilt;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Initialize(
        RoguelikeCardSelectUI cardSelectUi,
        RoguelikeMagicHandUI handUi,
        RoguelikeMagicTargetController targetController)
    {
        selectUi = cardSelectUi;
        magicHandUi = handUi;
        magicTargetController = targetController;
        magicTargetController?.Initialize(this);
        magicHandUi?.Bind(this);
    }

    public bool ShouldOfferAfterStageClear(int clearedStage)
    {
        return offerSettings != null && offerSettings.ShouldOfferAfterStageClear(clearedStage);
    }

    public IEnumerator TryRunCardOfferAfterStageClear(int clearedStage)
    {
        RecordStageCleared();

        if (!ShouldOfferAfterStageClear(clearedStage))
            yield break;

        var table = ResolveCardTable();
        if (table == null || table.All == null || table.All.Count == 0)
        {
            Debug.LogWarning("[RoguelikeCardManager] 카드 테이블이 비어 있습니다.");
            yield break;
        }

        if (selectUi == null)
        {
            Debug.LogWarning("[RoguelikeCardManager] RoguelikeCardSelectUI가 연결되지 않았습니다.");
            yield break;
        }

        int choiceCount = Mathf.Max(1, offerSettings.choiceCount);
        var choices = RoguelikeCardPool.RollChoices(
            table.All,
            choiceCount,
            runState,
            offerSettings.maxMagicHandSize);

        if (choices.Count == 0)
            yield break;

        bool completed = false;
        RoguelikeCardData picked = null;
        if (!selectUi.TryShow(
                this,
                choices,
                clearedStage,
                card =>
                {
                    picked = card;
                    completed = true;
                }))
        {
            Debug.LogError("[RoguelikeCardManager] 카드 선택 UI를 표시하지 못했습니다. Canvas/Initialize를 확인하세요.");
            yield break;
        }

        isOfferingCards = true;
        DefenseUIParticlePause.Suspend();
        float previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        const float pickTimeoutSeconds = 120f;
        float elapsedUnscaled = 0f;
        try
        {
            while (!completed)
            {
                elapsedUnscaled += Time.unscaledDeltaTime;
                if (elapsedUnscaled >= pickTimeoutSeconds)
                {
                    Debug.LogWarning("[RoguelikeCardManager] 카드 선택 대기 시간 초과 — 첫 번째 카드를 자동 선택합니다.");
                    picked = choices[0];
                    completed = true;
                    break;
                }

                yield return null;
            }

            if (picked != null)
                ApplyPickedCard(picked);

            selectUi.Hide();
        }
        finally
        {
            Time.timeScale = previousTimeScale;
            DefenseUIParticlePause.Resume();
            isOfferingCards = false;
            NotifyChanged();
        }
    }

    public void ApplyPickedCard(RoguelikeCardData card)
    {
        if (card == null)
            return;

        switch (card.cardType)
        {
            case RoguelikeCardType.Passive:
                RoguelikeCardEffectApplier.ApplyImmediate(card, runState);
                break;
            case RoguelikeCardType.Magic:
                AddMagicCard(card);
                break;
            case RoguelikeCardType.Conditional:
                runState.Conditionals.Add(new RoguelikeConditionalProgress { card = card });
                EvaluateConditionals();
                break;
        }

        NotifyChanged();
        TowerStatsManager.RefreshFromSheetIfExists();
    }

    public bool TryUseMagicCard(int handIndex)
    {
        if (magicTargetController != null && magicTargetController.IsTargeting)
            magicTargetController.CancelTargeting();

        if (handIndex < 0 || handIndex >= runState.MagicHand.Count)
            return false;

        var owned = runState.MagicHand[handIndex];
        if (owned?.card == null)
            return false;

        if (owned.card.IsGroundTargetMagic)
        {
            return magicTargetController != null
                && magicTargetController.BeginTargeting(handIndex, owned.card);
        }

        if (!RoguelikeCardEffectApplier.TryConsumeInstantMagic(owned))
            return false;

        runState.MagicHand.RemoveAt(handIndex);
        NotifyChanged();
        return true;
    }

    public void ConfirmGroundMagicCast(int handIndex, RoguelikeCardData card, Vector3 groundPoint)
    {
        if (handIndex < 0 || handIndex >= runState.MagicHand.Count)
            return;

        var owned = runState.MagicHand[handIndex];
        if (owned?.card == null || owned.card != card)
            return;

        if (card.effectType == RoguelikeCardEffectType.MagicSkill)
        {
            DefenseRoguelikeSkillCaster.TryCastAtGround(
                card.skillCode,
                groundPoint,
                card.effectValue);
        }

        runState.MagicHand.RemoveAt(handIndex);
        NotifyChanged();
    }

    public void RecordStageCleared()
    {
        runState.stagesCleared++;
        EvaluateConditionals();
    }

    private void HandleEnemyKilled()
    {
        runState.killCount++;
        EvaluateConditionals();
    }

    private void HandleGoldSpent(long amount)
    {
        if (amount <= 0)
            return;

        runState.goldSpent += amount;
        EvaluateConditionals();
    }

    private void HandleTowerBuilt()
    {
        runState.towersBuilt++;
        EvaluateConditionals();
    }

    private void EvaluateConditionals()
    {
        bool changed = false;

        for (int i = 0; i < runState.Conditionals.Count; i++)
        {
            var progress = runState.Conditionals[i];
            if (progress == null || progress.card == null || progress.isFulfilled)
                continue;

            progress.currentValue = ResolveConditionProgress(progress.card.conditionType);
            if (progress.currentValue < progress.card.conditionValue)
                continue;

            progress.isFulfilled = true;
            RoguelikeCardEffectApplier.ApplyImmediate(progress.card, runState);
            changed = true;
        }

        if (changed)
        {
            NotifyChanged();
            TowerStatsManager.RefreshFromSheetIfExists();
        }
    }

    private static int ResolveConditionProgress(RoguelikeConditionType conditionType)
    {
        var state = Instance?.runState;
        if (state == null)
            return 0;

        return conditionType switch
        {
            RoguelikeConditionType.KillEnemies => state.killCount,
            RoguelikeConditionType.ClearStages => state.stagesCleared,
            RoguelikeConditionType.SpendGold => (int)Mathf.Min(int.MaxValue, state.goldSpent),
            RoguelikeConditionType.BuildTowers => state.towersBuilt,
            _ => 0
        };
    }

    private void AddMagicCard(RoguelikeCardData card)
    {
        int max = Mathf.Max(1, offerSettings.maxMagicHandSize);
        if (runState.MagicHand.Count >= max)
            runState.MagicHand.RemoveAt(0);

        runState.MagicHand.Add(new RoguelikeOwnedMagicCard
        {
            card = card
        });
    }

    private RoguelikeCardDataSo ResolveCardTable()
    {
        var table = DataManager.Instance != null ? DataManager.Instance.RoguelikeCards : null;
        if (table != null && table.list.Count > 0)
            return table;

        return LoadFallbackCardTable();
    }

    private RoguelikeCardDataSo fallbackCardTable;

    private RoguelikeCardDataSo LoadFallbackCardTable()
    {
        if (fallbackCardTable != null)
            return fallbackCardTable;

        fallbackCardTable = ScriptableObject.CreateInstance<RoguelikeCardDataSo>();
        var tsvAsset = Resources.Load<TextAsset>("RoguelikeCard");
        if (tsvAsset != null)
            fallbackCardTable.ImportFromTsv(tsvAsset.text);

        return fallbackCardTable;
    }

    private void NotifyChanged()
    {
        OnRunStateChanged?.Invoke();
        magicHandUi?.Refresh();
    }
}
