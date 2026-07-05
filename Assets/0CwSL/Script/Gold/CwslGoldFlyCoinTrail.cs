using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 코인 뒤에 남는 짧은 잔상 트레일.
/// </summary>
public class CwslGoldFlyCoinTrail : MonoBehaviour
{
    [SerializeField] private int maxDots = 12;
    [SerializeField] private float spawnInterval = 0.025f;
    [SerializeField] private float dotSize = 8f;
    [SerializeField] private float dotLifetime = 0.28f;
    [SerializeField] private Color trailColor = new(1f, 0.82f, 0.15f, 0.5f);

    private RectTransform coinRect;
    private RectTransform canvasRect;
    private Sprite coinSprite;
    private readonly List<TrailDot> dots = new();
    private float spawnTimer;
    private bool active;

    private struct TrailDot
    {
        public RectTransform Rect;
        public Image Image;
        public float Life;
    }

    public void Begin(RectTransform coin, RectTransform canvas)
    {
        coinRect = coin;
        canvasRect = canvas;
        coinSprite = coin.GetComponent<Image>()?.sprite;
        active = coinRect != null && canvasRect != null;
        spawnTimer = 0f;
    }

    public void Stop()
    {
        active = false;
    }

    private void Update()
    {
        if (!active || coinRect == null || canvasRect == null)
            return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnDot(GetCoinCanvasLocal());
        }

        for (var i = dots.Count - 1; i >= 0; i--)
        {
            var dot = dots[i];
            dot.Life -= Time.deltaTime;
            if (dot.Life <= 0f)
            {
                Destroy(dot.Rect.gameObject);
                dots.RemoveAt(i);
                continue;
            }

            var alpha = Mathf.Clamp01(dot.Life / dotLifetime);
            var color = trailColor;
            color.a *= alpha;
            dot.Image.color = color;
            dot.Rect.localScale = Vector3.one * Mathf.Lerp(0.25f, 0.75f, alpha);
            dots[i] = dot;
        }
    }

    private Vector2 GetCoinCanvasLocal()
    {
        var worldCenter = coinRect.TransformPoint(coinRect.rect.center);
        var screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldCenter);
        var canvas = canvasRect.GetComponent<Canvas>();
        var eventCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            eventCam,
            out var local);
        return local;
    }

    private void SpawnDot(Vector2 canvasLocal)
    {
        var dotObject = new GameObject("CwslGoldFlyTrailDot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dotObject.transform.SetParent(canvasRect, false);

        var rect = dotObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.one * dotSize;
        rect.anchoredPosition = canvasLocal;
        rect.SetSiblingIndex(coinRect.GetSiblingIndex());

        var image = dotObject.GetComponent<Image>();
        image.sprite = coinSprite;
        image.raycastTarget = false;
        image.color = trailColor;

        dots.Add(new TrailDot
        {
            Rect = rect,
            Image = image,
            Life = dotLifetime
        });

        while (dots.Count > maxDots)
        {
            Destroy(dots[0].Rect.gameObject);
            dots.RemoveAt(0);
        }
    }

    private void OnDestroy()
    {
        for (var i = dots.Count - 1; i >= 0; i--)
        {
            if (dots[i].Rect != null)
                Destroy(dots[i].Rect.gameObject);
        }

        dots.Clear();
    }
}
