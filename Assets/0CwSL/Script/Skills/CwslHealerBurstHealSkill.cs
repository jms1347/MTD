using Unity.Netcode;
using UnityEngine;

public class CwslHealerBurstHealSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotE;

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
        var radius = CwslGameConstants.HealerBurstHealRadius;
        var radiusSq = radius * radius;
        var players = CwslCombatRegistry.AlivePlayers;
        foreach (var player in players)
        {
            if (player == null || !player.IsAlive)
                continue;

            var flat = player.transform.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSq)
                continue;

            var amount = player.MaxHealth * CwslGameConstants.HealerBurstHealRatio;
            player.TryHealServer(amount);
            PlayHealBurstClientRpc(player.transform.position);
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
    private void PlayHealBurstClientRpc(Vector3 position)
    {
        CwslVfxSpawner.SpawnHealerHealBurst(position);
    }
}
