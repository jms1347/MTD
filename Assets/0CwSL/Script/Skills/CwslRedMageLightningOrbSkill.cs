using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>빨간 마법사 W — 전방 라이트닝 구슬 + 체인 번개(최대 5회) + 감전.</summary>
public class CwslRedMageLightningOrbSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = 3;

    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private Coroutine castRoutine;

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

        var direction = ResolveDirection(worldPoint);
        FaceDirection(direction);
        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        PlayCastClientRpc();

        if (castRoutine != null)
            StopCoroutine(castRoutine);

        castRoutine = StartCoroutine(CastRoutine(direction));
        return true;
    }

    public bool CanCastServer(ulong senderClientId)
    {
        if (!IsServer || senderClientId != OwnerClientId)
            return false;

        if (castRoutine != null)
            return false;

        if (skillCooldowns != null && !skillCooldowns.IsReady(BoundSlotIndex))
            return false;

        if (playerHealth != null && !playerHealth.IsAlive)
            return false;

        return playerStun == null || !playerStun.IsStunned;
    }

    private IEnumerator CastRoutine(Vector3 direction)
    {
        var orbPosition = ResolveOrbPosition(direction);
        PlayLightningOrbClientRpc(orbPosition);

        yield return new WaitForSeconds(CwslGameConstants.RedMageLightningOrbChargeSeconds);

        var damage = playerCharacter != null
            ? CwslCharacterStatCatalog.GetAttackPower(playerCharacter.CharacterId)
            : CwslGameConstants.AttackDamage;

        ExecuteChainLightningServer(orbPosition, damage);
        castRoutine = null;
    }

    private void ExecuteChainLightningServer(Vector3 startPoint, float damage)
    {
        var hitIds = new HashSet<ulong>();
        var chainPoint = startPoint;
        var chainRadius = CwslGameConstants.RedMageLightningChainRadius;
        var maxHits = CwslGameConstants.RedMageLightningChainMaxHits;
        var shockDuration = CwslGameConstants.RedMageLightningShockDuration;

        for (var i = 0; i < maxHits; i++)
        {
            var target = FindNearestMonster(chainPoint, chainRadius, hitIds);
            if (target == null)
                break;

            var hitPoint = target.GetFlatHitPoint();
            if (target.NetworkObject != null)
                hitIds.Add(target.NetworkObject.NetworkObjectId);

            target.DamageFromPlayer(OwnerClientId, damage);
            CwslMonsterStatusController.Ensure(target)?.ApplyShockServer(shockDuration);
            PlayChainExplosionClientRpc(hitPoint);
            chainPoint = hitPoint;
        }
    }

    private static CwslMonsterHealth FindNearestMonster(
        Vector3 origin,
        float radius,
        HashSet<ulong> excludeIds)
    {
        CwslMonsterHealth best = null;
        var bestDistance = float.MaxValue;
        var radiusSq = radius * radius;
        var monsters = Object.FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);

        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (monster.NetworkObject != null && excludeIds.Contains(monster.NetworkObject.NetworkObjectId))
                continue;

            var flat = monster.GetFlatHitPoint() - origin;
            flat.y = 0f;
            var distanceSq = flat.sqrMagnitude;
            if (distanceSq > radiusSq || distanceSq >= bestDistance)
                continue;

            bestDistance = distanceSq;
            best = monster;
        }

        return best;
    }

    private Vector3 ResolveDirection(Vector3 worldPoint)
    {
        var flat = worldPoint - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            flat = transform.forward;

        return flat.normalized;
    }

    private void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    private Vector3 ResolveOrbPosition(Vector3 direction)
    {
        var forward = direction.sqrMagnitude > 0.0001f ? direction : transform.forward;
        return transform.position
               + forward * CwslGameConstants.RedMageLightningOrbForwardDistance
               + Vector3.up * CwslGameConstants.RedMageLightningOrbHeight;
    }

    [ClientRpc]
    private void PlayCastClientRpc()
    {
        var visual = transform.Find("Visual");
        visual?.GetComponent<CwslPlayerStaffCastVisual>()?.PlayCast();
    }

    [ClientRpc]
    private void PlayLightningOrbClientRpc(Vector3 orbPosition)
    {
        var prefab = CwslGameSession.Instance?.Assets?.redMageLightningOrbVfx;
        if (prefab == null)
            return;

        CwslVfxSpawner.Spawn(
            prefab,
            orbPosition,
            Quaternion.identity,
            CwslGameConstants.RedMageLightningOrbLifetime,
            0.85f);
    }

    [ClientRpc]
    private void PlayChainExplosionClientRpc(Vector3 hitPoint)
    {
        var prefab = CwslGameSession.Instance?.Assets?.redMageLightningExplosionVfx;
        if (prefab != null)
        {
            CwslVfxSpawner.Spawn(prefab, hitPoint + Vector3.up * 0.2f, Quaternion.identity, 1.4f, 0.9f);
            return;
        }

        CwslSimpleVfx.SpawnBurst(hitPoint, new Color(0.45f, 0.75f, 1f), 1.2f, 0.35f);
    }
}
