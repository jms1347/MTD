using UnityEngine;

public static class CoopFarmGateSync
{
    public static bool ComputeShouldOpen(bool waveActive, bool pressurePlateOccupied)
    {
        if (!waveActive)
            return true;

        return pressurePlateOccupied;
    }

    public static void ApplyState(bool open)
    {
        foreach (var gate in Object.FindObjectsByType<FarmGateController>(FindObjectsSortMode.None))
            gate.SetGateOpen(open);
    }
}
