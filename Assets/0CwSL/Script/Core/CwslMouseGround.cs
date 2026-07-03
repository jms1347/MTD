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
        if (!Physics.Raycast(ray, out var hit, 500f, ~0, QueryTriggerInteraction.Collide))
            return false;

        var monsterHealth = hit.collider.GetComponentInParent<CwslMonsterHealth>();
        if (monsterHealth != null && monsterHealth.IsAlive)
        {
            target = monsterHealth.GetComponent<NetworkObject>();
            return target != null;
        }

        var playerHealth = hit.collider.GetComponentInParent<CwslPlayerHealth>();
        if (playerHealth != null)
        {
            target = playerHealth.GetComponent<NetworkObject>();
            return target != null;
        }

        return false;
    }
}
