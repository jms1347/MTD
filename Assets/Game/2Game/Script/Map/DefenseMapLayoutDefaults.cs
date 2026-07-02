using System.Collections.Generic;
using UnityEngine;

public static class DefenseMapLayoutDefaults
{
    private const string DefaultMapPath = "Assets/Game/2Game/Data/SO/DefenseMapLayout_Default.asset";

    private const int FarmWidthCells = 4;
    private const int FarmDepthCells = 3;
    private const int FarmGapFromNexusCells = 4;

    public static void ApplyDefaultLayout(DefenseMapLayout map)
    {
        map.width = 48;
        map.height = 48;
        map.cellSize = 1f;
        map.origin = Vector3.zero;
        map.autoGenerateLanes = false;

        map.EnsureTiles();
        map.Fill(DefenseMapTileType.Grass);

        map.nexusCell = GetMapCenterCell(map);
        map.playerSpawnCell = map.nexusCell + new Vector2Int(0, -2);

        PaintReferenceFarms(map);
        GenerateLanes(map, paintTiles: true);
        PaintDefaultTowers(map);
    }

    /// <summary>기존 타일은 유지하고 농장 2곳만 다시 칠합니다.</summary>
    public static void PaintReferenceFarms(DefenseMapLayout map)
    {
        ClearFarmTiles(map);

        Vector2Int nexus = map.nexusCell;
        int farmMinX = nexus.x - FarmWidthCells / 2;
        int northSoilMinY = nexus.y + FarmGapFromNexusCells;
        int southSoilMinY = nexus.y - FarmGapFromNexusCells - FarmDepthCells + 1;

        bool assignedGate = false;
        PaintRectangularFarm(
            map,
            farmMinX,
            northSoilMinY,
            FarmWidthCells,
            FarmDepthCells,
            Vector2Int.down,
            ref assignedGate);

        PaintRectangularFarm(
            map,
            farmMinX,
            southSoilMinY,
            FarmWidthCells,
            FarmDepthCells,
            Vector2Int.up,
            ref assignedGate);
    }

    public static void GenerateLanes(DefenseMapLayout map, bool paintTiles = true)
    {
        DefenseMonsterLaneRegistry.Rebuild(map, paintTiles);
    }

    public static Vector2Int GetMapCenterCell(DefenseMapLayout map)
    {
        return new Vector2Int(map.width / 2, map.height / 2);
    }

    private static void ClearFarmTiles(DefenseMapLayout map)
    {
        for (int y = 0; y < map.height; y++)
        {
            for (int x = 0; x < map.width; x++)
            {
                var cell = new Vector2Int(x, y);
                var type = map.GetTile(cell);
                if (type is DefenseMapTileType.FarmSoil
                    or DefenseMapTileType.FarmGate
                    or DefenseMapTileType.Obstacle)
                {
                    map.SetTile(cell, DefenseMapTileType.Grass);
                }
            }
        }
    }

    private static void PaintRectangularFarm(
        DefenseMapLayout map,
        int soilMinX,
        int soilMinY,
        int width,
        int depth,
        Vector2Int gateTowardNexus,
        ref bool assignedFarmGate)
    {
        int soilMaxX = soilMinX + width - 1;
        int soilMaxY = soilMinY + depth - 1;

        for (int y = soilMinY; y <= soilMaxY; y++)
        {
            for (int x = soilMinX; x <= soilMaxX; x++)
                map.SetTile(new Vector2Int(x, y), DefenseMapTileType.FarmSoil);
        }

        int wallMinX = soilMinX - 1;
        int wallMaxX = soilMaxX + 1;
        int wallMinY = soilMinY - 1;
        int wallMaxY = soilMaxY + 1;

        for (int x = wallMinX; x <= wallMaxX; x++)
        {
            TrySetObstacle(map, new Vector2Int(x, wallMinY));
            TrySetObstacle(map, new Vector2Int(x, wallMaxY));
        }

        for (int y = wallMinY; y <= wallMaxY; y++)
        {
            TrySetObstacle(map, new Vector2Int(wallMinX, y));
            TrySetObstacle(map, new Vector2Int(wallMaxX, y));
        }

        var gateCells = ResolveGateCells(soilMinX, soilMaxX, soilMinY, soilMaxY, gateTowardNexus);
        foreach (var cell in gateCells)
            map.SetTile(cell, DefenseMapTileType.FarmGate);

        if (gateCells.Count > 0 && !assignedFarmGate)
        {
            map.farmGateCell = gateCells[0];
            assignedFarmGate = true;
        }
    }

    private static List<Vector2Int> ResolveGateCells(
        int soilMinX,
        int soilMaxX,
        int soilMinY,
        int soilMaxY,
        Vector2Int gateTowardNexus)
    {
        gateTowardNexus = NormalizeGateStep(gateTowardNexus);
        var cells = new List<Vector2Int>(3);

        if (Mathf.Abs(gateTowardNexus.x) >= Mathf.Abs(gateTowardNexus.y))
        {
            int gateX = gateTowardNexus.x < 0 ? soilMinX - 1 : soilMaxX + 1;
            int centerY = (soilMinY + soilMaxY) / 2;
            for (int i = -1; i <= 1; i++)
                cells.Add(new Vector2Int(gateX, centerY + i));
        }
        else
        {
            int gateY = gateTowardNexus.y < 0 ? soilMinY - 1 : soilMaxY + 1;
            int centerX = (soilMinX + soilMaxX) / 2;
            for (int i = -1; i <= 1; i++)
                cells.Add(new Vector2Int(centerX + i, gateY));
        }

        return cells;
    }

    private static void TrySetObstacle(DefenseMapLayout map, Vector2Int cell)
    {
        if (!map.IsInside(cell))
            return;

        if (map.GetTile(cell) == DefenseMapTileType.Grass)
            map.SetTile(cell, DefenseMapTileType.Obstacle);
    }

    private static Vector2Int NormalizeGateStep(Vector2Int step)
    {
        step.x = Mathf.Clamp(step.x, -1, 1);
        step.y = Mathf.Clamp(step.y, -1, 1);
        if (step == Vector2Int.zero)
            return Vector2Int.down;

        return step;
    }

    private static void PaintDefaultTowers(DefenseMapLayout map)
    {
        if (map.towerLayout == null)
            return;

        map.towerLayout.SetDefaultLayout();
        map.SyncTowerLayout();

        foreach (var tower in map.towerLayout.towers)
        {
            Vector3 world = map.towerLayout.TowerOrigin + tower.positionOffset;
            Vector3 snapped = DefenseMapGrid.SnapWorldToCellCenter(map, world);
            tower.positionOffset = snapped - map.towerLayout.TowerOrigin;
        }
    }

#if UNITY_EDITOR
    public static string DefaultAssetPath => DefaultMapPath;
#endif
}
