using System;
using System.Collections.Generic;
using UnityEngine;

public enum CoopSpawnPattern
{
    MapEdge,
    AmbushNearPlayer,
    AmbushZone,
    BeaconSurge,
    RandomInterior
}

public class CoopMapGimmicks : MonoBehaviour
{
    public static CoopMapGimmicks Instance { get; private set; }

    private readonly List<Vector3> ambushPoints = new();
    private readonly List<Vector3> beaconPoints = new();
    private readonly List<Vector3> interiorPoints = new();

    public IReadOnlyList<Vector3> AmbushPoints => ambushPoints;
    public IReadOnlyList<Vector3> BeaconPoints => beaconPoints;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static CoopMapGimmicks Build(DefenseMapLayout layout, Transform parent)
    {
        var root = new GameObject("CoopMapGimmicks");
        if (parent != null)
            root.transform.SetParent(parent, false);

        var gimmicks = root.AddComponent<CoopMapGimmicks>();
        gimmicks.Populate(layout);
        return gimmicks;
    }

    private void Populate(DefenseMapLayout layout)
    {
        ambushPoints.Clear();
        beaconPoints.Clear();
        interiorPoints.Clear();

        if (layout == null)
        {
            CreateFallbackPoints();
            return;
        }

        CollectStrategicCells(layout);
        CreateWorldMarkers(layout);
    }

    private void CollectStrategicCells(DefenseMapLayout layout)
    {
        var center = layout.playerSpawnCell;
        const int margin = 5;
        for (var y = margin; y < layout.height - margin; y += 4)
        {
            for (var x = margin; x < layout.width - margin; x += 4)
            {
                var cell = new Vector2Int(x, y);
                if (!layout.IsInside(cell) || !DefenseMapPathfinder.IsWalkable(cell))
                    continue;

                var world = DefenseMapGrid.CellToWorld(layout, cell);
                var snapped = CoopMapSpawnUtility.SnapToWalkableWorld(world);
                if (Vector3.Distance(snapped, world) > 2.5f)
                    continue;

                if (IsNearMapEdge(layout, cell))
                {
                    if (beaconPoints.Count < 10 && !ContainsNear(beaconPoints, snapped, 4f))
                        beaconPoints.Add(snapped);
                }
                else if ((cell - center).sqrMagnitude > 36)
                {
                    if (ambushPoints.Count < 14 && !ContainsNear(ambushPoints, snapped, 3f))
                        ambushPoints.Add(snapped);
                }

                if (interiorPoints.Count < 20 && !ContainsNear(interiorPoints, snapped, 3f))
                    interiorPoints.Add(snapped);
            }
        }

        var fixedPoints = new[]
        {
            center + new Vector2Int(-18, 12),
            center + new Vector2Int(18, 12),
            center + new Vector2Int(-18, -12),
            center + new Vector2Int(18, -12),
            center + new Vector2Int(0, 22),
            center + new Vector2Int(0, -22),
            center + new Vector2Int(22, 0),
            center + new Vector2Int(-22, 0),
            new Vector2Int(8, 8),
            new Vector2Int(layout.width - 9, 8),
            new Vector2Int(8, layout.height - 9),
            new Vector2Int(layout.width - 9, layout.height - 9)
        };

        foreach (var cell in fixedPoints)
        {
            if (!layout.IsInside(cell))
                continue;

            var snapped = CoopMapSpawnUtility.SnapToWalkableWorld(DefenseMapGrid.CellToWorld(layout, cell));
            if (ambushPoints.Count < 14 && !ContainsNear(ambushPoints, snapped, 3f))
                ambushPoints.Add(snapped);
            if (beaconPoints.Count < 10 && !ContainsNear(beaconPoints, snapped, 4f))
                beaconPoints.Add(snapped);
        }

        if (ambushPoints.Count == 0)
            ambushPoints.Add(CoopMapSpawnUtility.SnapToWalkableWorld(layout.GetPlayerSpawnWorld() + new Vector3(-12f, 0f, 10f)));
        if (beaconPoints.Count == 0)
            beaconPoints.Add(CoopMapSpawnUtility.SnapToWalkableWorld(layout.GetPlayerSpawnWorld() + new Vector3(14f, 0f, -10f)));
    }

    private void CreateWorldMarkers(DefenseMapLayout layout)
    {
        for (var i = 0; i < ambushPoints.Count; i++)
        {
            var marker = CreateMarker(
                $"AmbushZone_{i + 1}",
                ambushPoints[i],
                new Vector3(1.5f, 0.06f, 1.5f),
                new Color(0.85f, 0.2f, 0.18f, 0.55f));
            marker.AddComponent<CoopAmbushTrigger>();
        }

        for (var i = 0; i < beaconPoints.Count; i++)
        {
            CreateMarker(
                $"SpawnBeacon_{i + 1}",
                beaconPoints[i],
                new Vector3(0.75f, 1.8f, 0.75f),
                new Color(0.25f, 0.55f, 1f, 0.75f));
        }

        PlaceSupplyCaches(layout);
        PlaceHealZones(layout);
        PlaceSpeedPads(layout);
        PlaceDangerZones(layout);
        PlaceGoldPickups(layout);
    }

