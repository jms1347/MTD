using UnityEngine;

public class DefenseProjectile : MonoBehaviour
{
    [SerializeField] private GameObject impactParticle;
    [SerializeField] private GameObject projectileParticle;
    [SerializeField] private GameObject muzzleParticle;
    [SerializeField] private float colliderRadius = 0.25f;
    [SerializeField] private float collideOffset = 0.1f;
    [SerializeField] private float damage = 5f;

    private GameObject spawnedProjectileParticle;
    private float spawnTime;
    private Vector3 lastPosition;
    private bool isActive;
    private GameObject sourcePrefab;
    private Rigidbody cachedRigidbody;
    private DamageElement damageElement = DamageElement.Blue;
    private DefenseSkillProjectileContext skillContext;
    private bool hasSkillContext;
    private bool useBallistic;
    private bool useAdHocBallistic;
    private bool useVolcanoFallHoming;
    private bool adHocVolcanoInterceptActive;
    private bool suppressLaunchMuzzleVfx;
    private int adHocStrikeSkillId;
    private DefenseSkillElement adHocStrikeElement = DefenseSkillElement.Physical;
    private float adHocVisualScale = 1f;
    private Vector3 adHocLandPoint;
    private float adHocSplashRadius;
    private Vector3 adHocHomingSearchOrigin;
    private float adHocHomingSearchRange;
    private float adHocFallHomingSpeedMultiplier = 1f;
    private string adHocTargetMobility;
    private Vector3 defaultLocalScale = Vector3.one;
    private float frozenOrbShardVisualScale = 1f;
    private const float MaxLifetime = 6f;
    private const float GroundY = 0.05f;
    private const float HomingTurnSpeed = 8f;
    private const float BallisticLandSnapRadius = 2.25f;
    private const float BallisticLandSnapMaxHeight = 0.55f;
    private const float SkyBurstSnapRadius = 1.85f;
    private const float StormMissileMinAirClearance = 0.45f;

    public float ColliderRadius => colliderRadius;
    public float CollideOffset => collideOffset;
    public bool IsFlightActive => isActive;
    public bool IsFrozenOrbShard => hasSkillContext && skillContext.isFrozenOrbShard;
    public GameObject SourcePrefab => sourcePrefab;
    public Vector3 FlightVelocity =>
        cachedRigidbody != null ? cachedRigidbody.linearVelocity : Vector3.zero;

    public void SetFlightScaleMultiplier(float multiplier)
    {
        transform.localScale = defaultLocalScale * Mathf.Max(0f, multiplier);
    }

    public GameObject ImpactParticle => impactParticle;
    public GameObject ProjectileParticle => projectileParticle;
    public GameObject MuzzleParticle => muzzleParticle;

    public void SetBaseConfig(
        GameObject impact,
        GameObject projectile,
        GameObject muzzle,
        float radius,
        float offset)
    {
        impactParticle = impact;
        projectileParticle = projectile;
        muzzleParticle = muzzle;
        colliderRadius = Mathf.Max(radius, 0.25f);
        collideOffset = offset;
        EnsureCollider();
    }

    public void BindSourcePrefab(GameObject prefab)
    {
        sourcePrefab = prefab;
    }

    public void Launch(float attackDamage, Vector3 velocity, DamageElement element)
    {
        hasSkillContext = false;
        useBallistic = false;
        useAdHocBallistic = false;
        useVolcanoFallHoming = false;
        adHocVolcanoInterceptActive = false;
        adHocHomingSearchRange = 0f;
        adHocFallHomingSpeedMultiplier = 1f;
        adHocTargetMobility = null;
        adHocSplashRadius = 0f;
        damage = attackDamage;
        damageElement = element;
        BeginFlight(velocity);
    }

    public void LaunchScatterRock(
        float attackDamage,
        Vector3 velocity,
        Vector3 landPoint,
        float splashRadius,
        float visualScale = 0.52f,
        DefenseSkillData strikeSkill = null,
        bool enableFallHoming = false,
        Vector3 homingSearchOrigin = default,
        float homingSearchRange = 0f,
        string targetMobility = null,
        float fallHomingSpeedMultiplier = 1f)
    {
        hasSkillContext = false;
        useBallistic = true;
        useAdHocBallistic = true;
        useVolcanoFallHoming = enableFallHoming;
        adHocVolcanoInterceptActive = false;
        suppressLaunchMuzzleVfx = true;
        adHocVisualScale = Mathf.Clamp(visualScale, 0.35f, 0.85f);
        adHocLandPoint = DefenseBallisticUtility.ProjectToGround(landPoint);
        adHocSplashRadius = Mathf.Max(0.35f, splashRadius);
        adHocHomingSearchOrigin = DefenseBallisticUtility.ProjectToGround(homingSearchOrigin);
        adHocHomingSearchRange = enableFallHoming ? Mathf.Max(0f, homingSearchRange) : 0f;
        adHocFallHomingSpeedMultiplier = enableFallHoming
            ? Mathf.Max(1f, fallHomingSpeedMultiplier)
            : 1f;
        adHocTargetMobility = targetMobility;
        adHocStrikeSkillId = strikeSkill != null ? strikeSkill.skillId : 0;
        adHocStrikeElement = strikeSkill != null ? strikeSkill.element : DefenseSkillElement.Physical;
        damage = attackDamage;
        damageElement = DamageElement.Blue;
        BeginFlight(velocity);
    }

