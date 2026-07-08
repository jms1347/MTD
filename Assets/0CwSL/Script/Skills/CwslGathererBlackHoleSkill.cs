using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>링거 R — 블랙홀 장판 10초, 적을 중심으로 서서히 흡인.</summary>
public class CwslGathererBlackHoleSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = 2;

    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private Coroutine blackHoleRoutine;

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
        if (blackHoleRoutine != null)
            StopCoroutine(blackHoleRoutine);

        var center = worldPoint;
        if (center.sqrMagnitude < 0.01f)
            center = transform.position + transform.forward * 3f;
        center.y = 0.05f;
        center = CwslArenaUtility.ClampToPlayArea(center, 0.5f);

        blackHoleRoutine = StartCoroutine(BlackHoleRoutine(center));
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

    private IEnumerator BlackHoleRoutine(Vector3 center)
    {
        var duration = CwslGameConstants.GathererBlackHoleDuration;
        var radius = CwslGameConstants.GathererBlackHoleRadius;
        PlayBlackHoleClientRpc(center, radius, duration);

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            PullUnitsServer(center, radius);
            yield return null;
        }

        blackHoleRoutine = null;
    }

    private void PullUnitsServer(Vector3 center, float radius)
    {
        var radiusSq = radius * radius;
        var pullSpeed = CwslGameConstants.GathererBlackHolePullSpeed;

        var monsters = CwslCombatRegistry.AliveMonsters;
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            PullTransform(monster.transform, center, radiusSq, pullSpeed);
        }

        if (NetworkManager.Singleton == null)
            return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null || playerObject.NetworkObjectId == NetworkObjectId)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health == null || !health.IsAlive)
                continue;

            PullTransform(playerObject.transform, center, radiusSq, pullSpeed * 0.75f);
        }
    }

    private static void PullTransform(Transform target, Vector3 center, float radiusSq, float pullSpeed)
    {
        var flat = center - target.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > radiusSq || flat.sqrMagnitude < 0.25f)
            return;

        var distance = flat.magnitude;
        var strength = Mathf.Lerp(1.4f, 0.45f, distance / Mathf.Sqrt(radiusSq));
        var next = target.position + flat.normalized * (pullSpeed * strength * Time.deltaTime);
        next.y = target.position.y;
        next = CwslArenaUtility.ClampToPlayArea(next, 0.4f);

        var rammer = target.GetComponent<CwslMomentumRammerSkill>();
        if (rammer != null && rammer.IsMomentumActive)
        {
            target.position = next;
            return;
        }

        var agent = target.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.Warp(next);
        else
            target.position = next;
    }

    [ClientRpc]
    private void PlayBlackHoleClientRpc(Vector3 center, float radius, float duration)
    {
        CwslVfxSpawner.SpawnGathererBlackHole(center, radius, duration);
    }
}
