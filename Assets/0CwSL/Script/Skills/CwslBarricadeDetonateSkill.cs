using Unity.Netcode;
using UnityEngine;

/// <summary>바리케이드 R — 내가 설치한 모든 벽 동시 폭파 + 화상.</summary>
public class CwslBarricadeDetonateSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotR;

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
        CwslBarricadeWallRegistry.DetonateOwnerWallsServer(OwnerClientId);
        return true;
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
