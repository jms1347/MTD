using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>링거 E — 클릭한 유닛과 자리 교환(아군/적군).</summary>
public class CwslGathererSwapSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = 1;

    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private CwslPlayerSelection selection;
    private CwslPlayerMovement movement;

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
        movement = GetComponent<CwslPlayerMovement>();
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

        var selfPos = transform.position;
        var targetPos = target.transform.position;
        WarpUnit(transform, targetPos);
        WarpUnit(target.transform, selfPos);
        PlaySwapFxClientRpc(selfPos, targetPos);
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
            selected.NetworkObjectId != NetworkObjectId &&
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
        var max = CwslGameConstants.GathererSwapMaxDistance;
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

    private void WarpUnit(Transform unit, Vector3 destination)
    {
        var bodyRadius = unit.GetComponent<CwslPlayerBodyCollider>()?.Radius
            ?? CwslGameConstants.PlayerBodyColliderRadiusDefault;
        destination = CwslArenaUtility.ClampToPlayArea(destination, bodyRadius);
        destination.y = unit.position.y;

        var rammer = unit.GetComponent<CwslMomentumRammerSkill>();
        if (rammer != null && rammer.IsMomentumActive)
        {
            unit.position = destination;
            return;
        }

        if (unit == transform)
            movement?.StopMovement();

        var agent = unit.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.Warp(destination);
        else
            unit.position = destination;
    }

    [ClientRpc]
    private void PlaySwapFxClientRpc(Vector3 a, Vector3 b)
    {
        CwslVfxSpawner.SpawnGathererSwap(a);
        CwslVfxSpawner.SpawnGathererSwap(b);
    }
}
