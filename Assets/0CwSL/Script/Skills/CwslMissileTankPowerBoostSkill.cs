using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>총잡이 W — 10초 버프: 관통, 이동속도 2배, 평타 쿨타임 제거.</summary>
public class CwslMissileTankPowerBoostSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotW;

    private CwslPlayerSkillCooldowns skillCooldowns;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslMissileTankSmokeDashSkill smokeDashSkill;
    private Coroutine activeRoutine;
    private GameObject attachedGlow;

    public bool IsActive { get; private set; }

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.MissileTank;

    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        smokeDashSkill = GetComponent<CwslMissileTankSmokeDashSkill>();
    }

    public override bool CanUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint) =>
        slotIndex == BoundSlotIndex && CanCastServer(senderClientId);

    public override bool TryUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint)
    {
        if (!IsServer || slotIndex != BoundSlotIndex)
            return false;

        return TryActivateServer(senderClientId);
    }

    public bool TryActivateServer(ulong senderClientId)
    {
        if (!CanCastServer(senderClientId))
            return false;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(ActiveRoutine());
        return true;
    }

    public bool CanCastServer(ulong senderClientId)
    {
        if (!IsServer || senderClientId != OwnerClientId)
            return false;

        if (IsActive)
            return false;

        if (skillCooldowns != null && !skillCooldowns.IsReady(BoundSlotIndex))
            return false;

        if (playerHealth != null && !playerHealth.IsAlive)
            return false;

        if (playerStun != null && playerStun.IsStunned)
            return false;

        if (smokeDashSkill != null && smokeDashSkill.IsDashing)
            return false;

        return true;
    }

    private IEnumerator ActiveRoutine()
    {
        IsActive = true;
        skillCooldowns?.BeginCooldown(BoundSlotIndex);

        CwslMoveSpeedBuff.Ensure(this)?.ApplyBuff(
            CwslGameConstants.MissileTankPowerBoostSpeedMultiplier,
            CwslGameConstants.MissileTankPowerBoostDuration);

        PlayBoostClientRpc(true);
        yield return new WaitForSeconds(CwslGameConstants.MissileTankPowerBoostDuration);

        IsActive = false;
        CwslMoveSpeedBuff.Ensure(this)?.ClearBuff();
        PlayBoostClientRpc(false);
        activeRoutine = null;
    }

    [ClientRpc]
    private void PlayBoostClientRpc(bool active)
    {
        if (attachedGlow != null)
        {
            Destroy(attachedGlow);
            attachedGlow = null;
        }

        if (!active)
            return;

        var visual = transform.Find("Visual");
        if (visual == null)
            return;

        attachedGlow = CwslVfxSpawner.AttachMissileTankPowerBoostGlow(visual);
    }

    private void OnDisable()
    {
        if (attachedGlow != null)
        {
            Destroy(attachedGlow);
            attachedGlow = null;
        }
    }
}