    private void PlaceSupplyCaches(DefenseMapLayout layout)
    {
        var center = layout.playerSpawnCell;
        var cells = new[]
        {
            center + new Vector2Int(-8, 4),
            center + new Vector2Int(8, 4),
            center + new Vector2Int(-8, -6),
            center + new Vector2Int(8, -6),
            center + new Vector2Int(-16, 0),
            center + new Vector2Int(16, 0),
            layout.farmGateCell + new Vector2Int(-4, -3),
            layout.farmGateCell + new Vector2Int(4, 3)
        };

        for (var i = 0; i < cells.Length; i++)
        {
            if (!layout.IsInside(cells[i]))
                continue;

            var world = DefenseMapGrid.CellToWorld(layout, cells[i]);
            var depot = CreateMarker(
                $"SupplyCache_{i + 1}",
                world,
                new Vector3(1.1f, 0.08f, 1.1f),
                new Color(0.95f, 0.78f, 0.15f, 0.8f));
            depot.AddComponent<CoopSupplyCache>();
        }
    }

    private void PlaceHealZones(DefenseMapLayout layout)
    {
        var center = layout.playerSpawnCell;
        var cells = new[]
        {
            center + new Vector2Int(-12, 10),
            center + new Vector2Int(12, 10),
            center + new Vector2Int(-12, -10),
            center + new Vector2Int(12, -10),
            center + new Vector2Int(0, 16),
            center + new Vector2Int(0, -16)
        };

        for (var i = 0; i < cells.Length; i++)
        {
            if (!layout.IsInside(cells[i]))
                continue;

            var world = DefenseMapGrid.CellToWorld(layout, cells[i]);
            var zone = CreateMarker(
                $"HealZone_{i + 1}",
                world,
                new Vector3(2f, 0.05f, 2f),
                new Color(0.2f, 0.9f, 0.45f, 0.45f));
            zone.AddComponent<CoopHealZone>();
        }
    }

    private void PlaceSpeedPads(DefenseMapLayout layout)
    {
        var center = layout.playerSpawnCell;
        var cells = new[]
        {
            center + new Vector2Int(0, 10),
            center + new Vector2Int(0, -10),
            center + new Vector2Int(10, 0),
            center + new Vector2Int(-10, 0),
            center + new Vector2Int(20, 14),
            center + new Vector2Int(-20, -14)
        };

        for (var i = 0; i < cells.Length; i++)
        {
            if (!layout.IsInside(cells[i]))
                continue;

            var world = DefenseMapGrid.CellToWorld(layout, cells[i]);
            var pad = CreateMarker(
                $"SpeedPad_{i + 1}",
                world,
                new Vector3(1.3f, 0.06f, 1.3f),
                new Color(0.2f, 0.85f, 0.95f, 0.7f));
            pad.AddComponent<CoopSpeedPad>();
        }
    }

    private void PlaceDangerZones(DefenseMapLayout layout)
    {
        var center = layout.playerSpawnCell;
        var cells = new[]
        {
            center + new Vector2Int(-22, 18),
            center + new Vector2Int(22, 18),
            center + new Vector2Int(-22, -18),
            center + new Vector2Int(22, -18),
            new Vector2Int(10, layout.height / 2),
            new Vector2Int(layout.width - 11, layout.height / 2)
        };

        for (var i = 0; i < cells.Length; i++)
        {
            if (!layout.IsInside(cells[i]))
                continue;

            var world = DefenseMapGrid.CellToWorld(layout, cells[i]);
            var zone = CreateMarker(
                $"DangerZone_{i + 1}",
                world,
                new Vector3(2.2f, 0.05f, 2.2f),
                new Color(0.65f, 0.15f, 0.85f, 0.5f));
            zone.AddComponent<CoopDangerZone>();
        }
    }

    private void PlaceGoldPickups(DefenseMapLayout layout)
    {
        var rng = new System.Random(4242);
        var placed = 0;
        for (var attempt = 0; attempt < 80 && placed < 10; attempt++)
        {
            var cell = new Vector2Int(rng.Next(8, layout.width - 8), rng.Next(8, layout.height - 8));
            if (!DefenseMapPathfinder.IsWalkable(cell))
                continue;

            var world = DefenseMapGrid.CellToWorld(layout, cell);
            var pickup = CreateMarker(
                $"GoldPickup_{placed + 1}",
                world,
                new Vector3(0.8f, 0.8f, 0.8f),
                new Color(1f, 0.86f, 0.2f, 0.95f));
            pickup.AddComponent<CoopGoldPickup>();
            placed++;
        }
    }

    private static GameObject CreateMarker(string name, Vector3 world, Vector3 scale, Color color)
    {
        var root = new GameObject(name);
        root.transform.SetParent(Instance.transform, false);
        root.transform.position = new Vector3(world.x, 0.04f, world.z);

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = scale;
        visual.transform.localPosition = new Vector3(0f, scale.y * 0.5f, 0f);
        UnityEngine.Object.Destroy(visual.GetComponent<Collider>());

        var renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = color;

        return root;
    }

