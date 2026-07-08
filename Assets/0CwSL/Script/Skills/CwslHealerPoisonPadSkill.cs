using Unity.Netcode;
using UnityEngine;

public class CwslHealerPoisonPadSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = 3;

    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;
    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.Healer;
    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
    }

    public override bool CanUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint) =>
        slotIndex == BoundSlotIndex && CanCast(senderClientId);

    public override bool TryUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint)
    {
        if (!IsServer || slotIndex != BoundSlotIndex || !CanCast(senderClientId))
            return false;

        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        var point = worldPoint.sqrMagnitude < 0.01f ? transform.position + transform.forward * 2f : worldPoint;
        point.y = 0.05f;
        point = CwslArenaUtility.ClampToPlayArea(point, 0.5f);
        CwslHealerPoisonPad.SpawnServer(point, OwnerClientId);
        return true;
    }

    private bool CanCast(ulong senderClientId)
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
