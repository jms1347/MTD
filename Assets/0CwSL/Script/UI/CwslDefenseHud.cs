using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>방어 모드 준비/카운트다운/타이머 + 넥서스 HP HUD.</summary>
public class CwslDefenseHud : MonoBehaviour
{
    private TextMeshProUGUI timerLabel;
    private TextMeshProUGUI nexusLabel;
    private TextMeshProUGUI spawnGuideLabel;
    private Image nexusFill;
    private GameObject nexusBarRoot;

    public static void Ensure(Transform canvasTransform)
    {
        if (!CwslGameConstants.UseDefenseMode)
            return;

        if (FindFirstObjectByType<CwslDefenseHud>() != null)
            return;

        var go = new GameObject("CwslDefenseHud", typeof(RectTransform));
        go.transform.SetParent(canvasTransform, false);
        go.AddComponent<CwslDefenseHud>();
        CwslDefenseStartPadVisual.Ensure();
    }

    private void OnEnable()
    {
        CwslDefenseModeController.OnTimerChanged += HandleTimerChanged;
        CwslDefenseModeController.OnPrepStateChanged += RefreshPrepUi;
        CwslNexus.OnHealthChanged += HandleNexusHealthChanged;
        BuildUi();
        RefreshAll();
    }

    private void OnDisable()
    {
        CwslDefenseModeController.OnTimerChanged -= HandleTimerChanged;
        CwslDefenseModeController.OnPrepStateChanged -= RefreshPrepUi;
        CwslNexus.OnHealthChanged -= HandleNexusHealthChanged;
    }

    private void Update()
    {
        var controller = CwslDefenseModeController.Instance;
        if (controller == null)
            return;

        if (controller.MatchPhase is CwslDefenseMatchPhase.PreMatch or CwslDefenseMatchPhase.Countdown)
            RefreshPrepUi();
        else if (controller.MatchPhase == CwslDefenseMatchPhase.Active)
            RefreshSpawnGuide(controller);
    }

    private void BuildUi()
    {
        if (timerLabel != null)
            return;

        var rect = GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -18f);
        rect.sizeDelta = new Vector2(520f, 108f);

        timerLabel = CreateLabel("Timer", 28f, FontStyles.Bold, new Vector2(0f, -8f), new Vector2(520f, 36f));
        timerLabel.color = new Color(1f, 0.92f, 0.55f);

        nexusLabel = CreateLabel("Nexus", 18f, FontStyles.Normal, new Vector2(0f, -44f), new Vector2(520f, 24f));
        nexusLabel.color = new Color(0.85f, 0.9f, 0.95f);

        spawnGuideLabel = CreateLabel("SpawnGuide", 15f, FontStyles.Normal, new Vector2(0f, -88f), new Vector2(520f, 36f));
        spawnGuideLabel.color = new Color(1f, 0.72f, 0.45f);
        spawnGuideLabel.alignment = TextAlignmentOptions.Center;
        spawnGuideLabel.gameObject.SetActive(false);

