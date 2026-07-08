using Unity.Netcode;
using UnityEngine;

public abstract class CwslMonsterBase : NetworkBehaviour
{
    [SerializeField] protected float moveSpeed = CwslMonsterStatCatalog.MeleeMoveSpeed;

    protected CwslMonsterHealth health;
    protected NetworkObject currentTarget;
    protected CwslBarricadeWall currentWallTarget;
    protected float targetRefreshTimer;
    protected CwslMonsterTargetingMode targetingMode = CwslMonsterTargetingMode.Nearest;
    protected float localDamageMultiplier = 1f;
    protected float localSpeedMultiplier = 1f;
    protected float localScaleMultiplier = 1f;

    public CwslMonsterType MonsterType { get; protected set; }
    public CwslMonsterTargetingMode TargetingMode => targetingMode;
    public float LastWalkSpeed { get; private set; }

    public virtual void Initialize(CwslMonsterType type)
    {
        MonsterType = type;
        targetingMode = CwslMonsterTypeUtil.GetDefaultTargeting(type);
        health = GetComponent<CwslMonsterHealth>();
        var healthMultiplier = ApplyDefenseProfile(type);
        var goldDrop = CwslMonsterTypeUtil.IsElite(type) || CwslMonsterTypeUtil.IsNexusPriority(type) ? 0 : -1;
        health?.Configure(type, goldDrop: goldDrop, healthMultiplier: healthMultiplier);
        moveSpeed = ResolveMoveSpeed(type);
        CwslMonsterVisualRefresh.Refresh(transform, type);
        EnsureMeleeLungeVisual();
        EnsureLegWalkVisual();
        StripMonsterThreatLights();
        ApplyScaleMultiplier();
        health?.RefreshCombatHitCollider();
        health?.SyncHealthAfterConfigureServer();
    }

    private float ApplyDefenseProfile(CwslMonsterType type)
    {
        var manager = CwslMonsterManager.Instance;
        if (manager == null || !CwslGameConstants.UseDefenseMode)
            return 1f;

        if (CwslMonsterTypeUtil.IsNexusPriority(type))
        {
            localDamageMultiplier = 1f;
            localScaleMultiplier = manager.NexusVariantScaleMultiplier;
            return manager.NexusVariantHealthMultiplier;
        }

        switch (type)
        {
            case CwslMonsterType.MidBoss:
                localScaleMultiplier = manager.MidBossScaleMultiplier;
                GetComponent<CwslDefenseMidBoss>()?.ConfigureBuff((CwslMidBossBuffKind)Random.Range(0, 3));
                return manager.MidBossHealthMultiplier;
            case CwslMonsterType.DefenseBoss:
                localScaleMultiplier = manager.DefenseBossScaleMultiplier;
                return manager.DefenseBossHealthMultiplier;
            case CwslMonsterType.SeniorCoach:
                localScaleMultiplier = manager.SeniorCoachScaleMultiplier;
                return manager.SeniorCoachHealthMultiplier;
            default:
                return 1f;
        }
    }

    private float ResolveMoveSpeed(CwslMonsterType type)
    {
        var manager = CwslMonsterManager.Instance;
        var nexusMult = manager != null ? manager.NexusVariantSpeedMultiplier : 0.72f;
        var midMult = manager != null ? manager.MidBossSpeedMultiplier : 0.667f;
        return CwslMonsterStatCatalog.GetMoveSpeed(type, nexusMult, midMult);
    }

    private void ApplyScaleMultiplier()
    {
        if (Mathf.Approximately(localScaleMultiplier, 1f))
            return;

        transform.localScale = Vector3.one * localScaleMultiplier;
    }

    /// <summary>게스트 클라이언트 — 타입·비주얼만 동기화 (서버 로직 없음).</summary>
    public void EnsureClientVisuals(CwslMonsterType type)
    {
        MonsterType = type;
        targetingMode = CwslMonsterTypeUtil.GetDefaultTargeting(type);
        EnsureMeleeLungeVisual();
        EnsureLegWalkVisual();
        StripMonsterThreatLights();
        ApplyScaleMultiplier();
    }

    private void EnsureLegWalkVisual()
    {
        var visual = transform.Find("Visual");
        if (visual == null || visual.Find("LegL") == null || visual.Find("LegR") == null)
            return;

        if (visual.GetComponent<CwslMonsterLegWalkVisual>() == null)
            visual.gameObject.AddComponent<CwslMonsterLegWalkVisual>();
    }

    private void StripMonsterThreatLights()
    {
        CwslThreatLight.RemoveFromHierarchy(transform);
    }

