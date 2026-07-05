using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>랜덤 생성 보일링 장판 — 산성(독뎀), 용암(골드 흘림), 물(슬로우).</summary>
public class CwslArenaHazardPadSystem : NetworkBehaviour
{
    private struct ActivePad
    {
        public int Id;
        public CwslHazardPadKind Kind;
        public Vector3 Center;
        public float Radius;
        public float ExpireTime;
    }

    public static CwslArenaHazardPadSystem Instance { get; private set; }

    private readonly List<ActivePad> activePads = new();
    private readonly Dictionary<ulong, float> lavaLeakNextTime = new();
    private int nextPadId = 1;
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
        CwslArenaTrapVisuals.EnsureLocal();
        if (IsServer)
            nextSpawnTime = Time.time + CwslGameConstants.HazardPadInitialDelaySeconds;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        RemoveExpiredPads();
        TickSpawns();
        TickPadEffects();
    }

    private void TickSpawns()
    {
        if (Time.time < nextSpawnTime)
            return;

        nextSpawnTime = Time.time + Random.Range(
            CwslGameConstants.HazardPadSpawnIntervalMin,
            CwslGameConstants.HazardPadSpawnIntervalMax);

        if (activePads.Count >= CwslGameConstants.HazardPadMaxAlive)
            return;

        if (!TryPickSpawnPoint(out var center))
            return;

        var kind = (CwslHazardPadKind)Random.Range(0, 3);
        SpawnPadServer(kind, center, CwslGameConstants.HazardPadRadius);
    }

    private void SpawnPadServer(
        CwslHazardPadKind kind,
        Vector3 center,
        float radius,
        float durationSeconds = -1f)
    {
        if (durationSeconds <= 0f)
            durationSeconds = CwslGameConstants.HazardPadDurationSeconds;

        var pad = new ActivePad
        {
            Id = nextPadId++,
            Kind = kind,
            Center = center,
            Radius = radius,
            ExpireTime = Time.time + durationSeconds
        };
        activePads.Add(pad);
        SpawnHazardPadClientRpc(pad.Id, (int)pad.Kind, pad.Center, pad.Radius);
    }

    public void TrySpawnMeteorCraterPadServer(Vector3 center)
    {
        if (!IsServer)
            return;

        if (Random.value > CwslGameConstants.MeteorHazardPadChance)
            return;

        if (activePads.Count >= CwslGameConstants.HazardPadMaxAlive)
            return;

        if (IsTooCloseToExisting(center, CwslGameConstants.HazardPadMinSeparation * 0.55f))
            return;

        if (NavMeshUtility.TryProject(center, out var grounded))
            center = grounded;

        var kind = (CwslHazardPadKind)Random.Range(0, 3);
        SpawnPadServer(
            kind,
            center,
            CwslGameConstants.MeteorHazardPadRadius,
            CwslGameConstants.MeteorHazardPadDurationSeconds);
    }

    private void RemoveExpiredPads()
    {
        for (var i = activePads.Count - 1; i >= 0; i--)
        {
            if (Time.time < activePads[i].ExpireTime)
                continue;

            RemoveHazardPadClientRpc(activePads[i].Id);
            activePads.RemoveAt(i);
        }
    }

    private void TickPadEffects()
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

            if (!TryGetPadAt(playerObject.transform.position, out var kind, out _))
                continue;

            var position = playerObject.transform.position;
            switch (kind)
            {
                case CwslHazardPadKind.Acid:
                    health.TryReceiveEnvironmentHitServer(
                        CwslGameConstants.HazardAcidDamagePerSecond * Time.deltaTime,
                        position);
                    break;

                case CwslHazardPadKind.Lava:
                    TickLavaGoldLeak(playerObject, position);
                    break;

                case CwslHazardPadKind.Water:
                    CwslSlowModifier.Ensure(playerObject)?.ApplySlow(
                        CwslGameConstants.HazardWaterSlowMultiplier,
                        0.25f);
                    break;
            }
        }
    }

    private void TickLavaGoldLeak(NetworkObject playerObject, Vector3 position)
    {
        var clientId = playerObject.OwnerClientId;
        if (lavaLeakNextTime.TryGetValue(clientId, out var nextTime) && Time.time < nextTime)
            return;

        lavaLeakNextTime[clientId] = Time.time + CwslGameConstants.HazardLavaGoldLeakIntervalSeconds;

        var gold = playerObject.GetComponent<CwslPlayerGold>();
        if (gold == null || gold.Gold <= 0)
            return;

        if (!gold.TrySpendGoldServer(CwslGameConstants.HazardLavaGoldLeakAmount, playSpendEffect: false))
            return;

        CwslGoldDropService.SpawnDrop(position, CwslGameConstants.HazardLavaGoldLeakAmount);
        PlayLavaGoldLeakClientRpc(position);
    }

    private bool TryGetPadAt(Vector3 position, out CwslHazardPadKind kind, out Vector3 center)
    {
        kind = default;
        center = default;

        for (var i = 0; i < activePads.Count; i++)
        {
            var pad = activePads[i];
            if (FlatDistance(position, pad.Center) > pad.Radius)
                continue;

            kind = pad.Kind;
            center = pad.Center;
            return true;
        }

        return false;
    }

    private static bool TryPickSpawnPoint(out Vector3 center)
    {
        center = default;
        for (var attempt = 0; attempt < 14; attempt++)
        {
            center = CwslArenaUtility.GetRandomSpawnPosition();
            center.y = 0f;

            if (Instance != null && Instance.IsTooCloseToExisting(center))
                continue;

            if (NavMeshUtility.TryProject(center, out var grounded))
                center = grounded;

            return true;
        }

        return false;
    }

    private bool IsTooCloseToExisting(Vector3 center, float minSeparation = -1f)
    {
        if (minSeparation <= 0f)
            minSeparation = CwslGameConstants.HazardPadMinSeparation;
        for (var i = 0; i < activePads.Count; i++)
        {
            if (FlatDistance(center, activePads[i].Center) < minSeparation)
                return true;
        }

        return false;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        var flat = a - b;
        flat.y = 0f;
        return flat.magnitude;
    }

    [ClientRpc]
    private void SpawnHazardPadClientRpc(int padId, int kind, Vector3 center, float radius)
    {
        CwslArenaTrapVisuals.SpawnHazardPad(padId, (CwslHazardPadKind)kind, center, radius);
    }

    [ClientRpc]
    private void RemoveHazardPadClientRpc(int padId)
    {
        CwslArenaTrapVisuals.RemoveHazardPad(padId);
    }

    [ClientRpc]
    private void PlayLavaGoldLeakClientRpc(Vector3 position)
    {
        CwslArenaTrapVisuals.PlayLavaGoldLeak(position);
    }
}