        nexusBarRoot = new GameObject("NexusBarBg", typeof(RectTransform), typeof(Image));
        nexusBarRoot.transform.SetParent(transform, false);
        var barRect = nexusBarRoot.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 1f);
        barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0f, -68f);
        barRect.sizeDelta = new Vector2(280f, 12f);
        nexusBarRoot.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.9f);

        var fillGo = new GameObject("NexusBarFill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(nexusBarRoot.transform, false);
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        nexusFill = fillGo.GetComponent<Image>();
        nexusFill.color = new Color(1f, 0.82f, 0.2f, 1f);
        nexusFill.type = Image.Type.Filled;
        nexusFill.fillMethod = Image.FillMethod.Horizontal;
    }

    private TextMeshProUGUI CreateLabel(string name, float size, FontStyles style, Vector2 pos, Vector2 dim)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = dim;
        var label = go.GetComponent<TextMeshProUGUI>();
        CwslTmpFontUtil.ApplyFont(label);
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        return label;
    }

    private void RefreshAll()
    {
        RefreshPrepUi();

        var controller = CwslDefenseModeController.Instance;
        if (controller != null && controller.IsMatchStarted)
            HandleTimerChanged(controller.RemainingSeconds);

        var nexus = CwslNexus.Instance;
        if (nexus != null)
            HandleNexusHealthChanged(nexus.CurrentHealth, nexus.MaxHealth);
    }

    private void RefreshPrepUi()
    {
        if (timerLabel == null)
            return;

        var controller = CwslDefenseModeController.Instance;
        if (controller == null)
        {
            timerLabel.text = "넥서스 방어 준비";
            SetNexusBarVisible(false);
            return;
        }

        switch (controller.MatchPhase)
        {
            case CwslDefenseMatchPhase.PreMatch:
                timerLabel.text = $"시작 발판 준비 {controller.GetReadyCount()} / {controller.RequiredPlayerCount}";
                nexusLabel.text = "공용 시작 발판에 모두 모이세요";
                SetSpawnGuideVisible(false);
                SetNexusBarVisible(false);
                break;
            case CwslDefenseMatchPhase.Countdown:
                var seconds = Mathf.CeilToInt(Mathf.Max(0f, controller.CountdownSeconds));
                timerLabel.text = seconds > 0 ? seconds.ToString() : "시작!";
                nexusLabel.text = "곧 전투가 시작됩니다";
                SetSpawnGuideVisible(false);
                SetNexusBarVisible(false);
                break;
            default:
                SetNexusBarVisible(true);
                RefreshSpawnGuide(controller);
                break;
        }
    }

    private void RefreshSpawnGuide(CwslDefenseModeController controller)
    {
        if (spawnGuideLabel == null)
            return;

        if (!controller.IsDefenseActive)
        {
            SetSpawnGuideVisible(false);
            return;
        }

        var manager = CwslMonsterManager.Instance;
        var baseInterval = manager != null ? Mathf.RoundToInt(manager.BaseSpawnIntervalSeconds) : 60;
        var spawnInterval = manager != null ? manager.SpawnIntervalPerBase.ToString("0.#") : "4";
        var maxBases = manager != null ? manager.MaxBases : 8;
        var baseCount = controller.EnemyBaseCount;

        spawnGuideLabel.text =
            $"적 기지 {baseCount}/{maxBases} · 기지마다 {spawnInterval}초마다 몬스터\n" +
            $"{baseInterval}초마다 기지 추가 + 분당 강화 · 중간보스/보스 등장";
        SetSpawnGuideVisible(true);
    }

    private void SetSpawnGuideVisible(bool visible)
    {
        if (spawnGuideLabel != null)
            spawnGuideLabel.gameObject.SetActive(visible);
    }

    private void SetNexusBarVisible(bool visible)
    {
        if (nexusBarRoot != null)
            nexusBarRoot.SetActive(visible);
    }

    private void HandleTimerChanged(float remaining)
    {
        if (timerLabel == null)
            return;

        var controller = CwslDefenseModeController.Instance;
        if (controller != null && controller.MatchPhase != CwslDefenseMatchPhase.Active)
            return;

        var minutes = Mathf.FloorToInt(remaining / 60f);
        var seconds = Mathf.FloorToInt(remaining % 60f);
        timerLabel.text = $"넥서스 방어 {minutes:00}:{seconds:00}";
    }

    private void HandleNexusHealthChanged(float current, float max)
    {
        var controller = CwslDefenseModeController.Instance;
        if (controller != null && controller.MatchPhase != CwslDefenseMatchPhase.Active)
            return;

        if (nexusLabel != null)
            nexusLabel.text = $"넥서스 {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

        if (nexusFill != null)
            nexusFill.fillAmount = max > 0f ? current / max : 0f;
    }
}
