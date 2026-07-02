using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스케치 맵 레인 — 넥서스 좌·우에서 출발해 ㄱ자 경로로 좌상·우하 스폰에 연결.
/// </summary>
public static class DefenseMonsterLaneRegistry
{
    private const int MapEdgeMarginCells = 3;
    private const int HorizontalArmCells = 13;
    private const int VerticalArmCells = 16;

    private static readonly Dictionary<SpawnDirection, List<Vector3>> LaneWaypoints = new();
    private static readonly HashSet<Vector2Int> LaneCells = new();

    private static DefenseMapLayout layout;

    public static bool IsReady => layout != null && LaneWaypoints.Count > 0;

    public static void Clear()
    {
        layout = null;
        LaneWaypoints.Clear();
        LaneCells.Clear();
    }

    public static void Rebuild(DefenseMapLayout mapLayout, bool paintTiles = true)
    {
        layout = mapLayout;
        LaneWaypoints.Clear();
        LaneCells.Clear();

        if (layout == null)
            return;

        layout.EnsureTiles();
        Vector2Int nexus = layout.nexusCell;

        int westElbowX = nexus.x - HorizontalArmCells;
        int eastElbowX = nexus.x + HorizontalArmCells - 1;
        int northArmY = nexus.y + VerticalArmCells;
        int southArmY = nexus.y - VerticalArmCells + 1;
        int westSpawnX = MapEdgeMarginCells;
        int eastSpawnX = layout.width - 1 - MapEdgeMarginCells;

        var westLane = BuildCornerPath(
            new Vector2Int(westSpawnX, northArmY),
            new Vector2Int(westElbowX, northArmY),
            new Vector2Int(westElbowX, nexus.y),
            nexus);

        var eastLane = BuildCornerPath(
            new Vector2Int(eastSpawnX, southArmY),
            new Vector2Int(eastElbowX, southArmY),
            new Vector2Int(eastElbowX, nexus.y),
            nexus);

        RegisterLane(SpawnDirection.West, westLane, paintTiles);
        RegisterLane(SpawnDirection.East, eastLane, paintTiles);
    }

    public static bool IsLaneCell(Vector2Int cell) => LaneCells.Contains(cell);

    public static bool TryGetLaneWaypoints(SpawnDirection direction, out IReadOnlyList<Vector3> waypoints)
    {
        if (LaneWaypoints.TryGetValue(direction, out var list) && list.Count > 0)
        {
            waypoints = list;
            return true;
        }

        waypoints = null;
        return false;
    }

    public static bool TryGetLaneSpawnWorld(SpawnDirection direction, int spreadIndex, out Vector3 spawnWorld)
    {
        spawnWorld = default;
        if (!TryGetLaneWaypoints(direction, out var waypoints))
            return false;

        int index = Mathf.Clamp(spreadIndex, 0, Mathf.Min(2, waypoints.Count - 1));
        spawnWorld = waypoints[index];
        return true;
    }

    public static SpawnDirection ResolveDirectionFromAngle(float angleRadians)
    {
        var flat = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
        if (flat.x <= 0f && flat.y >= 0f)
            return SpawnDirection.West;

        return SpawnDirection.East;
    }

    private static void RegisterLane(SpawnDirection direction, List<Vector2Int> cells, bool paintTiles)
    {
        var waypoints = new List<Vector3>(cells.Count);
        foreach (var cell in cells)
        {
            if (!DefenseMapGrid.IsInside(layout, cell))
                continue;

            if (paintTiles && CanPaintLaneOn(layout.GetTile(cell)))
            {
                layout.SetTile(cell, DefenseMapTileType.Path);
                LaneCells.Add(cell);
            }

            waypoints.Add(ToWorld(cell));
        }

        if (waypoints.Count > 0)
            LaneWaypoints[direction] = waypoints;
    }

    private static List<Vector2Int> BuildCornerPath(params Vector2Int[] corners)
    {
        var cells = new List<Vector2Int>(128);
        if (corners.Length == 0)
            return cells;

        for (int i = 0; i < corners.Length - 1; i++)
            WalkStraight(cells, corners[i], corners[i + 1]);

        return cells;
    }

    private static void WalkStraight(List<Vector2Int> cells, Vector2Int from, Vector2Int to)
    {
        AddUniqueCell(cells, from);
        Vector2Int pos = from;
        Vector2Int step = new(
            Mathf.Clamp(to.x - from.x, -1, 1),
            Mathf.Clamp(to.y - from.y, -1, 1));

        if (step == Vector2Int.zero)
            return;

        while (pos != to)
        {
            pos += step;
            AddUniqueCell(cells, pos);
        }
    }

    private static bool CanPaintLaneOn(DefenseMapTileType tile)
    {
        return tile == DefenseMapTileType.Grass || tile == DefenseMapTileType.Path;
    }

    private static Vector3 ToWorld(Vector2Int cell)
    {
        Vector3 world = DefenseMapGrid.CellToWorld(layout, cell);
        world.y = 0.05f;
        return world;
    }

    private static void AddUniqueCell(List<Vector2Int> cells, Vector2Int cell)
    {
        if (cells.Count > 0 && cells[cells.Count - 1] == cell)
            return;

        cells.Add(cell);
    }
}
