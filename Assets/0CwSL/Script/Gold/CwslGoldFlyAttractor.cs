using UnityEngine;

/// <summary>
/// UI 파티클의 종착점 — 월드 플레이어 위치를 캔버스 좌표로 매 프레임 갱신.
/// </summary>
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
        worldTarget = ResolveAttractTarget(target);
        worldOffset = offset;
        rect = GetComponent<RectTransform>();
        RefreshPosition();
    }

    public void SetTarget(Transform target)
    {
        worldTarget = ResolveAttractTarget(target);
        RefreshPosition();
    }

    public void RefreshPosition()
    {
        UpdatePosition();
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    private static Transform ResolveAttractTarget(Transform player) => player;

    private void UpdatePosition()
    {
        if (rect == null || canvasRect == null || worldTarget == null)
            return;

        var worldPosition = worldTarget.position + worldOffset;
        if (!CwslGoldFlyToPlayer.TryWorldToCanvasLocal(worldPosition, out var localPoint))
            return;

        rect.anchoredPosition = localPoint;
    }
}
