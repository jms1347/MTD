using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>질주자 W — 급브레이크: 모은 속도만큼 피해 + 주변 넉백.</summary>
public class CwslRammerBrakeSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotW;

    private CwslMomentumRammerSkill rammerSkill;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private Coroutine brakeSlideRoutine;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.MomentumRammer;

    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        rammerSkill = GetComponent<CwslMomentumRammerSkill>();
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

        return TryCastServer(senderClientId);
    }

    public bool TryCastServer(ulong senderClientId)
    {
        if (!CanCastServer(senderClientId))
            return false;

        var speed = rammerSkill != null ? rammerSkill.CurrentSpeed : 0f;
        if (speed < CwslGameConstants.RammerBrakeMinSpeed)
            return false;

        skillCooldowns?.BeginCooldown(BoundSlotIndex);

        var center = transform.position;
        var damage = CwslCombatMath.ResolveSpeedScaledSkillDamage(
            CwslCharacterId.MomentumRammer,
            CwslGameConstants.RammerBrakeSkillCoeff,
            speed,
            CwslGameConstants.RammerMaxSpeed);
        var knockDistance = CwslGameConstants.RammerBrakeKnockDistance
                            * Mathf.Clamp01(speed / CwslGameConstants.RammerMaxSpeed);
        var slideDistance = speed * CwslGameConstants.RammerBrakeSlideDistanceMultiplier;

        rammerSkill?.StopMomentumForStunServer();
        if (brakeSlideRoutine != null)
            StopCoroutine(brakeSlideRoutine);
        brakeSlideRoutine = StartCoroutine(BrakeSlideRoutine(transform.forward, slideDistance));
        ApplyBrakeBurstServer(center, damage, knockDistance);
        PlayBrakeFxClientRpc(center, speed);
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

        return rammerSkill != null &&
               rammerSkill.CurrentSpeed >= CwslGameConstants.RammerBrakeMinSpeed;
    }

    private void ApplyBrakeBurstServer(Vector3 center, float damage, float knockDistance)
    {
        var radius = CwslGameConstants.RammerBrakeRadius * CwslGameConstants.RammerBrakeRadiusMultiplier;
        var radiusSq = radius * radius;
        var monsters = CwslCombatRegistry.AliveMonsters;
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var flat = monster.transform.position - center;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSq)
                continue;

            monster.DamageFromPlayer(OwnerClientId, damage);
            var knockback = monster.GetComponent<CwslMonsterKnockback>();
            if (knockback == null)
                knockback = monster.gameObject.AddComponent<CwslMonsterKnockback>();

            var dir = flat.sqrMagnitude > 0.0001f ? flat.normalized : transform.forward;
            knockback.ApplyKnockbackServer(dir, knockDistance, CwslGameConstants.RammerBrakeKnockDuration);
            monster.NotifyHitFlinchServer(dir, knockDistance * 0.2f);
        }
    }

    [ClientRpc]
    private void PlayBrakeFxClientRpc(Vector3 center, float speed)
    {
        var scale = Mathf.Lerp(1.1f, 1.8f, Mathf.Clamp01(speed / CwslGameConstants.RammerMaxSpeed))
                    * CwslGameConstants.RammerBrakeVfxScaleMultiplier;
        CwslVfxSpawner.SpawnRammerBrakeBurst(center, scale);
        CwslRammerAudioFeedback.PlayBrakeNeigh(center);
        PlayBrakeScreech(center);
        if (IsOwner)
            CwslCameraShake.Play(0.28f, 0.22f * scale);
    }

    private IEnumerator BrakeSlideRoutine(Vector3 forward, float distance)
    {
        var duration = CwslGameConstants.RammerBrakeSlideDuration;
        if (duration <= 0.01f || distance <= 0.05f)
        {
            brakeSlideRoutine = null;
            yield break;
        }

        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        var elapsed = 0f;
        var start = transform.position;
        var bodyRadius = GetComponent<CwslPlayerBodyCollider>()?.Radius
            ?? CwslGameConstants.PlayerBodyColliderRadiusDefault;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var eased = 1f - Mathf.Pow(1f - t, 2.5f);
            var target = start + forward * (distance * eased);
            transform.position = CwslArenaUtility.ClampToPlayArea(target, bodyRadius);
            yield return null;
        }

        brakeSlideRoutine = null;
    }

    private static void PlayBrakeScreech(Vector3 center)
    {
        var clip = CwslRammerAudioFeedback.ResolveStunClip();
        if (clip == null)
            return;

        var soundObject = new GameObject("CwslRammerBrakeScreech");
        soundObject.transform.position = center + Vector3.up * 0.4f;
        var source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = 0.5f;
        source.pitch = 1.35f;
        source.spatialBlend = 0.6f;
        source.minDistance = 2f;
        source.maxDistance = 20f;
        source.Play();
        Object.Destroy(soundObject, Mathf.Min(clip.length, 0.35f) + 0.05f);
    }
}
