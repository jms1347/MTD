using Unity.Netcode;
using UnityEngine;

public class CwslRangedCannonAim : NetworkBehaviour
{
    private const float MinPitch = -12f;
    private const float MaxPitch = 58f;
    private const float MaxYaw = 95f;
    private const float NeutralPitch = 12f;
    private const float AimSmoothSpeed = 14f;

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

    public override void OnNetworkSpawn()
    {
        CacheTransforms();
        displayPitch = syncedPitch.Value;
        displayYaw = syncedYaw.Value;
        ApplyLocalAim(displayPitch, displayYaw);
    }

    public void SetAimServer(Vector3 aimWorldPoint)
    {
        if (!IsServer)
            return;

        CacheTransforms();
        if (cannonPivot == null)
            return;

        var origin = cannonPivot.position;
        var localDir = transform.InverseTransformDirection((aimWorldPoint - origin).normalized);
        if (localDir.sqrMagnitude < 0.0001f)
            return;

        var flat = new Vector2(localDir.x, localDir.z);
        var yaw = flat.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg
            : 0f;
        var pitch = -Mathf.Atan2(localDir.y, flat.magnitude) * Mathf.Rad2Deg;

        syncedYaw.Value = Mathf.Clamp(yaw, -MaxYaw, MaxYaw);
        syncedPitch.Value = Mathf.Clamp(pitch, MinPitch, MaxPitch);
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
            return cannonPivot.position + cannonPivot.forward * 0.48f;

        return transform.position + Vector3.up * 1.1f + transform.forward * 0.8f;
    }

    private void Update()
    {
        if (cannonPivot == null)
            return;

        displayPitch = Mathf.LerpAngle(displayPitch, syncedPitch.Value, Time.deltaTime * AimSmoothSpeed);
        displayYaw = Mathf.LerpAngle(displayYaw, syncedYaw.Value, Time.deltaTime * AimSmoothSpeed);
        ApplyLocalAim(displayPitch, displayYaw);
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

    private void ApplyLocalAim(float pitch, float yaw)
    {
        if (cannonPivot != null)
            cannonPivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
