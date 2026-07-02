using System.Collections.Generic;
using UnityEngine;

public static class DefenseMapBuilder
{
    private static readonly Color GrassColor = new(0.35f, 0.42f, 0.32f);
    private static readonly Color PathColor = new(0.45f, 0.4f, 0.34f);
    private static readonly Color SoilColor = new(0.45f, 0.3f, 0.16f);
    private static readonly Color SoilDarkColor = new(0.36f, 0.24f, 0.12f);
    private static readonly Color ObstacleColor = new(0.5f, 0.41f, 0.3f);
    private static readonly Color SpawnColor = new(0.2f, 0.45f, 0.95f);

    public static Transform Build(DefenseMapLayout layout)
    {
        if (layout == null)
            return null;

        layout.EnsureTiles();

        var existing = GameObject.Find("DefenseMap");
        if (existing != null)
            Object.Destroy(existing);

        var oldGround = GameObject.Find("DefenseGround");
        if (oldGround != null)
            Object.Destroy(oldGround);

        var oldFarm = GameObject.Find("DefenseFarm");
        if (oldFarm != null)
            Object.Destroy(oldFarm);

        var root = new GameObject("DefenseMap");
        var tilesRoot = new GameObject("Tiles").transform;
        tilesRoot.SetParent(root.transform, false);

        bool hasFarmSoil = false;

        for (int y = 0; y < layout.height; y++)
        {
            for (int x = 0; x < layout.width; x++)
            {
                var cell = new Vector2Int(x, y);
                var type = layout.GetTile(cell);
                Vector3 world = DefenseMapGrid.CellToWorld(layout, cell);
                CreateTile(tilesRoot, type, world, layout.cellSize, (x + y) % 2 == 0);

                if (type == DefenseMapTileType.FarmSoil)
                    hasFarmSoil = true;
            }
        }

        if (hasFarmSoil)
            SetupFarmSystems(root.transform, layout);

        CreateSpawnMarkers(root.transform, layout);

        return root.transform;
    }

    private static void CreateTile(
        Transform parent,
        DefenseMapTileType type,
        Vector3 worldCenter,
        float cellSize,
        bool light)
    {
        switch (type)
        {
            case DefenseMapTileType.Obstacle:
                CreateObstacleTile(parent, worldCenter, cellSize);
                return;
            case DefenseMapTileType.FarmGate:
                CreateGateMarker(parent, worldCenter, cellSize);
                return;
            case DefenseMapTileType.FarmSoil:
                CreateSoilTile(parent, worldCenter, cellSize, light);
                return;
            default:
                CreateGroundTile(parent, type, worldCenter, cellSize, light);
                return;
        }
    }

    private static void CreateGroundTile(
        Transform parent,
        DefenseMapTileType type,
        Vector3 center,
        float size,
        bool light)
    {
        var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = $"Tile_{type}";
        tile.tag = "Ground";
        tile.transform.SetParent(parent, false);
        tile.transform.position = center + new Vector3(0f, 0.02f, 0f);
        tile.transform.localScale = new Vector3(size * 0.98f, 0.04f, size * 0.98f);

        var collider = tile.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = false;

        Color color = type switch
        {
            DefenseMapTileType.Path => PathColor,
            _ => light ? GrassColor : GrassColor * 0.92f
        };
        ApplyColor(tile, color);
    }

    private static void CreateSoilTile(Transform parent, Vector3 center, float size, bool light)
    {
        var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = "FarmSoil";
        tile.tag = "FarmSoil";
        tile.transform.SetParent(parent, false);
        tile.transform.position = center + new Vector3(0f, 0.02f, 0f);
        tile.transform.localScale = new Vector3(size * 0.98f, 0.04f, size * 0.98f);
        ApplyColor(tile, light ? SoilColor : SoilDarkColor);
        tile.AddComponent<FarmDrillTile>();
    }

    private static void CreateObstacleTile(Transform parent, Vector3 center, float size)
    {
        var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = "MapObstacle";
        tile.tag = "Obstacle";
        tile.transform.SetParent(parent, false);
        tile.transform.position = center + new Vector3(0f, 0.42f, 0f);
        tile.transform.localScale = new Vector3(size * 0.96f, 0.84f, size * 0.96f);

        var collider = tile.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = false;

        ApplyColor(tile, ObstacleColor);
    }

