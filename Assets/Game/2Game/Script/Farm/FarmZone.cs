using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 농장 구역 경계와 문 열림 상태를 기준으로 플레이어 진입을 제한합니다.
/// 전투 전·휴식 중에는 자유롭게 출입 가능하고, 전투 중에는 경계를 넘을 수 없습니다.
/// </summary>
public class FarmZone : MonoBehaviour
{
    private static readonly List<FarmZone> Zones = new();

    public static FarmZone Instance => Zones.Count > 0 ? Zones[0] : null;

    private Vector3 localMin;
    private Vector3 localMax;

    public bool IsGateOpen => FarmGateController.AreAllOpen();

    public static bool CanMoveBetween(Vector3 fromWorld, Vector3 toWorld)
    {
        if (Zones.Count == 0)
            return true;

        if (IsFarmBoundaryOpen())
            return true;

        foreach (var zone in Zones)
        {
            bool toInside = zone.ContainsWorldPoint(toWorld);
            bool fromInside = zone.ContainsWorldPoint(fromWorld);
            if (toInside != fromInside)
                return false;
        }

        return true;
    }

    public void Configure(Vector3 minLocal, Vector3 maxLocal)
    {
        localMin = minLocal;
        localMax = maxLocal;
    }

    private void Awake()
    {
        Zones.Add(this);
    }

    private void OnDestroy()
    {
        Zones.Remove(this);
    }

    public bool ContainsWorldPoint(Vector3 worldPoint)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint);
        return local.x >= localMin.x && local.x <= localMax.x
            && local.z >= localMin.z && local.z <= localMax.z;
    }

    public bool CanMoveTo(Vector3 fromWorld, Vector3 toWorld)
    {
        if (IsFarmBoundaryOpen())
            return true;

        bool toInside = ContainsWorldPoint(toWorld);
        bool fromInside = ContainsWorldPoint(fromWorld);
        return toInside == fromInside;
    }

    private static bool IsFarmBoundaryOpen()
    {
        if (DefenseStageTimerManager.Instance != null)
        {
            if (!DefenseStageTimerManager.Instance.ShouldLockFarmBoundary())
                return true;

            return false;
        }

        return FarmGateController.AreAllOpen();
    }
}
