using UnityEngine;

/// <summary>
/// 건설 모드일 때 마우스 위치에 선택 건물 고스트(반투명 실루엣)를 표시합니다.
/// </summary>
public class DefenseBuildPreview : MonoBehaviour
{
    private GameObject previewObject;
    private bool previewIsWall;
    private int previewTowerId;
    private bool previewGhostValid = true;

    private void LateUpdate()
    {
        var manager = DefenseBuildManager.Instance;
        if (manager == null || !manager.IsBuildMode || !manager.ShowPlacementPreview || !TryGetGroundPoint(out Vector3 worldPoint))
        {
            Hide();
            return;
        }

        if (!manager.TryGetPlacementPreview(worldPoint, out Vector3 snapped, out bool isValid))
        {
            Hide();
            return;
        }

        bool isWall = manager.SelectedType == DefenseBuildType.Wall;
        int towerId = manager.SelectedTowerSheetId ?? 0;
        EnsurePreview(isWall, towerId);

        float y = isWall ? 0.42f : 0f;
        previewObject.transform.position = snapped + Vector3.up * y;
        previewObject.transform.rotation = Quaternion.identity;

        if (previewGhostValid != isValid)
        {
            if (isWall)
            {
                DefenseBuildGhostVisual.ApplyWallGhostTint(
                    previewObject,
                    DefenseBuildCatalog.GetPreviewColor(DefenseBuildType.Wall),
                    isValid);
            }
            else
            {
                DefenseBuildGhostVisual.ApplyGhostTint(previewObject, isValid);
            }

            previewGhostValid = isValid;
        }

        previewObject.SetActive(true);
        UpdateRangeRing(isWall, towerId);
    }

    private void UpdateRangeRing(bool isWall, int towerSheetId)
    {
        if (isWall || previewObject == null || !DefenseTowerSheetTable.TryGetAttackRange(towerSheetId, out float attackRange))
        {
            DefenseTowerRangeRing.Hide();
            return;
        }

        DefenseTowerRangeRing.Show(previewObject.transform, attackRange);
    }

    private void EnsurePreview(bool isWall, int towerSheetId)
    {
        if (previewObject != null && previewIsWall == isWall && previewTowerId == towerSheetId)
            return;

        if (previewObject != null)
            Destroy(previewObject);

        previewIsWall = isWall;
        previewTowerId = towerSheetId;
        previewGhostValid = true;
        previewObject = new GameObject("BuildPreview");
        previewObject.transform.SetParent(transform, false);

        if (isWall)
        {
            float cell = DefenseBuildManager.Instance != null
                ? DefenseBuildManager.Instance.CellSize
                : 1f;
            DefenseBuildGhostVisual.BuildWall(previewObject.transform, cell);
            DefenseBuildGhostVisual.ApplyWallGhostTint(
                previewObject,
                DefenseBuildCatalog.GetPreviewColor(DefenseBuildType.Wall),
                true);
        }
        else if (towerSheetId > 0)
        {
            DefenseBuildGhostVisual.BuildTower(previewObject.transform, towerSheetId);
            DefenseBuildGhostVisual.ApplyGhostTint(previewObject, true);
        }
    }

    private void Hide()
    {
        if (previewObject != null)
            previewObject.SetActive(false);

        DefenseTowerRangeRing.Hide();
    }

    private static bool TryGetGroundPoint(out Vector3 worldPoint)
    {
        worldPoint = default;
        var cam = Camera.main;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;

        if (!hit.collider.CompareTag("Ground")
            && !hit.collider.CompareTag("FarmSoil")
            && hit.collider.gameObject.name != "DefenseGround")
            return false;

        worldPoint = hit.point;
        return true;
    }
}
