using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CwslMinimap : MonoBehaviour
{
    private const float MinimapSize = 240f;
    private const float DotRefreshInterval = 0.05f;
    private const int MaxMonsterDots = 96;
    private const int MaxGoldDots = 64;

    private static readonly Color PanelColor = new(0.05f, 0.06f, 0.09f, 0.86f);
    private static readonly Color BorderColor = new(0.18f, 0.22f, 0.3f, 1f);
    private static readonly Color GridColor = new(0.2f, 0.24f, 0.3f, 0.55f);
    private static readonly Color LocalPlayerColor = new(0.35f, 1f, 0.4f);
    private static readonly Color OtherPlayerColor = new(0.4f, 0.75f, 1f);
    private static readonly Color GoldColor = new(1f, 0.85f, 0.25f);

    private RectTransform iconRoot;
    private RectTransform localPlayerIcon;
    private readonly List<RectTransform> playerIcons = new();
    private readonly List<RectTransform> monsterIcons = new();
    private readonly List<RectTransform> goldIcons = new();
    private Transform localPlayerTransform;
    private float dotRefreshTimer;

    public void SetLocalPlayer(Transform localPlayer)
    {
        localPlayerTransform = localPlayer;
    }

    private void Awake()
    {
        BuildPanel();
    }

    private void Update()
    {
        dotRefreshTimer -= Time.deltaTime;
        if (dotRefreshTimer > 0f)
            return;

        dotRefreshTimer = DotRefreshInterval;
        RefreshDots();
    }

    private void BuildPanel()
    {
        var rect = gameObject.GetComponent<RectTransform>();
        if (rect == null)
            rect = gameObject.AddComponent<RectTransform>();

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, -24f);
        rect.sizeDelta = new Vector2(MinimapSize, MinimapSize);

        var background = gameObject.GetComponent<Image>();
        if (background == null)
            background = gameObject.AddComponent<Image>();
        SetupSlicedImage(background, PanelColor);
        background.raycastTarget = false;

        var border = CreateChildRect("Border", rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        border.offsetMin = Vector2.zero;
        border.offsetMax = Vector2.zero;
        var borderImage = border.gameObject.AddComponent<Image>();
        SetupSlicedImage(borderImage, BorderColor);
        borderImage.raycastTarget = false;
        borderImage.type = Image.Type.Sliced;

        var inner = CreateChildRect("Content", rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        inner.offsetMin = new Vector2(6f, 6f);
        inner.offsetMax = new Vector2(-6f, -6f);

        BuildGrid(inner);

        iconRoot = CreateChildRect("Icons", inner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        iconRoot.offsetMin = Vector2.zero;
        iconRoot.offsetMax = Vector2.zero;

        var label = CreateLabel(rect, "미니맵", new Vector2(-12f, -6f), new Vector2(120f, 22f));
        label.alignment = TextAlignmentOptions.Right;
        label.color = new Color(0.85f, 0.9f, 1f, 0.85f);

        localPlayerIcon = CreateIcon(iconRoot, LocalPlayerColor, 12f, "LocalPlayer");
    }

    private void BuildGrid(RectTransform parent)
    {
        var hLine = CreateChildRect("GridH", parent, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f));
        hLine.sizeDelta = new Vector2(0f, 1f);
        var hImage = hLine.gameObject.AddComponent<Image>();
        SetupSlicedImage(hImage, GridColor);
        hImage.raycastTarget = false;

        var vLine = CreateChildRect("GridV", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f));
        vLine.sizeDelta = new Vector2(1f, 0f);
        var vImage = vLine.gameObject.AddComponent<Image>();
        SetupSlicedImage(vImage, GridColor);
        vImage.raycastTarget = false;
    }

    private void RefreshDots()
    {
        EnsureLocalPlayerReference();

        if (localPlayerIcon != null && localPlayerTransform != null)
        {
            localPlayerIcon.gameObject.SetActive(true);
            localPlayerIcon.anchoredPosition = WorldToMinimap(localPlayerTransform.position);
            var localImage = localPlayerIcon.GetComponent<Image>();
            if (localImage != null)
            {
                localImage.color = CwslBossWatchHud.IsLocalPlayerWatched
                    ? new Color(1f, 0.2f, 0.2f)
                    : LocalPlayerColor;
            }
        }
        else if (localPlayerIcon != null)
        {
            localPlayerIcon.gameObject.SetActive(false);
        }

        UpdatePlayerDots();
        UpdateMonsterDots();
        UpdateGoldDots();
    }

    private void UpdatePlayerDots()
    {
        var index = 0;
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening)
        {
            HideRemaining(playerIcons, 0);
            return;
        }

        foreach (var clientId in networkManager.ConnectedClientsIds)
        {
            if (!networkManager.ConnectedClients.TryGetValue(clientId, out var client))
                continue;

            var playerObject = client.PlayerObject;
            if (playerObject == null || !playerObject.IsSpawned)
                continue;

            if (playerObject.IsOwner)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health == null || !health.IsAlive)
                continue;

            var icon = GetOrCreateIcon(playerIcons, index, OtherPlayerColor, 10f, "OtherPlayer");
            var watchedId = CwslBossWatchState.Instance != null
                ? CwslBossWatchState.Instance.WatchedClientId
                : ulong.MaxValue;
            var iconImage = icon.GetComponent<Image>();
            if (iconImage != null && playerObject.OwnerClientId == watchedId
                && CwslBossWatchState.IsWatching(watchedId))
                iconImage.color = new Color(1f, 0.25f, 0.25f);
            else if (iconImage != null)
                iconImage.color = OtherPlayerColor;

            icon.anchoredPosition = WorldToMinimap(playerObject.transform.position);
            index++;
        }

        HideRemaining(playerIcons, index);
    }

    private void EnsureLocalPlayerReference()
    {
        if (localPlayerTransform != null)
            return;

        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || networkManager.LocalClient == null)
            return;

        localPlayerTransform = networkManager.LocalClient.PlayerObject != null
            ? networkManager.LocalClient.PlayerObject.transform
            : null;
    }

    private void UpdateMonsterDots()
    {
        var index = 0;
        var filterByVision = ShouldFilterMinimapByWorldVision();
        var monsters = FindObjectsByType<CwslMonsterBase>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
        {
            if (monster == null || index >= MaxMonsterDots)
                break;

            var monsterHealth = monster.GetComponent<CwslMonsterHealth>();
            if (monsterHealth != null && !monsterHealth.IsAlive)
                continue;
            if (filterByVision && !CwslPlayerVision.IsInLocalVision(monster.transform.position))
                continue;

            var (color, size) = GetMonsterStyle(monster.MonsterType);
            var icon = GetOrCreateIcon(monsterIcons, index, color, size, $"Monster_{monster.MonsterType}");
            var iconImage = icon.GetComponent<Image>();
            if (iconImage != null)
                iconImage.color = color;
            icon.sizeDelta = new Vector2(size, size);
            icon.anchoredPosition = WorldToMinimap(monster.transform.position);
            index++;
        }

        HideRemaining(monsterIcons, index);
    }

    private void UpdateGoldDots()
    {
        var index = 0;
        var filterByVision = ShouldFilterMinimapByWorldVision();
        var pickups = FindObjectsByType<CwslGoldPickup>(FindObjectsSortMode.None);
        foreach (var pickup in pickups)
        {
            if (pickup == null || index >= MaxGoldDots)
                break;
            if (filterByVision && !CwslPlayerVision.IsInLocalVision(pickup.transform.position))
                continue;

            var icon = GetOrCreateIcon(goldIcons, index, GoldColor, 5f, "Gold");
            icon.anchoredPosition = WorldToMinimap(pickup.transform.position);
            index++;
        }

        HideRemaining(goldIcons, index);
    }

    private static bool ShouldFilterMinimapByWorldVision()
    {
        if (CwslPlayerVision.Local == null)
            return true;

        if (CwslPlayerVision.Local.IsAbsoluteBlindVision)
            return true;

        // 시야 0 캐릭터는 월드는 어둡지만, 미니맵은 UI 정보로 전체 표시
        return !CwslPlayerVision.Local.IsBlindVision;
    }

    private static (Color color, float size) GetMonsterStyle(CwslMonsterType type)
    {
        return type switch
        {
            CwslMonsterType.Ranged => (new Color(0.7f, 0.35f, 1f), 7f),
            CwslMonsterType.Suicide => (new Color(1f, 0.55f, 0.2f), 7f),
            CwslMonsterType.Melee => (new Color(0.9f, 0.35f, 0.4f), 7f),
            CwslMonsterType.KoreaUniversitySoldier => (new Color(0.92f, 0.2f, 0.24f), 8f),
            CwslMonsterType.StickySuicide => (new Color(0.98f, 0.42f, 0.18f), 8f),
            CwslMonsterType.BossHongmyeongbo => (new Color(1f, 0.15f, 0.15f), 42f),
            _ => (new Color(0.9f, 0.9f, 0.9f), 6f)
        };
    }

    private RectTransform GetOrCreateIcon(List<RectTransform> list, int index, Color color, float size, string name)
    {
        while (list.Count <= index)
            list.Add(CreateIcon(iconRoot, color, size, $"{name}_{list.Count}"));

        var icon = list[index];
        icon.gameObject.SetActive(true);
        icon.sizeDelta = new Vector2(size, size);
        var image = icon.GetComponent<Image>();
        if (image != null)
            image.color = color;
        return icon;
    }

    private static void HideRemaining(List<RectTransform> list, int fromIndex)
    {
        for (var i = fromIndex; i < list.Count; i++)
        {
            if (list[i] != null)
                list[i].gameObject.SetActive(false);
        }
    }

    private static RectTransform CreateIcon(RectTransform parent, Color color, float size, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);

        var image = go.GetComponent<Image>();
        image.color = color;
        image.sprite = CwslUiSpriteUtil.WhiteSprite;
        image.type = Image.Type.Simple;
        image.raycastTarget = false;

        return rect;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string text, Vector2 anchoredPosition, Vector2 size)
    {
        var go = new GameObject("MinimapLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var label = go.GetComponent<TextMeshProUGUI>();
        CwslTmpFontUtil.ApplyFont(label);
        label.text = text;
        label.fontSize = 14f;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform CreateChildRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        return rect;
    }

    private static Vector2 WorldToMinimap(Vector3 worldPosition)
    {
        var extent = CwslArenaUtility.GetPlayHalfExtent();
        var half = MinimapSize * 0.5f - 12f;
        var x = Mathf.Clamp(worldPosition.x / extent, -1f, 1f) * half;
        var y = Mathf.Clamp(worldPosition.z / extent, -1f, 1f) * half;
        return new Vector2(x, y);
    }

    private static void SetupSlicedImage(Image image, Color color)
    {
        image.sprite = CwslUiSpriteUtil.WhiteSprite;
        image.type = Image.Type.Simple;
        image.color = color;
    }
}
