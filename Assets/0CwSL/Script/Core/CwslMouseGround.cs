using System;
using Unity.Netcode;
using UnityEngine;

public static class CwslMouseGround
{
    private static readonly Plane ArenaGroundPlane = new(Vector3.up, Vector3.zero);

    public static bool TryGetGroundPoint(Camera camera, out Vector3 point, out Collider hitCollider)
    {
        point = default;
        hitCollider = null;
        if (camera == null)
            return false;

        var ray = camera.ScreenPointToRay(Input.mousePosition);

        // 시야 0: 화면 어두운 구역(시야 밖) 클릭도 바닥 평면으로 이동/스킬 지정
        if (ShouldUseBlindGroundPlanePick() && TryIntersectArenaGround(ray, out point))
            return true;

        if (Physics.Raycast(ray, out var hit, 500f, ~0, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            if (CwslDefensePrepUtility.IsPrepBoundaryActive())
            {
                point = CwslDefensePrepUtility.ClampToPrepArea(
                    point,
                    CwslGameConstants.PlayerBodyColliderRadiusDefault);
            }

            hitCollider = hit.collider;
            return true;
        }

        return TryIntersectArenaGround(ray, out point);
    }

    private static bool ShouldUseBlindGroundPlanePick()
    {
        return CwslPlayerVision.Local != null && CwslPlayerVision.Local.IsBlindVision;
    }

    private static bool TryIntersectArenaGround(Ray ray, out Vector3 point)
    {
        point = default;
        if (!ArenaGroundPlane.Raycast(ray, out var distance) || distance < 0f)
            return false;

        point = ray.GetPoint(distance);
        point.y = 0f;

        if (CwslDefensePrepUtility.IsPrepBoundaryActive())
        {
            point = CwslDefensePrepUtility.ClampToPrepArea(
                point,
                CwslGameConstants.PlayerBodyColliderRadiusDefault);
            return true;
        }

        var extent = CwslArenaUtility.GetPlayHalfExtent();
        point.x = Mathf.Clamp(point.x, -extent, extent);
        point.z = Mathf.Clamp(point.z, -extent, extent);
        return true;
    }

    public static bool TryGetSelectableTarget(Camera camera, out NetworkObject target)
    {
        target = null;
        if (camera == null)
            return false;

        var ray = camera.ScreenPointToRay(Input.mousePosition);
        var hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
            return false;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // 몬스터를 최우선으로 선택 (바닥보다 앞에 있어도 트리거 콜라이더로 잡힘)
        foreach (var hit in hits)
        {
            if (hit.collider == null)
                continue;

            var monsterHealth = hit.collider.GetComponentInParent<CwslMonsterHealth>();
            if (monsterHealth == null || !monsterHealth.IsAlive)
                continue;

            target = monsterHealth.NetworkObject;
            if (target != null)
                return true;
        }

        foreach (var hit in hits)
        {
            if (hit.collider == null)
                continue;

            var playerHealth = hit.collider.GetComponentInParent<CwslPlayerHealth>();
            if (playerHealth == null)
                continue;

            target = playerHealth.NetworkObject;
            if (target != null)
                return true;
        }

        return false;
    }

    public static bool TryGetMonsterOrGround(
        Camera camera,
        out NetworkObject monsterTarget,
        out Vector3 groundPoint,
        out bool hasGround)
    {
        monsterTarget = null;
        groundPoint = default;
        hasGround = false;

        if (TryGetSelectableTarget(camera, out var target))
        {
            var monsterHealth = target.GetComponent<CwslMonsterHealth>();
            if (monsterHealth != null && monsterHealth.IsAlive)
            {
                monsterTarget = target;
                return true;
            }
        }

        hasGround = TryGetGroundPoint(camera, out groundPoint, out _);
        return hasGround;
    }
}
