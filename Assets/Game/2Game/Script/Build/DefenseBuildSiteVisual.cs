using UnityEngine;

/// <summary>
/// 건설 중인 위치에 고스트 모형을 표시하고 진행률에 따라 커집니다.
/// </summary>
public class DefenseBuildSiteVisual : MonoBehaviour
{
    private GameObject visualRoot;

    public static DefenseBuildSiteVisual Create(
        Vector3 worldPosition,
        DefenseBuildJob job,
        float cellSize)
    {
        var root = new GameObject("BuildSiteVisual");
        var visual = root.AddComponent<DefenseBuildSiteVisual>();
        visual.Initialize(worldPosition, job, cellSize);
        return visual;
    }

    private void Initialize(Vector3 worldPosition, DefenseBuildJob job, float cellSize)
    {
        bool isWall = job != null && job.IsWall;
        int towerSheetId = job != null ? job.TowerSheetId : 0;

        visualRoot = new GameObject("Scaffold");
        visualRoot.transform.SetParent(transform, false);

        float y = isWall ? 0.42f : 0f;
        visualRoot.transform.position = worldPosition + Vector3.up * y;

        if (isWall)
            DefenseBuildGhostVisual.BuildWall(visualRoot.transform, cellSize);
        else if (towerSheetId > 0)
            DefenseBuildGhostVisual.BuildTower(visualRoot.transform, towerSheetId);

        if (isWall)
        {
            DefenseBuildGhostVisual.ApplyWallGhostTint(
                visualRoot,
                DefenseBuildCatalog.GetPreviewColor(DefenseBuildType.Wall),
                true);
        }
        else
        {
            DefenseBuildGhostVisual.ApplyGhostTint(visualRoot, true);
        }
        SetProgress(0.08f);
    }

    public void SetProgress(float normalized)
    {
        if (visualRoot == null)
            return;

        normalized = Mathf.Clamp01(normalized);
        visualRoot.transform.localScale = Vector3.one * Mathf.Lerp(0.15f, 1f, normalized);
    }

    public void DestroyVisual()
    {
        if (gameObject != null)
            Destroy(gameObject);
    }
}
