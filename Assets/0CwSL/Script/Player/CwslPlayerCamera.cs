using UnityEngine;

public class CwslPlayerCamera : MonoBehaviour
{
    private const float Pitch = 52f;
    private const float Yaw = 45f;
    private const float Distance = 37.5f;
    private const float FieldOfView = 42f;
    private const float FollowSmoothTime = 0.38f;

    private Transform target;
    private Camera cameraComponent;
    private Vector3 followPosition;
    private Vector3 followVelocity;

    public void Initialize(Transform followTarget, Camera camera)
    {
        target = followTarget;
        cameraComponent = camera;

        if (target != null)
            followPosition = target.position;

        if (cameraComponent == null)
            return;

        cameraComponent.orthographic = false;
        cameraComponent.fieldOfView = FieldOfView;
        cameraComponent.nearClipPlane = 0.3f;
        cameraComponent.farClipPlane = 120f;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        transform.rotation = Quaternion.Euler(Pitch, Yaw, 0f);

        followPosition = Vector3.SmoothDamp(
            followPosition,
            target.position,
            ref followVelocity,
            FollowSmoothTime,
            Mathf.Infinity,
            Time.deltaTime);

        transform.position = followPosition - transform.forward * Distance;
    }
}
