using System.Collections.Generic;
using UnityEngine;

public class CoopFarmGatePressurePlate : MonoBehaviour
{
    private static readonly HashSet<CoopPlayerTowerUnit> Occupants = new();

    public static bool IsOccupied => Occupants.Count > 0;

    private void OnTriggerEnter(Collider other)
    {
        var unit = other.GetComponentInParent<CoopPlayerTowerUnit>();
        if (unit == null || !Occupants.Add(unit))
            return;

        NotifyGateChanged();
    }

    private void OnTriggerExit(Collider other)
    {
        var unit = other.GetComponentInParent<CoopPlayerTowerUnit>();
        if (unit == null || !Occupants.Remove(unit))
            return;

        NotifyGateChanged();
    }

    private static void NotifyGateChanged()
    {
        var session = CoopGameSession.Instance;
        if (session == null || !session.IsHostAuthority)
            return;

        session.RefreshFarmGates();
    }
}
