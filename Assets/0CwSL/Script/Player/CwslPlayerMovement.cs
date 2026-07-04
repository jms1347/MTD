using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CwslPlayerMovement : NetworkBehaviour
{
    private NavMeshAgent agent;
    private Vector3 lastSampledPosition;
    private CwslMomentumRammerSkill rammerSkill;
    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerHealth playerHealth;
    private float speedMultiplier = 1f;

    public float CurrentMoveSpeed { get; private set; }
    public bool IsMoving => CurrentMoveSpeed > 0.12f;

    public float SpeedMultiplier
    {
        get => speedMultiplier;
        set
        {
            speedMultiplier = Mathf.Max(0.05f, value);
            ApplySpeed();
        }
    }

    public override void OnNetworkSpawn()
    {
        agent = GetComponent<NavMeshAgent>();
        rammerSkill = GetComponent<CwslMomentumRammerSkill>();
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        playerHealth = GetComponent<CwslPlayerHealth>();
        agent.enabled = IsServer;
        agent.speed = CwslGameConstants.BaseMoveSpeed;
        agent.angularSpeed = 720f;
        agent.acceleration = 48f;
        agent.stoppingDistance = 0.15f;
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.avoidancePriority = 40;
        lastSampledPosition = transform.position;
    }

    private void Update()
    {
        if (playerHealth != null && !playerHealth.IsAlive)
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
        if (agent != null)
            agent.speed = CwslGameConstants.BaseMoveSpeed * speedMultiplier;
    }

    private CwslCharacterId GetCharacterId()
    {
        return playerCharacter != null ? playerCharacter.CharacterId : CwslCharacterId.Tank;
    }
}
