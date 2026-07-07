using Unity.Netcode;
using UnityEngine;

/// <summary>미사일 탱커 R — 일반/Q 탄환 종류 순환.</summary>
public class CwslMissileTankAmmoController : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = 2;

    private readonly NetworkVariable<byte> networkAmmoKind = new(
        (byte)CwslMissileTankAmmoKind.Basic,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private CwslPlayerSkillCooldowns skillCooldowns;

    public CwslMissileTankAmmoKind CurrentAmmo => (CwslMissileTankAmmoKind)networkAmmoKind.Value;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.MissileTank;

    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
        networkAmmoKind.OnValueChanged += HandleAmmoChanged;
    }

    public override void OnNetworkDespawn()
    {
        networkAmmoKind.OnValueChanged -= HandleAmmoChanged;
    }

    public override bool CanUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint) =>
        slotIndex == BoundSlotIndex && CanCastServer(senderClientId);

    public override bool TryUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint)
    {
        if (!IsServer || slotIndex != BoundSlotIndex)
            return false;

        return TryCycleServer(senderClientId);
    }

    public bool TryCycleServer(ulong senderClientId)
    {
        if (!CanCastServer(senderClientId))
            return false;

        var next = (byte)(((int)CurrentAmmo + 1) % 4);
        networkAmmoKind.Value = next;
        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        PlayAmmoSwitchClientRpc(next);
        return true;
    }

    public bool CanCastServer(ulong senderClientId)
    {
        if (!IsServer || senderClientId != OwnerClientId)
            return false;

        return skillCooldowns == null || skillCooldowns.IsReady(BoundSlotIndex);
    }

    private void HandleAmmoChanged(byte previous, byte current)
    {
        if (IsServer)
            return;
    }

    [ClientRpc]
    private void PlayAmmoSwitchClientRpc(byte ammoKind)
    {
        var visual = transform.Find("Visual");
        visual?.GetComponent<CwslMissileTankAmmoHudVisual>()?.ShowAmmoSwitch((CwslMissileTankAmmoKind)ammoKind);
    }
}