    private static void CreateGateMarker(Transform parent, Vector3 center, float size)
    {
        var gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = "FarmGate";
        gate.tag = "Ground";
        gate.transform.SetParent(parent, false);
        gate.transform.position = center + new Vector3(0f, 0.02f, 0f);
        gate.transform.localScale = new Vector3(size * 0.96f, 0.04f, size * 0.96f);

        var collider = gate.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        ApplyColor(gate, PathColor);
    }

    private static void SetupFarmSystems(Transform mapRoot, DefenseMapLayout layout)
    {
        var farmRoot = new GameObject("DefenseFarm").transform;
        farmRoot.SetParent(mapRoot, false);

        var soilClusters = CollectFarmSoilClusters(layout);
        var gateCells = CollectGateCells(layout);

        for (int i = 0; i < soilClusters.Count; i++)
        {
            var cluster = soilClusters[i];
            var clusterGates = FilterGateCellsForCluster(cluster, gateCells, layout);
            CreateFarmCluster(farmRoot, layout, cluster, clusterGates, i);
        }
    }

    private static void CreateFarmCluster(
        Transform farmRoot,
        DefenseMapLayout layout,
        List<Vector2Int> soilCells,
        List<Vector2Int> gateCells,
        int index)
    {
        if (soilCells.Count == 0)
            return;

        float half = layout.cellSize * 0.5f;
        Vector3 worldMin = Vector3.positiveInfinity;
        Vector3 worldMax = Vector3.negativeInfinity;

        foreach (var cell in soilCells)
        {
            Vector3 world = DefenseMapGrid.CellToWorld(layout, cell);
            worldMin = Vector3.Min(worldMin, world);
            worldMax = Vector3.Max(worldMax, world);
        }

        worldMin -= new Vector3(half, 0f, half);
        worldMax += new Vector3(half, 0f, half);

        var clusterRoot = new GameObject($"FarmCluster_{index}").transform;
        clusterRoot.SetParent(farmRoot, false);

        var zone = clusterRoot.gameObject.AddComponent<FarmZone>();
        zone.Configure(
            clusterRoot.InverseTransformPoint(worldMin),
            clusterRoot.InverseTransformPoint(worldMax));

        if (gateCells.Count > 0)
            CreateFarmGate(layout, clusterRoot, gateCells);
    }

    private static List<Vector2Int> FilterGateCellsForCluster(
        List<Vector2Int> soilCells,
        List<Vector2Int> allGateCells,
        DefenseMapLayout layout)
    {
        var soilSet = new HashSet<Vector2Int>(soilCells);
        var matched = new List<Vector2Int>();

        foreach (var gate in allGateCells)
        {
            foreach (var dir in CardinalDirs)
            {
                if (soilSet.Contains(gate + dir))
                {
                    matched.Add(gate);
                    break;
                }
            }

            if (matched.Contains(gate))
                continue;

            foreach (var dir in DiagonalDirs)
            {
                if (soilSet.Contains(gate + dir))
                {
                    matched.Add(gate);
                    break;
                }
            }
        }

        return matched;
    }

    private static readonly Vector2Int[] CardinalDirs =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private static readonly Vector2Int[] DiagonalDirs =
    {
        new(1, 1),
        new(1, -1),
        new(-1, 1),
        new(-1, -1)
    };

    private static List<List<Vector2Int>> CollectFarmSoilClusters(DefenseMapLayout layout)
    {
        var clusters = new List<List<Vector2Int>>();
        var visited = new HashSet<Vector2Int>();

        for (int y = 0; y < layout.height; y++)
        {
            for (int x = 0; x < layout.width; x++)
            {
                var start = new Vector2Int(x, y);
                if (visited.Contains(start) || layout.GetTile(start) != DefenseMapTileType.FarmSoil)
                    continue;

                var cluster = new List<Vector2Int>();
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(start);
                visited.Add(start);

                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue();
                    cluster.Add(cell);

                    foreach (var dir in CardinalDirs)
                    {
                        var next = cell + dir;
                        if (visited.Contains(next) || !layout.IsInside(next))
                            continue;

                        if (layout.GetTile(next) != DefenseMapTileType.FarmSoil)
                            continue;

                        visited.Add(next);
                        queue.Enqueue(next);
                    }
                }

                clusters.Add(cluster);
            }
        }

