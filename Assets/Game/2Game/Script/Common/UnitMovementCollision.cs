using UnityEngine;

/// <summary>
/// 벽·문 등 고체 장애물과의 충돌을 처리하는 공통 이동 헬퍼.
/// 바닥·농장 타일은 밟고 지나가며, 플레이어 자신은 무시합니다.
/// </summary>
public static class UnitMovementCollision
{
    private const int MaxSlidePasses = 2;

    public static Vector3 MoveWithCollision(
        Vector3 current,
        Vector3 desired,
        float radius,
        float bodyHeight,
        float groundY)
    {
        current.y = groundY;
        desired.y = groundY;

        Vector3 position = current;
        Vector3 remaining = desired - current;
        remaining.y = 0f;

        for (int pass = 0; pass <= MaxSlidePasses; pass++)
        {
            float distance = remaining.magnitude;
            if (distance < 0.0001f)
                break;

            Vector3 direction = remaining / distance;
            if (!TryFindBlockingHit(position, radius, bodyHeight, groundY, direction, distance, out RaycastHit blockHit))
            {
                position += remaining;
                break;
            }

            float moveDistance = Mathf.Max(0f, blockHit.distance - 0.02f);
            if (moveDistance > 0.0001f)
                position += direction * moveDistance;

            Vector3 slide = Vector3.ProjectOnPlane(remaining - direction * Mathf.Min(distance, blockHit.distance), blockHit.normal);
            slide.y = 0f;
            remaining = slide;

            if (slide.sqrMagnitude < 0.0001f)
                break;
        }

        position.y = groundY;
        return position;
    }

    public static bool IsPositionBlocked(
        Vector3 position,
        float radius,
        float bodyHeight,
        float groundY)
    {
        GetCapsulePoints(position, radius, bodyHeight, groundY, out Vector3 bottom, out Vector3 top);
        var overlaps = Physics.OverlapCapsule(
            bottom,
            top,
            radius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        foreach (var overlap in overlaps)
        {
            if (IsBlockingCollider(overlap))
                return true;
        }

        return false;
    }

    public static bool CanReachPosition(
        Vector3 from,
        Vector3 to,
        float radius,
        float bodyHeight,
        float groundY)
    {
        from.y = groundY;
        to.y = groundY;

        Vector3 delta = to - from;
        delta.y = 0f;
        float distance = delta.magnitude;
        if (distance < 0.05f)
            return true;

        Vector3 direction = delta / distance;
        return !TryFindBlockingHit(from, radius, bodyHeight, groundY, direction, distance, out _);
    }

    public static bool TryFindNearestFreePosition(
        Vector3 origin,
        float radius,
        float bodyHeight,
        float groundY,
        float maxSearchRadius,
        out Vector3 freePosition)
    {
        origin.y = groundY;
        freePosition = origin;

        if (!IsPositionBlocked(origin, radius, bodyHeight, groundY))
            return true;

        const int maxRings = 6;
        float step = Mathf.Max(0.45f, radius * 1.1f);
        Vector3[] dirs =
        {
            Vector3.right,
            Vector3.left,
            Vector3.forward,
            Vector3.back,
            (Vector3.right + Vector3.forward).normalized,
            (Vector3.right + Vector3.back).normalized,
            (Vector3.left + Vector3.forward).normalized,
            (Vector3.left + Vector3.back).normalized
        };

        float bestSqr = float.MaxValue;
        bool found = false;

        for (int ring = 1; ring <= maxRings; ring++)
        {
            float ringRadius = step * ring;
            if (ringRadius > maxSearchRadius)
                break;

            foreach (var dir in dirs)
            {
                Vector3 candidate = origin + dir * ringRadius;
                candidate.y = groundY;
                if (IsPositionBlocked(candidate, radius, bodyHeight, groundY))
                    continue;

                float sqr = (candidate - origin).sqrMagnitude;
                if (sqr >= bestSqr)
                    continue;

                bestSqr = sqr;
                freePosition = candidate;
                found = true;
            }

            if (found)
                return true;
        }

        return found;
    }

    private static bool TryFindBlockingHit(
        Vector3 current,
        float radius,
        float bodyHeight,
        float groundY,
        Vector3 direction,
        float distance,
        out RaycastHit closestHit)
    {
        closestHit = default;
        float closestDistance = float.MaxValue;
        bool found = false;

        GetCapsulePoints(current, radius, bodyHeight, groundY, out Vector3 bottom, out Vector3 top);
        var hits = Physics.CapsuleCastAll(
            bottom,
            top,
            radius,
            direction,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        foreach (var hit in hits)
        {
            if (!IsBlockingCollider(hit.collider))
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                found = true;
            }
        }

        return found;
    }

    private static bool IsBlockingCollider(Collider collider)
    {
        if (collider == null || collider.isTrigger)
            return false;

        if (collider.CompareTag("Player") || collider.CompareTag("AllyMinion"))
            return false;

        if (collider.GetComponentInParent<PlayerCharacterController>() != null)
            return false;

        if (collider.CompareTag("Ground") || collider.CompareTag("FarmSoil"))
            return false;

        return collider.CompareTag("Obstacle");
    }

    private static void GetCapsulePoints(
        Vector3 current,
        float radius,
        float bodyHeight,
        float groundY,
        out Vector3 bottom,
        out Vector3 top)
    {
        current.y = groundY;
        bottom = current + Vector3.up * (radius + 0.05f);
        top = bottom + Vector3.up * Mathf.Max(0.25f, bodyHeight - radius * 2f);
    }
}
