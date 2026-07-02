using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적 웨이브 스폰 방향을 유저에게 미리 알려주는 경고 UI.
/// 쿼터뷰 카메라에 맞게 맵 가장자리(스폰 반경)를 월드→스크린 투영해 표시합니다.
/// StageManager.OnSpawnDirectionWarning 이벤트를 구독해 동작합니다.
/// </summary>
public class SpawnDirectionWarningUI : MonoBehaviour
{
    [Header("점멸 설정")]
    [SerializeField] private Color warningColor = new Color(1f, 0.12f, 0.1f, 0.65f);
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float minAlpha = 0.15f;
    [SerializeField] private float maxAlpha = 0.7f;

    [Header("맵 가장자리 표시")]
    [SerializeField] private float screenEdgeMargin = 48f;
    [SerializeField] private float edgeStripLength = 220f;
    [SerializeField] private float edgeStripThickness = 28f;

    [Header("화살표")]
    [SerializeField] private float arrowSize = 48f;
    [SerializeField] private float arrowOffsetFromEdge = 36f;

    private readonly Dictionary<SpawnDirection, RectTransform> edgeRects = new();
    private readonly Dictionary<SpawnDirection, Image> edgePanels = new();
    private readonly Dictionary<SpawnDirection, RectTransform> arrowRects = new();
    private readonly Dictionary<SpawnDirection, Text> arrowLabels = new();
    private readonly Dictionary<SpawnDirection, Coroutine> activeWarnings = new();
    private Canvas parentCanvas;
    private bool isInitialized;

    public void Initialize()
    {
        if (isInitialized)
            return;

        isInitialized = true;

        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas == null)
        {
            Debug.LogError("[SpawnDirectionWarningUI] 상위 Canvas를 찾을 수 없습니다.");
            return;
        }

