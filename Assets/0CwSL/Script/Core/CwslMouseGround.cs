using System;
using Unity.Netcode;
using UnityEngine;

public static class CwslMouseGround
{
    public static bool TryGetGroundPoint(Camera camera, out Vector3 point, out Collider hitCollider)
    {
        point = default;
        hitCollider = null;
        if (camera == null)
            return false;

        var ray = camera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 500f, ~0, QueryTriggerInteraction.Ignore))
            return false;

        point = hit.point;
        hitCollider = hit.collider;
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
