using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>왼쪽 파티원 정보 패널 — 닉네임, ID, 캐릭터, HP.</summary>
public class CwslPartyPanel : MonoBehaviour
{
    private const float PanelWidth = 300f;
    private const float RowHeight = 76f;
    private const float RowSpacing = 8f;
    private const float RefreshInterval = 0.12f;

    private static readonly Color PanelColor = new(0.05f, 0.06f, 0.09f, 0.9f);
    private static readonly Color BorderColor = new(0.18f, 0.22f, 0.3f, 1f);
    private static readonly Color MutedTextColor = new(0.72f, 0.78f, 0.88f, 1f);
    private static readonly Color LocalAccentColor = new(0.35f, 0.95f, 0.55f, 1f);
    private static readonly Color AllyAccentColor = new(0.35f, 0.68f, 1f, 1f);
    private static readonly Color DeadAccentColor = new(0.45f, 0.45f, 0.48f, 1f);

    private readonly List<PartyRowView> rows = new();
    private RectTransform rowsRoot;
    private float refreshTimer;
    private bool subscribed;

    private sealed class PartyRowView
    {
        public GameObject Root;
        public Image Accent;
        public TextMeshProUGUI NameLabel;
        public TextMeshProUGUI InfoLabel;
        public RectTransform HpBackRect;
        public Image HpFill;
        public TextMeshProUGUI HpLabel;
        public readonly List<GameObject> SegmentDividers = new();
        public float LastSegmentMaxHealth = -1f;
    }

    private void Awake()
    {
        BuildPanel();
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshRows();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
            return;

        refreshTimer = RefreshInterval;
        RefreshRows();
    }

    private void Subscribe()
    {
        if (subscribed)
            return;

        subscribed = true;
        CwslPlayerCharacter.OnAnyCharacterChanged += HandlePartyDataChanged;
        CwslPlayerProfile.OnAnyProfileChanged += HandlePartyDataChanged;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        subscribed = false;
        CwslPlayerCharacter.OnAnyCharacterChanged -= HandlePartyDataChanged;
        CwslPlayerProfile.OnAnyProfileChanged -= HandlePartyDataChanged;
    }

    private void HandlePartyDataChanged() => RefreshRows();

    private void BuildPanel()
    {
        var rect = GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -104f);
        rect.sizeDelta = new Vector2(PanelWidth, 420f);

        var background = GetComponent<Image>();
        if (background == null)
            background = gameObject.AddComponent<Image>();
        background.color = PanelColor;
        background.raycastTarget = false;

        var border = CreateChildRect("Border", rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        border.offsetMin = Vector2.zero;
        border.offsetMax = Vector2.zero;
        var borderImage = border.gameObject.AddComponent<Image>();
        borderImage.color = BorderColor;
        borderImage.raycastTarget = false;

        var header = CreateLabel(rect, "파티", new Vector2(14f, -10f), new Vector2(PanelWidth - 28f, 28f), 20f);
        header.fontStyle = FontStyles.Bold;
        header.color = new Color(0.9f, 0.94f, 1f, 1f);

        rowsRoot = CreateChildRect("Rows", rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
        rowsRoot.anchoredPosition = new Vector2(0f, -42f);
        rowsRoot.sizeDelta = new Vector2(-20f, 360f);
        rowsRoot.pivot = new Vector2(0.5f, 1f);
    }

    private void RefreshRows()
    {
        var players = CollectPlayers();
        players.Sort((a, b) => a.ClientId.CompareTo(b.ClientId));

        for (var i = 0; i < players.Count; i++)
            BindRow(GetOrCreateRow(i), players[i], i);

        for (var i = players.Count; i < rows.Count; i++)
        {
            if (rows[i].Root != null)
                rows[i].Root.SetActive(false);
        }

        var panelHeight = 52f + players.Count * (RowHeight + RowSpacing);
        var rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(PanelWidth, Mathf.Max(120f, panelHeight));
    }

    private List<PartyPlayerSnapshot> CollectPlayers()
    {
        var result = new List<PartyPlayerSnapshot>();
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening)
            return result;

        foreach (var clientId in networkManager.ConnectedClientsIds)
        {
            if (!networkManager.ConnectedClients.TryGetValue(clientId, out var client))
                continue;

            var playerObject = client.PlayerObject;
            if (playerObject == null || !playerObject.IsSpawned)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            var character = playerObject.GetComponent<CwslPlayerCharacter>();
            var profile = playerObject.GetComponent<CwslPlayerProfile>();
            if (health == null)
                continue;

            var characterId = character != null ? character.CharacterId : CwslCharacterId.Tank;
            var displayName = profile != null ? profile.DisplayName : $"Player {clientId}";

            result.Add(new PartyPlayerSnapshot
            {
                ClientId = clientId,
                IsLocal = playerObject.IsOwner,
                DisplayName = displayName,
                CharacterId = characterId,
                CurrentHealth = health.CurrentHealth,
                MaxHealth = health.MaxHealth,
                IsAlive = health.IsAlive,
                IsDead = health.IsDead
            });
        }

        return result;
    }