    public void LaunchWithSkill(
        float attackDamage,
        Vector3 velocity,
        DamageElement element,
        DefenseSkillProjectileContext context)
    {
        skillContext = context;
        hasSkillContext = context.skillId > 0 || context.isFrozenOrbShard;
        useAdHocBallistic = false;
        useVolcanoFallHoming = false;
        adHocVolcanoInterceptActive = false;
        adHocHomingSearchRange = 0f;
        adHocFallHomingSpeedMultiplier = 1f;
        adHocTargetMobility = null;
        adHocSplashRadius = 0f;
        useBallistic = hasSkillContext &&
                       (context.moveType == DefenseMoveType.Parabola || context.moveType == DefenseMoveType.Fixed);
        damage = attackDamage;
        damageElement = element;
        BeginFlight(velocity);
    }

    private void BeginFlight(Vector3 velocity)
    {
        isActive = true;
        spawnTime = Time.time;
        lastPosition = transform.position;
        enabled = true;

        ApplyLaunchScale();

        if (IsFrozenOrbShard)
            PrepareFrozenOrbShardFlight();

        ConfigureFrozenOrbVisual();

        CleanupParticles();
        SpawnParticles();
        if (useAdHocBallistic && spawnedProjectileParticle != null)
            DefenseCombatVfxSpawn.RestartParticleHierarchy(spawnedProjectileParticle);

        if (UsesLinkedShellDetonation())
            MuteAudioOnHierarchy(gameObject);

        cachedRigidbody = GetComponent<Rigidbody>();
        if (cachedRigidbody != null)
        {
            // Kinematic이면 linearVelocity로 이동하지 않습니다 (FrozenOrb 등).
            cachedRigidbody.isKinematic = false;
            cachedRigidbody.useGravity = useBallistic;
            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.linearVelocity = velocity;
        }

        if (!IsFrozenOrbShard)
            GetComponent<DefenseFrozenOrbEmitter>()?.OnOrbLaunched(this);
        else
            ScheduleShardExpiry();
    }

    private void PrepareFrozenOrbShardFlight()
    {
        var frozenEmitter = GetComponent<DefenseFrozenOrbEmitter>();
        if (frozenEmitter != null)
            frozenEmitter.enabled = false;

        Transform visual = frozenEmitter != null
            ? frozenEmitter.OrbVisual
            : transform.Find(DefenseFrozenOrbEmitter.OrbVisualName);
        if (visual != null)
            visual.gameObject.SetActive(false);
    }

    private void ScheduleShardExpiry()
    {
        float lifetime = hasSkillContext && skillContext.frozenOrbShardLifetime > 0.05f
            ? skillContext.frozenOrbShardLifetime
            : 0.7f;

        CancelInvoke(nameof(ExpireFrozenOrbShard));
        Invoke(nameof(ExpireFrozenOrbShard), lifetime);
    }

    private void ExpireFrozenOrbShard()
    {
        if (!isActive || !IsFrozenOrbShard)
            return;

        ReturnToPool();
    }

    private void Awake()
    {
        defaultLocalScale = transform.localScale;
        cachedRigidbody = GetComponent<Rigidbody>();
        EnsureCollider();
    }

    private void ApplyLaunchScale()
    {
        if (useAdHocBallistic)
        {
            transform.localScale = defaultLocalScale * adHocVisualScale;
            return;
        }

        if (hasSkillContext && skillContext.isFrozenOrbShard)
        {
            frozenOrbShardVisualScale = Mathf.Clamp(skillContext.visualScaleMultiplier, 0.4f, 0.85f);
            transform.localScale = defaultLocalScale * frozenOrbShardVisualScale;
            return;
        }

        if (adHocVisualScale > 0f && !Mathf.Approximately(adHocVisualScale, 1f))
        {
            transform.localScale = defaultLocalScale * adHocVisualScale;
            return;
        }

        frozenOrbShardVisualScale = 1f;
        transform.localScale = defaultLocalScale;
    }

    public void ApplyVisualScaleMultiplier(float scaleMultiplier)
    {
        adHocVisualScale = Mathf.Clamp(scaleMultiplier, 0.35f, 0.85f);
        ApplyLaunchScale();
    }

    private void ConfigureFrozenOrbVisual()
    {
        if (IsFrozenOrbShard)
            return;

        var emitter = GetComponent<DefenseFrozenOrbEmitter>();
        if (emitter == null)
            return;

        Transform visual = emitter.OrbVisual;
        if (visual == null)
            visual = transform.Find(DefenseFrozenOrbEmitter.OrbVisualName);

        if (visual == null)
            return;
    }

    private void EnsureCollider()
    {
        var sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();

        sphereCollider.isTrigger = true;
        sphereCollider.radius = colliderRadius;
    }

