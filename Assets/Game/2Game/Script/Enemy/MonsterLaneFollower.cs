using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스폰 방향별 고정 레인 웨이포인트를 따라 이동합니다.
/// </summary>
public class MonsterLaneFollower : MonoBehaviour
{
    [SerializeField] private float waypointReachDistance = 0.42f;

    private readonly List<Vector3> waypoints = new();
    private int waypointIndex;
    private bool isActive;

    public bool IsActive => isActive && waypointIndex < waypoints.Count;

    public void Configure(SpawnDirection direction, Vector3 spawnWorldPosition)
    {
        waypoints.Clear();
        waypointIndex = 0;
        isActive = false;

        if (!DefenseMonsterLaneRegistry.TryGetLaneWaypoints(direction, out var lane) || lane.Count == 0)
            return;

        waypoints.AddRange(lane);
        waypointIndex = FindClosestWaypointIndex(spawnWorldPosition);
        isActive = true;
    }

    public void ClearLane()
    {
        waypoints.Clear();
        waypointIndex = 0;
        isActive = false;
    }

    public bool TryGetMoveTarget(out Vector3 target)
    {
        target = default;
        if (!IsActive)
            return false;

        AdvanceIfReached(transform.position);
        if (waypointIndex >= waypoints.Count)
        {
            isActive = false;
            return false;
        }

        target = waypoints[waypointIndex];
        return true;
    }

    private void AdvanceIfReached(Vector3 currentPosition)
    {
        while (waypointIndex < waypoints.Count)
        {
            Vector3 flat = waypoints[waypointIndex] - currentPosition;
            flat.y = 0f;
            if (flat.sqrMagnitude > waypointReachDistance * waypointReachDistance)
                break;

            waypointIndex++;
        }
    }

    private int FindClosestWaypointIndex(Vector3 spawnWorldPosition)
    {
        int best = 0;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 flat = waypoints[i] - spawnWorldPosition;
            flat.y = 0f;
            float sqr = flat.sqrMagnitude;
            if (sqr >= bestSqr)
                continue;

            bestSqr = sqr;
            best = i;
        }

        return best;
    }
}
