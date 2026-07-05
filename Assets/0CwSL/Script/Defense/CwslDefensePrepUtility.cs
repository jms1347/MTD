using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public static class CwslDefensePrepUtility
{
    public const int MaxLineupSlots = 5;
    public const float LineupRadius = 8.8f;
    public const float StartPadRadius = 2.6f;
    public const float SharedStartPadDistance = 12.2f;

    private static readonly float[] SlotAnglesDeg = { 215f, 238f, 270f, 302f, 325f };

    public static void CollectSortedClientIds(List<ulong> buffer)
    {
        buffer.Clear();
        var network = NetworkManager.Singleton;
        if (network == null)
            return;

        foreach (var clientId in network.ConnectedClientsIds)
            buffer.Add(clientId);

        buffer.Sort();
    }

    public static int GetSlotIndex(ulong clientId, IReadOnlyList<ulong> sortedClientIds)
    {
        for (var i = 0; i < sortedClientIds.Count; i++)
        {
            if (sortedClientIds[i] == clientId)
                return i;
        }

        return 0;
    }

    public static Vector3 GetLineupWorldPosition(int slotIndex)
    {
        var angle = SlotAnglesDeg[Mathf.Clamp(slotIndex, 0, SlotAnglesDeg.Length - 1)] * Mathf.Deg2Rad;
        var flat = new Vector3(Mathf.Cos(angle) * LineupRadius, 0f, Mathf.Sin(angle) * LineupRadius);
        return SampleNavMesh(flat);
    }

    public static Vector3 GetSharedStartPadWorldPosition()
    {
        return SampleNavMesh(new Vector3(0f, 0f, -SharedStartPadDistance));
    }

    public static bool IsOnStartPad(Vector3 playerWorldPosition)
    {
        var pad = GetSharedStartPadWorldPosition();
        var playerFlat = new Vector3(playerWorldPosition.x, 0f, playerWorldPosition.z);
        var padFlat = new Vector3(pad.x, 0f, pad.z);
        return Vector3.Distance(playerFlat, padFlat) <= StartPadRadius;
    }

    public static bool IsPrepBoundaryActive()
    {
        if (!CwslGameConstants.UseDefenseMode)
            return false;

        var controller = CwslDefenseModeController.Instance;
        if (controller == null)
            return true;

        return controller.MatchPhase is CwslDefenseMatchPhase.PreMatch or CwslDefenseMatchPhase.Countdown;
    }

    public static float GetPrepInnerRadius(float bodyRadius = 0f)
    {
        return Mathf.Max(
            2f,
            CwslGameConstants.DefensePrepBarrierRadius
            - CwslGameConstants.DefensePrepBarrierThickness * 0.5f
            - bodyRadius
            - 0.2f);
    }

    public static Vector3 ClampToPrepArea(Vector3 worldPosition, float bodyRadius = 0f)
    {
        var innerRadius = GetPrepInnerRadius(bodyRadius);
        var flat = new Vector3(worldPosition.x, 0f, worldPosition.z);
        if (flat.sqrMagnitude <= innerRadius * innerRadius)
            return worldPosition;

        var clamped = flat.normalized * innerRadius;
        return SampleNavMesh(new Vector3(clamped.x, worldPosition.y, clamped.z));
    }

    public static int CountReadyMaskBits(int mask, int requiredPlayers)
    {
        var count = 0;
        for (var i = 0; i < requiredPlayers; i++)
        {
            if ((mask & (1 << i)) != 0)
                count++;
        }

        return count;
    }

    private static Vector3 SampleNavMesh(Vector3 worldPosition)
    {
        worldPosition.y = CwslGameConstants.SpawnHeight;
        if (NavMesh.SamplePosition(worldPosition, out var hit, 4f, NavMesh.AllAreas))
            return hit.position;

        return worldPosition;
    }
}
