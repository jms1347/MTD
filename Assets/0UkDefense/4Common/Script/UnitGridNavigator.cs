using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 그리드 A* 경로를 따라 장애물을 우회하며 이동합니다.
/// </summary>
public class UnitGridNavigator : MonoBehaviour
{
    [SerializeField] private float repathInterval = 0.45f;
    [SerializeField] private float waypointReachDistance = 0.28f;
    [SerializeField] private float repathOnTargetMoved = 0.75f;
    [SerializeField] private float stuckRepathDelay = 0.25f;

    private readonly List<Vector3> path = new();
    private int pathIndex;
    private Vector3 lastTarget;
    private float repathTimer;
    private float stuckTimer;

    public Vector3 MoveTowards(
        Vector3 currentPosition,
        Vector3 targetPosition,
        float moveSpeed,
        float radius,
        float bodyHeight,
        float groundY)
    {
        currentPosition.y = groundY;
        targetPosition.y = groundY;

        RefreshPathIfNeeded(currentPosition, targetPosition);

        Vector3 moveGoal = path.Count > 0 && pathIndex < path.Count
            ? path[pathIndex]
            : targetPosition;

        Vector3 toGoal = moveGoal - currentPosition;
        toGoal.y = 0f;
        if (toGoal.sqrMagnitude <= waypointReachDistance * waypointReachDistance && pathIndex < path.Count)
        {
            pathIndex++;
            if (pathIndex < path.Count)
                moveGoal = path[pathIndex];
        }

        Vector3 toMove = moveGoal - currentPosition;
        toMove.y = 0f;
        if (toMove.sqrMagnitude < 0.0001f)
            return currentPosition;

        Vector3 direction = toMove.normalized;
        float step = moveSpeed * Time.deltaTime;
        Vector3 desired = currentPosition + direction * step;
        Vector3 next = TryMoveOnGrid(currentPosition, desired, groundY, radius, bodyHeight);

        if ((next - currentPosition).sqrMagnitude < 0.000001f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckRepathDelay)
            {
                ForceRepath(currentPosition, targetPosition);
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        return next;
    }

    public void ClearPath()
    {
        path.Clear();
        pathIndex = 0;
        repathTimer = 0f;
        stuckTimer = 0f;
    }

    private void OnDisable()
    {
        ClearPath();
    }

    private Vector3 TryMoveOnGrid(
        Vector3 current,
        Vector3 desired,
        float groundY,
        float radius,
        float bodyHeight)
    {
        desired.y = groundY;

        if (DefenseMapPathfinder.IsReady && DefenseMapPathfinder.IsWorldWalkable(desired))
        {
            if (!UnitMovementCollision.IsPositionBlocked(desired, radius, bodyHeight, groundY))
                return desired;
        }

        Vector3 delta = desired - current;
        delta.y = 0f;
        if (delta.sqrMagnitude < 0.0001f)
            return current;

        Vector3 axisX = current + new Vector3(Mathf.Sign(delta.x) * delta.magnitude, 0f, 0f);
        axisX.y = groundY;
        if (Mathf.Abs(delta.x) > 0.0001f
            && DefenseMapPathfinder.IsWorldWalkable(axisX)
            && !UnitMovementCollision.IsPositionBlocked(axisX, radius, bodyHeight, groundY))
            return axisX;

        Vector3 axisZ = current + new Vector3(0f, 0f, Mathf.Sign(delta.z) * delta.magnitude);
        axisZ.y = groundY;
        if (Mathf.Abs(delta.z) > 0.0001f
            && DefenseMapPathfinder.IsWorldWalkable(axisZ)
            && !UnitMovementCollision.IsPositionBlocked(axisZ, radius, bodyHeight, groundY))
            return axisZ;

        return UnitMovementCollision.MoveWithCollision(current, desired, radius, bodyHeight, groundY);
    }

    private void RefreshPathIfNeeded(Vector3 currentPosition, Vector3 targetPosition)
    {
        repathTimer -= Time.deltaTime;

        bool targetMoved = (targetPosition - lastTarget).sqrMagnitude
            >= repathOnTargetMoved * repathOnTargetMoved;
        bool needsPath = path.Count == 0 || pathIndex >= path.Count;

        if (!needsPath && !targetMoved && repathTimer > 0f)
            return;

        lastTarget = targetPosition;
        repathTimer = repathInterval;

        if (!DefenseMapPathfinder.TryFindPath(currentPosition, targetPosition, path))
        {
            path.Clear();
            pathIndex = 0;
            return;
        }

        pathIndex = 0;
    }

    private void ForceRepath(Vector3 currentPosition, Vector3 targetPosition)
    {
        repathTimer = 0f;
        lastTarget = targetPosition + Vector3.one * 999f;
        RefreshPathIfNeeded(currentPosition, targetPosition);
    }
}
