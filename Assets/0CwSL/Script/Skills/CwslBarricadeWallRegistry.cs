using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>바리케이드가 설치한 벽 목록.</summary>
public static class CwslBarricadeWallRegistry
{
    private static readonly List<CwslBarricadeWall> walls = new(32);

    public static IReadOnlyList<CwslBarricadeWall> Walls => walls;

    public static void Register(CwslBarricadeWall wall)
    {
        if (wall == null || walls.Contains(wall))
            return;
        walls.Add(wall);
    }

    public static void Unregister(CwslBarricadeWall wall)
    {
        if (wall == null)
            return;
        walls.Remove(wall);
    }

    public static bool TryGetNearestBlockingWall(
        Vector3 from,
        Vector3 to,
        out CwslBarricadeWall wall,
        out Vector3 hitPoint)
    {
        wall = null;
        hitPoint = to;
        var best = float.MaxValue;
        var move = to - from;
        move.y = 0f;
        if (move.sqrMagnitude < 0.0001f)
            return false;

        for (var i = walls.Count - 1; i >= 0; i--)
        {
            var candidate = walls[i];
            if (candidate == null || !candidate.IsAlive)
            {
                if (candidate == null)
                    walls.RemoveAt(i);
                continue;
            }

            if (!candidate.TryGetSegmentCrossing(from, to, out var cross, out var dist))
                continue;

            if (dist >= best)
                continue;

            best = dist;
            wall = candidate;
            hitPoint = cross;
        }

        return wall != null;
    }

    public static bool TryGetNearestAliveWall(Vector3 from, float maxDistance, out CwslBarricadeWall wall)
    {
        wall = null;
        var best = maxDistance;
        for (var i = 0; i < walls.Count; i++)
        {
            var candidate = walls[i];
            if (candidate == null || !candidate.IsAlive)
                continue;

            var d = Vector3.Distance(from, candidate.transform.position);
            if (d >= best)
                continue;

            best = d;
            wall = candidate;
        }

        return wall != null;
    }

    public static void DetonateOwnerWallsServer(ulong ownerClientId)
    {
        for (var i = walls.Count - 1; i >= 0; i--)
        {
            var wall = walls[i];
            if (wall == null)
            {
                walls.RemoveAt(i);
                continue;
            }

            if (wall.OwnerClientId != ownerClientId || !wall.IsAlive)
                continue;

            wall.DetonateServer();
        }
    }
}
