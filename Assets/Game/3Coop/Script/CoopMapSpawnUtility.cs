using UnityEngine;

public static class CoopMapSpawnUtility
{
    public static Vector3 ResolveTowerSpawn(int playerIndex, int playerCount, Vector3 spawnCenter, bool useMapLayout)
    {
        if (!useMapLayout)
        {
            var angle = (360f / playerCount) * playerIndex * Mathf.Deg2Rad;
            return spawnCenter + new Vector3(
                Mathf.Sin(angle) * 11f,
                0f,
                Mathf.Cos(angle) * 11f);
        }

        if (playerCount <= 1)
            return SnapToWalkableWorld(spawnCenter);

        var ringAngle = (360f / playerCount) * playerIndex * Mathf.Deg2Rad;
        const float ringRadius = 1.6f;
        var offset = new Vector3(
            Mathf.Sin(ringAngle) * ringRadius,
            0f,
            Mathf.Cos(ringAngle) * ringRadius * 0.25f);
        return SnapToWalkableWorld(spawnCenter + offset);
    }

    public static Vector3 SnapToWalkableWorld(Vector3 world)
    {
        var map = CoopMapBootstrap.Instance != null ? CoopMapBootstrap.Instance.MapLayout : null;
        if (map == null || !DefenseMapPathfinder.IsReady)
            return world;

        var snapped = DefenseMapGrid.SnapWorldToCellCenter(map, world);
        if (DefenseMapPathfinder.IsWorldWalkable(snapped))
            return snapped;

        var origin = DefenseMapGrid.WorldToCell(map, world);
        for (var radius = 1; radius <= 10; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dz = -radius; dz <= radius; dz++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dz) != radius)
                        continue;

                    var cell = new Vector2Int(origin.x + dx, origin.y + dz);
                    if (!DefenseMapPathfinder.IsWalkable(cell))
                        continue;

                    return DefenseMapGrid.CellToWorld(map, cell);
                }
            }
        }

        return snapped;
    }
}
