using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 협동 모드 우측 상단 미니맵 — 동기화된 플레이어·적 위치 표시.
/// </summary>
public class CoopMinimapUI : MonoBehaviour
{
    private CoopGameSession session;
    private RectTransform iconContainer;
    private Vector3 mapCenter = Vector3.zero;
    private float mapHalfExtent = 36f;

    private readonly Dictionary<string, Image> playerIcons = new();
    private readonly List<Image> enemyIconPool = new();
    private Image localPlayerIcon;

    private void Start()
    {
        session = CoopGameSession.Instance;
        BuildMinimap();
        ConfigureMapBounds();

        if (session == null)
            return;

        session.OnStateUpdated += Refresh;
        if (session.LatestState != null)
            Refresh(session.LatestState);
    }

    private void OnDestroy()
    {
        if (session != null)
            session.OnStateUpdated -= Refresh;
    }

    private void ConfigureMapBounds()
    {
        if (CoopMapBootstrap.Instance != null)
        {
            mapCenter = CoopMapBootstrap.Instance.MapCenter;
            mapHalfExtent = Mathf.Max(12f, CoopMapBootstrap.Instance.MapHalfExtent);
        }
    }

    private void BuildMinimap()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObject = new GameObject("CoopMinimapCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        var panel = new GameObject("CoopMinimap", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-16f, -70f);
        panelRect.sizeDelta = new Vector2(220f, 220f);

        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.08f, 0.12f, 0.82f);
        panelImage.raycastTarget = false;

        var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(panel.transform, false);
        var borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        border.GetComponent<Image>().color = new Color(0.35f, 0.75f, 1f, 0.35f);
        border.GetComponent<Image>().raycastTarget = false;

        var containerObject = new GameObject("IconContainer", typeof(RectTransform));
        containerObject.transform.SetParent(panel.transform, false);
        iconContainer = containerObject.GetComponent<RectTransform>();
        iconContainer.anchorMin = new Vector2(0.06f, 0.06f);
        iconContainer.anchorMax = new Vector2(0.94f, 0.94f);
        iconContainer.offsetMin = Vector2.zero;
        iconContainer.offsetMax = Vector2.zero;

