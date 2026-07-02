using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CoopGameUI : MonoBehaviour
{
    private CoopGameSession session;

    private TextMeshProUGUI waveText;
    private TextMeshProUGUI goldText;
    private TextMeshProUGUI statsText;
    private TextMeshProUGUI announceText;
    private TextMeshProUGUI costText;
    private TextMeshProUGUI skillText;
    private Button skillButton;

    private void Start()
    {
        EnsureEventSystem();
        BuildUI();

        session = CoopGameSession.Instance;
        if (session == null)
            return;

        session.OnStateUpdated += Refresh;
        session.OnAnnouncement += msg => announceText.text = msg;
        session.OnGameEnded += HandleGameOver;
    }

    private void OnDestroy()
    {
        if (session == null)
            return;

        session.OnStateUpdated -= Refresh;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void BuildUI()
    {
        var canvasObject = new GameObject("CoopUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var panel = CreatePanel(canvas.transform, new Color(0f, 0f, 0f, 0.45f));
        Stretch(panel);

        waveText = CreateLabel(panel.transform, "웨이브 0", 28, new Vector2(20f, -20f), TextAlignmentOptions.TopLeft);
        goldText = CreateLabel(panel.transform, "골드 0", 28, new Vector2(20f, -60f), TextAlignmentOptions.TopLeft);
        statsText = CreateLabel(panel.transform, "스탯", 22, new Vector2(20f, -110f), TextAlignmentOptions.TopLeft);
        announceText = CreateLabel(panel.transform, "", 24, new Vector2(0f, -20f), TextAlignmentOptions.Top);
        costText = CreateLabel(panel.transform, "", 20, new Vector2(-20f, -100f), TextAlignmentOptions.TopRight);

        CreateUpgradeButton(panel.transform, "공격력 +", new Vector2(-20f, -160f), CoopGameProtocol.UpgradeAttack);
        CreateUpgradeButton(panel.transform, "체력 +", new Vector2(-20f, -220f), CoopGameProtocol.UpgradeHealth);
        CreateUpgradeButton(panel.transform, "공속 +", new Vector2(-20f, -280f), CoopGameProtocol.UpgradeSpeed);
        CreateUpgradeButton(panel.transform, "관통 +", new Vector2(-20f, -340f), CoopGameProtocol.UpgradePenetration);

        skillText = CreateLabel(panel.transform, "스킬", 20, new Vector2(20f, -400f), TextAlignmentOptions.TopLeft);
        skillButton = CreateButton(panel.transform, "스킬 (Q)", new Vector2(-20f, -400f), new Vector2(180f, 48f));
        skillButton.onClick.AddListener(CastSkillFromUi);

        CreateLabel(panel.transform, "우클릭:이동 | A+클릭:어택땅 | Q:스킬 | 빨강:매복 | 보라:위험 | 초록:회복 | 하늘:가속 | 노랑:보급/보물", 16,
            new Vector2(0f, 20f), TextAlignmentOptions.Bottom);

        var lobbyButton = CreateButton(panel.transform, "로비로", new Vector2(-20f, -20f), new Vector2(140f, 44f));
        lobbyButton.onClick.AddListener(() => SceneManager.LoadScene("LobbyScene"));
    }

    private void Refresh(CoopSyncPayload state)
    {
        if (state == null)
            return;

        waveText.text = state.goldRush
            ? $"웨이브 {state.wave}  |  GOLD RUSH x2  |  적 {state.aliveEnemies}"
            : $"웨이브 {state.wave}  |  적 {state.aliveEnemies}";

        if (!session.TryGetLocalPlayer(out var local))
        {
            goldText.text = "플레이어 정보 없음";
            skillText.text = string.Empty;
            return;
        }

        goldText.text = $"골드 {local.gold}";
        statsText.text =
            $"탱크 {ResolveTankLabel(local.towerCode)}\n" +
            $"공격력 {local.attack:0} (Lv.{local.atkLevel})\n" +
            $"체력 {local.towerHp:0}/{local.towerMaxHp:0} (Lv.{local.hpLevel})\n" +
            $"공격속도 {local.fireInterval:0.0}초 (Lv.{local.spdLevel})\n" +
            $"관통력 {local.penetration} (Lv.{local.penLevel})";

        var skillName = CoopSkillCatalog.ResolveDisplayName(local.skillId);
        if (local.skillCooldown > 0f)
            skillText.text = $"스킬: {skillName}  |  쿨다운 {Mathf.CeilToInt(local.skillCooldown)}초";
        else
            skillText.text = $"스킬: {skillName}  |  사용 가능 (Q)";

        if (skillButton != null)
            skillButton.interactable = local.skillCooldown <= 0f && !string.IsNullOrEmpty(local.skillId);

        costText.text =
            $"공격 {CoopUpgradeRules.GetCost(CoopGameProtocol.UpgradeAttack, local.atkLevel)}G\n" +
            $"체력 {CoopUpgradeRules.GetCost(CoopGameProtocol.UpgradeHealth, local.hpLevel)}G\n" +
            $"공속 {CoopUpgradeRules.GetCost(CoopGameProtocol.UpgradeSpeed, local.spdLevel)}G\n" +
            $"관통 {CoopUpgradeRules.GetCost(CoopGameProtocol.UpgradePenetration, local.penLevel)}G";

        if (!string.IsNullOrEmpty(state.announcement))
            announceText.text = state.announcement;
    }

    private void HandleGameOver(bool victory, string message)
    {
        announceText.text = message;
        announceText.color = new Color(1f, 0.45f, 0.45f);
    }

    private static string ResolveTankLabel(string tankCode)
    {
        if (CoopTankCatalog.TryGet(tankCode, out var tank))
            return $"{tank.DisplayName} ({tank.Code})";

        return tankCode;
    }

    private void CastSkillFromUi()
    {
        if (session == null || !session.TryGetLocalPlayer(out var local))
            return;

        if (local.skillCooldown > 0f || string.IsNullOrEmpty(local.skillId))
            return;

        if (session.TryGetLivingTower(local.playerId, out var unit))
        {
            var input = unit.GetComponent<CoopRtsTowerInput>();
            if (input != null)
            {
                input.BeginSkillCast();
                return;
            }
        }

        if (CoopSkillCatalog.RequiresGroundTarget(local.skillId))
        {
            if (!TryGetMouseWorldPoint(out var point))
                return;

            session.RequestSkill(local.playerId, point.x, point.z);
            return;
        }

        session.RequestSkill(local.playerId, local.towerX, local.towerZ);
    }

    private static bool TryGetMouseWorldPoint(out Vector3 point)
    {
        point = default;
        if (Camera.main == null)
            return false;

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out var distance))
            return false;

        point = ray.GetPoint(distance);
        point.y = 0f;
        return true;
    }

    private void CreateUpgradeButton(Transform parent, string label, Vector2 pos, string upgradeKey)
    {
        var button = CreateButton(parent, label, pos, new Vector2(180f, 48f));
        button.onClick.AddListener(() => session?.TryUpgrade(upgradeKey));
    }

    private static GameObject CreatePanel(Transform parent, Color color)
    {
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        var image = panel.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return panel;
    }

    private static void Stretch(GameObject go)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string text, float size, Vector2 anchoredPos, TextAlignmentOptions align)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = align == TextAlignmentOptions.Top ? new Vector2(0.5f, 1f) : align == TextAlignmentOptions.TopRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = rect.anchorMin;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(700f, 120f);

        var label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = align;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.85f, 0.9f);

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(go.transform, false);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 20f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        Stretch(textObject);

        return go.GetComponent<Button>();
    }
}
