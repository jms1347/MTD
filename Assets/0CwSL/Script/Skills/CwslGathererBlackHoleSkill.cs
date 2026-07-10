using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>링거 R — 회오리. 적을 중심에서 회전·상승 후 상단에서 던짐.</summary>
public class CwslGathererBlackHoleSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotR;

    private sealed class WhirlwindVictim
    {
        public Transform Transform;
        public float StartAngle;
        public float OrbitDistance;
        public float BaseY;
    }

    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private Coroutine whirlwindRoutine;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.CrowdGatherer;

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

        return TryCastServer(senderClientId, worldPoint);
    }

    public bool TryCastServer(ulong senderClientId, Vector3 worldPoint)
    {
        if (!CanCastServer(senderClientId))
            return false;

        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        if (whirlwindRoutine != null)
            StopCoroutine(whirlwindRoutine);

        var center = worldPoint;
        if (center.sqrMagnitude < 0.01f)
            center = transform.position + transform.forward * 3f;
        center.y = 0.05f;
        center = CwslArenaUtility.ClampToPlayArea(center, 0.5f);

        whirlwindRoutine = StartCoroutine(WhirlwindRoutine(center));
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

        return whirlwindRoutine == null;
    }

    private IEnumerator WhirlwindRoutine(Vector3 center)
    {
        var radius = CwslGameConstants.GathererWhirlwindRadius;
        var liftDuration = CwslGameConstants.GathererWhirlwindLiftSeconds;
        var throwDuration = CwslGameConstants.GathererWhirlwindThrowSeconds;
        var totalDuration = liftDuration + throwDuration;
        PlayWhirlwindClientRpc(center, radius, totalDuration);

        var victims = CollectVictims(center, radius);
        var elapsed = 0f;
        while (elapsed < liftDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / liftDuration);
            var spin = CwslGameConstants.GathererWhirlwindSpinDegreesPerSecond * elapsed;
            RefreshVictimStun(victims);
            UpdateVictimsLift(center, victims, spin, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < throwDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / throwDuration);
            RefreshVictimStun(victims);
            UpdateVictimsThrow(center, victims, t);
            yield return null;
        }

        whirlwindRoutine = null;
    }

    private static List<WhirlwindVictim> CollectVictims(Vector3 center, float radius)
    {
        var results = new List<WhirlwindVictim>();
        var radiusSq = radius * radius;

        foreach (var monster in CwslCombatRegistry.AliveMonsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            var target = monster.transform;
            if (!CwslGathererSkillUtil.IsInsideFlatRadius(center, target.position, radiusSq))
                continue;

            var flat = target.position - center;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.05f)
                flat = Vector3.right * 0.6f;

            results.Add(new WhirlwindVictim
            {
                Transform = target,
                StartAngle = Mathf.Atan2(flat.z, flat.x),
                OrbitDistance = flat.magnitude,
                BaseY = target.position.y,
            });

            monster.GetComponent<CwslMonsterStun>()?.ApplyStunServer(
                CwslGameConstants.GathererWhirlwindStunDuration,
                target.position + Vector3.up * 0.6f);
        }

        return results;
    }

    private static void RefreshVictimStun(List<WhirlwindVictim> victims)
    {
        const float refreshSeconds = 0.35f;
        foreach (var victim in victims)
        {
            if (victim.Transform == null)
                continue;

            victim.Transform.GetComponent<CwslMonsterStun>()
                ?.ApplyStunServer(refreshSeconds, victim.Transform.position + Vector3.up * 0.6f);
        }
    }

    private static void UpdateVictimsLift(
        Vector3 center,
        List<WhirlwindVictim> victims,
        float spinDegrees,
        float liftT)
    {
        var eased = Mathf.SmoothStep(0f, 1f, liftT);
        var orbitScale = Mathf.Lerp(1f, 0.45f, eased);
        var height = Mathf.Lerp(0f, CwslGameConstants.GathererWhirlwindMaxHeight, eased);
        var spinRad = spinDegrees * Mathf.Deg2Rad;

        foreach (var victim in victims)
        {
            if (victim.Transform == null)
                continue;

            var dist = victim.OrbitDistance * orbitScale;
            var angle = victim.StartAngle + spinRad;
            var next = center + new Vector3(Mathf.Cos(angle) * dist, victim.BaseY + height, Mathf.Sin(angle) * dist);
            CwslGathererSkillUtil.WarpTransform(victim.Transform, next);
        }
    }

    private static void UpdateVictimsThrow(
        Vector3 center,
        List<WhirlwindVictim> victims,
        float throwT)
    {
        var throwSpeed = CwslGameConstants.GathererWhirlwindThrowSpeed;
        foreach (var victim in victims)
        {
            if (victim.Transform == null)
                continue;

            var flat = victim.Transform.position - center;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.05f)
                flat = Random.insideUnitCircle.normalized;

            var launchDir = (flat.normalized + Vector3.up * 1.1f).normalized;
            var distance = throwSpeed * throwT;
            var next = victim.Transform.position + launchDir * distance;
            CwslGathererSkillUtil.WarpTransform(victim.Transform, next);
        }
    }

    [ClientRpc]
    private void PlayWhirlwindClientRpc(Vector3 center, float radius, float duration) =>
        CwslVfxSpawner.SpawnGathererWhirlwind(center, radius, duration);
}
