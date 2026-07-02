using System.Collections;
using UnityEngine;

/// <summary>
/// 타워에서 마커 큐브를 지면에 배치해 적을 추적하고,
/// 해당 위치에 경고 반경을 표시한 뒤 Nuke 미사일을 낙하시켜 폭발시킵니다.
/// 마커는 메테오 낙하 후에도 사라지지 않고 그 자리에서 계속 적을 탐색합니다.
/// </summary>
public class MeteorTowerController : MonoBehaviour
{
    [Header("타겟팅")]
    [SerializeField] private float targetingRange = 28f;
    [SerializeField] private float markerCubeSpeed = 28f;
    [SerializeField] private float markerCubeScale = 0.38f;

    [Header("메테오")]
    [SerializeField] private float strikeCooldown = 4.5f;
    [SerializeField] private float impactRadius = 5.5f;
    [SerializeField] private float strikeDamage = 5f;
    [SerializeField] private float meteorDropHeight = 22f;
    [SerializeField] private float meteorFallSpeed = 38f;
    [SerializeField] private float explosionVisualScalePerRadius = 0.35f;
    [SerializeField] private float targetingScoutDuration = 1.8f;
    [SerializeField] private float strikeWarningDelay = 2f;
    [SerializeField] private float markerWanderRadius = 2.2f;
    [SerializeField] private float meteorDiagonalSpread = 0.55f;

    [Header("VFX")]
    [SerializeField] private GameObject meteorProjectilePrefab;
    [SerializeField] private GameObject meteorExplosionPrefab;

    [Header("경고 연출")]
    [SerializeField] private Color warningZoneColor = new Color(1f, 0.15f, 0.05f, 0.45f);
    [SerializeField] private Color markerCubeColor = new Color(1f, 0.35f, 0.1f, 0.95f);

    private const float GroundY = 0.05f;

    private float nextStrikeTime;
    private bool isStriking;
    private bool isMarkerReady;
    private Transform markerLaunchPoint;
    private GameObject persistentMarker;
    private GameObject activeWarningZone;
    private Vector3 currentStrikePosition;
    private Coroutine markerBehaviourCoroutine;

    public float TargetingRange => targetingRange;

    public void Initialize(GameObject projectilePrefab, GameObject explosionPrefab)
    {
        meteorProjectilePrefab = projectilePrefab;
        meteorExplosionPrefab = explosionPrefab;

        if (TowerStatsManager.Instance != null)
            TowerStatsManager.Instance.ApplyTo(this);

        EnsureLaunchPoint();
        StartMarkerBehaviour();
        nextStrikeTime = Time.time + strikeCooldown * 0.35f;
    }

    public void ApplyStats(MeteorTowerStats stats)
    {
        if (stats == null)
            return;

        targetingRange = stats.targetingRange;
        markerCubeSpeed = stats.markerCubeSpeed;
        strikeDamage = stats.strikeDamage;
        strikeCooldown = stats.strikeCooldown;
        impactRadius = stats.impactRadius;
        meteorDropHeight = stats.meteorDropHeight;
        meteorFallSpeed = stats.meteorFallSpeed;
        explosionVisualScalePerRadius = stats.explosionVisualScalePerRadius;
        markerCubeScale = stats.markerCubeScale;
        targetingScoutDuration = stats.targetingScoutDuration;
        strikeWarningDelay = stats.strikeWarningDelay;
        markerWanderRadius = stats.markerWanderRadius;
        meteorDiagonalSpread = stats.meteorDiagonalSpread;
    }

