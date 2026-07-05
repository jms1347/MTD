using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CwslLocalPlayerHud : NetworkBehaviour
{
    private CwslPlayerGold playerGold;
    private CwslPlayerCharacter playerCharacter;
    private TextMeshProUGUI goldLabel;
    private TextMeshProUGUI hintLabel;
    private TextMeshProUGUI toastLabel;
    private Transform hudCanvasTransform;
    private bool introPopupShown;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        playerGold = GetComponent<CwslPlayerGold>();
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        CreateHud();
        if (playerGold != null)
        {
            playerGold.OnGoldChanged += RefreshGold;
            RefreshGold(playerGold.Gold);
        }

        if (playerCharacter != null)
        {
            playerCharacter.OnCharacterChanged += HandleCharacterChangedForHud;
            HandleCharacterChangedForHud(playerCharacter.CharacterId);
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        CwslSkillGoldFeedback.Tick();
    }

    public override void OnNetworkDespawn()
    {
        if (playerGold != null)
            playerGold.OnGoldChanged -= RefreshGold;
        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged -= HandleCharacterChangedForHud;
    }

    private void HandleCharacterChangedForHud(CwslCharacterId characterId)
    {
        RefreshHint(characterId);
        TryShowCharacterIntroPopup(characterId);
    }

    private void TryShowCharacterIntroPopup(CwslCharacterId characterId)
    {
        if (introPopupShown || hudCanvasTransform == null)
            return;

        introPopupShown = true;
        CwslCharacterIntroPopup.Show(characterId);
    }

    private void CreateHud()
    {
        EnsureEventSystem();

        var existingCanvas = GameObject.Find("CwslGameHudCanvas");
        Transform canvasTransform;
        if (existingCanvas != null)
        {
            canvasTransform = existingCanvas.transform;
        }
        else
        {
            var canvasObject = new GameObject("CwslGameHudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CwslGameConstants.HudCanvasSortOrder;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasTransform = canvasObject.transform;
        }

        hudCanvasTransform = canvasTransform;

        var hudCanvas = canvasTransform.GetComponent<Canvas>();
        if (hudCanvas != null)
            hudCanvas.sortingOrder = CwslGameConstants.HudCanvasSortOrder;

        RemoveSidePanel(canvasTransform);
        RemoveLegacyLabels(canvasTransform);

        EnsureHintLabel(canvasTransform);
        EnsureToastLabel(canvasTransform);
        EnsureGoldPanel(canvasTransform);
        EnsureKarmaLabel(canvasTransform);
        CwslCharacterIntroPopup.Ensure(canvasTransform);
        CwslBossHealthHud.Ensure(canvasTransform);
        EnsurePartyPanel(canvasTransform);
        EnsureGameOverHud(canvasTransform);
        EnsureMinimap(canvasTransform);
        if (GetComponent<CwslArenaGimmickVisualRunner>() == null)
            gameObject.AddComponent<CwslArenaGimmickVisualRunner>();
        if (GetComponent<CwslArenaTrapVisualRunner>() == null)
            gameObject.AddComponent<CwslArenaTrapVisualRunner>();
    }

    private void EnsureHintLabel(Transform canvasTransform)
    {
        var existing = canvasTransform.Find("CwslHintLabel");
        if (existing != null)
            hintLabel = existing.GetComponent<TextMeshProUGUI>();

        if (hintLabel == null)
        {
            hintLabel = CreateLabel(
                canvasTransform,
                "CwslHintLabel",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(24f, -16f),
                new Vector2(1200f, 36f),
                20f);
            hintLabel.alignment = TextAlignmentOptions.MidlineLeft;
        }

        var characterId = playerCharacter != null ? playerCharacter.CharacterId : CwslCharacterId.Tank;
        RefreshHint(characterId);
    }

    private void EnsureToastLabel(Transform canvasTransform)
    {
        var existing = canvasTransform.Find("CwslSkillToast");
        if (existing != null)
        {
            toastLabel = existing.GetComponent<TextMeshProUGUI>();
            CwslSkillGoldFeedback.BindToast(toastLabel);
            return;
        }

        toastLabel = CreateLabel(
            canvasTransform,
            "CwslSkillToast",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 120f),
            new Vector2(920f, 52f),
            28f);
        toastLabel.alignment = TextAlignmentOptions.Center;
        toastLabel.fontStyle = FontStyles.Bold;
        toastLabel.color = new Color(1f, 0.55f, 0.45f);
        toastLabel.gameObject.SetActive(false);
        if (toastLabel.font != null)
        {
            toastLabel.outlineWidth = 0.2f;
            toastLabel.outlineColor = new Color32(0, 0, 0, 220);
        }

        CwslSkillGoldFeedback.BindToast(toastLabel);
    }

    private void RefreshHint(CwslCharacterId characterId)
    {
        if (hintLabel == null)
            return;

        var entry = CwslCharacterCatalog.Get(characterId);
        hintLabel.text = entry.ControlHint;
    }

    private void EnsureGoldPanel(Transform canvasTransform)
    {
        var existing = canvasTransform.Find("CwslGoldPanel");
        if (existing != null)
        {
            goldLabel = existing.Find("Label")?.GetComponent<TextMeshProUGUI>();
            return;
        }

        var panelObject = new GameObject("CwslGoldPanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvasTransform, false);
        var panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -18f);
        panelRect.sizeDelta = new Vector2(360f, 68f);

        var background = panelObject.GetComponent<Image>();
        background.color = new Color(0.05f, 0.06f, 0.08f, 0.82f);
        background.raycastTarget = false;

        var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(panelObject.transform, false);
        var accentRect = (RectTransform)accent.transform;
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(1f, 0f);
        accentRect.pivot = new Vector2(0.5f, 0f);
        accentRect.sizeDelta = new Vector2(-16f, 3f);
        accentRect.anchoredPosition = new Vector2(0f, 4f);
        accent.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f, 0.95f);
        accent.GetComponent<Image>().raycastTarget = false;

        goldLabel = CreateLabel(
            panelObject.transform,
            "Label",
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            36f);
        goldLabel.alignment = TextAlignmentOptions.Center;
        goldLabel.fontStyle = FontStyles.Bold;
        goldLabel.color = new Color(1f, 0.92f, 0.4f);
        var goldRect = goldLabel.rectTransform;
        goldRect.anchorMin = Vector2.zero;
        goldRect.anchorMax = Vector2.one;
        goldRect.offsetMin = new Vector2(24f, 6f);
        goldRect.offsetMax = new Vector2(-24f, -4f);

        if (goldLabel.font != null)
        {
            goldLabel.outlineWidth = 0.15f;
            goldLabel.outlineColor = new Color32(0, 0, 0, 200);
        }
    }

    private void EnsureKarmaLabel(Transform canvasTransform)
    {
        if (CwslKarmaSystem.Instance == null)
            return;

        var existing = canvasTransform.Find("KarmaLabel");
        if (existing != null)
            return;

        var karmaLabel = CreateLabel(
            canvasTransform,
            "KarmaLabel",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(24f, -56f),
            new Vector2(640f, 40f),
            24f);
        karmaLabel.alignment = TextAlignmentOptions.MidlineLeft;

        var karmaUi = canvasTransform.gameObject.GetComponent<CwslKarmaUI>()
                      ?? canvasTransform.gameObject.AddComponent<CwslKarmaUI>();
        karmaUi.Bind(CwslKarmaSystem.Instance, CwslTeamGoldCollectedSystem.Instance, karmaLabel);
    }

    private static void EnsurePartyPanel(Transform canvasTransform)
    {
        var existing = canvasTransform.Find("CwslPartyPanel");
        if (existing != null)
            return;

        var panelObject = new GameObject("CwslPartyPanel", typeof(RectTransform), typeof(Image), typeof(CwslPartyPanel));
        panelObject.transform.SetParent(canvasTransform, false);
    }

    private static void EnsureGameOverHud(Transform canvasTransform)
    {
        CwslGameOverHud.Ensure(canvasTransform);
        if (CwslGameFlow.Instance != null)
            CwslGameOverHud.SetVisible(CwslGameFlow.Instance.AllPlayersDefeated);
    }

    private static void EnsureMinimap(Transform canvasTransform)
    {
        var existing = canvasTransform.Find("CwslMinimap");
        if (existing != null)
            return;

        var minimapObject = new GameObject("CwslMinimap", typeof(RectTransform), typeof(CwslMinimap));
        minimapObject.transform.SetParent(canvasTransform, false);
        var minimap = minimapObject.GetComponent<CwslMinimap>();
        minimap.SetLocalPlayer(FindLocalPlayerTransform());
    }

    private static Transform FindLocalPlayerTransform()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null)
            return null;

        return NetworkManager.Singleton.LocalClient.PlayerObject?.transform;
    }

    private static void RemoveSidePanel(Transform canvasTransform)
    {
        var existing = canvasTransform.Find("CwslGameSidePanel");
        if (existing != null)
            Destroy(existing.gameObject);
    }

    private static void RemoveLegacyLabels(Transform canvasTransform)
    {
        var legacyGold = canvasTransform.Find("CwslGoldLabel");
        if (legacyGold != null)
            Destroy(legacyGold.gameObject);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void RefreshGold(int gold)
    {
        if (goldLabel != null)
            goldLabel.text = $"골드 {CwslCurrencyDisplay.FormatGold(gold)} ({gold})";
    }

    private static TextMeshProUGUI CreateLabel(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var label = go.GetComponent<TextMeshProUGUI>();
        CwslTmpFontUtil.ApplyFont(label);
        label.fontSize = fontSize;
        label.color = Color.white;
        return label;
    }
}
