using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>??? W ? ???? ??? ???? ?? ? ??? ?? ??.</summary>
public class CwslRedMageLightningOrbSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotW;

    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private Coroutine castRoutine;
    private static readonly List<CwslMonsterHealth> strikeTargets = new(24);

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

        if (playerHealth != null)
            playerHealth.OnDied += HandleOwnerDied;
    }

    public override void OnNetworkDespawn()
    {
        if (playerHealth != null)
            playerHealth.OnDied -= HandleOwnerDied;

        CancelSkillServer();
    }

    private void OnDisable()
    {
        CancelSkillServer();
    }

    private void HandleOwnerDied()
    {
        CancelSkillServer();
    }

    public void CancelSkillServer()
    {
        if (castRoutine != null)
        {
            StopCoroutine(castRoutine);
            castRoutine = null;
        }
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
        try
        {
            var flatDirection = direction;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude < 0.0001f)
                flatDirection = transform.forward;
            flatDirection.Normalize();

            var startPosition = ResolveOrbPosition(flatDirection);
            var travelDistance = CwslGameConstants.RedMageLightningOrbTravelDistance;
            var travelSpeed = CwslGameConstants.RedMageLightningOrbTravelSpeed;
            var travelDuration = travelDistance / travelSpeed;
            var strikeInterval = CwslGameConstants.RedMageLightningOrbStrikeInterval;
            var strikeRadius = CwslGameConstants.RedMageLightningOrbStrikeRadius;

            PlayLightningOrbTravelClientRpc(
                startPosition,
                flatDirection,
                travelDuration,
                CwslGameConstants.RedMageLightningOrbVisualScale,
                strikeRadius);

            yield return new WaitForSeconds(CwslGameConstants.RedMageLightningOrbChargeSeconds);

            var damage = playerCharacter != null
                ? CwslCharacterStatCatalog.GetAttackPower(playerCharacter.CharacterId)
                : CwslGameConstants.AttackDamage;
            var shockDuration = CwslGameConstants.RedMageLightningShockDuration;
            var strikeDamage = damage * CwslGameConstants.RedMageLightningOrbStrikeDamageRatio;

            var elapsed = 0f;
            var strikeTimer = strikeInterval;
            while (elapsed < travelDuration)
            {
                elapsed += Time.deltaTime;
                strikeTimer += Time.deltaTime;

                var orbPosition = startPosition + flatDirection * (travelDistance * Mathf.Clamp01(elapsed / travelDuration));

                if (strikeTimer >= strikeInterval)
                {
                    strikeTimer -= strikeInterval;
                    StrikeMonstersNearOrbServer(orbPosition, strikeRadius, strikeDamage, shockDuration);
                }

                yield return null;
            }

            var endPosition = startPosition + flatDirection * travelDistance;
            StrikeMonstersNearOrbServer(endPosition, strikeRadius, strikeDamage, shockDuration);
            PlayLightningOrbImpactClientRpc(endPosition);
        }
        finally
        {
            castRoutine = null;
        }
    }

    private void StrikeMonstersNearOrbServer(
        Vector3 center,
        float radius,
        float strikeDamage,
        float shockDuration)
    {
        var radiusSq = radius * radius;
        var monsters = CwslCombatRegistry.AliveMonsters;
        strikeTargets.Clear();

        for (var i = 0; i < monsters.Count; i++)
        {
            var monster = monsters[i];
            if (monster == null || !monster.IsAlive)
                continue;

            var flat = monster.GetFlatHitPoint() - center;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSq)
                continue;

            strikeTargets.Add(monster);
        }

        for (var i = 0; i < strikeTargets.Count; i++)
        {
            var monster = strikeTargets[i];
            if (monster == null || !monster.IsAlive)
                continue;

            var hitPoint = monster.GetFlatHitPoint();
            monster.DamageFromPlayer(OwnerClientId, strikeDamage);
            CwslMonsterStatusController.Ensure(monster)?.ApplyShockServer(shockDuration);
            PlayLightningStrikeClientRpc(hitPoint);
        }
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
        CwslSkillAudioFeedback.PlayLightningOrbCast(transform.position);
    }

    [ClientRpc]
    private void PlayLightningOrbTravelClientRpc(
        Vector3 startPosition,
        Vector3 direction,
        float travelDuration,
        float scale,
        float strikeRadius)
    {
        var runnerObject = new GameObject("RedMageLightningOrbTravel");
        runnerObject.AddComponent<CwslRedMageLightningOrbTravelVisual>()
            .Play(startPosition, direction, travelDuration, scale, strikeRadius);
    }

    [ClientRpc]
    private void PlayLightningStrikeClientRpc(Vector3 hitPoint)
    {
        CwslVfxSpawner.SpawnRedMageLightningStrike(hitPoint);
        CwslSkillAudioFeedback.PlayLightningOrbStrike(hitPoint);
    }

    [ClientRpc]
    private void PlayLightningOrbImpactClientRpc(Vector3 hitPoint)
    {
        var prefab = CwslGameSession.Instance?.Assets?.redMageLightningExplosionVfx;
        if (prefab != null)
        {
            CwslVfxSpawner.Spawn(
                prefab,
                hitPoint + Vector3.up * 0.2f,
                Quaternion.identity,
                1.6f,
                CwslGameConstants.RedMageLightningOrbVisualScale * 0.85f);
            return;
        }

        CwslSimpleVfx.SpawnBurst(hitPoint, new Color(0.45f, 0.75f, 1f), 1.4f, 0.4f);
        CwslSkillAudioFeedback.PlayLightningOrbImpact(hitPoint);
    }
}