    private void EnsureMeleeLungeVisual()
    {
        if (MonsterType != CwslMonsterType.Melee &&
            MonsterType != CwslMonsterType.NexusMelee &&
            MonsterType != CwslMonsterType.MidBoss &&
            MonsterType != CwslMonsterType.KoreaUniversitySoldier)
            return;

        var visual = transform.Find("Visual");
        if (visual == null)
            return;

        if (visual.GetComponent<CwslSlimeMeleeVisual>() != null)
        {
            var legacyLunge = visual.GetComponent<CwslMeleeLungeVisual>();
            if (legacyLunge != null)
            {
                if (Application.isPlaying)
                    Destroy(legacyLunge);
                else
                    DestroyImmediate(legacyLunge);
            }

            return;
        }

        if (visual.GetComponent<CwslMeleeLungeVisual>() == null)
            visual.gameObject.AddComponent<CwslMeleeLungeVisual>();
    }

    protected virtual void Update()
    {
        LastWalkSpeed = 0f;

        if (!IsServer || !IsSpawned)
            return;

        if (health == null || !health.IsAlive)
            return;

        if (GetComponent<CwslMonsterKnockback>()?.IsKnockedBack == true)
            return;

        var status = GetComponent<CwslMonsterStatusController>();
        if (GetComponent<CwslMonsterStun>()?.IsStunned == true
            || (status != null && status.BlocksAction))
            return;

        targetRefreshTimer -= Time.deltaTime;
        if (targetRefreshTimer <= 0f || (!IsValidTarget(currentTarget) && !IsValidWallTarget(currentWallTarget)))
        {
            targetRefreshTimer = 0.35f;
            RefreshTarget();
        }

        // 이동 중 벽 충돌이면 벽 우선 타겟
        TryAcquireBlockingWallAlongPath();

        if (!IsValidTarget(currentTarget) && !IsValidWallTarget(currentWallTarget))
            return;

        TickServerAI();
    }

    protected abstract void TickServerAI();

    protected void RefreshTarget()
    {
        currentWallTarget = null;

        var forcedComponent = GetComponent<CwslMonsterForcedTarget>();
        if (forcedComponent != null && forcedComponent.TryGetTarget(out var localForced))
        {
            currentTarget = localForced;
            return;
        }

        if (CwslMonsterGlobalAggro.TryGetForcedPlayerTarget(out var forcedTarget))
        {
            currentTarget = forcedTarget;
            return;
        }

        if (targetingMode == CwslMonsterTargetingMode.NexusFirst)
        {
            if (TryGetNexusTarget(out var nexusTarget))
            {
                currentTarget = nexusTarget;
                return;
            }
        }

        if (CwslTargetQuery.TryGetNearestCombatTarget(transform.position, targetingMode, out var target, out _))
            currentTarget = target;
        else
            currentTarget = null;

        TryAcquireBlockingWallAlongPath();
    }

    protected void TryAcquireBlockingWallAlongPath()
    {
        if (!IsValidTarget(currentTarget))
            return;

        var desired = GetPrimaryMovePosition();
        if (!CwslBarricadeWallRegistry.TryGetNearestBlockingWall(
                transform.position,
                desired,
                out var wall,
                out _))
            return;

        currentWallTarget = wall;
    }

    protected static bool IsValidWallTarget(CwslBarricadeWall wall) =>
        wall != null && wall.IsAlive;

    protected static bool TryGetNexusTarget(out NetworkObject nexusObject)
    {
        nexusObject = null;
        var nexus = CwslNexus.Instance;
        if (nexus == null || !nexus.IsAlive)
            return false;

        nexusObject = nexus.GetComponent<NetworkObject>();
        return nexusObject != null && nexusObject.IsSpawned;
    }

    protected static bool IsValidTarget(NetworkObject target)
    {
        if (target == null || !target.IsSpawned)
            return false;

        var nexus = target.GetComponent<CwslNexus>();
        if (nexus != null)
            return nexus.IsAlive;

        var playerHealth = target.GetComponent<CwslPlayerHealth>();
        return playerHealth == null || playerHealth.IsAlive;
    }

    protected float GetScaledDamage(float baseDamage)
    {
        var managerMult = CwslMonsterManager.Instance != null
            ? CwslMonsterManager.Instance.GetScaledDamage(1f)
            : 1f;
        var runtime = GetComponent<CwslMonsterRuntimeStats>();
        var runtimeMult = runtime != null ? runtime.DamageMultiplier : 1f;
        return baseDamage * managerMult * localDamageMultiplier * runtimeMult;
    }

