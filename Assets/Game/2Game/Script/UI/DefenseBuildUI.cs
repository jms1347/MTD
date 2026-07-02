using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하단 타워 건설 버튼 UI — 구글 시트 Tower DB 전체.
/// </summary>
public class DefenseBuildUI : MonoBehaviour
{
    private RectTransform panelRect;
    private ScrollRect scrollRect;
    private readonly Dictionary<int, Button> towerButtons = new();

    public void Initialize()
    {
        EnsureUi();
        BindManager();
        SyncTowerButtons();
        RefreshAllButtons();
        StartCoroutine(RefreshWhenTowerDataReady());
    }

    private void OnEnable()
    {
        BindManager();
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoneyChanged += OnMoneyChanged;

        SyncTowerButtons();
        RefreshAllButtons();
    }

    private void OnDisable()
    {
        if (DefenseBuildManager.Instance != null)
            DefenseBuildManager.Instance.OnSelectionChanged -= OnSelectionChanged;

        if (GameManager.Instance != null)
            GameManager.Instance.OnMoneyChanged -= OnMoneyChanged;
    }

    private IEnumerator RefreshWhenTowerDataReady()
    {
        const float timeoutSeconds = 8f;
        float elapsed = 0f;
        int lastCount = DefenseBuildCatalog.GetBuildableTowerIds().Count;

        while (elapsed < timeoutSeconds)
        {
            int currentCount = DefenseBuildCatalog.GetBuildableTowerIds().Count;
            if (currentCount > lastCount)
            {
                EnsureUi();
                SyncTowerButtons();
                RefreshAllButtons();
                lastCount = currentCount;
            }

            if (currentCount > 0 && GoogleSheetManager.Instance != null && GoogleSheetManager.Instance.IsLoaded)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        EnsureUi();
        SyncTowerButtons();
        RefreshAllButtons();
    }

    private void BindManager()
    {
        if (DefenseBuildManager.Instance == null)
            return;

        DefenseBuildManager.Instance.OnSelectionChanged -= OnSelectionChanged;
        DefenseBuildManager.Instance.OnSelectionChanged += OnSelectionChanged;
    }

    private void OnMoneyChanged(long _)
    {
        RefreshAllButtons();
    }

    private void OnSelectionChanged()
    {
        RefreshAllButtons();
    }

    private void EnsureUi()
    {
        if (panelRect != null)
            return;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        var panel = new GameObject("BuildPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(920f, 88f);
        panelRect.anchoredPosition = new Vector2(0f, 12f);

        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.06f, 0.08f, 0.06f, 0.9f);

        var scrollObject = new GameObject("TowerScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(panel.transform, false);
        var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(6f, 6f);
        scrollRectTransform.offsetMax = new Vector2(-6f, -6f);

        var scrollBg = scrollObject.GetComponent<Image>();
        scrollBg.color = new Color(0.04f, 0.05f, 0.04f, 0.65f);

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
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

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
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
    }

    private void SyncTowerButtons()
    {
        if (scrollRect == null || scrollRect.content == null)
            return;

        var towerIds = DefenseBuildCatalog.GetBuildableTowerIds();
        var activeIds = new HashSet<int>(towerIds);

        var staleIds = new List<int>();
        foreach (var pair in towerButtons)
        {
            if (!activeIds.Contains(pair.Key))
                staleIds.Add(pair.Key);
        }

        for (int i = 0; i < staleIds.Count; i++)
        {
            int staleId = staleIds[i];
            if (towerButtons.TryGetValue(staleId, out var staleButton) && staleButton != null)
                Destroy(staleButton.gameObject);
            towerButtons.Remove(staleId);
        }

        for (int i = 0; i < towerIds.Count; i++)
        {
            int towerId = towerIds[i];
            if (towerButtons.ContainsKey(towerId))
                continue;

            int capturedId = towerId;
            var button = CreateButton(
                scrollRect.content,
                $"Tower_{towerId}",
                BuildTowerLabel(towerId),
                () => ToggleTower(capturedId));
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(108f, 72f);
            towerButtons[towerId] = button;
        }

        for (int i = 0; i < towerIds.Count; i++)
        {
            if (!towerButtons.TryGetValue(towerIds[i], out var button) || button == null)
                continue;

            button.transform.SetSiblingIndex(i);
        }
    }

    private static string BuildTowerLabel(int towerId)
    {
        return $"{DefenseBuildCatalog.GetTowerDisplayName(towerId)}\n{DefenseBuildCatalog.GetTowerCost(towerId)}G";
    }

    private Button CreateButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var image = buttonObject.GetComponent<Image>();
        image.sprite = DefenseUISprites.White;
        image.color = new Color(0.16f, 0.18f, 0.16f, 0.95f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(3f, 3f);
        labelRect.offsetMax = new Vector2(-3f, -3f);

        var text = labelObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 14;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color32(0, 0, 0, 200);
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        text.text = label;
        return button;
    }

    private void ToggleTower(int towerSheetId)
    {
        if (DefenseBuildManager.Instance == null)
            return;

        if (DefenseBuildManager.Instance.SelectedTowerSheetId == towerSheetId)
            DefenseBuildManager.Instance.ClearSelection();
        else
            DefenseBuildManager.Instance.SelectTower(towerSheetId);
    }

    private void RefreshAllButtons()
    {
        SyncTowerButtons();

        var manager = DefenseBuildManager.Instance;
        bool buildAllowed = manager == null
            || DefenseStageTimerManager.Instance == null
            || DefenseStageTimerManager.Instance.CanPlayerBuild();

        foreach (var pair in towerButtons)
        {
            int towerId = pair.Key;
            var button = pair.Value;
            if (button == null)
                continue;

            bool selected = manager != null && manager.SelectedTowerSheetId == towerId;
            bool affordable = manager == null || manager.CanAffordTower(towerId);
            ApplyButtonStyle(button, selected, affordable, buildAllowed);
            SetButtonLabel(button, towerId, BuildTowerLabel(towerId), affordable);
        }
    }

    private static void SetButtonLabel(Button button, int towerSheetId, string label, bool affordable)
    {
        var text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text == null)
            return;

        text.text = label;
        text.color = DefenseTowerBuildTable.GetUiTextColor(towerSheetId, affordable);
    }

    private static void ApplyButtonStyle(Button button, bool selected, bool affordable, bool buildAllowed)
    {
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = selected
                ? new Color(0.28f, 0.45f, 0.28f, 0.98f)
                : affordable
                    ? new Color(0.16f, 0.18f, 0.16f, 0.95f)
                    : new Color(0.22f, 0.14f, 0.14f, 0.9f);
        }

        button.interactable = buildAllowed;
    }
}
