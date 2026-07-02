using UnityEngine;

public static class DefenseMapGrid
{
    public static Vector3 CellToWorld(DefenseMapLayout map, Vector2Int cell)
    {
        float x = (cell.x - map.width * 0.5f + 0.5f) * map.cellSize;
        float z = (cell.y - map.height * 0.5f + 0.5f) * map.cellSize;
        return map.origin + new Vector3(x, 0f, z);
    }

    public static Vector2Int WorldToCell(DefenseMapLayout map, Vector3 world)
    {
        Vector3 local = world - map.origin;
        int x = Mathf.FloorToInt(local.x / map.cellSize + map.width * 0.5f);
        int y = Mathf.FloorToInt(local.z / map.cellSize + map.height * 0.5f);
        return new Vector2Int(x, y);
    }

    public static Vector3 SnapWorldToCellCenter(DefenseMapLayout map, Vector3 world)
    {
        return CellToWorld(map, WorldToCell(map, world));
    }

    public static bool IsInside(DefenseMapLayout map, Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < map.width && cell.y >= 0 && cell.y < map.height;
    }

    public static float GetMapHalfExtent(DefenseMapLayout map)
    {
        return Mathf.Max(map.width, map.height) * map.cellSize * 0.5f;
    }
}