    private void FixedUpdate()
    {
        if (!isActive)
            return;

        if (TryDetonateFromExpDurationFuse())
            return;

        if (Time.time - spawnTime > MaxLifetime)
        {
            FinishAtPosition(transform.position, Vector3.up, null);
            return;
        }

        if (cachedRigidbody == null)
            return;

        ApplyHoming();

        Vector3 currentPosition = transform.position;

        if (TryFinishSkyBurstInFlight(currentPosition))
            return;

        if (TryFinishBallisticAtLandPoint(currentPosition))
            return;

        if (cachedRigidbody.linearVelocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(cachedRigidbody.linearVelocity);

        if (!UsesContactDetonation())
        {
            if (TryFinishAdHocBallisticAtGround(currentPosition))
                return;

            lastPosition = currentPosition;
            return;
        }

        Vector3 displacement = currentPosition - lastPosition;
        float castDistance = displacement.magnitude;

        if (castDistance > 0.0001f)
        {
            if (Physics.SphereCast(
                    lastPosition,
                    colliderRadius,
                    displacement.normalized,
                    out RaycastHit hit,
                    castDistance + colliderRadius,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore)
                && TryResolveHit(hit, out bool isEnemy))
            {
                if (isEnemy)
                {
                    if (!UsesGroundSplashDetonation())
                        HandleEnemyHit(hit.collider, hit.point, hit.normal);
                }
                else
                    FinishAtPosition(ResolveImpactPoint(hit.point), hit.normal, null);

                if (!isActive)
                    return;
            }
        }

        if (cachedRigidbody.linearVelocity.y < 0f
            && currentPosition.y <= GroundY
            && lastPosition.y > GroundY)
        {
            if (IsStormMissileAnchorSkill())
            {
                lastPosition = currentPosition;
                return;
            }

            if (!UsesLinkedShellDetonation() || CanLinkedShellDetonateAtGround(currentPosition))
            {
                Vector3 landPoint = ResolveImpactPoint(currentPosition);
                Transform anchor = UsesLinkedShellDetonation()
                    ? ResolveStormCloudAnchor(landPoint, null)
                    : null;
                FinishAtPosition(landPoint, Vector3.up, anchor);
                return;
            }
        }

        var overlaps = Physics.OverlapSphere(
            currentPosition,
            colliderRadius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        foreach (var overlap in overlaps)
        {
            if (overlap.CompareTag("Enemy") && IsEnemyAttackable(overlap))
            {
                if (!UsesGroundSplashDetonation())
                    HandleEnemyHit(overlap, currentPosition, Vector3.up);

                if (!isActive)
                    return;
            }
            else if (ShouldDetonateOnGround(overlap, currentPosition))
            {
                FinishAtPosition(ResolveImpactPoint(currentPosition), Vector3.up, null);
                return;
            }
        }

        lastPosition = currentPosition;
    }

    private void ApplyHoming()
    {
        if (useVolcanoFallHoming && useBallistic && useAdHocBallistic && !adHocVolcanoInterceptActive)
        {
            TryBeginVolcanoFallIntercept();
            return;
        }

        if (useBallistic)
            return;

        if (!hasSkillContext || skillContext.homingTarget == null)
            return;

        if (!skillContext.homingTarget.gameObject.activeInHierarchy)
            return;

        Vector3 toTarget = skillContext.homingTarget.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f)
            return;

        Vector3 desired = toTarget.normalized * cachedRigidbody.linearVelocity.magnitude;
        cachedRigidbody.linearVelocity = Vector3.Lerp(
            cachedRigidbody.linearVelocity,
            desired,
            HomingTurnSpeed * Time.fixedDeltaTime);
    }

    private void TryBeginVolcanoFallIntercept()
    {
        if (cachedRigidbody == null || cachedRigidbody.linearVelocity.y >= 0f)
            return;

        Transform target = ResolveVolcanoFallHomingTarget();
        if (target == null)
            return;

        Vector3 aimPoint = DefenseCombatTargeting.ResolveEnemyAimPoint(target);
        float speedMultiplier = adHocFallHomingSpeedMultiplier > 1f
            ? adHocFallHomingSpeedMultiplier
            : 1f;
        float speed = Mathf.Max(cachedRigidbody.linearVelocity.magnitude, 5f) * speedMultiplier;

        Vector3 direction = aimPoint - transform.position;
        if (direction.sqrMagnitude < 0.01f)
            return;

        direction.Normalize();

        adHocVolcanoInterceptActive = true;
        useBallistic = false;
        cachedRigidbody.useGravity = false;
        cachedRigidbody.linearVelocity = direction * speed;
    }

    private Transform ResolveVolcanoFallHomingTarget()
    {
        if (adHocHomingSearchRange <= 0f)
            return null;

        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;
        float nearestSqr = adHocHomingSearchRange * adHocHomingSearchRange;

        for (int i = 0; i < enemies.Length; i++)
        {
            var enemy = enemies[i];
            if (!DefenseEnemyQuery.IsLivingEnemy(enemy, targetMobility: adHocTargetMobility))
                continue;

            float sqr = HorizontalDistanceSqr(adHocHomingSearchOrigin, enemy.transform.position);
            if (sqr > nearestSqr)
                continue;

            nearestSqr = sqr;
            nearest = enemy.transform;
        }

        return nearest;
    }

    private static float HorizontalDistanceSqr(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return (b - a).sqrMagnitude;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || !UsesContactDetonation())
            return;

        if (other.CompareTag("Enemy") && IsEnemyAttackable(other))
        {
            if (!UsesGroundSplashDetonation())
                HandleEnemyHit(other, transform.position, Vector3.up);
        }
        else if (ShouldDetonateOnGround(other, transform.position))
            FinishAtPosition(ResolveImpactPoint(transform.position), Vector3.up, null);
    }

