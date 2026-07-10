using UnityEngine;

/// <summary>
/// 플레이어 추적 카메라.
/// 월드 XZ 축에 맞춘 방송 시점 — 맵이 마름모가 아니라 축구장처럼 직사각형으로 보임.
/// </summary>
public class CwslPlayerCamera : MonoBehaviour
{
    private const float Pitch = 56f;
    private const float Yaw = 0f;
    private const float Distance = 50f;
    private const float FieldOfView = 58f;
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
