using Unity.Netcode;
using UnityEngine;

/// <summary>바리케이드 W — 점프 발판 설치.</summary>
public class CwslBarricadeJumpPadSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotW;

    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;
    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.Barricade;
    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
    }

    public override bool CanUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint) =>
        slotIndex == BoundSlotIndex && CanCastServer(senderClientId);

    public override bool TryUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint)
    {
        if (!IsServer || slotIndex != BoundSlotIndex)
            return false;

        if (!CanCastServer(senderClientId))
            return false;

        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        var point = worldPoint;
        if (point.sqrMagnitude < 0.01f)
            point = transform.position + transform.forward * 2f;
        point.y = 0.05f;
        point = CwslArenaUtility.ClampToPlayArea(point, 0.5f);
        CwslBarricadeJumpPad.SpawnServer(point);
        PlayJumpPadClientRpc(point);
        return true;
    }

    [ClientRpc]
    private void PlayJumpPadClientRpc(Vector3 point)
    {
        if (IsServer)
            return;
        CwslBarricadeJumpPad.SpawnVisualOnly(point, CwslGameConstants.BarricadeJumpPadLifetime);
    }

    private bool CanCastServer(ulong senderClientId)
    {
        if (!IsServer || senderClientId != OwnerClientId)
            return false;
        if (skillCooldowns != null && !skillCooldowns.IsReady(BoundSlotIndex))
            return false;
        if (playerHealth != null && !playerHealth.IsAlive)
            return false;
        if (playerStun != null && playerStun.IsStunned)
            return false;
        return true;
    }
}
