using Unity.Netcode;
using UnityEngine;

/// <summary>플레이어 도원결의 역할 — 순번 배정 후 비주얼·전투 보정에 사용.</summary>
public class StllBrotherhoodRoleState : NetworkBehaviour
{
    private readonly NetworkVariable<byte> syncedRole = new(
        (byte)StllBrotherhoodRole.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private StllMountAssembly mountAssembly;

    public StllBrotherhoodRole Role => (StllBrotherhoodRole)syncedRole.Value;

    private void Awake()
    {
        mountAssembly = GetComponent<StllMountAssembly>();
    }

    public override void OnNetworkSpawn()
    {
        syncedRole.OnValueChanged += HandleRoleChanged;
        ApplyRoleVisual(Role);
    }

    public override void OnNetworkDespawn()
    {
        syncedRole.OnValueChanged -= HandleRoleChanged;
    }

    public void AssignRoleServer(StllBrotherhoodRole role)
    {
        if (!IsServer)
            return;

        syncedRole.Value = (byte)role;
    }

    private void HandleRoleChanged(byte previous, byte current)
    {
        ApplyRoleVisual((StllBrotherhoodRole)current);
    }

    private void ApplyRoleVisual(StllBrotherhoodRole role)
    {
        if (mountAssembly == null || role == StllBrotherhoodRole.None)
            return;

        mountAssembly.RebuildForRole(role);
    }
}
