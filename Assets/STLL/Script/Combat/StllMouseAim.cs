using UnityEngine;

public static class StllMouseAim
{
    public static bool TryGetFlatAimDirection(Transform origin, out Vector3 direction)
    {
        direction = Vector3.forward;
        var camera = Camera.main;
        if (camera == null || origin == null)
            return false;

        var ray = camera.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, origin.position);
        if (!plane.Raycast(ray, out var distance))
            return false;

        var point = ray.GetPoint(distance);
        direction = point - origin.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return false;

        direction.Normalize();
        return true;
    }
}