        var title = new GameObject("Title", typeof(RectTransform), typeof(Text));
        title.transform.SetParent(panel.transform, false);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 0f);
        titleRect.anchoredPosition = new Vector2(0f, 2f);
        titleRect.sizeDelta = new Vector2(0f, 18f);
        var titleText = title.GetComponent<Text>();
        titleText.text = "미니맵";
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 13;
        titleText.color = new Color(0.85f, 0.92f, 1f, 0.95f);
        titleText.raycastTarget = false;
    }

    private void Refresh(CoopSyncPayload state)
    {
        if (state == null || iconContainer == null)
            return;

        ConfigureMapBounds();
        RefreshPlayers(state);
        RefreshEnemies(state);
    }

    private void RefreshPlayers(CoopSyncPayload state)
    {
        var activeIds = new HashSet<string>();
        if (state.players == null)
            return;

        var localId = session != null ? session.LocalPlayerId : string.Empty;
        for (var i = 0; i < state.players.Length; i++)
        {
            var player = state.players[i];
            if (player == null || string.IsNullOrEmpty(player.playerId))
                continue;

            activeIds.Add(player.playerId);
            var isLocal = player.playerId == localId;
            var icon = GetOrCreatePlayerIcon(player.playerId, isLocal);
            icon.gameObject.SetActive(true);
            SetIconWorldPosition(icon.rectTransform, new Vector3(player.towerX, 0f, player.towerZ));
        }

        var remove = new List<string>();
        foreach (var pair in playerIcons)
        {
            if (activeIds.Contains(pair.Key))
                continue;
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
            remove.Add(pair.Key);
        }

        foreach (var id in remove)
            playerIcons.Remove(id);
    }

    private void RefreshEnemies(CoopSyncPayload state)
    {
        var activeCount = 0;
        if (state.enemies != null)
        {
            for (var i = 0; i < state.enemies.Length; i++)
            {
                var enemy = state.enemies[i];
                if (enemy.hp <= 0f)
                    continue;

                var icon = GetOrCreateEnemyIcon(activeCount, enemy);
                icon.gameObject.SetActive(true);
                SetIconWorldPosition(icon.rectTransform, new Vector3(enemy.x, 0f, enemy.z));
                activeCount++;
            }
        }

        for (var i = activeCount; i < enemyIconPool.Count; i++)
            enemyIconPool[i].gameObject.SetActive(false);
    }

    private Image GetOrCreatePlayerIcon(string playerId, bool isLocal)
    {
        if (playerIcons.TryGetValue(playerId, out var existing) && existing != null)
            return existing;

        var color = isLocal
            ? new Color(0.3f, 1f, 0.45f, 1f)
            : new Color(0.35f, 0.85f, 1f, 1f);
        var icon = CreateDotIcon($"Player_{playerId}", color, isLocal ? 12f : 10f);
        playerIcons[playerId] = icon;
        if (isLocal)
            localPlayerIcon = icon;
        return icon;
    }

    private Image GetOrCreateEnemyIcon(int index, CoopEnemyState enemy)
    {
        while (enemyIconPool.Count <= index)
        {
            var icon = CreateDotIcon($"Enemy_{enemyIconPool.Count}", Color.red, 6f);
            enemyIconPool.Add(icon);
        }

        var image = enemyIconPool[index];
        image.color = ResolveEnemyColor(enemy);
        var size = ResolveEnemySize(enemy);
        image.rectTransform.sizeDelta = new Vector2(size, size);
        return image;
    }

    private static Color ResolveEnemyColor(CoopEnemyState enemy)
    {
        if (enemy.isBoss)
            return new Color(0.85f, 0.2f, 1f, 1f);

        if (!CoopEnemyArchetypeUtil.TryParse(enemy.archetype, out var archetype))
            return new Color(1f, 0.25f, 0.2f, 1f);

        return archetype switch
        {
            CoopEnemyArchetype.Rusher => new Color(1f, 0.55f, 0.15f, 1f),
            CoopEnemyArchetype.Tank => new Color(0.75f, 0.3f, 0.3f, 1f),
            CoopEnemyArchetype.Bomber => new Color(1f, 0.7f, 0.1f, 1f),
            CoopEnemyArchetype.Missile => new Color(1f, 0.35f, 0.1f, 1f),
            CoopEnemyArchetype.HeavyBomber => new Color(1f, 0.15f, 0.05f, 1f),
            _ => new Color(1f, 0.25f, 0.2f, 1f)
        };
    }

    private static float ResolveEnemySize(CoopEnemyState enemy)
    {
        if (enemy.isBoss)
            return 10f;

        if (!CoopEnemyArchetypeUtil.TryParse(enemy.archetype, out var archetype))
            return 6f;

        return archetype switch
        {
            CoopEnemyArchetype.Tank => 8f,
            CoopEnemyArchetype.Missile => 5f,
            CoopEnemyArchetype.HeavyBomber => 8f,
            CoopEnemyArchetype.Rusher => 5.5f,
            _ => 6f
        };
    }

    private void SetIconWorldPosition(RectTransform iconRect, Vector3 worldPosition)
    {
        var normalizedX = (worldPosition.x - mapCenter.x) / mapHalfExtent * 0.5f + 0.5f;
        var normalizedZ = (worldPosition.z - mapCenter.z) / mapHalfExtent * 0.5f + 0.5f;
        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedZ = Mathf.Clamp01(normalizedZ);

        iconRect.anchorMin = new Vector2(normalizedX, normalizedZ);
        iconRect.anchorMax = new Vector2(normalizedX, normalizedZ);
        iconRect.anchoredPosition = Vector2.zero;
    }

    private Image CreateDotIcon(string name, Color color, float size)
    {
        var iconObject = new GameObject(name, typeof(RectTransform));
        iconObject.transform.SetParent(iconContainer, false);

        var rect = iconObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var image = iconObject.AddComponent<Image>();
        image.sprite = DefenseUISprites.White;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
