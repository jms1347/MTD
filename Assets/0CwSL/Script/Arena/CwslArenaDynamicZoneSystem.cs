using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>맵 기믹 랜덤 등장·만료·판정 (서버).</summary>
public class CwslArenaDynamicZoneSystem : NetworkBehaviour
{
    public struct ActiveZone
    {
        public int Id;
        public CwslDynamicZoneKind Kind;
        public Vector3 Center;
        public float Radius;
        public float ExpireTime;
    }

    public static CwslArenaDynamicZoneSystem Instance { get; private set; }

    private readonly List<ActiveZone> activeZones = new();
    private readonly Dictionary<int, float> trapCooldownUntil = new();
    private readonly Dictionary<int, float> donationCooldownUntil = new();
    private int nextZoneId = 1;
    private float nextSpawnTime;

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
        CwslArenaDynamicZoneVisuals.EnsureLocal();
        if (IsServer)
            nextSpawnTime = Time.time + CwslGameConstants.DynamicGimmickInitialDelaySeconds;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        RemoveExpiredZones();
        TickRandomSpawns();
    }

    public IReadOnlyList<ActiveZone> GetActiveZones() => activeZones;

    public bool TryGetZone(int zoneId, out ActiveZone zone)
    {
        for (var i = 0; i < activeZones.Count; i++)
        {
            if (activeZones[i].Id == zoneId)
            {
                zone = activeZones[i];
                return true;
            }
        }

        zone = default;
        return false;
    }

    public int FindZoneAt(Vector3 position, CwslDynamicZoneKind kind)
    {
        for (var i = 0; i < activeZones.Count; i++)
        {
            var zone = activeZones[i];
            if (zone.Kind != kind)
                continue;

            if (IsInsideZone(position, zone))
                return zone.Id;
        }

        return -1;
    }

    public int FindTrapZoneAt(Vector3 position, out Vector3 padCenter)
    {
        padCenter = Vector3.zero;
        for (var i = 0; i < activeZones.Count; i++)
        {
            var zone = activeZones[i];
            if (zone.Kind != CwslDynamicZoneKind.TrapSuicide && zone.Kind != CwslDynamicZoneKind.TrapRanged)
                continue;

            if (!IsInsideZone(position, zone))
                continue;

            padCenter = zone.Center;
            return zone.Id;
        }

        return -1;
    }

    public int FindDonationPadAt(Vector3 position, out Vector3 padCenter)
    {
        padCenter = Vector3.zero;
        var zoneId = FindZoneAt(position, CwslDynamicZoneKind.DonationPad);
        if (zoneId < 0)
            return -1;

        if (TryGetZone(zoneId, out var zone))
            padCenter = zone.Center;

        return zoneId;
    }

    public bool IsInZoneKind(Vector3 position, CwslDynamicZoneKind kind)
    {
        return FindZoneAt(position, kind) >= 0;
    }

    public CwslMonsterType GetTrapMonsterType(int zoneId)
    {
        if (!TryGetZone(zoneId, out var zone))
            return CwslMonsterType.Ranged;

        return zone.Kind == CwslDynamicZoneKind.TrapSuicide
            ? CwslMonsterType.Suicide
            : CwslMonsterType.Ranged;
    }

    public bool IsTrapOnCooldown(int zoneId)
    {
        return trapCooldownUntil.TryGetValue(zoneId, out var until) && Time.time < until;
    }

    public void MarkTrapTriggered(int zoneId)
    {
        trapCooldownUntil[zoneId] = Time.time + CwslGameConstants.TrapPadCooldownSeconds;
    }

    public bool IsDonationOnCooldown(int zoneId)
    {
        return donationCooldownUntil.TryGetValue(zoneId, out var until) && Time.time < until;
    }

    public void MarkDonationTriggered(int zoneId)
    {
        donationCooldownUntil[zoneId] = Time.time + CwslGameConstants.DonationPadCooldownSeconds;
    }

    public int CountAlliesInRallyZone(int zoneId)
    {
        if (!TryGetZone(zoneId, out var zone) || zone.Kind != CwslDynamicZoneKind.RallyZone)
            return 0;

        if (NetworkManager.Singleton == null)
            return 0;

        var count = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health == null || !health.IsAlive)
                continue;

            if (IsInsideZone(playerObject.transform.position, zone))
                count++;
        }

        return count;
    }

    private void TickRandomSpawns()
    {
        if (Time.time < nextSpawnTime)
            return;

        nextSpawnTime = Time.time + Random.Range(
            CwslGameConstants.DynamicGimmickSpawnIntervalMin,
            CwslGameConstants.DynamicGimmickSpawnIntervalMax);

        if (activeZones.Count >= CwslGameConstants.DynamicGimmickMaxAliveTotal)
            return;

        if (!TryPickSpawnPoint(out var center))
            return;

        var kind = (CwslDynamicZoneKind)Random.Range(0, 8);
        StartCoroutine(SpawnZoneWithWarningRoutine(kind, center));
    }

    private IEnumerator SpawnZoneWithWarningRoutine(CwslDynamicZoneKind kind, Vector3 center)
    {
        var radius = ResolveRadius(kind);
        ShowZoneWarningClientRpc(center, radius, (int)kind, CwslGameConstants.DynamicGimmickWarningSeconds);

        yield return new WaitForSeconds(CwslGameConstants.DynamicGimmickWarningSeconds);

        if (!IsServer)
            yield break;

        if (activeZones.Count >= CwslGameConstants.DynamicGimmickMaxAliveTotal)
            yield break;

        ActivateZoneServer(kind, center, radius);
    }

    private void ActivateZoneServer(CwslDynamicZoneKind kind, Vector3 center, float radius)
    {
        var duration = Random.Range(
            CwslGameConstants.DynamicGimmickDurationMin,
            CwslGameConstants.DynamicGimmickDurationMax);

        var zone = new ActiveZone
        {
            Id = nextZoneId++,
            Kind = kind,
            Center = center,
            Radius = radius,
            ExpireTime = Time.time + duration
        };
        activeZones.Add(zone);
        SpawnZoneVisualClientRpc(zone.Id, (int)zone.Kind, zone.Center, zone.Radius);
    }

    private void RemoveExpiredZones()
    {
        for (var i = activeZones.Count - 1; i >= 0; i--)
        {
            if (Time.time < activeZones[i].ExpireTime)
                continue;

            var zoneId = activeZones[i].Id;
            activeZones.RemoveAt(i);
            trapCooldownUntil.Remove(zoneId);
            donationCooldownUntil.Remove(zoneId);
            RemoveZoneVisualClientRpc(zoneId);
        }
    }

    private bool TryPickSpawnPoint(out Vector3 center)
    {
        center = Vector3.zero;
        for (var attempt = 0; attempt < CwslGameConstants.DynamicGimmickSpawnAttempts; attempt++)
        {
            var candidate = CwslArenaUtility.GetRandomSpawnPosition();
            candidate.y = 0f;

            if (IsTooCloseToExisting(candidate))
                continue;

            if (CwslArenaZones.IsInFightZone(candidate) || CwslArenaZones.IsInTianyuan(candidate))
                continue;

            if (NavMeshUtility.TryProject(candidate, out var grounded))
                candidate = grounded;

            center = candidate;
            return true;
        }

        return false;
    }

    private bool IsTooCloseToExisting(Vector3 candidate)
    {
        for (var i = 0; i < activeZones.Count; i++)
        {
            var flat = candidate - activeZones[i].Center;
            flat.y = 0f;
            if (flat.sqrMagnitude < CwslGameConstants.DynamicGimmickMinSeparation *
                CwslGameConstants.DynamicGimmickMinSeparation)
                return true;
        }

        return false;
    }

    private static bool IsInsideZone(Vector3 position, ActiveZone zone)
    {
        var flat = position - zone.Center;
        flat.y = 0f;
        return flat.sqrMagnitude <= zone.Radius * zone.Radius;
    }

    private static float ResolveRadius(CwslDynamicZoneKind kind)
    {
        return kind switch
        {
            CwslDynamicZoneKind.TrapSuicide => CwslGameConstants.TrapPadRadius,
            CwslDynamicZoneKind.TrapRanged => CwslGameConstants.TrapPadRadius,
            CwslDynamicZoneKind.DonationPad => CwslGameConstants.DonationPadRadius,
            CwslDynamicZoneKind.BadGrass => CwslGameConstants.BadGrassPatchRadius,
            CwslDynamicZoneKind.HealingSpring => CwslGameConstants.HealingSpringRadius,
            CwslDynamicZoneKind.TailwindGrass => CwslGameConstants.TailwindGrassRadius,
            CwslDynamicZoneKind.RallyZone => CwslGameConstants.RallyZoneRadius,
            _ => CwslGameConstants.GoldSpringRadius
        };
    }

    [ClientRpc]
    private void ShowZoneWarningClientRpc(Vector3 center, float radius, int kind, float durationSeconds)
    {
        CwslArenaDynamicZoneVisuals.ShowSpawnWarning(
            center,
            radius,
            (CwslDynamicZoneKind)kind,
            durationSeconds);
    }

    [ClientRpc]
    private void SpawnZoneVisualClientRpc(int zoneId, int kind, Vector3 center, float radius)
    {
        CwslArenaDynamicZoneVisuals.SpawnZoneVisual(zoneId, (CwslDynamicZoneKind)kind, center, radius);
    }

    [ClientRpc]
    private void RemoveZoneVisualClientRpc(int zoneId)
    {
        CwslArenaDynamicZoneVisuals.RemoveZoneVisual(zoneId);
    }

    public void SyncRallyZoneClient(int zoneId, bool active)
    {
        if (!IsServer)
            return;

        SyncRallyZoneClientRpc(zoneId, active);
    }

    [ClientRpc]
    private void SyncRallyZoneClientRpc(int zoneId, bool active)
    {
        CwslArenaDynamicZoneVisuals.SetRallyZoneActive(zoneId, active);
    }
}