    private void BindRow(PartyRowView row, PartyPlayerSnapshot player, int index)
    {
        row.Root.SetActive(true);
        row.Root.transform.SetSiblingIndex(index);

        var y = -index * (RowHeight + RowSpacing);
        var rowRect = (RectTransform)row.Root.transform;
        rowRect.anchoredPosition = new Vector2(0f, y);

        var characterEntry = CwslCharacterCatalog.Get(player.CharacterId);
        var localMark = player.IsLocal ? " · 나" : string.Empty;
        row.NameLabel.text = $"{player.DisplayName}{localMark}";
        row.InfoLabel.text = $"ID {player.ClientId}  |  {characterEntry.DisplayName}";

        var accent = player.IsDead
            ? DeadAccentColor
            : player.IsLocal
                ? LocalAccentColor
                : AllyAccentColor;
        row.Accent.color = accent;
        row.NameLabel.color = player.IsDead ? MutedTextColor : Color.white;

        if (player.IsDead)
        {
            row.HpFill.fillAmount = 0f;
            row.HpFill.color = DeadAccentColor;
            row.HpLabel.text = "사망";
            row.HpLabel.color = new Color(0.95f, 0.45f, 0.45f, 1f);
            return;
        }

        var maxHealth = player.MaxHealth > 0f ? player.MaxHealth : CwslGameConstants.PlayerMaxHealth;
        EnsureRowSegments(row, maxHealth);
        var ratio = Mathf.Clamp01(player.CurrentHealth / maxHealth);
        row.HpFill.fillAmount = ratio;
        row.HpFill.color = Color.Lerp(new Color(0.95f, 0.25f, 0.25f), new Color(0.3f, 0.92f, 0.45f), ratio);
        row.HpLabel.text = $"{Mathf.CeilToInt(player.CurrentHealth)} / {maxHealth:0}";
        row.HpLabel.color = MutedTextColor;
    }

    private static void EnsureRowSegments(PartyRowView row, float maxHealth)
    {
        if (row.HpBackRect == null || Mathf.Approximately(row.LastSegmentMaxHealth, maxHealth))
            return;

        CwslHealthBarSegments.BuildUiDividers(row.HpBackRect, maxHealth, row.SegmentDividers);
        row.LastSegmentMaxHealth = maxHealth;
    }

    private PartyRowView GetOrCreateRow(int index)
    {
        while (rows.Count <= index)
            rows.Add(CreateRow(rows.Count));

        return rows[index];
    }

    private PartyRowView CreateRow(int index)
    {
        var rowObject = new GameObject($"PartyRow_{index}", typeof(RectTransform), typeof(Image));
        rowObject.transform.SetParent(rowsRoot, false);
        var rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(0f, RowHeight);

        var rowBackground = rowObject.GetComponent<Image>();
        rowBackground.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);
        rowBackground.raycastTarget = false;

        var accentObject = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accentObject.transform.SetParent(rowObject.transform, false);
        var accentRect = accentObject.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.sizeDelta = new Vector2(4f, -8f);
        accentRect.anchoredPosition = new Vector2(6f, 0f);
        var accent = accentObject.GetComponent<Image>();
        accent.raycastTarget = false;

        var nameLabel = CreateLabel(
            rowObject.transform,
            "Name",
            new Vector2(16f, -8f),
            new Vector2(PanelWidth - 52f, 24f),
            18f);
        nameLabel.fontStyle = FontStyles.Bold;
        nameLabel.alignment = TextAlignmentOptions.MidlineLeft;

        var infoLabel = CreateLabel(
            rowObject.transform,
            "Info",
            new Vector2(16f, -30f),
            new Vector2(PanelWidth - 52f, 20f),
            14f);
        infoLabel.color = MutedTextColor;
        infoLabel.alignment = TextAlignmentOptions.MidlineLeft;

        var hpBack = new GameObject("HpBack", typeof(RectTransform), typeof(Image));
        hpBack.transform.SetParent(rowObject.transform, false);
        var hpBackRect = hpBack.GetComponent<RectTransform>();
        hpBackRect.anchorMin = new Vector2(0f, 0f);
        hpBackRect.anchorMax = new Vector2(1f, 0f);
        hpBackRect.pivot = new Vector2(0.5f, 0f);
        hpBackRect.anchoredPosition = new Vector2(0f, 10f);
        hpBackRect.sizeDelta = new Vector2(-28f, 10f);
        hpBack.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.16f, 1f);
        hpBack.GetComponent<Image>().raycastTarget = false;

        var hpFillObject = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
        hpFillObject.transform.SetParent(hpBack.transform, false);
        var hpFillRect = hpFillObject.GetComponent<RectTransform>();
        hpFillRect.anchorMin = Vector2.zero;
        hpFillRect.anchorMax = Vector2.one;
        hpFillRect.offsetMin = Vector2.zero;
        hpFillRect.offsetMax = Vector2.zero;
        var hpFill = hpFillObject.GetComponent<Image>();
        CwslUiSpriteUtil.ConfigureHorizontalFill(hpFill, new Color(0.3f, 0.92f, 0.45f, 1f));

        var hpLabel = CreateLabel(
            rowObject.transform,
            "HpLabel",
            new Vector2(16f, -50f),
            new Vector2(PanelWidth - 52f, 18f),
            13f);
        hpLabel.alignment = TextAlignmentOptions.MidlineRight;

        return new PartyRowView
        {
            Root = rowObject,
            Accent = accent,
            NameLabel = nameLabel,
            InfoLabel = infoLabel,
            HpBackRect = hpBackRect,
            HpFill = hpFill,
            HpLabel = hpLabel
        };
    }

    private static RectTransform CreateChildRect(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        var rect = child.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        return rect;
    }

    private static TextMeshProUGUI CreateLabel(
        Transform parent,
        string name,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize)
    {
        var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        var rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var label = labelObject.GetComponent<TextMeshProUGUI>();
        CwslTmpFontUtil.ApplyFont(label);
        label.fontSize = fontSize;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private struct PartyPlayerSnapshot
    {
        public ulong ClientId;
        public bool IsLocal;
        public string DisplayName;
        public CwslCharacterId CharacterId;
        public float CurrentHealth;
        public float MaxHealth;
        public bool IsAlive;
        public bool IsDead;
    }
}
