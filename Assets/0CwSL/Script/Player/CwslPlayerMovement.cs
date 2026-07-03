using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CwslPlayerMovement : NetworkBehaviour
{
    private NavMeshAgent agent;
    private float speedMultiplier = 1f;

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
        agent.enabled = IsServer;
        agent.speed = CwslGameConstants.BaseMoveSpeed;
        agent.angularSpeed = 720f;
        agent.acceleration = 48f;
        agent.stoppingDistance = 0.15f;
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.avoidancePriority = 40;
    }

    private void Update()
    {
        if (!IsServer || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (agent.isStopped)
            agent.isStopped = false;
    }

    public void RequestMoveTo(Vector3 worldPoint)
    {
        if (!IsServer || agent == null)
            return;

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var selfHit, 4f, NavMesh.AllAreas))
                agent.Warp(selfHit.position);
            else
                return;
        }

        if (!NavMesh.SamplePosition(worldPoint, out var hit, 6f, NavMesh.AllAreas))
            return;

        agent.isStopped = false;
        agent.SetDestination(hit.position);
    }

    public void StopMovement()
    {
        if (!IsServer || agent == null || !agent.enabled || !agent.isOnNavMesh)
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
}
