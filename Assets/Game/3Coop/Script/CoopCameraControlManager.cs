using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 협동 모드 카메라 — 드래그 팬만 지원 (줌 없음).
/// </summary>
public class CoopCameraControlManager : MonoBehaviour
{
    [SerializeField] private DefenseIsometricCamera isoCamera;
    [SerializeField] private int panMouseButton = 0;
    [SerializeField] private float panDragThresholdPixels = 8f;
    [SerializeField] private bool blockPanOverUi = true;

    private bool isPanning;
    private bool panGestureActive;
    private Vector2 panStartScreen;
    private Vector3 lastPanWorldPoint;
    private bool hasLastPanPoint;

    public static bool IsPanning { get; private set; }
    public static bool DidPanThisGesture { get; private set; }

    private void Awake()
    {
        if (isoCamera == null)
            isoCamera = GetComponent<DefenseIsometricCamera>();
    }

    private void Update()
    {
        if (isoCamera == null)
            return;

        HandlePan();
    }

    private void HandlePan()
    {
        if (Input.GetMouseButtonDown(panMouseButton))
        {
            if (blockPanOverUi && IsPointerOverUi())
                return;

            panGestureActive = true;
            DidPanThisGesture = false;
            isPanning = false;
            IsPanning = false;
            panStartScreen = Input.mousePosition;
            hasLastPanPoint = TryGetGroundPointUnderMouse(out lastPanWorldPoint);
            return;
        }

        if (Input.GetMouseButtonUp(panMouseButton))
        {
            panGestureActive = false;
            isPanning = false;
            IsPanning = false;
            hasLastPanPoint = false;
            return;
        }

        if (!panGestureActive || !Input.GetMouseButton(panMouseButton))
            return;

        if (!isPanning)
        {
            if (Vector2.Distance(panStartScreen, Input.mousePosition) < panDragThresholdPixels)
                return;

            isPanning = true;
            IsPanning = true;
            DidPanThisGesture = true;
        }

        if (!TryGetGroundPointUnderMouse(out var currentPanPoint))
            return;

        if (!hasLastPanPoint)
        {
            lastPanWorldPoint = currentPanPoint;
            hasLastPanPoint = true;
            return;
        }

        var dragDelta = lastPanWorldPoint - currentPanPoint;
        dragDelta.y = 0f;
        isoCamera.AddPanOffset(dragDelta);
        lastPanWorldPoint = currentPanPoint;
    }

    private static bool TryGetGroundPointUnderMouse(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        var cam = Camera.main;
        if (cam == null)
            return false;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        var groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (!groundPlane.Raycast(ray, out var distance))
            return false;

        worldPoint = ray.GetPoint(distance);
        return true;
    }

    private static bool IsPointerOverUi()
        => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}
