using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>질주자 R — 불꽃 트레일 + 바닥 불길 장판(화상).</summary>
public class CwslRammerFireTrailSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotR;

    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private CwslMomentumRammerSkill rammerSkill;
    private CwslRammerRopeSkill ropeSkill;
    private Coroutine trailRoutine;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.MomentumRammer;

    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
        rammerSkill = GetComponent<CwslMomentumRammerSkill>();
        ropeSkill = GetComponent<CwslRammerRopeSkill>();
    }

    public override bool CanUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint) =>
        slotIndex == BoundSlotIndex && CanCastServer(senderClientId);

    public override bool TryUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint)
    {
        if (!IsServer || slotIndex != BoundSlotIndex)
            return false;

        return TryCastServer(senderClientId);
    }

    public bool TryCastServer(ulong senderClientId)
    {
        if (!CanCastServer(senderClientId))
            return false;

        if (ropeSkill != null && ropeSkill.HasActiveLink && ropeSkill.TryTriggerSpinServer())
        {
            skillCooldowns?.BeginCooldown(BoundSlotIndex);
            return true;
        }

        if (!CanActivateFireTrailBySpeed())
            return false;

        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        if (trailRoutine != null)
            StopCoroutine(trailRoutine);

        trailRoutine = StartCoroutine(FireTrailRoutine());
        PlayTrailStartClientRpc();
        return true;
    }

    public bool CanCastServer(ulong senderClientId)
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

    private IEnumerator FireTrailRoutine()
    {
        var duration = CwslGameConstants.RammerFireTrailDuration;
        var interval = CwslGameConstants.RammerFireTrailDropInterval;
        var elapsed = 0f;
        var nextDrop = 0f;
        var lastDrop = transform.position;

        while (elapsed < duration)
        {
            if (playerHealth != null && !playerHealth.IsAlive)
                break;

            elapsed += Time.deltaTime;
            if (Time.time >= nextDrop)
            {
                nextDrop = Time.time + interval;
                var dropPos = transform.position;
                dropPos.y = 0.05f;
                if ((dropPos - lastDrop).sqrMagnitude > 0.2f * 0.2f || elapsed < 0.05f)
                {
                    lastDrop = dropPos;
                    SpawnDenseTrailDropsServer(dropPos);
                }
            }

            yield return null;
        }

        StopTrailVisualClientRpc();
        trailRoutine = null;
    }

    [ClientRpc]
    private void PlayTrailStartClientRpc()
    {
        CwslVfxSpawner.AttachRammerFireTrail(transform);
    }

    [ClientRpc]
    private void SpawnTrailDropClientRpc(Vector3 position)
    {
        CwslVfxSpawner.SpawnRammerFireTrailZone(position);
    }

    [ClientRpc]
    private void StopTrailVisualClientRpc()
    {
        CwslVfxSpawner.ClearRammerFireTrail(transform);
    }

    private bool CanActivateFireTrailBySpeed()
    {
        var speed = rammerSkill != null ? rammerSkill.CurrentSpeed : 0f;
        var baseSpeed = CwslCharacterStatCatalog.GetMoveSpeed(CwslCharacterId.MomentumRammer);
        var threshold = baseSpeed * CwslGameConstants.RammerFireTrailActivationSpeedMultiplier;
        return speed >= threshold;
    }

    private void SpawnDenseTrailDropsServer(Vector3 center)
    {
        var count = Mathf.Max(1, CwslGameConstants.RammerFireTrailDropBurstCount);
        var radius = Mathf.Max(0f, CwslGameConstants.RammerFireTrailDropBurstRadius);
        for (var i = 0; i < count; i++)
        {
            var offset = i == 0
                ? Vector3.zero
                : Random.insideUnitSphere * radius;
            offset.y = 0f;
            var dropPos = center + offset;
            dropPos.y = 0.05f;
            CwslRammerFireTrailZone.SpawnServer(dropPos, OwnerClientId);
            SpawnTrailDropClientRpc(dropPos);
        }
    }
}
