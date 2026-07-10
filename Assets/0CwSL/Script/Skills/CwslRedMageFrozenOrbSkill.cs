using Unity.Netcode;
using UnityEngine;

/// <summary>빨간 마법사 E — 디아 오브. 직선 비행하며 8방향 얼음 파편 분사 + 동상 (UkDefense I-0007).</summary>
public class CwslRedMageFrozenOrbSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotE;

    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.RedMage;

    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        playerCharacter = GetComponent<CwslPlayerCharacter>();
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

        return TryCastServer(senderClientId, worldPoint);
    }

    public bool TryCastServer(ulong senderClientId, Vector3 worldPoint)
    {
        if (!CanCastServer(senderClientId))
            return false;

        var direction = ResolveFireDirection(worldPoint);
        FaceFireDirection(direction);
        FireOrbServer(direction);
        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        PlayCastClientRpc();
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

    private Vector3 ResolveFireDirection(Vector3 worldPoint)
    {
        var flat = worldPoint - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            flat = transform.forward;

        return flat.normalized;
    }

    private void FaceFireDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    private void FireOrbServer(Vector3 direction)
    {
        var session = CwslGameSession.Instance;
        if (session == null || session.Assets.frozenOrbPrefab == null)
            return;

        var spawnPosition = ResolveSpawnPosition(direction);
        var networkObject = CwslNetworkPoolService.Instance?.Get(
            session.Assets.frozenOrbPrefab,
            spawnPosition,
            Quaternion.LookRotation(direction, Vector3.up));
        if (networkObject == null)
            return;

        var attackPower = playerCharacter != null
            ? CwslCharacterStatCatalog.GetAttackPower(playerCharacter.CharacterId)
            : CwslGameConstants.AttackDamage;

        var lifetime = CwslGameConstants.RedMageFrozenOrbRange / CwslGameConstants.RedMageFrozenOrbSpeed;
        networkObject.GetComponent<CwslFrozenOrbProjectile>()?.ConfigureAsOrb(
            direction,
            CwslGameConstants.RedMageFrozenOrbSpeed,
            lifetime,
            OwnerClientId,
            attackPower,
            NetworkObject,
            CwslGameConstants.RedMageFrozenOrbFrostDuration,
            CwslGameConstants.RedMageFrozenOrbFrostStacks);
    }

    private Vector3 ResolveSpawnPosition(Vector3 direction)
    {
        var visual = transform.Find("Visual");
        if (visual != null)
        {
            var staffPivot = visual.Find("CastArmPivot/StaffPivot");
            if (staffPivot != null)
                return staffPivot.position + direction * 0.2f;
        }

        return transform.position + Vector3.up * 1.1f + direction * 0.7f;
    }

    [ClientRpc]
    private void PlayCastClientRpc()
    {
        var visual = transform.Find("Visual");
        visual?.GetComponent<CwslPlayerStaffCastVisual>()?.PlayCast();
        CwslSkillAudioFeedback.PlayFrozenOrbCast(transform.position);
    }
}
