using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int humanRp;
    private int mosquitoTeamRp;
    private readonly Dictionary<ulong, int> mosquitoRpByClient = new();

    public int HumanRp => humanRp;
    public int MosquitoTeamRp => mosquitoTeamRp;

    public event Action OnRpChanged;

    private bool IsServer => NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int GetMosquitoRp(ulong clientId)
    {
        return mosquitoRpByClient.TryGetValue(clientId, out var rp) ? rp : 0;
    }

    public void RegisterBloodTick(ulong mosquitoClientId)
    {
        if (!IsServer)
            return;

        humanRp -= PanicGameConstants.RpPerBloodTick;
        mosquitoTeamRp += PanicGameConstants.RpPerBloodTick;

        if (!mosquitoRpByClient.ContainsKey(mosquitoClientId))
            mosquitoRpByClient[mosquitoClientId] = 0;
        mosquitoRpByClient[mosquitoClientId] += PanicGameConstants.RpPerBloodTick;

        OnRpChanged?.Invoke();
    }

    public void ApplyEndBonusHumanWin(float remainingHealth)
    {
        if (!IsServer)
            return;

        humanRp += Mathf.RoundToInt(remainingHealth * PanicGameConstants.HumanHpWinMultiplier);
        OnRpChanged?.Invoke();
    }

    public void ApplyEndBonusMosquitoWin()
    {
        if (!IsServer)
            return;

        mosquitoTeamRp += PanicGameConstants.MosquitoTeamWinBonus;
        OnRpChanged?.Invoke();
    }
}
