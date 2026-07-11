using Unity.Netcode;
using UnityEngine;

/// <summary>마우스 조준 방향 동기화.</summary>
public class StllGlaiveAim : NetworkBehaviour
{
    private readonly NetworkVariable<Vector3> syncedAimDirection = new(
        Vector3.forward,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public Vector3 AimDirection
    {
        get
        {
            var dir = syncedAimDirection.Value;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (!StllMouseAim.TryGetFlatAimDirection(transform, out var direction))
            return;

        SubmitAimServerRpc(direction);
    }

    [ServerRpc]
    private void SubmitAimServerRpc(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        syncedAimDirection.Value = direction.normalized;
    }
}
