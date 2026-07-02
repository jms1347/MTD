using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 마우스 휠 줌·드래그 팬 입력을 받아 DefenseIsometricCamera를 제어합니다.
/// </summary>
public class DefenseCameraControlManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private DefenseIsometricCamera isoCamera;

    [Header("줌 (마우스 휠)")]
    [SerializeField] private float zoomScrollSpeed = 1.8f;

    [Header("팬 (마우스 드래그)")]
    [SerializeField] private int panMouseButton = 0;
    [SerializeField] private float panSpeed = 0.045f;
    [SerializeField] private float panDragThresholdPixels = 8f;
    [SerializeField] private bool blockPanOverUi = true;
    [SerializeField] private bool blockZoomOverUi = true;

    public static bool IsPanning { get; private set; }
    public static bool DidPanThisGesture { get; private set; }

    private bool isPanning;
    private bool panGestureActive;
    private Vector2 panStartScreen;
    private Vector3 lastPanWorldPoint;
    private bool hasLastPanPoint;

    private void Awake()
    {
        if (isoCamera == null)
            isoCamera = GetComponent<DefenseIsometricCamera>();
    }

    private void Update()
    {
        if (isoCamera == null)
            return;

        HandleZoom();
        HandlePan();
    }

    private void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f)
            return;

        if (blockZoomOverUi && IsPointerOverUi())
            return;

        isoCamera.AdjustZoom(scroll * zoomScrollSpeed);
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
            float dragDistance = Vector2.Distance(panStartScreen, Input.mousePosition);
            if (dragDistance < panDragThresholdPixels)
                return;

            isPanning = true;
            IsPanning = true;
            DidPanThisGesture = true;
        }

        if (!TryGetGroundPointUnderMouse(out Vector3 currentPanPoint))
            return;

        if (!hasLastPanPoint)
        {
            lastPanWorldPoint = currentPanPoint;
            hasLastPanPoint = true;
            return;
        }

        Vector3 dragDelta = lastPanWorldPoint - currentPanPoint;
        dragDelta.y = 0f;
        isoCamera.AddPanOffset(dragDelta);
        lastPanWorldPoint = currentPanPoint;
    }

    private bool TryGetGroundPointUnderMouse(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;

        var cam = Camera.main;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        var groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (!groundPlane.Raycast(ray, out float distance))
            return false;

        worldPoint = ray.GetPoint(distance);
        return true;
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
