using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>링거 E — 클릭 지역과 시전자 주변 동일 영역의 유닛·투사체를 맞바꿈.</summary>
public class CwslGathererSwapSkill : CwslPlayerSkillBase
{
    public const int BoundSlotIndex = CwslCharacterSkillCatalog.SlotE;

    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerSkillCooldowns skillCooldowns;
    private CwslPlayerMovement movement;
    private readonly List<Transform> regionATargets = new();
    private readonly List<Transform> regionBTargets = new();

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.CrowdGatherer;

    public override int SkillSlotIndex => BoundSlotIndex;

    public override void OnNetworkSpawn()
    {
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
        movement = GetComponent<CwslPlayerMovement>();
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

        worldPoint.y = 0f;
        worldPoint = CwslArenaUtility.ClampToPlayArea(worldPoint, 0.5f);
        var regionCenterA = worldPoint;
        var regionCenterB = transform.position;
        regionCenterB.y = 0f;
        var radius = CwslGameConstants.GathererSwapRegionRadius;

        CollectRegion(regionCenterA, radius, regionATargets, true);
        CollectRegion(regionCenterB, radius, regionBTargets, true);
        if (regionATargets.Count == 0 && regionBTargets.Count == 0)
            return false;

        skillCooldowns?.BeginCooldown(BoundSlotIndex);

        var swappedA = new List<Vector3>(regionATargets.Count);
        for (var i = 0; i < regionATargets.Count; i++)
        {
            var pos = regionATargets[i].position;
            swappedA.Add(OffsetToRegion(pos, regionCenterA, regionCenterB));
        }

        var swappedB = new List<Vector3>(regionBTargets.Count);
        for (var i = 0; i < regionBTargets.Count; i++)
        {
            var pos = regionBTargets[i].position;
            swappedB.Add(OffsetToRegion(pos, regionCenterB, regionCenterA));
        }

        for (var i = 0; i < regionATargets.Count; i++)
            CwslGathererSkillUtil.WarpTransform(regionATargets[i], swappedA[i]);

        for (var i = 0; i < regionBTargets.Count; i++)
            CwslGathererSkillUtil.WarpTransform(regionBTargets[i], swappedB[i]);

        movement?.StopMovement();
        PlayRegionSwapClientRpc(regionCenterA, regionCenterB, radius);
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

        return playerStun == null || !playerStun.IsStunned;
    }

    private static void CollectRegion(
        Vector3 center,
        float radius,
        List<Transform> results,
        bool swappableOnly)
    {
        CwslGathererSkillUtil.CollectInCircle(center, radius, results, swappableOnly);
    }

    private static Vector3 OffsetToRegion(Vector3 position, Vector3 fromCenter, Vector3 toCenter)
    {
        var offset = position - fromCenter;
        offset.y = 0f;
        var destination = toCenter + offset;
        destination.y = position.y;
        return destination;
    }

    [ClientRpc]
    private void PlayRegionSwapClientRpc(Vector3 regionA, Vector3 regionB, float radius)
    {
        CwslVfxSpawner.SpawnGathererRegionSwap(regionA, radius);
        CwslVfxSpawner.SpawnGathererRegionSwap(regionB, radius);
    }
}