    private bool UsesSkyBurstTargeting()
    {
        return hasSkillContext
            && TryGetSkill(out var skill)
            && DefenseSkillCombatTable.UsesSkyBurstTargeting(skill);
    }

    private bool TryFinishSkyBurstInFlight(Vector3 currentPosition)
    {
        if (!UsesSkyBurstTargeting() || !skillContext.hasSkyBurstPoint || cachedRigidbody == null)
            return false;

        Vector3 burstPoint = skillContext.skyBurstPoint;
        bool reachedBurst = Vector3.Distance(currentPosition, burstPoint) <= SkyBurstSnapRadius;
        bool passedBurstAltitude = cachedRigidbody.linearVelocity.y <= 0.05f
            && HorizontalDistance(currentPosition, burstPoint) <= BallisticLandSnapRadius
            && Mathf.Abs(currentPosition.y - burstPoint.y) <= 1.15f;

        if (!reachedBurst && !passedBurstAltitude)
            return false;

        FinishAtPosition(burstPoint, Vector3.down, null);
        return true;
    }

    private bool IsBlizzardAirburstSkill()
    {
        return hasSkillContext
            && TryGetSkill(out var skill)
            && DefenseSkillCombatTable.IsBlizzardAirburstSkill(skill);
    }

    private bool TryFinishBallisticAtLandPoint(Vector3 currentPosition)
    {
        if (UsesGroundSplashDetonation())
            return false;

        if (UsesSkyBurstTargeting())
            return false;

        if (IsStormMissileAnchorSkill())
            return false;

        if (IsBlizzardAirburstSkill())
            return false;

        if (UsesExpDurationFuse())
            return false;

        if (!useBallistic || (!hasSkillContext && !useAdHocBallistic) || !TryResolveBallisticLandPoint(out var land))
            return false;

        if (cachedRigidbody.linearVelocity.y >= 0f)
            return false;

        if (currentPosition.y > GroundY + GetBallisticLandSnapMaxHeight())
            return false;

        float horizontal = HorizontalDistance(currentPosition, land);
        if (horizontal > GetBallisticLandSnapRadius())
            return false;

        FinishAtPosition(land, Vector3.up, null);
        return true;
    }

    private float GetBallisticLandSnapRadius()
    {
        return useAdHocBallistic ? 5f : BallisticLandSnapRadius;
    }

    private float GetBallisticLandSnapMaxHeight()
    {
        return useAdHocBallistic ? 1.35f : BallisticLandSnapMaxHeight;
    }

    private bool TryFinishAdHocBallisticAtGround(Vector3 currentPosition)
    {
        if (!useAdHocBallistic || cachedRigidbody == null)
            return false;

        if (cachedRigidbody.linearVelocity.y >= 0f)
            return false;

        if (currentPosition.y <= GroundY && lastPosition.y > GroundY)
        {
            FinishAtPosition(adHocLandPoint, Vector3.up, null);
            return true;
        }

        if (currentPosition.y <= GroundY + GetBallisticLandSnapMaxHeight()
            && HorizontalDistance(currentPosition, adHocLandPoint) <= GetBallisticLandSnapRadius())
        {
            FinishAtPosition(adHocLandPoint, Vector3.up, null);
            return true;
        }

        return false;
    }

    private bool TryResolveBallisticLandPoint(out Vector3 landPoint)
    {
        if (useAdHocBallistic)
        {
            landPoint = adHocLandPoint;
            return true;
        }

        if (hasSkillContext && skillContext.hasBallisticLandPoint)
        {
            landPoint = skillContext.ballisticLandPoint;
            return true;
        }

        landPoint = default;
        return false;
    }

    private Transform ResolveStormCloudAnchor(Vector3 hitPoint, Transform hitTarget)
    {
        if (hitTarget != null)
            return hitTarget;

        if (!hasSkillContext || !TryGetSkill(out var skill))
            return null;

        if (!DefenseSkillExecutor.HasLinkedSkillSpawn(skill))
            return null;

        return DefenseSkillExecutor.FindNearestEnemy(
            hitPoint,
            2.5f,
            skillContext.targetMobility);
    }

    private Vector3 ResolveImpactPoint(Vector3 fallback)
    {
        return new Vector3(fallback.x, GroundY, fallback.z);
    }

    private bool ShouldDetonateOnGround(Collider collider, Vector3 currentPosition)
    {
        if (!IsGroundCollider(collider))
            return false;

        if (IsStormMissileAnchorSkill())
            return false;

        if (UsesLinkedShellDetonation())
            return CanLinkedShellDetonateAtGround(currentPosition);

        if (!useBallistic || (!hasSkillContext && !useAdHocBallistic) || !TryResolveBallisticLandPoint(out _))
            return true;

        return cachedRigidbody != null && cachedRigidbody.linearVelocity.y < 0f;
    }

