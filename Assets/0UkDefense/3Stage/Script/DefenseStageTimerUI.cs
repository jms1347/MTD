using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상단 중앙 흰색 초 카운트다운 + 중앙 안내 문구를 갱신합니다.
/// </summary>
public class DefenseStageTimerUI : MonoBehaviour
{
    [SerializeField] private Text timerText;
    [SerializeField] private Text messageText;

    private bool isBound;
    private float showStageStartUntil;

    public void Initialize()
    {
        EnsureUi();
        EnsureTimerStyle();
        BindEvents();
        SyncDisplay();
    }

    public void SyncFromManager()
    {
        EnsureUi();
        EnsureTimerStyle();
        BindEvents();
        SyncDisplay();
    }

    private void OnEnable()
    {
        BindEvents();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    private void BindEvents()
    {
        if (DefenseStageTimerManager.Instance == null)
            return;

        if (!isBound)
        {
            DefenseStageTimerManager.Instance.OnStageStateChanged -= HandleStageStateChanged;
            DefenseStageTimerManager.Instance.OnStageStateChanged += HandleStageStateChanged;
            isBound = true;
        }
    }

    private void UnbindEvents()
    {
        if (!isBound || DefenseStageTimerManager.Instance == null)
            return;

        DefenseStageTimerManager.Instance.OnStageStateChanged -= HandleStageStateChanged;
        isBound = false;
    }

    private void HandleStageStateChanged(int stage, DefenseStagePhase phase, float secondsRemaining, string message)
    {
        if (phase == DefenseStagePhase.Battle)
            showStageStartUntil = Time.time + 2.5f;

        ApplyDisplay(phase, secondsRemaining, message);
    }

    private void Update()
    {
        if (timerText == null)
            return;

        BindEvents();

        var manager = DefenseStageTimerManager.Instance;
        if (manager == null)
            return;

        var phase = manager.CurrentPhase;

        if (phase == DefenseStagePhase.PreBattleCountdown)
        {
            int seconds = Mathf.CeilToInt(manager.SecondsRemaining);
            ApplyDisplay(phase, manager.SecondsRemaining, manager.CurrentMessage);
            timerText.text = seconds.ToString();
            return;
        }

        if (phase != DefenseStagePhase.Battle)
            return;

        if (Time.time < showStageStartUntil)
            return;

        if (StageManager.Instance == null)
            return;

        int alive = StageManager.Instance.AliveEnemyCount;
        int spawned = StageManager.Instance.StageSpawnedTotal;
        int quota = StageManager.Instance.StageSpawnQuota;
        timerText.text = alive.ToString();
        messageText.text = $"스테이지 {manager.CurrentStage} 전투\n{spawned}/{quota}";
    }

    private void SyncDisplay()
    {
        var manager = DefenseStageTimerManager.Instance;
        if (manager == null)
        {
            ApplyDisplay(DefenseStagePhase.PreBattleCountdown, 60f, "1스테이지 시작 60초 전");
            timerText.text = "60";
            return;
        }

        ApplyDisplay(manager.CurrentPhase, manager.SecondsRemaining, manager.CurrentMessage);
    }

    private void ApplyDisplay(DefenseStagePhase phase, float secondsRemaining, string message)
    {
        if (timerText == null || messageText == null)
            return;

        if (phase == DefenseStagePhase.PreBattleCountdown)
            timerText.text = Mathf.CeilToInt(secondsRemaining).ToString();
        else if (phase == DefenseStagePhase.Battle && Time.time < showStageStartUntil)
            timerText.text = "0";
        else if (phase == DefenseStagePhase.Battle)
            return;

        messageText.text = message;
    }

    private void EnsureTimerStyle()
    {
        if (timerText == null)
            return;

        timerText.color = Color.white;
        timerText.fontSize = 54;
        timerText.fontStyle = FontStyle.Bold;
        timerText.alignment = TextAnchor.MiddleCenter;
    }

    private void EnsureUi()
    {
        if (timerText != null && messageText != null)
            return;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        var root = new GameObject("StageTimerRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.sizeDelta = new Vector2(520f, 180f);
        rootRect.anchoredPosition = new Vector2(0f, -12f);

        var timerGo = new GameObject("StageTimerText", typeof(RectTransform), typeof(Text));
        timerGo.transform.SetParent(root.transform, false);
        var timerRect = timerGo.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 1f);
        timerRect.anchorMax = new Vector2(0.5f, 1f);
        timerRect.pivot = new Vector2(0.5f, 1f);
        timerRect.sizeDelta = new Vector2(220f, 72f);
        timerRect.anchoredPosition = new Vector2(0f, 0f);

        timerText = timerGo.GetComponent<Text>();
        timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timerText.fontSize = 54;
        timerText.fontStyle = FontStyle.Bold;
        timerText.alignment = TextAnchor.MiddleCenter;
        timerText.color = Color.white;
        timerText.raycastTarget = false;

        var messageGo = new GameObject("StageMessageText", typeof(RectTransform), typeof(Text));
        messageGo.transform.SetParent(root.transform, false);
        var messageRect = messageGo.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.5f, 1f);
        messageRect.anchorMax = new Vector2(0.5f, 1f);
        messageRect.pivot = new Vector2(0.5f, 1f);
        messageRect.sizeDelta = new Vector2(500f, 90f);
        messageRect.anchoredPosition = new Vector2(0f, -78f);

        messageText = messageGo.GetComponent<Text>();
        messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        messageText.fontSize = 30;
        messageText.fontStyle = FontStyle.Bold;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = new Color(1f, 0.92f, 0.55f);
        messageText.raycastTarget = false;
    }
}
