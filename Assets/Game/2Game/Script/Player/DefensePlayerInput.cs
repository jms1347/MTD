using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 바닥 클릭 이동·농장 드릴 입력을 처리합니다.
/// </summary>
[RequireComponent(typeof(PlayerCharacterController))]
[RequireComponent(typeof(PlayerFarmDrillController))]
[RequireComponent(typeof(PlayerBuildController))]
public class DefensePlayerInput : MonoBehaviour
{
    [SerializeField] private float clickDragThreshold = 8f;

    private PlayerCharacterController player;
    private PlayerFarmDrillController drillController;
    private PlayerBuildController buildController;
    private Vector2 mouseDownScreen;
    private bool isMouseDown;

    private void Awake()
    {
        player = GetComponent<PlayerCharacterController>();
        drillController = GetComponent<PlayerFarmDrillController>();
        buildController = GetComponent<PlayerBuildController>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            buildController?.CancelBuild();
            DefenseBuildManager.Instance?.ClearSelection();
            DefenseTowerRangeRing.Hide();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUi())
                return;

            isMouseDown = true;
            mouseDownScreen = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (!isMouseDown || IsPointerOverUi())
            {
                isMouseDown = false;
                return;
            }

            if (!DefenseCameraControlManager.DidPanThisGesture
                && Vector2.Distance(mouseDownScreen, Input.mousePosition) <= clickDragThreshold)
            {
                HandleClick();
            }

            isMouseDown = false;
        }
    }

    private void HandleClick()
    {
        if (TryPickTower(Input.mousePosition, out Transform tower, out float range))
        {
            DefenseTowerRangeRing.Show(tower, range);
            return;
        }

        var buildManager = DefenseBuildManager.Instance;
        if (buildManager == null || !buildManager.IsBuildMode)
            DefenseTowerRangeRing.Hide();

        if (TryRaycastClick(Input.mousePosition, out RaycastHit hit, QueryTriggerInteraction.Ignore))
            HandleWorldClick(hit);
    }

    private static bool TryPickTower(Vector2 screenPosition, out Transform tower, out float range)
    {
        tower = null;
        range = 0f;

        var cam = Camera.main;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(screenPosition);
        var hits = Physics.RaycastAll(ray, 500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            var root = DefenseTowerCombatRange.ResolveTowerRoot(hits[i].collider);
            if (root == null)
                continue;

            if (DefenseTowerCombatRange.TryGetRange(root, out range))
            {
                tower = root;
                return true;
            }
        }

        return false;
    }

    private void HandleWorldClick(RaycastHit hit)
    {
        if (DefenseBuildManager.Instance != null && DefenseBuildManager.Instance.IsBuildMode)
        {
            if (buildController != null && buildController.HasActiveBuild)
                return;

            if (DefenseStageTimerManager.Instance == null || DefenseStageTimerManager.Instance.CanPlayerBuild())
                DefenseBuildManager.Instance.TryRequestBuild(hit.point);

            return;
        }

        if (buildController != null && buildController.IsWalkingToSite)
        {
            // 자동 이동이 막히는 경우가 있어, 일반 이동 입력은 건설을 취소하고 처리합니다.
            buildController.CancelBuild();
        }

        if (drillController.IsDrilling)
            return;

        if (hit.collider.CompareTag("FarmSoil"))
        {
            var tile = hit.collider.GetComponent<FarmDrillTile>();
            if (tile != null && PlayerFarmDrillController.IsGatherAllowed())
            {
                drillController.RequestDrill(tile);
                return;
            }
        }

        if (hit.collider.CompareTag("Ground")
            || hit.collider.CompareTag("FarmSoil")
            || hit.collider.gameObject.name == "DefenseGround")
        {
            if (DefenseStageTimerManager.Instance != null && !DefenseStageTimerManager.Instance.CanPlayerMove())
                return;

            drillController.CancelDrill();
            if (player.TrySetMoveTarget(hit.point))
                PlayerMoveMarker.Show(hit.point);
        }
    }

    private static bool TryRaycastClick(Vector2 screenPosition, out RaycastHit hit, QueryTriggerInteraction triggerInteraction)
    {
        hit = default;
        var cam = Camera.main;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(screenPosition);
        return Physics.Raycast(ray, out hit, 500f, Physics.DefaultRaycastLayers, triggerInteraction);
    }

    private static bool TryRaycastGround(Vector2 screenPosition, out RaycastHit hit)
    {
        return TryRaycastClick(screenPosition, out hit, QueryTriggerInteraction.Ignore);
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
