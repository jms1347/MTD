using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>함정(가짜 골드·기부 패드·늪·오프사이드) + 광역 랜덤 이벤트(메테오·라이트닝).</summary>
public class CwslArenaTrapSystem : NetworkBehaviour
{
    public static CwslArenaTrapSystem Instance { get; private set; }

    private float nextFakeGoldSpawnTime;
    private float nextOffsideLaserTime;
    private float nextRandomEventTime;
    private float nextLightningStrikeTime;
    private float lightningEndTime;
    private bool offsideWarningActive;
    private float offsideStrikeTime;
    private Vector3 offsideLineStart;
    private Vector3 offsideLineEnd;
    private Vector3 lightningCenter;
    private bool lightningActive;
    private bool lightningWarningActive;
    private bool meteorEventActive;
    private readonly float[] donationPadCooldownUntil = new float[CwslGameConstants.DonationPadCount];

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
        if (!IsServer)
        {
            CwslArenaTrapVisuals.EnsureLocal();
            return;
        }

        nextFakeGoldSpawnTime = Time.time + 8f;
        nextOffsideLaserTime = Time.time + CwslGameConstants.OffsideLaserIntervalSeconds;
        nextRandomEventTime = Time.time + Random.Range(
            CwslGameConstants.RandomEventIntervalMin,
            CwslGameConstants.RandomEventIntervalMax);
    }

    private void Update()
    {
        if (!IsServer)
            return;

        TickFakeGoldSpawns();
        TickDonationPads();
        TickBadGrassSlow();
        TickOffsideLaser();
        TickLightningMode();
        TickRandomEvents();
    }

    public void TriggerFakeGoldServer(NetworkObject player, Vector3 trapPosition)
    {
        if (!IsServer || player == null)
            return;

        var count = Random.Range(
            CwslGameConstants.FakeGoldSuicideSpawnMin,
            CwslGameConstants.FakeGoldSuicideSpawnMax + 1);

        var spawner = CwslGameSession.Instance?.MonsterSpawner
                      ?? FindFirstObjectByType<CwslMonsterSpawner>();
        spawner?.SpawnSuicidesNearServer(player.transform.position, count, 4.2f);
    }

    private void TickFakeGoldSpawns()
    {
        if (Time.time < nextFakeGoldSpawnTime)
            return;

        nextFakeGoldSpawnTime = Time.time + CwslGameConstants.FakeGoldRespawnInterval;
        if (CountAliveFakeGold() >= CwslGameConstants.FakeGoldMaxAlive)
            return;

        if (!TryGetRandomLivingPlayer(out var playerObject))
            return;

        CwslGoldDropService.TrySpawnFakeGoldNearPlayer(playerObject.transform.position);
    }

    private void TickDonationPads()
    {
        if (NetworkManager.Singleton == null)
            return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            var padIndex = CwslArenaZones.GetDonationPadIndexAt(playerObject.transform.position);
            if (padIndex < 0)
                continue;

            if (Time.time < donationPadCooldownUntil[padIndex])
                continue;

            var gold = playerObject.GetComponent<CwslPlayerGold>();
            if (gold == null || gold.Gold <= 0)
                continue;

            donationPadCooldownUntil[padIndex] = Time.time + CwslGameConstants.DonationPadCooldownSeconds;
            var origin = playerObject.transform.position;
            CwslGoldDropService.ScatterPlayerGoldAcrossArena(gold, origin);
            PlayDonationScatterClientRpc(origin, padIndex);
        }
    }

    private void TickBadGrassSlow()
    {
        if (NetworkManager.Singleton == null)
            return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health != null && !health.IsAlive)
                continue;

            if (!CwslArenaZones.IsInBadGrass(playerObject.transform.position))
                continue;

            CwslSlowModifier.Ensure(playerObject)?.ApplySlow(
                CwslGameConstants.BadGrassSlowMultiplier,
                0.25f);
        }
    }

    private void TickOffsideLaser()
    {
        if (offsideWarningActive)
        {
            if (Time.time >= offsideStrikeTime)
                ExecuteOffsidePenaltyServer();
            return;
        }

        if (Time.time < nextOffsideLaserTime)
            return;

        nextOffsideLaserTime = Time.time + CwslGameConstants.OffsideLaserIntervalSeconds;
        BeginOffsideWarningServer();
    }

    private void BeginOffsideWarningServer()
    {
        var extent = CwslGameConstants.ArenaHalfExtent - 2f;
        var angle = Random.Range(0f, Mathf.PI * 2f);
        var normal = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        var tangent = new Vector3(-normal.z, 0f, normal.x);
        var centerOffset = Random.Range(-extent * 0.35f, extent * 0.35f);
        var lineCenter = tangent * centerOffset;
        offsideLineStart = lineCenter - normal * extent;
        offsideLineEnd = lineCenter + normal * extent;
        offsideLineStart.y = 0.05f;
        offsideLineEnd.y = 0.05f;

        offsideWarningActive = true;
        offsideStrikeTime = Time.time + CwslGameConstants.OffsideLaserWarningSeconds;
        ShowOffsideWarningClientRpc(offsideLineStart, offsideLineEnd);
    }

    private void ExecuteOffsidePenaltyServer()
    {
        offsideWarningActive = false;
        HideOffsideLaserClientRpc();

        if (NetworkManager.Singleton == null)
            return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health != null && !health.IsAlive)
                continue;

            var position = playerObject.transform.position;
            var onLine = CwslArenaZones.DistanceToLineSegmentXZ(
                             position,
                             offsideLineStart,
                             offsideLineEnd)
                         <= CwslGameConstants.OffsideLineHitWidth;
            var deepBossSide = CwslArenaZones.IsInOffsideBossTerritory(position);

            if (!onLine && !deepBossSide)
                continue;

            playerObject.GetComponent<CwslPlayerVisionDebuff>()
                ?.ApplyForcedBlindServer(CwslGameConstants.OffsideBlindDurationSeconds);
            PlayOffsideBlindClientRpc(position);
        }
    }

    private void TickRandomEvents()
    {
        if (lightningActive || lightningWarningActive || meteorEventActive || offsideWarningActive)
            return;

        if (Time.time < nextRandomEventTime)
            return;

        nextRandomEventTime = Time.time + Random.Range(
            CwslGameConstants.RandomEventIntervalMin,
            CwslGameConstants.RandomEventIntervalMax);

        if (Random.value < 0.5f)
            StartMeteorShowerServer();
        else
            StartLightningModeServer();
    }

    private void StartMeteorShowerServer()
    {
        var points = new Vector3[CwslGameConstants.MeteorShowerHitCount];
        for (var i = 0; i < points.Length; i++)
            points[i] = CwslArenaUtility.GetRandomSpawnPosition();

        meteorEventActive = true;
        StartCoroutine(MeteorShowerRoutine(points));
    }

    private IEnumerator MeteorShowerRoutine(Vector3[] impactPoints)
    {
        var warningSeconds = CwslGameConstants.MeteorShowerWarningSeconds;
        var warningRadius = CwslGameConstants.MeteorShowerWarningRadius;

        foreach (var point in impactPoints)
            ShowMeteorWarningClientRpc(point, warningRadius, warningSeconds);

        yield return new WaitForSeconds(warningSeconds);

        ClearMeteorWarningsClientRpc();

        foreach (var point in impactPoints)
            PlayMeteorImpactClientRpc(point);

        yield return new WaitForSeconds(CwslGameConstants.MeteorShowerFallDelay + 0.25f);

        foreach (var point in impactPoints)
            CwslGoldDropService.SpawnDrop(point, CwslGameConstants.MeteorShowerGoldPerHit);

        yield return new WaitForSeconds(CwslGameConstants.MeteorHazardPadDelaySeconds);

        foreach (var point in impactPoints)
            CwslArenaHazardPadSystem.Instance?.TrySpawnMeteorCraterPadServer(point);

        meteorEventActive = false;
    }

    private void StartLightningModeServer()
    {
        lightningCenter = CwslArenaUtility.GetRandomSpawnPosition();
        lightningCenter.y = 0f;
        lightningWarningActive = true;
        ShowLightningWarningClientRpc(
            lightningCenter,
            CwslGameConstants.LightningModeZoneRadius,
            CwslGameConstants.LightningModeWarningSeconds);
        StartCoroutine(BeginLightningModeRoutine());
    }

    private IEnumerator BeginLightningModeRoutine()
    {
        yield return new WaitForSeconds(CwslGameConstants.LightningModeWarningSeconds);

        lightningWarningActive = false;
        ClearLightningWarningClientRpc();
        lightningActive = true;
        lightningEndTime = Time.time + CwslGameConstants.LightningModeDurationSeconds;
        nextLightningStrikeTime = Time.time;
        StartLightningModeClientRpc(lightningCenter, CwslGameConstants.LightningModeZoneRadius);
    }

    private void TickLightningMode()
    {
        if (!lightningActive)
            return;

        if (Time.time >= lightningEndTime)
        {
            lightningActive = false;
            EndLightningModeClientRpc();
            return;
        }

        if (Time.time < nextLightningStrikeTime)
            return;

        nextLightningStrikeTime = Time.time + CwslGameConstants.LightningStrikeIntervalSeconds;
        StrikePlayersInLightningRangeServer();
    }

    private void StrikePlayersInLightningRangeServer()
    {
        if (NetworkManager.Singleton == null)
            return;

        var radiusSqr = CwslGameConstants.LightningModeZoneRadius * CwslGameConstants.LightningModeZoneRadius;
        var orbOrigin = lightningCenter + Vector3.up * CwslGameConstants.LightningOrbHeight;
        var candidates = new System.Collections.Generic.List<NetworkObject>();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            var health = playerObject.GetComponent<CwslPlayerHealth>();
            if (health == null || !health.IsAlive)
                continue;

            var flat = playerObject.transform.position - lightningCenter;
            flat.y = 0f;
            if (flat.sqrMagnitude > radiusSqr)
                continue;

            candidates.Add(playerObject);
        }

        if (candidates.Count == 0)
            return;

        var target = candidates[Random.Range(0, candidates.Count)];
        var strikePoint = target.transform.position;
        var travelTime = Vector3.Distance(orbOrigin, strikePoint) / CwslGameConstants.LightningMissileSpeed;
        StartCoroutine(ApplyLightningStunAfterTravelRoutine(target, travelTime, strikePoint));
        PlayLightningMissileClientRpc(lightningCenter, strikePoint);
    }

    private IEnumerator ApplyLightningStunAfterTravelRoutine(
        NetworkObject playerObject,
        float travelTime,
        Vector3 strikePoint)
    {
        yield return new WaitForSeconds(travelTime);

        if (playerObject == null || !playerObject.IsSpawned)
            yield break;

        var health = playerObject.GetComponent<CwslPlayerHealth>();
        if (health == null || !health.IsAlive)
            yield break;

        var flat = playerObject.transform.position - lightningCenter;
        flat.y = 0f;
        if (flat.sqrMagnitude > CwslGameConstants.LightningModeZoneRadius * CwslGameConstants.LightningModeZoneRadius)
            yield break;

        playerObject.GetComponent<CwslPlayerStun>()
            ?.ApplyStunServer(CwslGameConstants.LightningStunDurationSeconds, strikePoint);
    }

    private static int CountAliveFakeGold()
    {
        var count = 0;
        var pickups = FindObjectsByType<CwslGoldPickup>(FindObjectsSortMode.None);
        foreach (var pickup in pickups)
        {
            if (pickup != null && pickup.IsSpawned && pickup.IsFake)
                count++;
        }

        return count;
    }

    private static bool TryGetRandomLivingPlayer(out NetworkObject playerObject)
    {
        playerObject = null;
        if (NetworkManager.Singleton == null)
            return false;

        var candidates = new System.Collections.Generic.List<NetworkObject>();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
                continue;

            var health = client.PlayerObject.GetComponent<CwslPlayerHealth>();
            if (health != null && !health.IsAlive)
                continue;

            candidates.Add(client.PlayerObject);
        }

        if (candidates.Count == 0)
            return false;

        playerObject = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    [ClientRpc]
    private void ShowOffsideWarningClientRpc(Vector3 lineStart, Vector3 lineEnd)
    {
        CwslArenaTrapVisuals.ShowOffsideLaser(lineStart, lineEnd);
    }

    [ClientRpc]
    private void HideOffsideLaserClientRpc()
    {
        CwslArenaTrapVisuals.HideOffsideLaser();
    }

    [ClientRpc]
    private void PlayOffsideBlindClientRpc(Vector3 position)
    {
        CwslArenaTrapVisuals.PlayOffsideBlind(position);
    }

    [ClientRpc]
    private void PlayDonationScatterClientRpc(Vector3 origin, int padIndex)
    {
        CwslArenaTrapVisuals.PlayDonationScatter(origin, padIndex);
    }

    [ClientRpc]
    private void ShowMeteorWarningClientRpc(Vector3 impactPoint, float radius, float durationSeconds)
    {
        CwslArenaTrapVisuals.ShowMeteorWarning(impactPoint, radius, durationSeconds);
    }

    [ClientRpc]
    private void ClearMeteorWarningsClientRpc()
    {
        CwslArenaTrapVisuals.ClearMeteorWarnings();
    }

    [ClientRpc]
    private void ShowLightningWarningClientRpc(Vector3 center, float radius, float durationSeconds)
    {
        CwslArenaTrapVisuals.ShowLightningWarning(center, radius, durationSeconds);
    }

    [ClientRpc]
    private void ClearLightningWarningClientRpc()
    {
        CwslArenaTrapVisuals.ClearLightningWarning();
    }

    [ClientRpc]
    private void PlayMeteorImpactClientRpc(Vector3 impactPoint)
    {
        CwslArenaTrapVisuals.PlayMeteorShower(new[] { impactPoint });
    }

    [ClientRpc]
    private void StartLightningModeClientRpc(Vector3 center, float radius)
    {
        CwslArenaTrapVisuals.StartLightningMode(center, radius);
    }

    [ClientRpc]
    private void PlayLightningMissileClientRpc(Vector3 orbCenter, Vector3 target)
    {
        CwslArenaTrapVisuals.PlayLightningMissile(orbCenter, target);
    }

    [ClientRpc]
    private void EndLightningModeClientRpc()
    {
        CwslArenaTrapVisuals.EndLightningMode();
    }
}
