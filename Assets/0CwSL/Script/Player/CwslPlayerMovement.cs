using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CwslPlayerMovement : NetworkBehaviour
{
    private NavMeshAgent agent;
    private Vector3 lastSampledPosition;
    private CwslMomentumRammerSkill rammerSkill;
    private CwslCrowdGatherSkill crowdGatherSkill;
    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private float speedMultiplier = 1f;
    private float whirlwindSpeedMultiplier = 1f;

    public float CurrentMoveSpeed { get; private set; }
    public bool IsMoving => CurrentMoveSpeed > 0.12f;

    /// <summary>현재 이동 중이면 평면 이동 방향을 반환합니다.</summary>
    public bool TryGetFlatMoveDirection(out Vector3 direction)
    {
        direction = Vector3.zero;

        if (agent != null && agent.enabled)
        {
            var velocity = agent.velocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > 0.2f)
            {
                direction = velocity.normalized;
                return true;
            }

            if (agent.isOnNavMesh && agent.hasPath && !agent.pathPending)
            {
                var toSteeringTarget = agent.steeringTarget - transform.position;
                toSteeringTarget.y = 0f;
                if (toSteeringTarget.sqrMagnitude > 0.2f)
                {
                    direction = toSteeringTarget.normalized;
                    return true;
                }
            }
        }

        var forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f)
        {
            direction = forward.normalized;
            return true;
        }

        return false;
    }

    public float SpeedMultiplier
    {
        get => speedMultiplier;
        set
        {
            speedMultiplier = Mathf.Max(0.05f, value);
            ApplySpeed();
        }
    }

    public float WhirlwindSpeedMultiplier
    {
        get => whirlwindSpeedMultiplier;
        set
        {
            whirlwindSpeedMultiplier = Mathf.Max(0.05f, value);
            ApplySpeed();
        }
    }

    public override void OnNetworkSpawn()
    {
        agent = GetComponent<NavMeshAgent>();
        rammerSkill = GetComponent<CwslMomentumRammerSkill>();
        crowdGatherSkill = GetComponent<CwslCrowdGatherSkill>();
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        agent.enabled = IsServer;
        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged += HandleCharacterChanged;
        ApplySpeed();
        agent.angularSpeed = 720f;
        agent.acceleration = 48f;
        agent.stoppingDistance = 0.15f;
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.avoidancePriority = 40;
        lastSampledPosition = transform.position;
    }

    public override void OnNetworkDespawn()
    {
        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged -= HandleCharacterChanged;
    }

    private void HandleCharacterChanged(CwslCharacterId characterId)
    {
        ApplySpeed();
    }

    private void Update()
    {
        if (playerHealth != null && !playerHealth.IsAlive)
        {
            CurrentMoveSpeed = 0f;
            return;
        }

        if (playerStun != null && playerStun.IsStunned)
        {
            CurrentMoveSpeed = 0f;
            if (IsServer)
                HoldStunnedMovementServer();
            return;
        }

        if (crowdGatherSkill != null && crowdGatherSkill.IsCharging)
        {
            CurrentMoveSpeed = 0f;
            if (IsServer)
                HoldGatherChargeMovementServer();
            return;
        }

        var tankDash = GetComponent<CwslTankShieldDashSkill>();
        if (tankDash != null && tankDash.IsDashing)
        {
            SampleMoveSpeedFromTransform();
            return;
        }

        var tankSlam = GetComponent<CwslTankShieldSlamSkill>();
        if (tankSlam != null && tankSlam.IsSlamming)
        {
            CurrentMoveSpeed = 0f;
            return;
        }

        var characterId = playerCharacter != null ? playerCharacter.CharacterId : CwslCharacterId.Tank;

        if (rammerSkill != null && rammerSkill.IsActiveForCharacter(characterId))
        {
            if (IsServer)
                rammerSkill.TickMovementServer();
            CurrentMoveSpeed = rammerSkill.CurrentSpeed;
            return;
        }

        if (!IsServer || agent == null)
        {
            SampleMoveSpeedFromTransform();
            return;
        }

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            CurrentMoveSpeed = 0f;
            lastSampledPosition = transform.position;
            return;
        }

        if (agent.isStopped)
            agent.isStopped = false;

        CurrentMoveSpeed = agent.velocity.magnitude;
        lastSampledPosition = transform.position;
        ClampPositionToMapServer();
    }

    private void ClampPositionToMapServer()
    {
        if (CwslDefensePrepUtility.IsPrepBoundaryActive())
            return;

        var bodyRadius = GetComponent<CwslPlayerBodyCollider>()?.Radius
            ?? CwslGameConstants.PlayerBodyColliderRadiusDefault;
        var clamped = CwslArenaUtility.ClampToPlayArea(transform.position, bodyRadius);
        if ((clamped - transform.position).sqrMagnitude < 0.0001f)
            return;

        transform.position = clamped;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.Warp(clamped);
    }

    private void SampleMoveSpeedFromTransform()
    {
        var delta = transform.position - lastSampledPosition;
        delta.y = 0f;
        CurrentMoveSpeed = Time.deltaTime > 0.0001f ? delta.magnitude / Time.deltaTime : 0f;
        lastSampledPosition = transform.position;
    }

    public void RequestMoveTo(Vector3 worldPoint)
    {
        if (!IsServer || agent == null)
            return;

        worldPoint = CwslPlayerBossDebuff.ApplyReverseControlIfNeeded(
            worldPoint,
            transform,
            GetComponent<CwslPlayerBossDebuff>());

        if (CwslDefensePrepUtility.IsPrepBoundaryActive())
        {
            var bodyRadius = GetComponent<CwslPlayerBodyCollider>()?.Radius
                ?? CwslGameConstants.PlayerBodyColliderRadiusDefault;
            worldPoint = CwslDefensePrepUtility.ClampToPrepArea(worldPoint, bodyRadius);
        }
        else
        {
            var mapBodyRadius = GetComponent<CwslPlayerBodyCollider>()?.Radius
                ?? CwslGameConstants.PlayerBodyColliderRadiusDefault;
            worldPoint = CwslArenaUtility.ClampToPlayArea(worldPoint, mapBodyRadius);
        }

        GetComponent<CwslBlackHoleEscape>()?.TryRegisterMoveAwayClickServer(worldPoint);

        if (playerStun != null && playerStun.IsStunned)
            return;

        if (crowdGatherSkill != null && crowdGatherSkill.IsCharging)
            return;

        if (GetCharacterId() == CwslCharacterId.Tank)
        {
            if (GetComponent<CwslTankShieldSlamSkill>()?.IsSlamming == true)
                return;
            if (GetComponent<CwslTankShieldDashSkill>()?.IsDashing == true)
                return;
        }

        if (rammerSkill != null && rammerSkill.IsActiveForCharacter(GetCharacterId()))
        {
            if (!NavMesh.SamplePosition(worldPoint, out var hit, 6f, NavMesh.AllAreas))
                return;

            rammerSkill.SetDestinationServer(hit.position);
            return;
        }

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var selfHit, 4f, NavMesh.AllAreas))
                agent.Warp(selfHit.position);
            else
                return;
        }

        if (!NavMesh.SamplePosition(worldPoint, out var destinationHit, 6f, NavMesh.AllAreas))
            return;

        agent.enabled = true;
        agent.isStopped = false;
        agent.SetDestination(destinationHit.position);
    }

    public void RequestRammerSteerTo(Vector3 worldPoint)
    {
        if (!IsServer || rammerSkill == null || !rammerSkill.IsActiveForCharacter(GetCharacterId()))
            return;

        worldPoint = CwslPlayerBossDebuff.ApplyReverseControlIfNeeded(
            worldPoint,
            transform,
            GetComponent<CwslPlayerBossDebuff>());

        GetComponent<CwslBlackHoleEscape>()?.TryRegisterMoveAwayClickServer(worldPoint);

        if (playerStun != null && playerStun.IsStunned)
            return;

        if (crowdGatherSkill != null && crowdGatherSkill.IsCharging)
            return;

        if (!NavMesh.SamplePosition(worldPoint, out var hit, 6f, NavMesh.AllAreas))
            return;

        rammerSkill.SetSteerDestinationServer(hit.position);
    }

    public void ReleaseRammerSteer()
    {
        if (!IsServer || rammerSkill == null)
            return;

        rammerSkill.ReleaseSteerServer();
    }

    public void StopMovement()
    {
        if (!IsServer || agent == null)
            return;

        if (rammerSkill != null && rammerSkill.IsActiveForCharacter(GetCharacterId()))
        {
            rammerSkill.StopMomentumServer();
            return;
        }

        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    public void SetAgentEnabled(bool enabled)
    {
        if (!IsServer || agent == null)
            return;

        if (!enabled && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        agent.enabled = enabled;
    }

    private void ApplySpeed()
    {
        if (agent == null)
            return;

        var characterId = GetCharacterId();
        var baseSpeed = characterId == CwslCharacterId.MomentumRammer
            ? CwslGameConstants.BaseMoveSpeed
            : CwslCharacterStatCatalog.GetMoveSpeed(characterId);

        agent.speed = baseSpeed * speedMultiplier
                      * whirlwindSpeedMultiplier
                      * (GetComponent<CwslSlowModifier>()?.SpeedMultiplier ?? 1f)
                      * (GetComponent<CwslMoveSpeedBuff>()?.SpeedMultiplier ?? 1f);
    }

    private CwslCharacterId GetCharacterId()
    {
        return playerCharacter != null ? playerCharacter.CharacterId : CwslCharacterId.Tank;
    }

    private void HoldStunnedMovementServer()
    {
        if (rammerSkill != null && rammerSkill.IsActiveForCharacter(GetCharacterId()))
            return;

        HoldAgentStoppedServer();
    }

    private void HoldGatherChargeMovementServer()
    {
        HoldAgentStoppedServer();
    }

    private void HoldAgentStoppedServer()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }
}