    private bool CanLinkedShellDetonateAtGround(Vector3 currentPosition)
    {
        if (!useBallistic || !hasSkillContext || !skillContext.hasBallisticLandPoint)
            return false;

        return cachedRigidbody != null
            && cachedRigidbody.linearVelocity.y < 0f
            && HorizontalDistance(currentPosition, skillContext.ballisticLandPoint) <= BallisticLandSnapRadius;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private bool TryResolveHit(RaycastHit hit, out bool isEnemy)
    {
        isEnemy = false;

        if (hit.collider.CompareTag("Enemy") && IsEnemyAttackable(hit.collider))
        {
            isEnemy = true;
            return true;
        }

        if (IsGroundCollider(hit.collider))
            return ShouldDetonateOnGround(hit.collider, hit.point);

        return false;
    }

    private static bool IsGroundCollider(Collider collider)
    {
        if (collider == null || collider.isTrigger)
            return false;

        return collider.CompareTag("Ground") || collider.gameObject.name == "DefenseGround";
    }

    private bool IsEnemyAttackable(Collider enemyCollider)
    {
        if (hasSkillContext && !string.IsNullOrWhiteSpace(skillContext.targetMobility))
            return DefenseSkillExecutor.IsEnemyAttackable(enemyCollider, skillContext.targetMobility);

        if (useAdHocBallistic && !string.IsNullOrWhiteSpace(adHocTargetMobility))
            return DefenseSkillExecutor.IsEnemyAttackable(enemyCollider, adHocTargetMobility);

        return IsEnemyAttackableLegacy(enemyCollider);
    }

    public static bool IsEnemyAttackable(Collider enemyCollider, string targetMobility)
    {
        return DefenseSkillExecutor.IsEnemyAttackable(enemyCollider, targetMobility);
    }

    private static bool IsEnemyAttackableLegacy(Collider enemyCollider)
    {
        return DefenseEnemyQuery.IsAttackableCollider(enemyCollider, out _, requireLanded: true);
    }

    private void HandleEnemyHit(Collider targetCollider, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (UsesExpDurationFuse())
            return;

        if (useAdHocBallistic)
        {
            FinishScatterRockDetonation(DefenseBallisticUtility.ProjectToGround(hitPoint));
            return;
        }

        transform.position = hitPoint + hitNormal * collideOffset;

        DefenseSkillElement element = DefenseSkillElement.Physical;
        if (hasSkillContext && TryGetSkill(out var skillForElement))
            element = skillForElement.element;
        else
            element = MonsterStatusReactionRules.InferElementFromDamage(damageElement);

        MonsterStatusCombatResolver.ApplyDamageToEnemy(
            targetCollider.gameObject,
            damage,
            element,
            damageElement,
            hitPoint);

        if (!ShouldSuppressEnemyImpactVfx())
            SpawnImpact(hitNormal);
        ApplySkillOnHit(targetCollider.gameObject, hitPoint);

        if (hasSkillContext && TryGetSkill(out var skill))
        {
            if (skill.element == DefenseSkillElement.Lightning
                && DefenseSkillCombatTable.IsArcHomingMissileSkill(skill))
            {
                DefenseLightningStrike.PlayStrikeVfx(transform.position, hitPoint);
            }

            if (!DefenseSkillCombatTable.SkipsMissileSplash(skill))
            {
                DefenseSkillExecutor.ApplySplash(
                    skill,
                    hitPoint,
                    damage,
                    targetCollider.transform,
                    skillContext.targetMobility);
            }
        }

        if (hasSkillContext && skillContext.pierceRemaining > 1)
        {
            skillContext.pierceRemaining--;
            lastPosition = transform.position;
            return;
        }

        FinishAtPosition(hitPoint, hitNormal, ResolveStormCloudAnchor(hitPoint, targetCollider.transform));
    }

    private void FinishAtPosition(Vector3 hitPoint, Vector3 hitNormal, Transform hitTarget)
    {
        if (!isActive)
            return;

        if (TryBeginStormMissileAnchor(hitPoint, hitTarget))
            return;

        if (TryBeginBlizzardAirburst(hitPoint))
            return;

        if (useAdHocBallistic)
        {
            FinishScatterRockDetonation(DefenseBallisticUtility.ProjectToGround(hitPoint));
            return;
        }

        isActive = false;
        transform.position = hitPoint + hitNormal * collideOffset;

        if (!ShouldSuppressGroundFinishImpactVfx())
            SpawnImpact(hitNormal);

        if (hasSkillContext && TryGetSkill(out var skill))
        {
            if (DefenseSkillCombatTable.IsDelayedMeteorBeaconSkill(skill))
            {
                DefenseCombatVfxSpawn.TrySpawnGroundBurst("GasExplosionFire", hitPoint, 1.2f, 0.85f);
                DefenseDelayedMeteorStrike.Schedule(hitPoint, skill, skillContext);
            }
            else if (!DefenseSkillCombatTable.SkipsMissileSplash(skill))
            {
                DefenseSkillExecutor.ApplySplash(
                    skill,
                    hitPoint,
                    damage,
                    null,
                    skillContext.targetMobility);
            }

            if (!DefenseSkillCombatTable.IsArcHomingMissileSkill(skill)
                && !DefenseSkillCombatTable.IsArcRepeaterChainSkill(skill))
            {
                Transform linkedTarget = DefenseSkillCombatTable.UsesAirAnchorLinkedSummon(skill)
                    ? hitTarget ?? ResolveStormCloudAnchor(hitPoint, null)
                    : null;

                DefenseSkillExecutor.TryExecuteLinkedSkill(
                    skill,
                    hitPoint,
                    linkedTarget,
                    skillContext.ToTowerCombatContext(),
                    skillContext.targetMobility,
                    skillContext.linkDepth);
            }
        }

        DetachTrails();
        ReturnToPool();
    }

    private void FinishScatterRockDetonation(Vector3 groundPoint)
    {
        if (!isActive)
            return;

        isActive = false;
        transform.position = groundPoint + Vector3.up * collideOffset;
        ApplyScatterRockSplash(groundPoint);
        DetachTrails();
        ReturnToPool();
    }

    private void ApplyScatterRockSplash(Vector3 hitPoint)
    {
        if (adHocSplashRadius <= 0f)
            return;

        float vfxScale = Mathf.Clamp(adHocSplashRadius * 0.28f, 0.22f, 0.36f);
        DefenseCombatVfxSpawn.TrySpawnGroundBurst("GasExplosionFire", hitPoint, 0.85f, vfxScale);
        DefenseCombatVfxSpawn.TrySpawnGroundScorch(hitPoint, adHocSplashRadius, 0.9f);

        var overlaps = Physics.OverlapSphere(
            hitPoint,
            adHocSplashRadius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlaps.Length; i++)
        {
            var overlap = overlaps[i];
            if (!overlap.CompareTag("Enemy"))
                continue;

            if (!DefenseEnemyQuery.IsAttackableCollider(overlap, out var enemy, requireLanded: true))
                continue;

            MonsterStatusCombatResolver.ApplyAoEDamageToEnemy(
                enemy,
                damage,
                adHocStrikeElement,
                overlap.ClosestPoint(hitPoint));

            if (adHocStrikeSkillId > 0
                && DataManager.Instance != null
                && DataManager.Instance.TryGetSkill(adHocStrikeSkillId, out var strikeSkill))
            {
                DefenseEffectApplicator.ApplySkillEffects(enemy, strikeSkill, hitPoint);
            }
        }
    }

    private void ApplySkillOnHit(GameObject enemyObject, Vector3 effectSource)
    {
        if (!hasSkillContext || !TryGetSkill(out var skill))
            return;

        DefenseEffectApplicator.ApplySkillEffects(enemyObject, skill, effectSource);
    }

    public bool TryGetFlightSkill(out DefenseSkillData skill, out DefenseSkillProjectileContext context)
    {
        skill = null;
        context = default;

        if (!hasSkillContext)
            return false;

        context = skillContext;
        if (DataManager.Instance == null)
            return false;

        return DataManager.Instance.TryGetSkill(skillContext.skillId, out skill);
    }

    private bool TryGetSkill(out DefenseSkillData skill)
    {
        skill = null;
        if (!hasSkillContext || DataManager.Instance == null)
            return false;

        return DataManager.Instance.TryGetSkill(skillContext.skillId, out skill);
    }

    private void SpawnImpact(Vector3 hitNormal)
    {
        var impactPrefab = ResolveImpactPrefab();
        if (impactPrefab == null)
            return;

        if (hasSkillContext && TryGetSkill(out var skill) && DefenseSkillCombatTable.UsesAirAnchorLinkedSummon(skill))
            return;

        var normal = hitNormal.sqrMagnitude > 0.001f ? hitNormal.normalized : Vector3.up;
        bool isGround = Vector3.Dot(normal, Vector3.up) >= 0.9f;
        var position = isGround
            ? DefenseCombatVfxSpawn.SnapToGround(transform.position)
            : transform.position;
        var rotation = DefenseCombatVfxSpawn.ResolveGroundBurstRotation(impactPrefab);

        if (MissilePoolManager.Instance != null)
        {
            MissilePoolManager.Instance.PlayVfxAt(impactPrefab, position, rotation, 3.5f);
        }
        else
        {
            var impact = Instantiate(impactPrefab, position, rotation);
            Destroy(impact, 3.5f);
        }

        if (isGround)
            TrySpawnGroundElementMark(position);
    }

    private GameObject ResolveImpactPrefab()
    {
        if (impactParticle == null)
            return null;

        if (!hasSkillContext || !TryGetSkill(out var skill))
            return impactParticle;

        if (skill.moveType == DefenseMoveType.Parabola
            && skill.element == DefenseSkillElement.Physical
            && skill.splashRadius > 0f
            && DefenseCombatVfxSpawn.TryLoadBurstPrefab("NukeExplosionPink", out var flatBurst)
            && flatBurst != null)
        {
            return flatBurst;
        }

        return impactParticle;
    }

    private void TrySpawnGroundElementMark(Vector3 groundPoint)
    {
        if (!hasSkillContext || !TryGetSkill(out var skill) || skill.splashRadius <= 0f)
            return;

        switch (skill.element)
        {
            case DefenseSkillElement.Physical:
                DefenseCombatVfxSpawn.TrySpawnGroundScorch(groundPoint, skill.splashRadius);
                break;
            case DefenseSkillElement.Fire:
                DefenseCombatVfxSpawn.TrySpawnGroundFireMark(
                    groundPoint,
                    skill.splashRadius,
                    DefenseCombatVfxSpawn.DefaultFireGroundLifetime);
                break;
            case DefenseSkillElement.Ice:
                DefenseCombatVfxSpawn.TrySpawnGroundBurst(
                    "ExplosionNovaBlue",
                    groundPoint,
                    DefenseCombatVfxSpawn.DefaultFireGroundLifetime);
                break;
            case DefenseSkillElement.Poison:
                DefenseCombatVfxSpawn.TrySpawnGroundBurst(
                    "PoisonExplosion",
                    groundPoint,
                    DefenseCombatVfxSpawn.DefaultFireGroundLifetime);
                break;
        }
    }

    private bool ShouldSuppressEnemyImpactVfx()
    {
        if (GetComponent<DefenseFrozenOrbEmitter>() != null && !IsFrozenOrbShard)
            return true;

        return hasSkillContext
            && TryGetSkill(out var skill)
            && DefenseSkillCombatTable.IsArcHomingMissileSkill(skill);
    }

    private bool ShouldSuppressGroundFinishImpactVfx()
    {
        if (!hasSkillContext || !TryGetSkill(out var skill))
            return false;

        return DefenseSkillCombatTable.IsVolcanoEruptionSkill(skill)
            || DefenseSkillCombatTable.IsDelayedMeteorBeaconSkill(skill);
    }

    private void ReturnToPool()
    {
        CancelInvoke(nameof(ExpireFrozenOrbShard));
        isActive = false;
        enabled = false;
        hasSkillContext = false;
        useBallistic = false;
        useAdHocBallistic = false;
        useVolcanoFallHoming = false;
        adHocVolcanoInterceptActive = false;
        adHocHomingSearchRange = 0f;
        adHocFallHomingSpeedMultiplier = 1f;
        adHocTargetMobility = null;
        adHocSplashRadius = 0f;
        adHocVisualScale = 1f;
        adHocStrikeSkillId = 0;
        adHocStrikeElement = DefenseSkillElement.Physical;
        suppressLaunchMuzzleVfx = false;
        transform.localScale = defaultLocalScale;
        frozenOrbShardVisualScale = 1f;

        var frozenEmitter = GetComponent<DefenseFrozenOrbEmitter>();
        if (frozenEmitter != null)
            frozenEmitter.enabled = true;

        CleanupParticles();
        frozenEmitter?.OnOrbEnded();

        if (cachedRigidbody != null)
        {
            cachedRigidbody.useGravity = false;
            cachedRigidbody.linearVelocity = Vector3.zero;
        }

        if (MissilePoolManager.Instance != null && sourcePrefab != null)
            MissilePoolManager.Instance.Release(sourcePrefab, gameObject);
        else
            Destroy(gameObject);
    }

    private void SpawnParticles()
    {
        bool muteStormShellAudio = UsesLinkedShellDetonation();

        if (projectileParticle != null)
        {
            if (MissilePoolManager.Instance != null)
            {
                spawnedProjectileParticle = MissilePoolManager.Instance.PlayVfxAttached(
                    projectileParticle,
                    transform);
            }
            else
            {
                spawnedProjectileParticle = Instantiate(projectileParticle, transform.position, transform.rotation);
                spawnedProjectileParticle.transform.SetParent(transform);
            }

            if (muteStormShellAudio && spawnedProjectileParticle != null)
                MuteAudioOnHierarchy(spawnedProjectileParticle);
        }

        if (muzzleParticle != null && !suppressLaunchMuzzleVfx)
        {
            if (MissilePoolManager.Instance != null)
            {
                MissilePoolManager.Instance.PlayVfxAt(
                    muzzleParticle,
                    transform.position,
                    transform.rotation,
                    1.5f);
            }
            else
            {
                var muzzle = Instantiate(muzzleParticle, transform.position, transform.rotation);
                if (muteStormShellAudio)
                    MuteAudioOnHierarchy(muzzle);
                Destroy(muzzle, 1.5f);
            }
        }
    }

    private bool IsLinkedSpawnShellSkill()
    {
        return hasSkillContext
            && TryGetSkill(out var skill)
            && DefenseSkillExecutor.HasLinkedSkillSpawn(skill);
    }

    private bool IsStormMissileAnchorSkill()
    {
        return hasSkillContext
            && TryGetSkill(out var skill)
            && DefenseSkillCombatTable.IsStormMissileAnchorSkill(skill);
    }

    private bool UsesGroundSplashDetonation()
    {
        if (!useBallistic || !hasSkillContext || !TryGetSkill(out var skill))
            return false;

        return skill.splashRadius > 0.05f;
    }

    private bool UsesLinkedShellDetonation()
    {
        return hasSkillContext
            && TryGetSkill(out var skill)
            && DefenseSkillCombatTable.UsesLinkedShellDetonation(skill);
    }

    private bool TryBeginBlizzardAirburst(Vector3 hitPoint)
    {
        if (!IsBlizzardAirburstSkill() || !TryGetSkill(out var skill))
            return false;

        isActive = false;
        Vector3 burstPoint = skillContext.hasSkyBurstPoint ? skillContext.skyBurstPoint : hitPoint;
        Vector3 groundPoint = DefenseBallisticUtility.ProjectToGround(burstPoint);
        float airBurstHeight = Mathf.Max(0.5f, burstPoint.y - groundPoint.y);
        transform.position = burstPoint;

        DefenseCombatVfxSpawn.TrySpawnAt("ExplosionNovaBlue", burstPoint, Quaternion.identity, 1.1f);

        DefenseSkillExecutor.TryExecuteLinkedSkill(
            skill,
            groundPoint,
            null,
            skillContext.ToTowerCombatContext(),
            skillContext.targetMobility,
            skillContext.linkDepth,
            airBurstHeight);

        DetachTrails();
        ReturnToPool();
        return true;
    }

    private bool TryBeginStormMissileAnchor(Vector3 hitPoint, Transform hitTarget)
    {
        if (!IsStormMissileAnchorSkill() || !TryGetSkill(out var skill))
            return false;

        isActive = false;
        enabled = false;

        Vector3 anchorPosition = skillContext.hasSkyBurstPoint ? skillContext.skyBurstPoint : hitPoint;
        Vector3 groundPoint = DefenseBallisticUtility.ProjectToGround(anchorPosition);
        transform.position = anchorPosition;
        transform.rotation = Quaternion.identity;

        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.useGravity = false;
            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
        }

        DefenseSkillExecutor.ApplySplash(
            skill,
            groundPoint,
            damage,
            hitTarget,
            skillContext.targetMobility);

        var context = LinkedSkillSpawnContext.Create(
            skill,
            groundPoint,
            hitTarget ?? DefenseSkillExecutor.FindNearestEnemy(
                groundPoint,
                2.5f,
                skillContext.targetMobility),
            skillContext.ToTowerCombatContext(),
            skillContext.targetMobility,
            skillContext.linkDepth);

        var anchor = GetComponent<DefenseStormMissileAnchor>();
        if (anchor == null)
            anchor = gameObject.AddComponent<DefenseStormMissileAnchor>();

        anchor.Begin(context, this);
        return true;
    }

