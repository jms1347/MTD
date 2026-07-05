using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>아군 버프 존 — 회복 샘, 순풍 잔디, 연합 거점, 골드 샘.</summary>
public class CwslArenaBuffSystem : NetworkBehaviour
{
    public static CwslArenaBuffSystem Instance { get; private set; }

    private readonly Dictionary<ulong, float> goldSpringNextTime = new();
    private readonly Dictionary<int, bool> rallyZoneActive = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void OnNetworkSpawn()
    {
        CwslArenaBuffVisuals.EnsureLocal();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        RefreshRallyZonesServer();
        TickPlayerBuffsServer();
    }

    public static float GetPlayerDamageMultiplier(Vector3 attackerPosition)
    {
        if (Instance == null)
            return 1f;

        var dynamicZones = CwslArenaDynamicZoneSystem.Instance;
        if (dynamicZones == null)
            return 1f;

        foreach (var zone in dynamicZones.GetActiveZones())
        {
            if (zone.Kind != CwslDynamicZoneKind.RallyZone)
                continue;

            if (!Instance.rallyZoneActive.TryGetValue(zone.Id, out var active) || !active)
                continue;

            if (CwslArenaZones.IsInRallyZone(attackerPosition, zone.Id))
                return CwslGameConstants.RallyZoneDamageMultiplier;
        }

        return 1f;
    }

    public static float GetRallyVisionBonus(Vector3 playerPosition)
    {
        if (Instance == null)
            return 0f;

        var dynamicZones = CwslArenaDynamicZoneSystem.Instance;
        if (dynamicZones == null)
            return 0f;

        foreach (var zone in dynamicZones.GetActiveZones())
        {
            if (zone.Kind != CwslDynamicZoneKind.RallyZone)
                continue;

            if (!Instance.rallyZoneActive.TryGetValue(zone.Id, out var active) || !active)
                continue;

            if (CwslArenaZones.IsInRallyZone(playerPosition, zone.Id))
                return CwslGameConstants.RallyZoneVisionBonus;
        }

        return 0f;
    }

    private void RefreshRallyZonesServer()
    {
        var dynamicZones = CwslArenaDynamicZoneSystem.Instance;
        if (dynamicZones == null)
            return;

        foreach (var zone in dynamicZones.GetActiveZones())
        {
            if (zone.Kind != CwslDynamicZoneKind.RallyZone)
                continue;

            var active = dynamicZones.CountAlliesInRallyZone(zone.Id) >= CwslGameConstants.RallyZoneMinAllies;
            if (rallyZoneActive.TryGetValue(zone.Id, out var previous) && previous == active)
                continue;

            rallyZoneActive[zone.Id] = active;
            dynamicZones.SyncRallyZoneClient(zone.Id, active);
        }
    }

    private void TickPlayerBuffsServer()
    {
        if (NetworkManager.Singleton == null)
            return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health == null || !health.IsAlive)
                continue;

            var position = playerObject.transform.position;
            TickTailwindBuffServer(playerObject, position);
            TickHealingSpringServer(health, position);
            TickGoldSpringServer(playerObject, client.ClientId, position);
        }
    }

    private static void TickTailwindBuffServer(NetworkObject playerObject, Vector3 position)
    {
        if (!CwslArenaZones.IsInTailwindGrass(position))
            return;

        CwslMoveSpeedBuff.Ensure(playerObject)?.ApplyBuff(
            CwslGameConstants.TailwindGrassSpeedMultiplier,
            0.25f);
    }

    private static void TickHealingSpringServer(CwslPlayerHealth health, Vector3 position)
    {
        if (!CwslArenaZones.IsInHealingSpring(position))
            return;

        if (!IsStandingForBuff(health.GetComponent<CwslPlayerMovement>()))
            return;

        var healAmount = CwslGameConstants.HealingSpringHpPerSecond * Time.deltaTime;
        if (healAmount > 0f)
            health.TryHealServer(healAmount);
    }

    private void TickGoldSpringServer(NetworkObject playerObject, ulong clientId, Vector3 position)
    {
        if (!CwslArenaZones.IsInGoldSpring(position))
            return;

        if (!IsStandingForBuff(playerObject.GetComponent<CwslPlayerMovement>()))
            return;

        if (!goldSpringNextTime.TryGetValue(clientId, out var nextTime))
            nextTime = 0f;

        if (Time.time < nextTime)
            return;

        goldSpringNextTime[clientId] = Time.time + CwslGameConstants.GoldSpringIntervalSeconds;
        var gold = playerObject.GetComponent<CwslPlayerGold>();
        if (gold == null)
            return;

        gold.AddGoldServer(CwslGameConstants.GoldSpringAmount);
        PlayGoldSpringClientRpc(playerObject.transform.position);
    }

    private static bool IsStandingForBuff(CwslPlayerMovement movement)
    {
        if (movement == null)
            return true;

        return movement.CurrentMoveSpeed <= 0.45f;
    }

    [ClientRpc]
    private void PlayGoldSpringClientRpc(Vector3 position)
    {
        CwslArenaBuffVisuals.PlayGoldSpringBurst(position);
    }
}
