using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 디펜스 HUD 프리팹 루트에 붙이는 초기화 스크립트.
/// 프리팹을 직접 제작한 뒤 DefenseSceneSetup의 defenseHudPrefab 슬롯에 연결합니다.
/// </summary>
public class DefenseHUDSetup : MonoBehaviour
{
    [SerializeField] private DefenseMinimapUI minimap;
    [SerializeField] private SpawnDirectionWarningUI spawnWarning;
    [SerializeField] private DefenseGoldUI goldUi;
    [SerializeField] private DefenseBuildUI buildUi;
    [SerializeField] private DefenseStageTimerUI stageTimerUi;

    /// <summary>
    /// 씬 시작 시 맵 중심·범위를 HUD에 전달합니다.
    /// </summary>
    public void Configure(Vector3 mapCenter, float mapHalfExtent)
    {
        if (minimap == null)
            minimap = GetComponentInChildren<DefenseMinimapUI>(true);

        if (spawnWarning == null)
            spawnWarning = GetComponentInChildren<SpawnDirectionWarningUI>(true);

        if (goldUi == null)
            goldUi = GetComponentInChildren<DefenseGoldUI>(true);

        if (buildUi == null)
            buildUi = GetComponentInChildren<DefenseBuildUI>(true);

        if (stageTimerUi == null)
            stageTimerUi = GetComponentInChildren<DefenseStageTimerUI>(true);

        if (goldUi == null)
        {
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null)
                goldUi = canvas.gameObject.AddComponent<DefenseGoldUI>();
        }

        if (buildUi == null)
        {
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null)
                buildUi = canvas.gameObject.AddComponent<DefenseBuildUI>();
        }

        if (stageTimerUi == null)
        {
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null)
                stageTimerUi = canvas.gameObject.AddComponent<DefenseStageTimerUI>();
        }

        RemoveStatusTestUi();
        minimap?.Configure(mapCenter, mapHalfExtent);
        spawnWarning?.Initialize();
        goldUi?.Initialize();
        buildUi?.Initialize();
        stageTimerUi?.Initialize();
        AdjustMinimapLayout();
    }

    private void RemoveStatusTestUi()
    {
        var statusPanels = GetComponentsInChildren<DefenseStatusTestUI>(true);
        for (int i = 0; i < statusPanels.Length; i++)
        {
            if (statusPanels[i] != null)
                Destroy(statusPanels[i].gameObject);
        }

        var canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
            return;

        var legacyPanel = canvas.transform.Find("StatusTestPanel");
        if (legacyPanel != null)
            Destroy(legacyPanel.gameObject);
    }

    private void AdjustMinimapLayout()
    {
        var canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
            return;

        var minimapPanel = canvas.transform.Find("MinimapPanel") as RectTransform;
        if (minimapPanel != null)
            minimapPanel.anchoredPosition = new Vector2(-16f, -80f);
    }
}

/// <summary>
/// 몬스터 상태 이펙트 테스트 패널. 버튼 클릭 시 가장 가까운 적에게 상태 부여.
/// </summary>
public class DefenseStatusTestUI : MonoBehaviour
{
    [SerializeField] private bool applyToAllEnemiesOnClick;

    private RectTransform panelRect;
    private TextMeshProUGUI hintText;

    public void Initialize()
    {
        EnsureUi();
        RefreshHint(null);
    }

    private void EnsureUi()
    {
        if (panelRect != null)
            return;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        var panel = new GameObject("StatusTestPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(920f, 72f);
        panelRect.anchoredPosition = new Vector2(0f, 108f);

        panel.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.12f, 0.88f);

        var title = CreateLabel(panel.transform, "StatusTestTitle", "상태 테스트", 13, FontStyles.Bold);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(8f, -4f);
        titleRect.sizeDelta = new Vector2(120f, 20f);
        title.alignment = TextAlignmentOptions.MidlineLeft;

        hintText = CreateLabel(panel.transform, "StatusTestHint", string.Empty, 11, FontStyles.Italic);
        var hintRect = hintText.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(1f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(1f, 1f);
        hintRect.anchoredPosition = new Vector2(-8f, -4f);
        hintRect.sizeDelta = new Vector2(280f, 20f);
        hintText.alignment = TextAlignmentOptions.MidlineRight;
        hintText.color = new Color(0.85f, 0.85f, 0.9f, 0.9f);

        var scrollObject = new GameObject("StatusScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(panel.transform, false);
        var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(6f, 6f);
        scrollRectTransform.offsetMax = new Vector2(-6f, -22f);
        scrollObject.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.55f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewport.transform.SetParent(scrollObject.transform, false);
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

        var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 0.5f);

        var layout = content.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = true;
        scroll.vertical = false;

        foreach (var status in MonsterStatusDisplayNames.AllTestable)
        {
            var captured = status;
            string label = MonsterStatusDisplayNames.Get(status);
            var button = CreateButton(content.transform, $"Status_{status}", label, () => ApplyStatus(captured));
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(78f, 40f);
        }

        var knockbackButton = CreateButton(content.transform, "Status_Knockback", "넉백", ApplyKnockback);
        knockbackButton.GetComponent<RectTransform>().sizeDelta = new Vector2(78f, 40f);
    }

    private void ApplyStatus(MonsterStatus status)
    {
        var effect = MonsterStatusTestFactory.Create(status);
        if (applyToAllEnemiesOnClick)
            ApplyToAllEnemies(effect, status);
        else if (!TryApplyToNearestEnemy(effect))
            RefreshHint($"{MonsterStatusDisplayNames.Get(status)} — 적 없음");
        else
            RefreshHint($"{MonsterStatusDisplayNames.Get(status)} 적용");
    }

    private void ApplyKnockback()
    {
        var effect = MonsterStatusTestFactory.CreateKnockback();
        if (!TryApplyToNearestEnemy(effect))
            RefreshHint("넉백 — 적 없음");
        else
            RefreshHint("넉백 적용");
    }

    private static bool TryApplyToNearestEnemy(DefenseEffectData effect)
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies == null || enemies.Length == 0)
            return false;

