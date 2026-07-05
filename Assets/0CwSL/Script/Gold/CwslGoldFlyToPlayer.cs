using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class CwslGoldFlyToPlayer
{
    private const int FlyCanvasSortOrder = 200;

    private static readonly Vector3 PlayerTargetOffset = new(0f, 1.1f, 0f);

    private static RectTransform canvasRect;
    private static readonly Dictionary<ulong, MagnetFlySession> magnetSessions = new();
    private static readonly HashSet<ulong> completedVisualSessions = new();

    private sealed class MagnetFlySession
    {
        public GameObject BurstObject;
    }

    public static void BeginDropSpread(ulong sessionId, Vector3 dropCenter, Vector3 finalWorldPosition)
    {
        if (sessionId == 0)
            return;

        EndMagnetFly(sessionId);
        completedVisualSessions.Remove(sessionId);

        EnsureCanvas();
        if (canvasRect == null)
            return;

        var burstObject = CreateBurstRoot();
        var burst = burstObject.GetComponent<CwslGoldFlyBurst>();
        burst.BeginDropSpreadSession(sessionId, canvasRect, dropCenter, finalWorldPosition);

        magnetSessions[sessionId] = new MagnetFlySession
        {
            BurstObject = burstObject
        };
    }

    public static void Play(Vector3 worldPosition, Transform playerTarget, int amount)
    {
        if (amount <= 0 || playerTarget == null)
            return;

        EnsureCanvas();
        if (canvasRect == null)
            return;

        if (!TryWorldToCanvasLocal(worldPosition + Vector3.up * 0.35f, out var startLocal))
            return;

        SpawnFlyBurst(startLocal, worldPosition, playerTarget, amount, playSound: true);
    }

    public static void BeginMagnetFly(
        ulong sessionId,
        Vector3 worldPosition,
        Transform playerTarget,
        int amount)
    {
        if (playerTarget == null)
            return;

        EndMagnetFly(sessionId);
        completedVisualSessions.Remove(sessionId);

        EnsureCanvas();
        if (canvasRect == null)
            return;

        if (!TryWorldToCanvasLocal(worldPosition + Vector3.up * 0.35f, out var startLocal))
            return;

        var burstObject = CreateBurstRoot();
        var burst = burstObject.GetComponent<CwslGoldFlyBurst>();
        burst.BeginSession(sessionId, canvasRect, startLocal, playerTarget, PlayerTargetOffset, amount);

        magnetSessions[sessionId] = new MagnetFlySession
        {
            BurstObject = burstObject
        };
    }

    public static void SetMagnetFlyTarget(ulong sessionId, Transform playerTarget)
    {
        if (playerTarget == null)
            return;

        if (!magnetSessions.TryGetValue(sessionId, out var session) || session.BurstObject == null)
            return;

        session.BurstObject.GetComponent<CwslGoldFlyBurst>()?.SetTarget(playerTarget);
    }

    public static void EndMagnetFly(ulong sessionId)
    {
        completedVisualSessions.Remove(sessionId);

        if (!magnetSessions.TryGetValue(sessionId, out var session))
            return;

        if (session.BurstObject != null)
            Object.Destroy(session.BurstObject);

        magnetSessions.Remove(sessionId);
    }

    public static void MarkVisualSessionComplete(ulong sessionId)
    {
        completedVisualSessions.Add(sessionId);
        EndMagnetFly(sessionId);
    }

    public static bool TryCompleteMagnetFly(
        ulong sessionId,
        Vector3 worldPosition,
        int amount,
        Transform playerTarget)
    {
        if (completedVisualSessions.Remove(sessionId))
        {
            CwslGoldFeedback.PlayCoinSound(worldPosition);
            return true;
        }

        if (!magnetSessions.TryGetValue(sessionId, out var session))
            return false;

        CwslGoldFeedback.PlayCoinSound(worldPosition);

        if (session.BurstObject == null)
        {
            EndMagnetFly(sessionId);
            return true;
        }

        var burst = session.BurstObject.GetComponent<CwslGoldFlyBurst>();
        if (burst != null)
            burst.CompleteCollect(playerTarget, () => EndMagnetFly(sessionId));
        else
            EndMagnetFly(sessionId);

        return true;
    }

    internal static RectTransform CanvasRect
    {
        get
        {
            EnsureCanvas();
            return canvasRect;
        }
    }

    private static void SpawnFlyBurst(
        Vector2 startLocal,
        Vector3 worldPosition,
        Transform playerTarget,
        int amount,
        bool playSound)
    {
        if (playSound)
            CwslGoldFeedback.PlayCoinSound(worldPosition);

        var burstObject = CreateBurstRoot();
        var burst = burstObject.GetComponent<CwslGoldFlyBurst>();
        burst.BeginOneShot(
            canvasRect,
            startLocal,
            playerTarget,
            PlayerTargetOffset,
            amount,
            () =>
            {
                if (burstObject != null)
                    Object.Destroy(burstObject);
            });
    }

    private static GameObject CreateBurstRoot()
    {
        var burstObject = new GameObject("CwslGoldFlyBurst", typeof(RectTransform), typeof(CwslGoldFlyBurst));
        burstObject.transform.SetParent(canvasRect, false);

        var rect = burstObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.SetAsLastSibling();

        return burstObject;
    }

    private static void EnsureCanvas()
    {
        if (canvasRect != null)
            return;

        var existing = GameObject.Find("CwslGoldFlyCanvas");
        if (existing != null)
        {
            canvasRect = existing.GetComponent<RectTransform>();
            ConfigureOverlayCanvasRect(canvasRect);
            return;
        }

        var canvasObject = new GameObject(
            "CwslGoldFlyCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = FlyCanvasSortOrder;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = canvasObject.GetComponent<RectTransform>();
        ConfigureOverlayCanvasRect(canvasRect);
        Object.DontDestroyOnLoad(canvasObject);
    }

    private static void ConfigureOverlayCanvasRect(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
    }

    internal static bool TryWorldToCanvasLocal(Vector3 worldPosition, out Vector2 localPoint)
    {
        localPoint = default;
        EnsureCanvas();
        if (canvasRect == null)
            return false;

        var camera = ResolveWorldCamera();
        if (camera == null)
            return false;

        var viewport = camera.WorldToViewportPoint(worldPosition);
        if (viewport.z <= 0f)
            return false;

        var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            null,
            out localPoint);
    }

    internal static Camera ResolveWorldCamera()
    {
        if (Camera.main != null)
            return Camera.main;

        var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (var i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].enabled && cameras[i].gameObject.activeInHierarchy)
                return cameras[i];
        }

        return null;
    }
}