        return clusters;
    }

    private static void CreateSpawnMarkers(Transform mapRoot, DefenseMapLayout layout)
    {
        if (!DefenseMonsterLaneRegistry.IsReady)
            return;

        var markersRoot = new GameObject("SpawnMarkers").transform;
        markersRoot.SetParent(mapRoot, false);

        foreach (SpawnDirection direction in System.Enum.GetValues(typeof(SpawnDirection)))
        {
            if (!DefenseMonsterLaneRegistry.TryGetLaneWaypoints(direction, out var waypoints) || waypoints.Count == 0)
                continue;

            CreateSpawnMarker(markersRoot, direction.ToString(), waypoints[0], layout.cellSize);
        }
    }

    private static void CreateSpawnMarker(Transform parent, string label, Vector3 world, float cellSize)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = $"Spawn_{label}";
        marker.transform.SetParent(parent, false);
        marker.transform.position = world + new Vector3(0f, 0.18f, 0f);
        marker.transform.localScale = new Vector3(cellSize * 0.55f, 0.12f, cellSize * 0.55f);

        var collider = marker.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        ApplyColor(marker, SpawnColor);
    }

    private static void CreateFarmGate(DefenseMapLayout layout, Transform farmRoot, List<Vector2Int> gateCells)
    {
        if (gateCells.Count == 0)
            return;

        Vector3 gateCenter = Vector3.zero;
        foreach (var cell in gateCells)
            gateCenter += DefenseMapGrid.CellToWorld(layout, cell);
        gateCenter /= gateCells.Count;

        bool gateAlongX = gateCells.Count > 1
            && gateCells[0].y == gateCells[1].y;

        float gateWidth = gateAlongX
            ? layout.cellSize * gateCells.Count * 0.98f
            : layout.cellSize * 0.98f;
        float gateDepth = gateAlongX
            ? layout.cellSize * 0.98f
            : layout.cellSize * gateCells.Count * 0.98f;

        var gateObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gateObject.name = "FarmGateCollider";
        gateObject.tag = "Obstacle";
        gateObject.transform.SetParent(farmRoot, false);
        gateObject.transform.position = gateCenter + new Vector3(0f, 0.42f, 0f);
        gateObject.transform.localScale = new Vector3(gateWidth, 0.84f, gateDepth);
        ApplyColor(gateObject, ObstacleColor);
        gateObject.SetActive(false);

        var extraColliders = new List<Collider>();
        foreach (var cell in gateCells)
        {
            Vector3 cellCenter = DefenseMapGrid.CellToWorld(layout, cell);
            var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = "FarmGateSegment";
            segment.tag = "Obstacle";
            segment.transform.SetParent(farmRoot, false);
            segment.transform.position = cellCenter + new Vector3(0f, 0.42f, 0f);
            segment.transform.localScale = new Vector3(layout.cellSize * 0.98f, 0.84f, layout.cellSize * 0.98f);
            ApplyColor(segment, ObstacleColor);

            var segmentCollider = segment.GetComponent<Collider>();
            segmentCollider.enabled = false;
            segment.SetActive(false);
            extraColliders.Add(segmentCollider);
        }

        var gateController = farmRoot.gameObject.AddComponent<FarmGateController>();
        gateController.Initialize(
            gateObject.GetComponent<Collider>(),
            gateObject,
            extraColliders.ToArray());
    }

    private static List<Vector2Int> CollectGateCells(DefenseMapLayout layout)
    {
        var cells = new List<Vector2Int>();
        for (int y = 0; y < layout.height; y++)
        {
            for (int x = 0; x < layout.width; x++)
            {
                var cell = new Vector2Int(x, y);
                if (layout.GetTile(cell) == DefenseMapTileType.FarmGate)
                    cells.Add(cell);
            }
        }

        return cells;
    }

    private static void ApplyColor(GameObject target, Color color)
    {
        var renderer = target.GetComponent<Renderer>();
        if (renderer == null)
            return;

        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        renderer.material = material;
    }
}
