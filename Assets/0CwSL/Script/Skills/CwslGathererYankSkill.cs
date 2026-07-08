using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>링거 W — 클릭한 유닛을 내 위치로 끌어옴.</summary>
public class CwslGathererYankSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = 3;

    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private CwslPlayerSelection selection;
    private Coroutine yankRoutine;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.CrowdGatherer;

    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
        selection = GetComponent<CwslPlayerSelection>();
    }

    public override bool CanUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint) =>
        slotIndex == BoundSlotIndex && CanCastServer(senderClientId, worldPoint);

    public override bool TryUseSkillSlotServer(ulong senderClientId, int slotIndex, Vector3 worldPoint)
    {
        if (!IsServer || slotIndex != BoundSlotIndex)
            return false;

        return TryCastServer(senderClientId, worldPoint);
    }

    public bool TryCastServer(ulong senderClientId, Vector3 worldPoint)
    {
        if (!CanCastServer(senderClientId, worldPoint))
            return false;

        if (!TryResolveTarget(worldPoint, out var target))
            return false;

        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        if (yankRoutine != null)
            StopCoroutine(yankRoutine);

        yankRoutine = StartCoroutine(YankRoutine(target));
        return true;
    }

    public bool CanCastServer(ulong senderClientId, Vector3 worldPoint)
    {
        if (!IsServer || senderClientId != OwnerClientId)
            return false;

        if (skillCooldowns != null && !skillCooldowns.IsReady(BoundSlotIndex))
            return false;

        if (playerHealth != null && !playerHealth.IsAlive)
            return false;

        if (playerStun != null && playerStun.IsStunned)
            return false;

        return TryResolveTarget(worldPoint, out _);
    }

    private bool TryResolveTarget(Vector3 worldPoint, out NetworkObject target)
    {
        target = null;
        if (selection != null &&
            selection.TryGetSelectedTarget(out var selected) &&
            selected != null &&
            IsValidTarget(selected) &&
            IsInRange(selected.transform.position))
        {
            target = selected;
            return true;
        }

        var best = 2.6f;
        var monsters = CwslCombatRegistry.AliveMonsters;
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive || !IsInRange(monster.transform.position))
                continue;

            var d = Vector3.Distance(worldPoint, monster.transform.position);
            if (d >= best)
                continue;

            best = d;
            target = monster.NetworkObject;
        }

        if (NetworkManager.Singleton != null)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerObject = client.PlayerObject;
                if (playerObject == null || playerObject.NetworkObjectId == NetworkObjectId)
                    continue;

                var health = playerObject.GetComponent<CwslPlayerHealth>();
                if (health == null || !health.IsAlive || !IsInRange(playerObject.transform.position))
                    continue;

                var d = Vector3.Distance(worldPoint, playerObject.transform.position);
                if (d >= best)
                    continue;

                best = d;
                target = playerObject;
            }
        }

        return target != null;
    }

    private bool IsInRange(Vector3 position)
    {
        var flat = position - transform.position;
        flat.y = 0f;
        var max = CwslGameConstants.GathererYankMaxDistance;
        return flat.sqrMagnitude <= max * max;
    }

    private static bool IsValidTarget(NetworkObject networkObject)
    {
        if (networkObject == null)
            return false;

        var monster = networkObject.GetComponent<CwslMonsterHealth>();
        if (monster != null)
            return monster.IsAlive;

        var player = networkObject.GetComponent<CwslPlayerHealth>();
        return player != null && player.IsAlive;
    }

    private IEnumerator YankRoutine(NetworkObject targetObject)
    {
        if (targetObject == null)
        {
            yankRoutine = null;
            yield break;
        }

        var target = targetObject.transform;
        var start = target.position;
        var end = transform.position + transform.forward * 1.1f;
        end.y = start.y;
        end = CwslArenaUtility.ClampToPlayArea(end, 0.4f);

        PlayYankFxClientRpc(start, end);

        var duration = CwslGameConstants.GathererYankPullSeconds;
        var elapsed = 0f;
        while (elapsed < duration && target != null)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            var next = Vector3.Lerp(start, end, t);
            WarpOrMove(target, next);
            yield return null;
        }

        yankRoutine = null;
    }

    private static void WarpOrMove(Transform target, Vector3 next)
    {
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
    private void PlayYankFxClientRpc(Vector3 from, Vector3 to)
    {
        CwslVfxSpawner.SpawnGathererYank(from, to);
    }
}
