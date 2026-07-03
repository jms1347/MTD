using Unity.Netcode;
using UnityEngine;

public class CwslPlayerCannonAim : NetworkBehaviour
{
    private const float MinPitch = -8f;
    private const float MaxPitch = 42f;
    private const float MaxYaw = 88f;
    private const float NeutralPitch = 8f;
    private const float AimSmoothSpeed = 16f;

    private readonly NetworkVariable<float> syncedPitch = new(
        NeutralPitch,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> syncedYaw = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Transform cannonPivot;
    private Transform muzzle;
    private float displayPitch;
    private float displayYaw;

    public void SetAimServer(Vector3 aimWorldPoint)
    {
        if (!IsServer)
            return;

        CacheTransforms();
        if (cannonPivot == null)
            return;

        var localDir = transform.InverseTransformDirection((aimWorldPoint - cannonPivot.position).normalized);
        if (localDir.sqrMagnitude < 0.0001f)
            return;

        var flat = new Vector2(localDir.x, localDir.z);
        syncedYaw.Value = flat.sqrMagnitude > 0.0001f
            ? Mathf.Clamp(Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg, -MaxYaw, MaxYaw)
            : 0f;
        syncedPitch.Value = Mathf.Clamp(
            -Mathf.Atan2(localDir.y, flat.magnitude) * Mathf.Rad2Deg,
            MinPitch,
            MaxPitch);
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

        if (cannonPivot != null)
            return cannonPivot.position + cannonPivot.forward * 0.95f;

        return transform.position + Vector3.up * 1.2f + transform.forward * 0.9f;
    }

    private void Update()
    {
        CacheTransforms();
        if (cannonPivot == null)
            return;

        displayPitch = Mathf.LerpAngle(displayPitch, syncedPitch.Value, Time.deltaTime * AimSmoothSpeed);
        displayYaw = Mathf.LerpAngle(displayYaw, syncedYaw.Value, Time.deltaTime * AimSmoothSpeed);
        cannonPivot.localRotation = Quaternion.Euler(displayPitch, displayYaw, 0f);
    }

    private void CacheTransforms()
    {
        if (cannonPivot != null && muzzle != null)
            return;

        var visual = transform.Find("Visual");
        if (visual == null)
            return;

        cannonPivot = visual.Find("CannonPivot");
        muzzle = cannonPivot != null ? cannonPivot.Find("Muzzle") : null;
    }
}