    private void CreateFallbackPoints()
    {
        ambushPoints.Add(new Vector3(-20f, 0f, 16f));
        ambushPoints.Add(new Vector3(20f, 0f, 16f));
        ambushPoints.Add(new Vector3(-20f, 0f, -16f));
        ambushPoints.Add(new Vector3(20f, 0f, -16f));
        beaconPoints.Add(new Vector3(0f, 0f, 28f));
        beaconPoints.Add(new Vector3(0f, 0f, -28f));
        interiorPoints.Add(new Vector3(12f, 0f, 0f));
        interiorPoints.Add(new Vector3(-12f, 0f, 0f));
    }

    public Vector3 PickSpawnPosition(CoopSpawnPattern pattern, System.Random rng, IReadOnlyList<Vector3> playerPositions)
    {
        switch (pattern)
        {
            case CoopSpawnPattern.AmbushNearPlayer when playerPositions != null && playerPositions.Count > 0:
            {
                var player = playerPositions[rng.Next(playerPositions.Count)];
                var angle = (float)(rng.NextDouble() * Math.PI * 2d);
                var distance = 8f + (float)rng.NextDouble() * 10f;
                var offset = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
                return CoopMapSpawnUtility.SnapToWalkableWorld(player + offset);
            }
            case CoopSpawnPattern.AmbushZone when ambushPoints.Count > 0:
                return ambushPoints[rng.Next(ambushPoints.Count)];
            case CoopSpawnPattern.BeaconSurge when beaconPoints.Count > 0:
                return beaconPoints[rng.Next(beaconPoints.Count)];
            case CoopSpawnPattern.RandomInterior when interiorPoints.Count > 0:
                return interiorPoints[rng.Next(interiorPoints.Count)];
            default:
                return PickRandomEdgePosition(rng);
        }
    }

    public Vector3 PickAmbushBurstOrigin(Vector3 nearWorld, System.Random rng)
    {
        if (ambushPoints.Count == 0)
            return PickSpawnPosition(CoopSpawnPattern.AmbushNearPlayer, rng, new[] { nearWorld });

        var best = ambushPoints[0];
        var bestDist = float.MaxValue;
        foreach (var point in ambushPoints)
        {
            var dist = Vector3.SqrMagnitude(point - nearWorld);
            if (dist >= bestDist)
                continue;

            bestDist = dist;
            best = point;
        }

        return best;
    }

    public Vector3 PickRandomEdgePosition(System.Random rng)
    {
        var layout = CoopMapBootstrap.Instance != null ? CoopMapBootstrap.Instance.MapLayout : null;
        if (layout == null)
        {
            var angle = (float)(rng.NextDouble() * Math.PI * 2d);
            var radius = 30f;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        for (var attempt = 0; attempt < 32; attempt++)
        {
            var edge = rng.Next(4);
            Vector2Int cell;
            switch (edge)
            {
                case 0:
                    cell = new Vector2Int(rng.Next(layout.width), 3);
                    break;
                case 1:
                    cell = new Vector2Int(rng.Next(layout.width), layout.height - 4);
                    break;
                case 2:
                    cell = new Vector2Int(3, rng.Next(layout.height));
                    break;
                default:
                    cell = new Vector2Int(layout.width - 4, rng.Next(layout.height));
                    break;
            }

            if (!layout.IsInside(cell) || !DefenseMapPathfinder.IsWalkable(cell))
                continue;

            return DefenseMapGrid.CellToWorld(layout, cell);
        }

        return CoopMapSpawnUtility.SnapToWalkableWorld(layout.GetPlayerSpawnWorld() + new Vector3(16f, 0f, 0f));
    }

    public static CoopSpawnPattern RollPattern(int wave, System.Random rng)
    {
        var roll = rng.NextDouble();
        if (wave % 5 == 0)
            return roll < 0.45d ? CoopSpawnPattern.BeaconSurge : CoopSpawnPattern.AmbushNearPlayer;

        if (roll < 0.28d)
            return CoopSpawnPattern.MapEdge;
        if (roll < 0.52d)
            return CoopSpawnPattern.AmbushNearPlayer;
        if (roll < 0.72d)
            return CoopSpawnPattern.AmbushZone;
        if (roll < 0.88d)
            return CoopSpawnPattern.BeaconSurge;
        return CoopSpawnPattern.RandomInterior;
    }

    private static bool ContainsNear(List<Vector3> points, Vector3 candidate, float minDistance)
    {
        var minSqr = minDistance * minDistance;
        foreach (var point in points)
        {
            if ((point - candidate).sqrMagnitude < minSqr)
                return true;
        }

        return false;
    }

    private static bool IsNearMapEdge(DefenseMapLayout layout, Vector2Int cell)
    {
        const int margin = 6;
        return cell.x <= margin
            || cell.y <= margin
            || cell.x >= layout.width - margin - 1
            || cell.y >= layout.height - margin - 1;
    }
}