        if (edgePanels.Count == 0)
            Build(parentCanvas);
        else
            SubscribeEvents();
    }

    public void Build(Canvas canvas)
    {
        parentCanvas = canvas;

        foreach (SpawnDirection direction in System.Enum.GetValues(typeof(SpawnDirection)))
        {
            CreateEdge(direction);
            CreateArrow(direction);
        }

        SubscribeEvents();
    }

    public void RefreshStageManagerSubscription()
    {
        SubscribeEvents();
    }

    public static void RefreshAllSubscriptions()
    {
        var warnings = UnityEngine.Object.FindObjectsByType<SpawnDirectionWarningUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < warnings.Length; i++)
            warnings[i]?.RefreshStageManagerSubscription();
    }

    private void SubscribeEvents()
    {
        if (StageManager.Instance == null)
            return;

        StageManager.Instance.OnSpawnDirectionWarning -= HandleSpawnWarning;
        StageManager.Instance.OnSpawnDirectionWarning += HandleSpawnWarning;
    }

    private void OnDestroy()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnSpawnDirectionWarning -= HandleSpawnWarning;
    }

    private void HandleSpawnWarning(SpawnDirection direction, float duration)
    {
        if (activeWarnings.TryGetValue(direction, out var running) && running != null)
            StopCoroutine(running);

        activeWarnings[direction] = StartCoroutine(PulseWarning(direction, duration));
    }

    private IEnumerator PulseWarning(SpawnDirection direction, float duration)
    {
        if (!edgePanels.TryGetValue(direction, out var edge) || !arrowLabels.TryGetValue(direction, out var arrow))
            yield break;

        edge.gameObject.SetActive(true);
        arrow.gameObject.SetActive(true);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            UpdateMarkerLayout(direction);

            float t = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

            var c = warningColor;
            c.a = alpha;
            edge.color = c;
            arrow.color = c;

            yield return null;
        }

        edge.gameObject.SetActive(false);
        arrow.gameObject.SetActive(false);
        activeWarnings[direction] = null;
    }

    /// <summary>
    /// 맵 스폰 반경 지점을 카메라로 투영해, 해당 방향 화면 가장자리에 경고를 배치합니다.
    /// </summary>
    private void UpdateMarkerLayout(SpawnDirection direction)
    {
        if (!edgeRects.TryGetValue(direction, out var edgeRect) || !arrowRects.TryGetValue(direction, out var arrowRect))
            return;

        if (!TryProjectSpawnEdgeToCanvas(direction, out var canvasPos, out var screenInward))
            return;

        edgeRect.anchoredPosition = canvasPos;
        edgeRect.localRotation = Quaternion.Euler(0f, 0f, screenInward);

        var inwardDir = new Vector2(
            Mathf.Cos(screenInward * Mathf.Deg2Rad),
            Mathf.Sin(screenInward * Mathf.Deg2Rad));
        arrowRect.anchoredPosition = canvasPos + inwardDir * (edgeStripThickness * 0.5f + arrowOffsetFromEdge);
        arrowRect.localRotation = Quaternion.Euler(0f, 0f, screenInward);
    }

    private bool TryProjectSpawnEdgeToCanvas(SpawnDirection direction, out Vector2 canvasPos, out float screenInwardAngle)
    {
        canvasPos = Vector2.zero;
        screenInwardAngle = 0f;

        var cam = Camera.main;
        var canvasRect = parentCanvas != null ? parentCanvas.transform as RectTransform : null;
        if (cam == null || canvasRect == null)
            return false;

        Vector3 spawnCenter = StageManager.Instance != null ? StageManager.Instance.SpawnCenter : Vector3.zero;
        float spawnRadius = StageManager.Instance != null ? StageManager.Instance.SpawnRadius : 35f;

        Vector3 worldDir = DirectionToWorldVector(direction);
        Vector3 edgeWorld = spawnCenter + worldDir * spawnRadius;

        Vector3 screenEdge = cam.WorldToScreenPoint(edgeWorld);
        Vector3 screenCenter = cam.WorldToScreenPoint(spawnCenter);

        if (screenEdge.z < 0f)
        {
            screenEdge.x = Screen.width - screenEdge.x;
            screenEdge.y = Screen.height - screenEdge.y;
        }

        Vector2 fromCenter = new Vector2(screenEdge.x - screenCenter.x, screenEdge.y - screenCenter.y);
        if (fromCenter.sqrMagnitude < 4f)
            fromCenter = new Vector2(worldDir.x, worldDir.z);

        fromCenter.Normalize();
        Vector2 screenPos = GetScreenEdgePoint(new Vector2(screenCenter.x, screenCenter.y), fromCenter, screenEdgeMargin);

        Camera eventCam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, eventCam, out canvasPos))
            return false;

        screenInwardAngle = Mathf.Atan2(-fromCenter.y, -fromCenter.x) * Mathf.Rad2Deg;
        return true;
    }

    private static Vector2 GetScreenEdgePoint(Vector2 screenCenter, Vector2 direction, float margin)
    {
        direction.Normalize();

        float minX = margin;
        float maxX = Screen.width - margin;
        float minY = margin;
        float maxY = Screen.height - margin;

        float t = float.MaxValue;

        if (direction.x > 0.0001f)
            t = Mathf.Min(t, (maxX - screenCenter.x) / direction.x);
        else if (direction.x < -0.0001f)
            t = Mathf.Min(t, (minX - screenCenter.x) / direction.x);

        if (direction.y > 0.0001f)
            t = Mathf.Min(t, (maxY - screenCenter.y) / direction.y);
        else if (direction.y < -0.0001f)
            t = Mathf.Min(t, (minY - screenCenter.y) / direction.y);

        if (t <= 0f || float.IsInfinity(t))
            return screenCenter;

        return screenCenter + direction * t;
    }

    private static Vector3 DirectionToWorldVector(SpawnDirection direction)
    {
        return direction switch
        {
            SpawnDirection.West => new Vector3(-1f, 0f, 1f).normalized,
            SpawnDirection.East => new Vector3(1f, 0f, -1f).normalized,
            _ => Vector3.forward
        };
    }

    private void CreateEdge(SpawnDirection direction)
    {
        var go = new GameObject($"WarningEdge_{direction}", typeof(RectTransform));
        go.transform.SetParent(parentCanvas.transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(edgeStripLength, edgeStripThickness);

        var image = go.AddComponent<Image>();
        image.sprite = DefenseUISprites.White;
        image.color = new Color(warningColor.r, warningColor.g, warningColor.b, 0f);
        image.raycastTarget = false;
        go.SetActive(false);

        edgeRects[direction] = rect;
        edgePanels[direction] = image;
    }

    private void CreateArrow(SpawnDirection direction)
    {
        var go = new GameObject($"WarningArrow_{direction}", typeof(RectTransform));
        go.transform.SetParent(parentCanvas.transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(arrowSize * 1.5f, arrowSize * 1.5f);

        var text = go.AddComponent<Text>();
        text.text = "\u25BA";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = Mathf.RoundToInt(arrowSize);
        text.alignment = TextAnchor.MiddleCenter;
        text.color = warningColor;
        text.raycastTarget = false;
        go.SetActive(false);

        arrowRects[direction] = rect;
        arrowLabels[direction] = text;
    }
}
