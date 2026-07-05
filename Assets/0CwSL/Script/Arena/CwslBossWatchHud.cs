using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>홍명보 감시 UI + 미니맵 표시.</summary>
public static class CwslBossWatchHud
{
    private static RectTransform bannerRoot;
    private static TextMeshProUGUI bannerLabel;
    private static float localHideTime;
    private static ulong watchedClientId = ulong.MaxValue;

    public static bool IsLocalPlayerWatched =>
        NetworkManager.Singleton != null
        && watchedClientId == NetworkManager.Singleton.LocalClientId
        && Time.time < localHideTime;

    public static void NotifyWatchStarted(ulong clientId, float duration)
    {
        EnsureBanner();
        watchedClientId = clientId;
        localHideTime = Time.time + duration;

        var isLocal = NetworkManager.Singleton != null
                      && clientId == NetworkManager.Singleton.LocalClientId;
        bannerLabel.text = isLocal
            ? "홍명보의 감시 — 스킬 사용 불가"
            : $"홍명보가 아군 #{clientId} 감시 중";
        bannerRoot.gameObject.SetActive(true);
        CwslArenaGimmickVisuals.SetWatchTarget(clientId, duration);
    }

    public static void NotifyWatchEnded()
    {
        watchedClientId = ulong.MaxValue;
        localHideTime = 0f;
        if (bannerRoot != null)
            bannerRoot.gameObject.SetActive(false);
        CwslArenaGimmickVisuals.ClearWatchTarget();
    }

    private static void EnsureBanner()
    {
        if (bannerRoot != null)
            return;

        var canvasObject = new GameObject("CwslBossWatchHud");
        Object.DontDestroyOnLoad(canvasObject);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CwslGameConstants.HudCanvasSortOrder + 5;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        bannerRoot = new GameObject("WatchBanner", typeof(RectTransform)).GetComponent<RectTransform>();
        bannerRoot.SetParent(canvasObject.transform, false);
        bannerRoot.anchorMin = new Vector2(0.5f, 1f);
        bannerRoot.anchorMax = new Vector2(0.5f, 1f);
        bannerRoot.pivot = new Vector2(0.5f, 1f);
        bannerRoot.anchoredPosition = new Vector2(0f, -12f);
        bannerRoot.sizeDelta = new Vector2(720f, 52f);

        var bg = bannerRoot.gameObject.AddComponent<Image>();
        bg.color = new Color(0.55f, 0.08f, 0.08f, 0.88f);

        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(bannerRoot, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 4f);
        labelRect.offsetMax = new Vector2(-12f, -4f);
        bannerLabel = labelObject.AddComponent<TextMeshProUGUI>();
        bannerLabel.fontSize = 24f;
        bannerLabel.alignment = TextAlignmentOptions.Center;
        bannerLabel.color = new Color(1f, 0.92f, 0.75f);
        bannerLabel.text = "홍명보의 감시";
        bannerRoot.gameObject.SetActive(false);
    }
}
