using AssetKits.ParticleImage;
using UnityEngine;

public class CwslGoldFlyAttractor : MonoBehaviour
{
    private RectTransform rect;
    private RectTransform canvasRect;
    private Transform worldTarget;
    private Vector3 worldOffset = new(0f, 1.1f, 0f);

    public RectTransform Rect => rect;

    public void Initialize(RectTransform canvas, Transform target, Vector3 offset)
    {
        canvasRect = canvas;
        worldTarget = target;
        worldOffset = offset;
        rect = GetComponent<RectTransform>();
        UpdatePosition();
    }

    public void SetTarget(Transform target)
    {
        worldTarget = target;
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (rect == null || canvasRect == null || worldTarget == null)
            return;

        var canvas = canvasRect.GetComponent<Canvas>();
        var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera ?? Camera.main
            : Camera.main;

        var worldPosition = worldTarget.position + worldOffset;
        var screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, cam, out var localPoint))
            rect.anchoredPosition = localPoint;
    }
}
