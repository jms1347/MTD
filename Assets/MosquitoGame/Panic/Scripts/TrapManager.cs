using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TrapManager : MonoBehaviour
{
    public static TrapManager Instance { get; private set; }

    private readonly Dictionary<PanicTrapType, int> placedCounts = new();
    private bool placementLocked;

    private bool IsServer => NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResetCounts();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void LockPlacement() => placementLocked = true;

    public bool CanPlace(PanicTrapType trapType)
    {
        if (placementLocked || !PanicGameManager.Instance || !PanicGameManager.Instance.IsPrep)
            return false;

        placedCounts.TryGetValue(trapType, out var count);
        return count < PanicGameConstants.MaxTrapsPerType;
    }

    public void RequestPlaceTrap(PanicTrapType trapType, Vector3 position, Quaternion rotation)
    {
        if (!IsServer)
            return;

        if (!CanPlace(trapType))
            return;

        var trapObject = CreateTrap(trapType, position, rotation);
        if (trapObject == null)
            return;

        placedCounts[trapType] = placedCounts.TryGetValue(trapType, out var count) ? count + 1 : 1;
    }

    public static GameObject CreateTrap(PanicTrapType trapType, Vector3 position, Quaternion rotation)
    {
        return trapType switch
        {
            PanicTrapType.MosquitoCoil => MosquitoCoilTrap.Create(position, rotation),
            PanicTrapType.StickyPad => StickyPadTrap.Create(position, rotation),
            PanicTrapType.DecoyHuman => DecoyHumanTrap.Create(position, rotation),
            _ => null
        };
    }

    private void ResetCounts()
    {
        placedCounts.Clear();
        foreach (PanicTrapType type in System.Enum.GetValues(typeof(PanicTrapType)))
            placedCounts[type] = 0;
    }
}
