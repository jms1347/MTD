using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 골드로 성벽·타워를 배치하는 건설 매니저.
/// </summary>
public class DefenseBuildManager : MonoBehaviour
{
    public static DefenseBuildManager Instance { get; private set; }

    private static readonly Color WallColor = new(0.5f, 0.41f, 0.3f);

    [SerializeField] private DefenseMapLayout mapLayout;
    [SerializeField] private DefenseSceneSetup sceneSetup;

    private readonly HashSet<Vector2Int> occupiedCells = new();
    private readonly HashSet<Vector2Int> reservedCells = new();
    private Transform buildRoot;
    private int placedTowerCount;
    private int placedWallCount;

    public DefenseBuildType? SelectedType { get; private set; }
    public int? SelectedTowerSheetId { get; private set; }
    public bool IsBuildMode => SelectedType.HasValue || SelectedTowerSheetId.HasValue;
    public bool ShowPlacementPreview { get; private set; }
    public float CellSize => mapLayout != null ? mapLayout.cellSize : 1f;

    public bool IsCellBlockedForApproach(Vector2Int cell)
    {
        if (mapLayout == null)
            return false;

        if (!DefenseMapGrid.IsInside(mapLayout, cell))
            return true;

        if (occupiedCells.Contains(cell) || reservedCells.Contains(cell))
            return true;

        var tile = mapLayout.GetTile(cell);
        return tile != DefenseMapTileType.Grass;
    }

    public bool IsCellBlockedForNavigation(Vector2Int cell)
    {
        return occupiedCells.Contains(cell) || reservedCells.Contains(cell);
    }

    public event System.Action OnSelectionChanged;