        Vector3 origin = ResolveOrigin();
        GameObject best = null;
        float bestSqr = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.activeInHierarchy)
                continue;

            var health = enemy.GetComponent<Health>();
            if (health != null && !health.IsAlive)
                continue;

            float sqr = (enemy.transform.position - origin).sqrMagnitude;
            if (sqr >= bestSqr)
                continue;

            bestSqr = sqr;
            best = enemy;
        }

        if (best == null)
            return false;

        DefenseEffectApplicator.ApplyEffect(best, effect, origin);
        return true;
    }

    private void ApplyToAllEnemies(DefenseEffectData effect, MonsterStatus status)
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int count = 0;
        Vector3 origin = ResolveOrigin();

        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.activeInHierarchy)
                continue;

            var health = enemy.GetComponent<Health>();
            if (health != null && !health.IsAlive)
                continue;

            DefenseEffectApplicator.ApplyEffect(enemy, effect, origin);
            count++;
        }

        RefreshHint(count > 0
            ? $"{MonsterStatusDisplayNames.Get(status)} ×{count}"
            : $"{MonsterStatusDisplayNames.Get(status)} — 적 없음");
    }

    private static Vector3 ResolveOrigin()
    {
        var player = Object.FindFirstObjectByType<PlayerCharacterController>();
        if (player != null)
            return player.transform.position;

        if (Nexus.Target != null)
            return Nexus.Target.position;

        return Vector3.zero;
    }

    private void RefreshHint(string message)
    {
        if (hintText != null)
            hintText.text = message ?? "버튼 → 가장 가까운 적";
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, int fontSize, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.text = text;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var image = buttonObject.GetComponent<Image>();
        image.sprite = DefenseUISprites.White;
        image.color = new Color(0.22f, 0.18f, 0.28f, 0.95f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(2f, 2f);
        labelRect.offsetMax = new Vector2(-2f, -2f);

        var text = labelObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 13;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = label;
        return button;
    }
}

/// <summary>MonsterStatus 인스펙터·테스트 UI용 한글 표시 이름.</summary>
public static class MonsterStatusDisplayNames
{
    private static readonly Dictionary<MonsterStatus, string> Names = new()
    {
        { MonsterStatus.Frozen, "빙결" },
        { MonsterStatus.Shocked, "감전" },
        { MonsterStatus.Wet, "젖음" },
        { MonsterStatus.Burning, "화상" },
        { MonsterStatus.Ablaze, "장판화염" },
        { MonsterStatus.Poisoned, "중독" },
        { MonsterStatus.Electrified, "약전기" },
        { MonsterStatus.Slowed, "슬로우" },
    };

    public static string Get(MonsterStatus status)
    {
        if (Names.TryGetValue(status, out var name))
            return name;

        return status.ToString();
    }

    public static IReadOnlyList<MonsterStatus> AllTestable { get; } = new[]
    {
        MonsterStatus.Frozen,
        MonsterStatus.Shocked,
        MonsterStatus.Wet,
        MonsterStatus.Burning,
        MonsterStatus.Ablaze,
        MonsterStatus.Poisoned,
        MonsterStatus.Electrified,
        MonsterStatus.Slowed,
    };
}

/// <summary>상태 이펙트 테스트용 더미 DefenseEffectData 생성.</summary>
public static class MonsterStatusTestFactory
{
    private const float DefaultDuration = 6f;

    public static DefenseEffectData Create(MonsterStatus status)
    {
        var effectType = MonsterStatusGrantRules.ToLegacyEffectType(status);
        var data = new DefenseEffectData
        {
            effectId = -(int)status,
            effectName = MonsterStatusDisplayNames.Get(status),
            effectType = effectType,
            duration = DefaultDuration,
            element = ResolveElement(status),
            description = "status test"
        };

        switch (status)
        {
            case MonsterStatus.Slowed:
                data.magnitude = 45f;
                break;
            case MonsterStatus.Poisoned:
                data.magnitude = 60f;
                data.tickDamage = 6f;
                break;
            case MonsterStatus.Burning:
            case MonsterStatus.Ablaze:
            case MonsterStatus.Electrified:
                data.tickDamage = 5f;
                break;
            case MonsterStatus.Wet:
                data.duration = 4f;
                break;
        }

        return data;
    }

    public static DefenseEffectData CreateKnockback(float distance = 2.5f)
    {
        return new DefenseEffectData
        {
            effectId = -900,
            effectName = "넉백",
            effectType = DefenseEffectType.Knockback,
            magnitude = distance,
            duration = 0f,
            element = DefenseSkillElement.Physical,
            description = "knockback test"
        };
    }

    private static DefenseSkillElement ResolveElement(MonsterStatus status)
    {
        return status switch
        {
            MonsterStatus.Frozen => DefenseSkillElement.Ice,
            MonsterStatus.Shocked or MonsterStatus.Electrified => DefenseSkillElement.Lightning,
            MonsterStatus.Wet => DefenseSkillElement.Water,
            MonsterStatus.Burning or MonsterStatus.Ablaze => DefenseSkillElement.Fire,
            MonsterStatus.Poisoned => DefenseSkillElement.Poison,
            _ => DefenseSkillElement.Physical
        };
    }
}