    private void OnEnable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageBattleBegan += HandleStageBattleBegan;
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnStageBattleBegan -= HandleStageBattleBegan;
    }

    private void HandleStageBattleBegan()
    {
        isStriking = false;
        ClearWarningZone();

        if (persistentMarker == null || markerLaunchPoint == null)
            return;

        float markerGroundY = GroundY + markerCubeScale * 0.5f;
        Vector3 startGround = ToGroundPoint(markerLaunchPoint.position);
        startGround.y = markerGroundY;
        persistentMarker.transform.position = startGround;
        currentStrikePosition = ToGroundPoint(startGround);
    }

    private void OnDestroy()
    {
        ClearWarningZone();

        if (persistentMarker != null)
            Destroy(persistentMarker);
    }

    private void EnsureLaunchPoint()
    {
        if (markerLaunchPoint != null)
            return;

        var existing = transform.Find("MeteorMarkerLaunch");
        if (existing != null)
        {
            markerLaunchPoint = existing;
            return;
        }

        var launch = new GameObject("MeteorMarkerLaunch").transform;
        launch.SetParent(transform, false);
        launch.localPosition = new Vector3(0f, 0.75f, 0f);
        markerLaunchPoint = launch;
    }

    private void Update()
    {
        if (isStriking || !isMarkerReady || Time.time < nextStrikeTime)
            return;

        if (!HasStrikeableEnemiesNearMarker())
            return;

        StartCoroutine(StrikeRoutine(currentStrikePosition));
        nextStrikeTime = Time.time + strikeCooldown;
    }

    private void StartMarkerBehaviour()
    {
        if (markerBehaviourCoroutine != null)
            StopCoroutine(markerBehaviourCoroutine);

        markerBehaviourCoroutine = StartCoroutine(MarkerBehaviourLoop());
    }

    /// <summary>
    /// 마커를 타워에서 지면으로 보낸 뒤, 메테오가 떨어져도 계속 그 주변에서 적을 탐색합니다.
    /// </summary>
    private IEnumerator MarkerBehaviourLoop()
    {
        EnsureLaunchPoint();
        EnsurePersistentMarker();

        float markerGroundY = GroundY + markerCubeScale * 0.5f;
        Vector3 startGround = ToGroundPoint(markerLaunchPoint.position);
        startGround.y = markerGroundY;
        persistentMarker.transform.position = startGround;
        currentStrikePosition = ToGroundPoint(startGround);

        float deployElapsed = 0f;
        while (deployElapsed < targetingScoutDuration)
        {
            deployElapsed += Time.deltaTime;
            UpdateMarkerMovement(markerGroundY, deployElapsed);
            currentStrikePosition = ToGroundPoint(persistentMarker.transform.position);
            yield return null;
        }

        isMarkerReady = true;
        float scoutTime = 0f;

        while (true)
        {
            scoutTime += Time.deltaTime;
            UpdateMarkerMovement(markerGroundY, scoutTime);
            currentStrikePosition = ToGroundPoint(persistentMarker.transform.position);
            yield return null;
        }
    }

    private void UpdateMarkerMovement(float markerGroundY, float elapsedTime)
    {
        if (persistentMarker == null)
            return;

        Vector3 markerPosition = persistentMarker.transform.position;
        Vector3 bestStrike = FindBestStrikePosition(markerPosition);

        float wanderScale = Mathf.Lerp(impactRadius * 0.25f, markerWanderRadius, Mathf.PingPong(elapsedTime * 1.4f, 1f));
        Vector3 wanderOffset = new Vector3(
            Mathf.Sin(elapsedTime * 5.5f) * wanderScale,
            0f,
            Mathf.Cos(elapsedTime * 4.3f) * wanderScale);

        Vector3 probePosition = bestStrike + wanderOffset;
        probePosition.y = markerGroundY;

        Vector3 toProbe = probePosition - markerPosition;
        toProbe.y = 0f;
        float step = markerCubeSpeed * Time.deltaTime;

        if (toProbe.sqrMagnitude <= step * step)
            persistentMarker.transform.position = probePosition;
        else
        {
            Vector3 next = markerPosition + toProbe.normalized * step;
            next.y = markerGroundY;
            persistentMarker.transform.position = next;
        }
    }

    private IEnumerator StrikeRoutine(Vector3 strikeAnchor)
    {
        isStriking = true;
        Vector3 lockedStrike = ToGroundPoint(strikeAnchor);

        ClearWarningZone();
        activeWarningZone = CreateWarningZone(lockedStrike);

        float warningElapsed = 0f;
        while (warningElapsed < strikeWarningDelay)
        {
            warningElapsed += Time.deltaTime;
            PulseWarningZone(activeWarningZone, warningElapsed / strikeWarningDelay);
            yield return null;
        }

        SpawnFallingMeteor(lockedStrike, null);
        ClearWarningZone();

        float timeout = meteorDropHeight / Mathf.Max(meteorFallSpeed, 1f) + 2f;
        yield return new WaitForSeconds(timeout);

        isStriking = false;
    }

    private bool HasStrikeableEnemiesNearMarker()
    {
        if (persistentMarker == null)
            return false;

        return CountEnemiesInRadius(ToGroundPoint(persistentMarker.transform.position)) > 0;
    }

    private Vector3 FindBestStrikePosition(Vector3 searchOrigin)
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Vector3 fallback = ToGroundPoint(searchOrigin);
        Vector3 best = fallback;
        int bestHitCount = -1;
        float rangeSqr = targetingRange * targetingRange;

        foreach (var enemy in enemies)
        {
            if (!DefenseEnemyQuery.IsLivingEnemy(enemy, requireLanded: true))
                continue;

            if ((enemy.transform.position - searchOrigin).sqrMagnitude > rangeSqr)
                continue;

            Vector3 candidate = ToGroundPoint(enemy.transform.position);
            int hitCount = CountEnemiesInRadius(candidate);
            if (hitCount > bestHitCount)
            {
                bestHitCount = hitCount;
                best = candidate;
            }
        }

        return bestHitCount > 0 ? best : fallback;
    }

    private int CountEnemiesInRadius(Vector3 center)
    {
        int count = 0;
        var overlaps = Physics.OverlapSphere(center, impactRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

        foreach (var overlap in overlaps)
        {
            if (!DefenseEnemyQuery.IsAttackableCollider(overlap, out _, requireLanded: true))
                continue;

            count++;
        }

        return count;
    }

    private static void PulseWarningZone(GameObject zone, float normalizedTime)
    {
        if (zone == null)
            return;

        var renderer = zone.GetComponent<Renderer>();
        if (renderer == null || renderer.material == null)
            return;

        float pulse = 0.55f + Mathf.Sin(normalizedTime * Mathf.PI * 8f) * 0.25f;
        var color = renderer.material.color;
        color.a = Mathf.Clamp01(0.45f * pulse);
        renderer.material.color = color;
    }

    private void SpawnFallingMeteor(Vector3 strikeGround, System.Action onImpact)
    {
        if (meteorProjectilePrefab == null)
        {
            MeteorStrikeProjectile.ExecuteInstantImpact(
                strikeGround,
                strikeDamage,
                impactRadius,
                meteorExplosionPrefab,
                null,
                explosionVisualScalePerRadius);
            onImpact?.Invoke();
            return;
        }

        Vector3 spawnPosition = strikeGround + GetDiagonalSpawnOffset(strikeGround);
        Vector3 fallDirection = (strikeGround - spawnPosition).normalized;
        var meteor = Instantiate(meteorProjectilePrefab, spawnPosition, Quaternion.LookRotation(fallDirection));

        var projectile = meteor.GetComponent<MeteorStrikeProjectile>();
        if (projectile == null)
            projectile = meteor.AddComponent<MeteorStrikeProjectile>();

        projectile.Launch(
            fallDirection * meteorFallSpeed,
            strikeDamage,
            impactRadius,
            meteorExplosionPrefab,
            explosionVisualScalePerRadius,
            _ => onImpact?.Invoke());
    }

    private Vector3 GetDiagonalSpawnOffset(Vector3 strikeGround)
    {
        Vector3 awayFromTower = strikeGround - transform.position;
        awayFromTower.y = 0f;
        if (awayFromTower.sqrMagnitude < 0.01f)
            awayFromTower = Vector3.forward;

        awayFromTower.Normalize();
        return awayFromTower * (meteorDropHeight * meteorDiagonalSpread) + Vector3.up * meteorDropHeight;
    }

    private void EnsurePersistentMarker()
    {
        if (persistentMarker != null)
            return;

        persistentMarker = CreateMarkerCube();
    }

    private void ClearWarningZone()
    {
        if (activeWarningZone == null)
            return;

        var renderer = activeWarningZone.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
            Destroy(renderer.material);

        Destroy(activeWarningZone);
        activeWarningZone = null;
    }

    private GameObject CreateMarkerCube()
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "MeteorTargetMarker";
        cube.transform.localScale = Vector3.one * markerCubeScale;

        var collider = cube.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        var renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = markerCubeColor;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", markerCubeColor * 1.2f);
            renderer.material = material;
        }

        return cube;
    }

    private GameObject CreateWarningZone(Vector3 groundPoint)
    {
        var zone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        zone.name = "MeteorWarningZone";
        zone.transform.position = groundPoint + Vector3.up * 0.04f;
        zone.transform.localScale = new Vector3(impactRadius * 2f, 0.04f, impactRadius * 2f);

        var collider = zone.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        var renderer = zone.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = CreateTransparentMaterial(warningZoneColor);

        return zone;
    }

    private static Vector3 ToGroundPoint(Vector3 worldPosition)
    {
        return new Vector3(worldPosition.x, GroundY, worldPosition.z);
    }

    private static Material CreateTransparentMaterial(Color color)
    {
        var material = new Material(Shader.Find("Standard"));
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        material.color = color;
        return material;
    }
}