    private void Awake()
    {
        Instance = this;
        EnsureBuildRoot();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Configure(DefenseMapLayout layout, DefenseSceneSetup setup)
    {
        mapLayout = layout;
        sceneSetup = setup;
    }

    public void SelectBuild(DefenseBuildType? type)
    {
        SelectedType = type;
        SelectedTowerSheetId = null;
        ShowPlacementPreview = type.HasValue;
        NotifySelectionChanged();
    }

    public void SelectTower(int? towerSheetId)
    {
        SelectedTowerSheetId = towerSheetId > 0 ? towerSheetId : null;
        SelectedType = null;
        ShowPlacementPreview = SelectedTowerSheetId.HasValue;
        NotifySelectionChanged();
    }

    public void ClearSelection()
    {
        SelectedType = null;
        SelectedTowerSheetId = null;
        ShowPlacementPreview = false;
        NotifySelectionChanged();
    }

    public bool CanAffordSelection()
    {
        if (DefenseStageTimerManager.Instance != null && !DefenseStageTimerManager.Instance.CanPlayerBuild())
            return false;

        if (GameManager.Instance == null)
            return true;

        if (SelectedTowerSheetId.HasValue)
            return GameManager.Instance.Money >= DefenseBuildCatalog.GetTowerCost(SelectedTowerSheetId.Value);

        if (SelectedType.HasValue)
            return GameManager.Instance.Money >= DefenseBuildCatalog.GetCost(SelectedType.Value);

        return false;
    }

    public bool CanAfford(DefenseBuildType type)
    {
        if (DefenseStageTimerManager.Instance != null && !DefenseStageTimerManager.Instance.CanPlayerBuild())
            return false;

        if (GameManager.Instance == null)
            return true;

        return GameManager.Instance.Money >= DefenseBuildCatalog.GetCost(type);
    }

    public bool CanAffordTower(int towerSheetId)
    {
        if (DefenseStageTimerManager.Instance != null && !DefenseStageTimerManager.Instance.CanPlayerBuild())
            return false;

        if (GameManager.Instance == null)
            return true;

        return GameManager.Instance.Money >= DefenseBuildCatalog.GetTowerCost(towerSheetId);
    }

    public bool TryRequestBuild(Vector3 worldPoint)
    {
        if (!IsBuildMode)
            return false;

        if (DefenseStageTimerManager.Instance != null && !DefenseStageTimerManager.Instance.CanPlayerBuild())
            return false;

        if (!CanAffordSelection())
            return false;

        var buildController = FindFirstObjectByType<PlayerBuildController>();
        if (buildController == null || buildController.HasActiveBuild)
            return false;

        if (!TryResolvePlacement(worldPoint, out Vector3 snapped, out Vector2Int cell, out string rejectReason))
        {
            Debug.Log($"[DefenseBuild] 배치 불가: {rejectReason}");
            return false;
        }

        DefenseBuildJob job = CreateJobForSelection(snapped, cell);
        if (job == null)
            return false;

        ReserveCell(cell);

        if (!buildController.TryStartBuild(job))
        {
            CancelReservedCell(cell);
            return false;
        }

        ClearSelection();
        return true;
    }

    private DefenseBuildJob CreateJobForSelection(Vector3 snapped, Vector2Int cell)
    {
        if (SelectedTowerSheetId.HasValue)
        {
            int towerId = SelectedTowerSheetId.Value;
            return DefenseBuildJob.CreateTower(
                towerId,
                snapped,
                cell,
                DefenseBuildCatalog.GetTowerCost(towerId),
                DefenseBuildCatalog.GetTowerBuildDurationSeconds(towerId));
        }

        if (SelectedType.HasValue)
        {
            var type = SelectedType.Value;
            if (type == DefenseBuildType.Wall)
            {
                return DefenseBuildJob.CreateWall(
                    snapped,
                    cell,
                    DefenseBuildCatalog.GetCost(type),
                    DefenseBuildCatalog.GetBuildDurationSeconds(type));
            }

            int legacySheetId = DefenseTowerSheetTable.GetTowerId(type);
            if (legacySheetId > 0)
            {
                return DefenseBuildJob.CreateTower(
                    legacySheetId,
                    snapped,
                    cell,
                    DefenseBuildCatalog.GetCost(type),
                    DefenseBuildCatalog.GetBuildDurationSeconds(type));
            }

            return new DefenseBuildJob(
                type,
                snapped,
                cell,
                DefenseBuildCatalog.GetCost(type),
                DefenseBuildCatalog.GetBuildDurationSeconds(type));
        }

        return null;
    }

    public void CompleteBuild(DefenseBuildJob job)
    {
        if (job == null)
            return;

        reservedCells.Remove(job.Cell);

        bool placed = job.IsWall
            ? PlaceWall(job.SitePosition, job.Cell)
            : PlaceTower(job, job.SitePosition, job.Cell);

        if (!placed)
        {
            if (job.GoldSpent && GameManager.Instance != null)
                GameManager.Instance.AddMoney(job.GoldCost);

            return;
        }

        occupiedCells.Add(job.Cell);
    }

    public void CancelReservedCell(Vector2Int cell)
    {
        reservedCells.Remove(cell);
    }

    private void ReserveCell(Vector2Int cell)
    {
        reservedCells.Add(cell);
    }

    private void NotifySelectionChanged()
    {
        OnSelectionChanged?.Invoke();
    }

    private bool TryResolvePlacement(
        Vector3 worldPoint,
        out Vector3 snapped,
        out Vector2Int cell,
        out string rejectReason)
    {
        snapped = worldPoint;
        cell = default;
        rejectReason = string.Empty;

        if (mapLayout == null)
        {
            rejectReason = "맵 레이아웃이 없습니다.";
            return false;
        }

        cell = DefenseMapGrid.WorldToCell(mapLayout, worldPoint);
        if (!DefenseMapGrid.IsInside(mapLayout, cell))
        {
            rejectReason = "맵 밖입니다.";
            return false;
        }

        snapped = DefenseMapGrid.CellToWorld(mapLayout, cell);
        snapped.y = 0f;

        if (occupiedCells.Contains(cell) || reservedCells.Contains(cell))
        {
            rejectReason = "이미 건설된 칸입니다.";
            return false;
        }

        var tile = mapLayout.GetTile(cell);
        if (tile != DefenseMapTileType.Grass)
        {
            rejectReason = tile == DefenseMapTileType.Path
                ? "몬스터 이동 경로 위에는 건설할 수 없습니다."
                : "이 칸에는 건설할 수 없습니다.";
            return false;
        }

        if (cell == mapLayout.nexusCell)
        {
            rejectReason = "넥서스 위에는 건설할 수 없습니다.";
            return false;
        }

        if (Physics.CheckBox(
                snapped + Vector3.up * 0.42f,
                new Vector3(mapLayout.cellSize * 0.42f, 0.45f, mapLayout.cellSize * 0.42f),
                Quaternion.identity,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            var overlaps = Physics.OverlapBox(
                snapped + Vector3.up * 0.42f,
                new Vector3(mapLayout.cellSize * 0.42f, 0.45f, mapLayout.cellSize * 0.42f),
                Quaternion.identity,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            foreach (var overlap in overlaps)
            {
                if (overlap == null)
                    continue;

                if (overlap.CompareTag("Ground") || overlap.CompareTag("FarmSoil"))
                    continue;

                rejectReason = "다른 오브젝트와 겹칩니다.";
                return false;
            }
        }

        return true;
    }

    public bool TryGetPlacementPreview(Vector3 worldPoint, out Vector3 snapped, out bool isValid)
    {
        snapped = worldPoint;
        isValid = false;

        if (!IsBuildMode || mapLayout == null)
            return false;

        if (!TryResolvePlacement(worldPoint, out snapped, out _, out _))
            return false;

        isValid = CanAffordSelection();
        return true;
    }

    private bool PlaceWall(Vector3 worldPosition, Vector2Int cell)
    {
        EnsureBuildRoot();

        placedWallCount++;
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = $"PlayerWall_{placedWallCount:D3}";
        wall.tag = "Obstacle";
        wall.transform.SetParent(buildRoot, false);
        wall.transform.position = worldPosition + new Vector3(0f, 0.42f, 0f);
        wall.transform.localScale = new Vector3(mapLayout.cellSize * 0.98f, 0.84f, mapLayout.cellSize * 0.98f);

        var renderer = wall.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = WallColor;
            renderer.material = material;
        }

        wall.AddComponent<DefensePlayerWall>();
        return true;
    }

    private bool PlaceTower(DefenseBuildJob job, Vector3 worldPosition, Vector2Int cell)
    {
        if (TowerManager.Instance == null)
            return false;

        placedTowerCount++;
        TowerSpawnData data;

        if (job.TowerSheetId > 0)
        {
            data = DefenseTowerBuildTable.CreateSpawnData(job.TowerSheetId, placedTowerCount);
        }
        else
        {
            data = CreateLegacyTowerSpawnData(job.LegacyType, placedTowerCount);
        }

        sceneSetup?.ApplyBuildTowerPrefabs(data);
        TowerManager.Instance.PlaceTowerAtWorld(data, worldPosition);
        RoguelikeRunEvents.NotifyTowerBuilt();
        return true;
    }

    private static TowerSpawnData CreateLegacyTowerSpawnData(DefenseBuildType buildType, int index)
    {
        return buildType switch
        {
            DefenseBuildType.MeteorTower => new TowerSpawnData
            {
                towerName = $"Player_Meteor_{index:D3}",
                kind = TowerKind.Meteor,
                towerSheetId = DefenseTowerSheetTable.FlameMortarTowerId,
                color = new Color(0.92f, 0.28f, 0.08f),
                scaleMultiplier = new Vector3(1.35f, 1.35f, 1.35f)
            },
            DefenseBuildType.ChainLightningTower => new TowerSpawnData
            {
                towerName = $"Player_Chain_{index:D3}",
                kind = TowerKind.ChainLightning,
                towerSheetId = DefenseTowerSheetTable.AutoLaserTowerId,
                color = new Color(0.35f, 0.65f, 1f),
                scaleMultiplier = new Vector3(1.2f, 1.35f, 1.2f)
            },
            DefenseBuildType.SummonTower => new TowerSpawnData
            {
                towerName = $"Player_Summon_{index:D3}",
                kind = TowerKind.Summon,
                color = new Color(0.22f, 0.78f, 0.38f),
                scaleMultiplier = new Vector3(1.15f, 1.25f, 1.15f)
            },
            _ => new TowerSpawnData
            {
                towerName = $"Player_Standard_{index:D3}",
                kind = TowerKind.Standard,
                towerSheetId = DefenseTowerSheetTable.MachineGunTowerId,
                color = new Color(0.2f, 0.45f, 1f)
            }
        };
    }

    private void EnsureBuildRoot()
    {
        if (buildRoot != null)
            return;

        var rootObject = new GameObject("PlayerBuildings");
        rootObject.transform.SetParent(transform, false);
        buildRoot = rootObject.transform;
    }
}