    public void ReturnToPoolFromStormAnchor()
    {
        var anchor = GetComponent<DefenseStormMissileAnchor>();
        if (anchor != null)
            Destroy(anchor);

        ReturnToPool();
    }

    private bool UsesExpDurationFuse()
    {
        if (!hasSkillContext || skillContext.expDuration <= 0f)
            return false;

        if (!TryGetSkill(out var skill))
            return true;

        return !DefenseSkillCombatTable.UsesSkyBurstTargeting(skill);
    }

    private bool UsesContactDetonation()
    {
        if (useAdHocBallistic)
            return true;

        return !UsesExpDurationFuse();
    }

    private bool TryDetonateFromExpDurationFuse()
    {
        if (!UsesExpDurationFuse())
            return false;

        if (Time.time - spawnTime < skillContext.expDuration)
            return false;

        Vector3 detonatePoint = transform.position;
        Transform anchor = UsesLinkedShellDetonation()
            ? ResolveStormCloudAnchor(detonatePoint, null)
            : null;
        FinishAtPosition(detonatePoint, Vector3.up, anchor);
        return true;
    }

    private static void MuteAudioOnHierarchy(GameObject root)
    {
        if (root == null)
            return;

        var sources = root.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
            sources[i].mute = true;
    }

    private void CleanupParticles()
    {
        if (spawnedProjectileParticle != null)
        {
            if (MissilePoolManager.Instance != null && projectileParticle != null)
                MissilePoolManager.Instance.ReleaseVfx(projectileParticle, spawnedProjectileParticle);
            else
                Destroy(spawnedProjectileParticle);

            spawnedProjectileParticle = null;
        }

        Transform orbVisualRoot = GetOrbVisualRoot();
        var childParticles = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var particle in childParticles)
        {
            if (particle.gameObject == gameObject)
                continue;

            if (spawnedProjectileParticle != null && particle.gameObject == spawnedProjectileParticle)
                continue;

            if (orbVisualRoot != null && particle.transform.IsChildOf(orbVisualRoot))
                continue;

            Destroy(particle.gameObject);
        }
    }

    private Transform GetOrbVisualRoot()
    {
        var emitter = GetComponent<DefenseFrozenOrbEmitter>();
        if (emitter != null && emitter.OrbVisual != null)
            return emitter.OrbVisual;

        return transform.Find(DefenseFrozenOrbEmitter.OrbVisualName);
    }

    private void DetachTrails()
    {
        var trails = GetComponentsInChildren<ParticleSystem>();
        for (int i = 1; i < trails.Length; i++)
        {
            var trail = trails[i];
            if (!trail.gameObject.name.Contains("Trail"))
                continue;

            trail.transform.SetParent(null);
            Destroy(trail.gameObject, 2f);
        }
    }
}
