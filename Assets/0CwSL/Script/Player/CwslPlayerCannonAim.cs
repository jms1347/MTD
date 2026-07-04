using Unity.Netcode;
using UnityEngine;

public class CwslPlayerCannonAim : NetworkBehaviour
{
    private const float MinPitch = -18f;
    private const float MaxPitch = 42f;
    private const float MaxYaw = 52f;
    private const float NeutralPitch = 0f;
    private const float AimSmoothSpeed = 22f;
    private const float HoldSmoothSpeed = 14f;

    private readonly NetworkVariable<float> syncedPitch = new(
        NeutralPitch,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> syncedYaw = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Transform aimPivot;
    private Transform aimPivotLeft;
    private Transform muzzle;
    private Transform muzzleLeft;
    private float displayPitch;
    private float displayYaw;
    private float targetPitch;
    private float targetYaw;

    public void SetAimServer(Vector3 aimWorldPoint)
    {
        if (!IsServer)
            return;

        if (!TryComputeAimAngles(aimWorldPoint, out var pitch, out var yaw))
            return;

        syncedPitch.Value = pitch;
        syncedYaw.Value = yaw;
    }

    public void SnapAimServer(Vector3 aimWorldPoint)
    {
        if (!IsServer)
            return;

        if (!TryComputeAimAngles(aimWorldPoint, out var pitch, out var yaw))
            return;

        syncedPitch.Value = pitch;
        syncedYaw.Value = yaw;
        displayPitch = pitch;
        displayYaw = yaw;
        ApplyLocalRotation();
    }

    public void SnapAimClient(Vector3 aimWorldPoint)
    {
        if (!TryComputeAimAngles(aimWorldPoint, out var pitch, out var yaw))
            return;

        displayPitch = pitch;
        displayYaw = yaw;
        targetPitch = pitch;
        targetYaw = yaw;
        ApplyLocalRotation();
    }

    public void ResetAimServer()
    {
        if (!IsServer)
            return;

        syncedYaw.Value = 0f;
        syncedPitch.Value = NeutralPitch;
    }

    public Vector3 GetMuzzlePosition()
    {
        CacheTransforms();
        if (muzzle != null)
            return muzzle.position;

        if (aimPivot != null)
            return aimPivot.position + aimPivot.forward * 0.95f;

        return transform.position + Vector3.up * 1.2f + transform.forward * 0.9f;
    }

    public Vector3 GetLeftMuzzlePosition()
    {
        CacheTransforms();
        if (muzzleLeft != null)
            return muzzleLeft.position;

        if (aimPivotLeft != null)
            return aimPivotLeft.position + aimPivotLeft.forward * 0.95f;

        return GetMuzzlePosition();
    }

    public Vector3 GetMuzzleForward()
    {
        CacheTransforms();
        if (muzzle != null)
            return muzzle.forward;
        if (aimPivot != null)
            return aimPivot.forward;
        return transform.forward;
    }

    public Vector3 GetLeftMuzzleForward()
    {
        CacheTransforms();
        if (muzzleLeft != null)
            return muzzleLeft.forward;
        if (aimPivotLeft != null)
            return aimPivotLeft.forward;
        return GetMuzzleForward();
    }

    public bool TryGetAimDirection(Vector3 aimWorldPoint, out Vector3 direction)
    {
        direction = transform.forward;
        if (!TryComputeAimAngles(aimWorldPoint, out var pitch, out var yaw))
            return false;

        CacheTransforms();
        var armBasis = aimPivot != null && aimPivot.parent != null
            ? aimPivot.parent
            : transform;

        direction = armBasis.TransformDirection(
            Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward);
        return direction.sqrMagnitude > 0.0001f;
    }

    private void Update()
    {
        CacheTransforms();
        if (aimPivot == null && aimPivotLeft == null)
            return;

        targetPitch = syncedPitch.Value;
        targetYaw = syncedYaw.Value;
        var smooth = Mathf.Abs(targetPitch) < 0.01f && Mathf.Abs(targetYaw) < 0.01f
            ? HoldSmoothSpeed
            : AimSmoothSpeed;

        displayPitch = Mathf.LerpAngle(displayPitch, targetPitch, Time.deltaTime * smooth);
        displayYaw = Mathf.LerpAngle(displayYaw, targetYaw, Time.deltaTime * smooth);
        ApplyLocalRotation();
    }

    private void ApplyLocalRotation()
    {
        var localRotation = Quaternion.Euler(displayPitch, displayYaw, 0f);
        if (aimPivot != null)
            aimPivot.localRotation = localRotation;
        if (aimPivotLeft != null)
            aimPivotLeft.localRotation = localRotation;
    }

    private bool TryComputeAimAngles(Vector3 aimWorldPoint, out float pitch, out float yaw)
    {
        pitch = NeutralPitch;
        yaw = 0f;

        CacheTransforms();
        if (aimPivot == null && aimPivotLeft == null)
            return false;

        var armBasis = aimPivot != null && aimPivot.parent != null
            ? aimPivot.parent
            : transform;
        var origin = aimPivot != null ? aimPivot.position : armBasis.position + Vector3.up * 1.1f;
        var localDir = armBasis.InverseTransformDirection((aimWorldPoint - origin).normalized);
        if (localDir.sqrMagnitude < 0.0001f)
            return false;

        var flat = new Vector2(localDir.x, localDir.z);
        yaw = flat.sqrMagnitude > 0.0001f
            ? Mathf.Clamp(Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg, -MaxYaw, MaxYaw)
            : 0f;
        pitch = Mathf.Clamp(
            -Mathf.Atan2(localDir.y, flat.magnitude) * Mathf.Rad2Deg,
            MinPitch,
            MaxPitch);
        return true;
    }

    private void CacheTransforms()
    {
        if (aimPivot != null && muzzle != null && aimPivotLeft != null && muzzleLeft != null)
            return;

        var visual = transform.Find("Visual");
        if (visual == null)
            return;

        if (aimPivot == null)
        {
            aimPivot = visual.Find("ArmRPivot/BowAimPivot");
            if (aimPivot == null)
            {
                aimPivot = visual.Find("ArmRPivot/CannonPivot");
                if (aimPivot == null)
                    aimPivot = visual.Find("CannonPivot");
            }
        }

        if (aimPivotLeft == null)
        {
            aimPivotLeft = visual.Find("ArmLPivot/BowAimPivotL");
            if (aimPivotLeft == null)
                aimPivotLeft = visual.Find("ArmLPivot/CannonPivotL");
        }

        if (muzzle == null)
        {
            var rightCannon = visual.Find("ArmRPivot/BowAimPivot/CannonPivot");
            if (rightCannon == null)
                rightCannon = visual.Find("ArmRPivot/CannonPivot");
            if (rightCannon == null)
                rightCannon = visual.Find("CannonPivot");
            muzzle = rightCannon != null ? rightCannon.Find("Muzzle") : null;
        }

        if (muzzleLeft == null)
        {
            var leftCannon = visual.Find("ArmLPivot/BowAimPivotL/CannonPivotL");
            if (leftCannon == null)
                leftCannon = visual.Find("ArmLPivot/CannonPivotL");
            muzzleLeft = leftCannon != null ? leftCannon.Find("Muzzle") : null;
        }
    }
}
