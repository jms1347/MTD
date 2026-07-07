using Unity.Netcode;
using UnityEngine;

public abstract class CwslMonsterBase : NetworkBehaviour
{
    [SerializeField] protected float moveSpeed = CwslMonsterStatCatalog.MeleeMoveSpeed;

    protected CwslMonsterHealth health;
    protected NetworkObject currentTarget;
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
        EnsureThreatLight();
        ApplyScaleMultiplier();
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

    /// <summary>게스트 클라이언트 — 타입·위협 라이트 등 비주얼만 동기화 (서버 로직 없음).</summary>
    public void EnsureClientVisuals(CwslMonsterType type)
    {
        MonsterType = type;
        targetingMode = CwslMonsterTypeUtil.GetDefaultTargeting(type);
        EnsureMeleeLungeVisual();
        EnsureLegWalkVisual();
        EnsureThreatLight();
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

    private void EnsureThreatLight()
    {
        var lightColor = CwslMonsterVisualPalette.GetThreatLightColor(MonsterType);
        var isSuicide = MonsterType is CwslMonsterType.Suicide
            or CwslMonsterType.NexusSuicide
            or CwslMonsterType.StickySuicide;
        var isRanged = MonsterType is CwslMonsterType.Ranged or CwslMonsterType.NexusRanged;
        var isNexus = CwslMonsterTypeUtil.IsNexusPriority(MonsterType);

        if (isSuicide || isRanged || isNexus)
        {
            var range = isSuicide ? 5.5f : isNexus ? 4.2f : 3.2f;
            var intensity = isSuicide ? 3.2f : isNexus ? 2.4f : 1.4f;
            var offsetY = isSuicide ? 0.8f : 1.0f;
            CwslThreatLight.Ensure(transform, lightColor, range, intensity, new Vector3(0f, offsetY, 0f));
        }
    }

    private void EnsureMeleeLungeVisual()
    {
        if (MonsterType != CwslMonsterType.Melee &&
            MonsterType != CwslMonsterType.NexusMelee &&
            MonsterType != CwslMonsterType.MidBoss &&
            MonsterType != CwslMonsterType.KoreaUniversitySoldier)
            return;

        var visual = transform.Find("Visual");
        if (visual != null && visual.GetComponent<CwslMeleeLungeVisual>() == null)
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

        if (GetComponent<CwslMonsterStun>()?.IsStunned == true)
            return;

        targetRefreshTimer -= Time.deltaTime;
        if (targetRefreshTimer <= 0f || !IsValidTarget(currentTarget))
        {
            targetRefreshTimer = 0.35f;
            RefreshTarget();
        }

        if (!IsValidTarget(currentTarget))
            return;

        TickServerAI();
    }

    protected abstract void TickServerAI();

    protected void RefreshTarget()
    {
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
    }

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

    protected Vector3 GetTargetMovePosition()
    {
        if (currentTarget == null)
            return transform.position;

        var nexus = currentTarget.GetComponent<CwslNexus>();
        if (nexus != null)
            return nexus.GetMeleeApproachPoint(transform.position, GetMovementClampRadius());

        return currentTarget.transform.position;
    }

    protected Vector3 GetTargetFacePosition()
    {
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
        LastWalkSpeed = moveSpeed * speedMultiplier * localSpeedMultiplier * runtimeSpeed
            * CwslMonsterStatCatalog.GlobalMoveSpeedMultiplier
            * CwslArenaZones.GetMonsterSpeedMultiplier(transform.position)
            * (GetComponent<CwslSlowModifier>()?.SpeedMultiplier ?? 1f);

        var step = flat.normalized * (LastWalkSpeed * Time.deltaTime);
        var next = transform.position + step;
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
        if (!IsValidTarget(currentTarget))
            return false;

        var distance = currentTarget.GetComponent<CwslNexus>() != null
            ? GetFlatDistanceToCombatPosition()
            : GetFlatDistanceTo(currentTarget);
        if (distance > maxDistance)
            return false;

        var damage = GetScaledDamage(baseDamage);
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