    protected Vector3 GetPrimaryMovePosition()
    {
        if (currentTarget == null)
            return transform.position;

        var nexus = currentTarget.GetComponent<CwslNexus>();
        if (nexus != null)
            return nexus.GetMeleeApproachPoint(transform.position, GetMovementClampRadius());

        return currentTarget.transform.position;
    }

    protected Vector3 GetTargetMovePosition()
    {
        if (IsValidWallTarget(currentWallTarget))
            return currentWallTarget.GetMeleeApproachPoint(transform.position, GetMovementClampRadius());

        return GetPrimaryMovePosition();
    }

    protected Vector3 GetTargetFacePosition()
    {
        if (IsValidWallTarget(currentWallTarget))
            return currentWallTarget.GetAimPoint();

        if (currentTarget == null)
            return transform.position + transform.forward;

        var nexus = currentTarget.GetComponent<CwslNexus>();
        if (nexus != null)
            return nexus.GetAimPoint();

        var enemyBase = currentTarget.GetComponent<CwslEnemyBase>();
        if (enemyBase != null)
            return enemyBase.GetAimPoint();

        return currentTarget.transform.position + Vector3.up * CwslGameConstants.MonsterHitCenterY;
    }

    protected float GetCombatStandDistance()
    {
        if (IsValidWallTarget(currentWallTarget))
            return 1.15f;

        return currentTarget != null && currentTarget.GetComponent<CwslNexus>() != null ? 1.2f : 1.05f;
    }

    protected float GetFlatDistanceToCombatPosition()
    {
        var flat = GetTargetMovePosition() - transform.position;
        flat.y = 0f;
        return flat.magnitude;
    }

    protected float GetMovementClampRadius()
    {
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
            return capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

        return CwslGameConstants.MonsterHitMinRadius;
    }

    protected void MoveToward(Vector3 worldPosition, float speedMultiplier = 1f)
    {
        var flat = worldPosition - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0004f)
            return;

        var runtime = GetComponent<CwslMonsterRuntimeStats>();
        var runtimeSpeed = runtime != null ? runtime.SpeedMultiplier : 1f;
        var statusSlow = GetComponent<CwslMonsterStatusController>()?.GetMoveSpeedMultiplier() ?? 1f;
        LastWalkSpeed = moveSpeed * speedMultiplier * localSpeedMultiplier * runtimeSpeed
            * CwslMonsterStatCatalog.GlobalMoveSpeedMultiplier
            * CwslArenaZones.GetMonsterSpeedMultiplier(transform.position)
            * (GetComponent<CwslSlowModifier>()?.SpeedMultiplier ?? 1f)
            * statusSlow;

        var step = flat.normalized * (LastWalkSpeed * Time.deltaTime);
        var next = transform.position + step;

        if (CwslBarricadeWallRegistry.TryGetNearestBlockingWall(
                transform.position,
                next,
                out var wall,
                out var hitPoint))
        {
            currentWallTarget = wall;
            next = hitPoint;
        }

        transform.position = CwslArenaUtility.ClampToPlayArea(next, GetMovementClampRadius());
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(flat.normalized),
            Time.deltaTime * 10f);
    }

    protected float GetFlatDistanceTo(NetworkObject target)
    {
        if (target == null)
            return float.MaxValue;

        var flat = target.transform.position - transform.position;
        flat.y = 0f;
        return flat.magnitude;
    }

    protected bool TryDamageCurrentTargetMelee(float baseDamage, float maxDistance, Vector3 hitPointOffset)
    {
        var damage = GetScaledDamage(baseDamage);

        if (IsValidWallTarget(currentWallTarget))
        {
            if (GetFlatDistanceToCombatPosition() > maxDistance)
                return false;

            currentWallTarget.DamageServer(damage);
            return true;
        }

        if (!IsValidTarget(currentTarget))
            return false;

        var distance = currentTarget.GetComponent<CwslNexus>() != null
            ? GetFlatDistanceToCombatPosition()
            : GetFlatDistanceTo(currentTarget);
        if (distance > maxDistance)
            return false;

        var nexus = currentTarget.GetComponent<CwslNexus>();
        if (nexus != null)
        {
            nexus.DamageServer(damage);
            return true;
        }

        var playerHealth = currentTarget.GetComponent<CwslPlayerHealth>();
        if (playerHealth != null)
            playerHealth.TryReceiveMeleeHitServer(damage, currentTarget.transform.position + hitPointOffset);

        return true;
    }
}
