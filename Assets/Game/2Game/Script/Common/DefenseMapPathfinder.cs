using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 그리드 A* 경로 탐색. 정적 장애물 타일과 플레이어 건설 칸을 반영합니다.
/// </summary>
public static class DefenseMapPathfinder
{
    private static readonly Vector2Int[] Neighbors =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
    };

    private static readonly List<Vector3> SharedWaypoints = new();
    private static readonly List<Vector2Int> SharedCellPath = new();

    private static DefenseMapLayout layout;

    public static bool IsReady => layout != null;

    public static void Initialize(DefenseMapLayout mapLayout)
    {
        layout = mapLayout;
    }

    public static bool HasPath(Vector3 fromWorld, Vector3 toWorld)
    {
        return TryFindPath(fromWorld, toWorld, SharedWaypoints);
    }

    public static bool IsWorldWalkable(Vector3 worldPosition)
    {
        if (layout == null)
            return false;

        Vector2Int cell = DefenseMapGrid.WorldToCell(layout, worldPosition);
        return IsWalkable(cell);
    }

    public static bool TryFindPath(Vector3 fromWorld, Vector3 toWorld, List<Vector3> waypoints)
    {
        waypoints.Clear();
        if (layout == null)
            return false;

        Vector2Int start = DefenseMapGrid.WorldToCell(layout, fromWorld);
        Vector2Int goal = DefenseMapGrid.WorldToCell(layout, toWorld);

        if (!TryResolveWalkable(start, out start))
            return false;

        if (FindPathCells(start, goal, SharedCellPath))
        {
            AppendWaypoints(fromWorld, SharedCellPath, start, waypoints);
            return waypoints.Count > 0;
        }

        if (!TryResolveWalkable(goal, out goal))
            return false;

        if (TryFindNearestGoalPath(start, goal, SharedCellPath))
        {
            AppendWaypoints(fromWorld, SharedCellPath, start, waypoints);
            return waypoints.Count > 0;
        }

        return false;
    }

    private static void AppendWaypoints(
        Vector3 fromWorld,
        List<Vector2Int> cells,
        Vector2Int startCell,
        List<Vector3> waypoints)
    {
        int begin = 0;
        if (cells.Count > 1 && cells[0] == startCell)
            begin = 1;

        for (int i = begin; i < cells.Count; i++)
        {
            Vector3 world = DefenseMapGrid.CellToWorld(layout, cells[i]);
            world.y = fromWorld.y;
            waypoints.Add(world);
        }

        SimplifyWaypoints(waypoints);
    }

    private static void SimplifyWaypoints(List<Vector3> waypoints)
    {
        if (waypoints.Count <= 2)
            return;

        int anchor = 0;
        for (int i = 1; i < waypoints.Count; i++)
        {
            bool isLast = i == waypoints.Count - 1;
            if (!isLast && HasLineOfSight(waypoints[anchor], waypoints[i + 1]))
                continue;

            anchor++;
            if (anchor != i)
                waypoints[anchor] = waypoints[i];
        }

        int removeCount = waypoints.Count - (anchor + 1);
        if (removeCount > 0)
            waypoints.RemoveRange(anchor + 1, removeCount);
    }

    private static bool HasLineOfSight(Vector3 fromWorld, Vector3 toWorld)
    {
        Vector2Int from = DefenseMapGrid.WorldToCell(layout, fromWorld);
        Vector2Int to = DefenseMapGrid.WorldToCell(layout, toWorld);
        return HasLineOfSightCells(from, to);
    }

    private static bool HasLineOfSightCells(Vector2Int from, Vector2Int to)
    {
        int x0 = from.x;
        int y0 = from.y;
        int x1 = to.x;
        int y1 = to.y;
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            var cell = new Vector2Int(x0, y0);
            if (!IsWalkable(cell))
                return false;

            if (x0 == x1 && y0 == y1)
                return true;

            int e2 = err * 2;
            int nextX = x0;
            int nextY = y0;

            if (e2 > -dy)
            {
                err -= dy;
                nextX += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                if (nextX != x0)
                {
                    if (!IsWalkable(new Vector2Int(x0, y0 + sy))
                        || !IsWalkable(new Vector2Int(x0 + sx, y0)))
                        return false;
                }

                nextY += sy;
            }

            x0 = nextX;
            y0 = nextY;
        }
    }

    private static bool TryFindNearestGoalPath(Vector2Int start, Vector2Int preferredGoal, List<Vector2Int> path)
    {
        const int maxSearchRadius = 10;
        var visited = new HashSet<Vector2Int> { preferredGoal };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(preferredGoal);

        while (queue.Count > 0)
        {
            Vector2Int candidate = queue.Dequeue();
            if (Manhattan(candidate, preferredGoal) > maxSearchRadius)
                continue;

            if (FindPathCells(start, candidate, path))
                return true;

            foreach (var offset in Neighbors)
            {
                Vector2Int next = candidate + offset;
                if (!DefenseMapGrid.IsInside(layout, next) || visited.Contains(next) || !IsWalkable(next))
                    continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return false;
    }

    private static bool FindPathCells(Vector2Int start, Vector2Int goal, List<Vector2Int> path)
    {
        path.Clear();
        if (!IsWalkable(start) || !IsWalkable(goal))
            return false;

        if (start == goal)
        {
            path.Add(goal);
            return true;
        }

        int width = layout.width;
        int height = layout.height;
        int size = width * height;

        var cameFrom = new Vector2Int[size];
        var gScore = new float[size];
        var closed = new bool[size];
        var inOpen = new bool[size];

        for (int i = 0; i < size; i++)
        {
            cameFrom[i] = new Vector2Int(int.MinValue, int.MinValue);
            gScore[i] = float.MaxValue;
        }

        int startIndex = ToIndex(start);
        int goalIndex = ToIndex(goal);
        gScore[startIndex] = 0f;

        var open = new List<Vector2Int>(128) { start };
        inOpen[startIndex] = true;

        while (open.Count > 0)
        {
            int bestOpen = 0;
            float bestScore = float.MaxValue;
            for (int i = 0; i < open.Count; i++)
            {
                Vector2Int node = open[i];
                float score = gScore[ToIndex(node)] + Heuristic(node, goal);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestOpen = i;
                }
            }

            Vector2Int current = open[bestOpen];
            open.RemoveAt(bestOpen);
            int currentIndex = ToIndex(current);
            inOpen[currentIndex] = false;
            if (closed[currentIndex])
                continue;

            closed[currentIndex] = true;
            if (currentIndex == goalIndex)
            {
                ReconstructPath(cameFrom, current, path);
                return true;
            }

            foreach (var offset in Neighbors)
            {
                Vector2Int next = current + offset;
                if (!IsWalkable(next))
                    continue;

                if (offset.x != 0 && offset.y != 0
                    && (!IsWalkable(current + new Vector2Int(offset.x, 0))
                        || !IsWalkable(current + new Vector2Int(0, offset.y))))
                    continue;

                int nextIndex = ToIndex(next);
                if (closed[nextIndex])
                    continue;

                float stepCost = offset.x != 0 && offset.y != 0 ? 1.4142135f : 1f;
                float tentative = gScore[currentIndex] + stepCost;
                if (tentative >= gScore[nextIndex])
                    continue;

                cameFrom[nextIndex] = current;
                gScore[nextIndex] = tentative;
                if (!inOpen[nextIndex])
                {
                    open.Add(next);
                    inOpen[nextIndex] = true;
                }
            }
        }

        return false;
    }

    private static void ReconstructPath(Vector2Int[] cameFrom, Vector2Int current, List<Vector2Int> path)
    {
        path.Clear();
        path.Add(current);
        while (true)
        {
            Vector2Int prev = cameFrom[ToIndex(current)];
            if (prev.x == int.MinValue)
                break;

            current = prev;
            path.Add(current);
        }

        path.Reverse();
    }

    public static bool IsWalkable(Vector2Int cell)
    {
        if (!DefenseMapGrid.IsInside(layout, cell))
            return false;

        if (DefenseBuildManager.Instance != null
            && DefenseBuildManager.Instance.IsCellBlockedForNavigation(cell))
            return false;

        return layout.GetTile(cell) switch
        {
            DefenseMapTileType.Grass => true,
            DefenseMapTileType.Path => true,
            DefenseMapTileType.FarmSoil => true,
            DefenseMapTileType.FarmGate => FarmGateController.AreAllOpen(),
            _ => false
        };
    }

    private static bool TryResolveWalkable(Vector2Int cell, out Vector2Int resolved)
    {
        if (IsWalkable(cell))
        {
            resolved = cell;
            return true;
        }

        var visited = new HashSet<Vector2Int> { cell };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(cell);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (var offset in Neighbors)
            {
                Vector2Int next = current + offset;
                if (!DefenseMapGrid.IsInside(layout, next) || visited.Contains(next))
                    continue;

                visited.Add(next);
                if (IsWalkable(next))
                {
                    resolved = next;
                    return true;
                }

                queue.Enqueue(next);
            }
        }

        resolved = cell;
        return false;
    }

    private static int ToIndex(Vector2Int cell) => cell.x + cell.y * layout.width;

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + 0.4142135f * Mathf.Min(dx, dy);
    }

    private static int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
