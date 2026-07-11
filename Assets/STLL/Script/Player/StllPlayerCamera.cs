using UnityEngine;

/// <summary>
/// STLL 고정 시점 추적 카메라 + 이동 방향 전방 오프셋.
/// </summary>
public class StllPlayerCamera : MonoBehaviour
{
    private Transform target;
    private StllHorseMotor motor;
    private Camera cameraComponent;
    private Vector3 followPosition;
    private Vector3 followVelocity;

    public void Initialize(Transform followTarget, Camera camera)
    {
        target = followTarget;
        cameraComponent = camera;
        motor = followTarget != null ? followTarget.GetComponent<StllHorseMotor>() : null;

        if (target != null)
            followPosition = target.position;

        if (cameraComponent == null)
            return;

        cameraComponent.orthographic = false;
        cameraComponent.fieldOfView = StllGameConstants.CameraFieldOfView;
        cameraComponent.nearClipPlane = 0.3f;
        cameraComponent.farClipPlane = 120f;

        ApplyFixedRotation();
        if (target != null)
            transform.position = ComputeCameraPosition(target.position);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (motor == null)
            motor = target.GetComponent<StllHorseMotor>();

        ApplyFixedRotation();

        var desiredFocus = target.position + ComputeLeadOffset();
        followPosition = Vector3.SmoothDamp(
            followPosition,
            desiredFocus,
            ref followVelocity,
            StllGameConstants.CameraFollowSmoothTime,
            Mathf.Infinity,
            Time.deltaTime);

        transform.position = ComputeCameraPosition(followPosition);
    }

    private Vector3 ComputeLeadOffset()
    {
        if (motor == null || motor.CurrentSpeed < 0.5f)
            return Vector3.zero;

        var leadDir = motor.MoveDirection.sqrMagnitude > 0.001f
            ? motor.MoveDirection
            : motor.HeadingDirection;
        return leadDir * StllGameConstants.CameraLeadDistance;
    }

    private Vector3 ComputeCameraPosition(Vector3 focusPoint)
    {
        return focusPoint - transform.forward * StllGameConstants.CameraDistance;
    }

    private void ApplyFixedRotation()
    {
        transform.rotation = Quaternion.Euler(
            StllGameConstants.CameraPitch,
            StllGameConstants.CameraYaw,
            0f);
    }
}
