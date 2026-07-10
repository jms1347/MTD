using Unity.Netcode;
using UnityEngine;

public class CwslHealerHasteBuffSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotR;

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
        PlayHasteCastBurstClientRpc(transform.position);
        var radius = CwslGameConstants.HealerHasteBuffRadius;
        var radiusSq = radius * radius;
        var duration = CwslGameConstants.HealerHasteBuffDuration;
        var players = CwslCombatRegistry.AlivePlayers;
        foreach (var player in players)
        {
            if (player == null || !player.IsAlive)
                continue;

            var flat = player.transform.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSq)
                continue;

            CwslMoveSpeedBuff.Ensure(player)?.ApplyBuff(
                CwslGameConstants.HealerHasteMoveMultiplier,
                duration);
            CwslAttackSpeedBuff.Ensure(player)?.ApplyBuff(
                CwslGameConstants.HealerHasteAttackMultiplier,
                duration);
            PlayBuffClientRpc(player.NetworkObjectId);
        }

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

    [ClientRpc]
    private void PlayBuffClientRpc(ulong targetObjectId)
    {
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out var obj) ||
            obj == null)
            return;

        var visual = obj.transform.Find("Visual") ?? obj.transform;
        CwslVfxSpawner.AttachHealerHasteBuff(visual, CwslGameConstants.HealerHasteBuffDuration);
    }

    [ClientRpc]
    private void PlayHasteCastBurstClientRpc(Vector3 center)
    {
        CwslVfxSpawner.SpawnHealerHealBurst(center, new Color(1f, 0.9f, 0.2f, 1f));
    }
}
