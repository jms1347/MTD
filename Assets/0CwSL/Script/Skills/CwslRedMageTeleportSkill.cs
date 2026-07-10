using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>빨간 마법사 R — 마우스 지정 지점 순간이동 + 골드 포털 연출.</summary>
public class CwslRedMageTeleportSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotR;

    private CwslPlayerMovement movement;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private NavMeshAgent agent;
    private Coroutine teleportRoutine;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.RedMage;

    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        movement = GetComponent<CwslPlayerMovement>();
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
        agent = GetComponent<NavMeshAgent>();
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

        skillCooldowns?.BeginCooldown(BoundSlotIndex);
        PlayCastClientRpc();

        if (teleportRoutine != null)
            StopCoroutine(teleportRoutine);

        teleportRoutine = StartCoroutine(TeleportRoutine(worldPoint));
        return true;
    }

    public bool CanCastServer(ulong senderClientId, Vector3 worldPoint)
    {
        if (!IsServer || senderClientId != OwnerClientId)
            return false;

        if (teleportRoutine != null)
            return false;

        if (skillCooldowns != null && !skillCooldowns.IsReady(BoundSlotIndex))
            return false;

        if (playerHealth != null && !playerHealth.IsAlive)
            return false;

        if (playerStun != null && playerStun.IsStunned)
            return false;

        return ResolveTeleportPosition(worldPoint) != transform.position;
    }

    private IEnumerator TeleportRoutine(Vector3 worldPoint)
    {
        movement?.StopMovement();

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.updatePosition = false;
        }

        var departPosition = transform.position;
        var arrivePosition = ResolveTeleportPosition(worldPoint);
        var faceDirection = arrivePosition - departPosition;
        faceDirection.y = 0f;

        PlayDepartPortalClientRpc(departPosition);
        PlayArrivePortalClientRpc(arrivePosition);

        yield return new WaitForSeconds(CwslGameConstants.RedMageTeleportArrivalDelay);

        if (faceDirection.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(faceDirection.normalized, Vector3.up);

        var bodyRadius = GetComponent<CwslPlayerBodyCollider>()?.Radius
            ?? CwslGameConstants.PlayerBodyColliderRadiusDefault;
        transform.position = CwslArenaUtility.ClampToPlayArea(arrivePosition, bodyRadius);

        if (agent != null && agent.enabled)
        {
            agent.updatePosition = true;
            if (agent.isOnNavMesh)
                agent.Warp(transform.position);
            agent.isStopped = false;
        }

        teleportRoutine = null;
    }

    private Vector3 ResolveTeleportPosition(Vector3 worldPoint)
    {
        var bodyRadius = GetComponent<CwslPlayerBodyCollider>()?.Radius
            ?? CwslGameConstants.PlayerBodyColliderRadiusDefault;

        var flatToMouse = worldPoint - transform.position;
        flatToMouse.y = 0f;
        if (flatToMouse.sqrMagnitude < 0.15f)
        {
            var fallback = transform.forward;
            fallback.y = 0f;
            if (fallback.sqrMagnitude < 0.0001f)
                fallback = Vector3.forward;

            worldPoint = transform.position + fallback.normalized * CwslGameConstants.RedMageTeleportDistance;
        }

        var target = worldPoint;
        target.y = transform.position.y;
        return CwslArenaUtility.ClampToPlayArea(target, bodyRadius);
    }

    [ClientRpc]
    private void PlayCastClientRpc()
    {
        var visual = transform.Find("Visual");
        visual?.GetComponent<CwslPlayerStaffCastVisual>()?.PlayCast();
    }

    [ClientRpc]
    private void PlayDepartPortalClientRpc(Vector3 position)
    {
        SpawnPortal(position, CwslGameConstants.RedMageTeleportDepartPortalLifetime);
        CwslSkillAudioFeedback.PlayRedMageTeleportCast(position);
    }

    [ClientRpc]
    private void PlayArrivePortalClientRpc(Vector3 position)
    {
        SpawnPortal(position, CwslGameConstants.RedMageTeleportArrivePortalLifetime);
    }

    private static void SpawnPortal(Vector3 position, float lifetime)
    {
        var prefab = CwslGameSession.Instance?.Assets?.redMageTeleportPortalVfx;
        if (prefab == null)
            return;

        CwslVfxSpawner.Spawn(
            prefab,
            position + Vector3.up * 0.05f,
            Quaternion.identity,
            lifetime,
            1f);
    }
}
