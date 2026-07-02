using UnityEngine;

public static class CoopMapLayoutBuilder
{
    public const int CoopWidth = 72;
    public const int CoopHeight = 72;

    public static DefenseMapLayout Build()
    {
        var layout = ScriptableObject.CreateInstance<DefenseMapLayout>();
        layout.width = CoopWidth;
        layout.height = CoopHeight;
        layout.cellSize = 1f;
        layout.origin = Vector3.zero;
        layout.autoGenerateLanes = false;
        layout.EnsureTiles();
        layout.Fill(DefenseMapTileType.Grass);

        var center = new Vector2Int(CoopWidth / 2, CoopHeight / 2);
        layout.nexusCell = center;
        layout.playerSpawnCell = center;
        layout.farmGateCell = center + new Vector2Int(0, 4);

        PaintCentralArena(layout, center);
        PaintQuadrantFarms(layout, center);
        PaintOuterPaths(layout, center);
        PaintObstacleClusters(layout, center);
        PaintScatteredCover(layout);

        return layout;
    }

    private static void PaintCentralArena(DefenseMapLayout layout, Vector2Int center)
    {
        PaintRect(layout, center.x - 5, center.y - 5, 11, 11, DefenseMapTileType.Path);
        PaintRect(layout, center.x - 3, center.y - 3, 7, 7, DefenseMapTileType.Grass);
        PaintRect(layout, center.x - 2, center.y + 2, 5, 3, DefenseMapTileType.FarmSoil);
        layout.SetTile(center + new Vector2Int(0, 4), DefenseMapTileType.FarmGate);
        PaintRect(layout, center.x - 2, center.y - 5, 5, 3, DefenseMapTileType.FarmSoil);
    }

    private static void PaintQuadrantFarms(DefenseMapLayout layout, Vector2Int center)
    {
        var offsets = new[]
        {
            new Vector2Int(-16, 14),
            new Vector2Int(12, 14),
            new Vector2Int(-16, -16),
            new Vector2Int(12, -16),
            new Vector2Int(-22, 0),
            new Vector2Int(18, 0)
        };

        for (var i = 0; i < offsets.Length; i++)
        {
            var origin = center + offsets[i];
            PaintRect(layout, origin.x, origin.y, 5, 4, DefenseMapTileType.FarmSoil);
            PaintRect(layout, origin.x + 1, origin.y - 1, 3, 1, DefenseMapTileType.Path);
            layout.SetTile(origin + new Vector2Int(2, 3), DefenseMapTileType.FarmGate);
        }
    }

    private static void PaintOuterPaths(DefenseMapLayout layout, Vector2Int center)
    {
        PaintRect(layout, center.x - 1, 4, 3, CoopHeight - 8, DefenseMapTileType.Path);
        PaintRect(layout, 4, center.y - 1, CoopWidth - 8, 3, DefenseMapTileType.Path);

        var ringPoints = new[]
        {
            new Vector2Int(center.x - 20, center.y - 20),
            new Vector2Int(center.x + 16, center.y - 20),
            new Vector2Int(center.x - 20, center.y + 16),
            new Vector2Int(center.x + 16, center.y + 16)
        };

        foreach (var point in ringPoints)
            PaintRect(layout, point.x, point.y, 6, 2, DefenseMapTileType.Path);
    }

    private static void PaintObstacleClusters(DefenseMapLayout layout, Vector2Int center)
    {
        var clusters = new[]
        {
            center + new Vector2Int(-10, 8),
            center + new Vector2Int(9, 8),
            center + new Vector2Int(-10, -9),
            center + new Vector2Int(9, -9),
            center + new Vector2Int(-24, 8),
            center + new Vector2Int(20, 8),
            center + new Vector2Int(-24, -10),
            center + new Vector2Int(20, -10),
            new Vector2Int(10, 10),
            new Vector2Int(CoopWidth - 12, 10),
            new Vector2Int(10, CoopHeight - 12),
            new Vector2Int(CoopWidth - 12, CoopHeight - 12)
        };

        foreach (var origin in clusters)
            PaintRect(layout, origin.x, origin.y, 3, 3, DefenseMapTileType.Obstacle);
    }

    private static void PaintScatteredCover(DefenseMapLayout layout)
    {
        var rng = new System.Random(90210);
        for (var i = 0; i < 28; i++)
        {
            var cell = new Vector2Int(rng.Next(6, CoopWidth - 7), rng.Next(6, CoopHeight - 7));
            if (layout.GetTile(cell) != DefenseMapTileType.Grass)
                continue;

            layout.SetTile(cell, DefenseMapTileType.Obstacle);
            if (rng.NextDouble() < 0.35d)
            {
                var neighbor = cell + new Vector2Int(rng.Next(-1, 2), rng.Next(-1, 2));
                if (layout.IsInside(neighbor) && layout.GetTile(neighbor) == DefenseMapTileType.Grass)
                    layout.SetTile(neighbor, DefenseMapTileType.Obstacle);
            }
        }
    }

    private static void PaintRect(DefenseMapLayout layout, int x, int y, int w, int h, DefenseMapTileType type)
    {
        for (var py = y; py < y + h; py++)
        {
            for (var px = x; px < x + w; px++)
            {
                var cell = new Vector2Int(px, py);
                if (layout.IsInside(cell))
                    layout.SetTile(cell, type);
            }
        }
    }
}
