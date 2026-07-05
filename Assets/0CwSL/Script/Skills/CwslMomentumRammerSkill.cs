using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 질주자: 이동 중 관성 가속, 고속 충돌 피해, Q 긴급 제동.
/// </summary>
public class CwslMomentumRammerSkill : CwslPlayerSkillBase
{
    private readonly NetworkVariable<float> syncedSpeed = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly Dictionary<ulong, float> nextHitTimeByTarget = new();
    private readonly Dictionary<ulong, float> nextAllyStunTimeByTarget = new();

    private NavMeshAgent agent;
    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerBodyCollider bodyCollider;

    private Vector3 moveDirection = Vector3.forward;
    private Vector3 destination;
    private bool hasDestination;
    private bool momentumActive;
    private float brakeTurnBoostUntil;
    private float nextBrakeTime;

    public float CurrentSpeed => syncedSpeed.Value;
    public bool IsMomentumActive => momentumActive;
    public bool IsStunned => playerStun != null && playerStun.IsStunned;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Instant;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.MomentumRammer;

    public override void OnNetworkSpawn()
    {
        agent = GetComponent<NavMeshAgent>();
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        bodyCollider = GetComponent<CwslPlayerBodyCollider>();
        moveDirection = transform.forward.sqrMagnitude > 0.0001f ? transform.forward : Vector3.forward;
        moveDirection.y = 0f;
        moveDirection.Normalize();

        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged += HandleCharacterChanged;
        if (playerHealth != null)
            playerHealth.OnDied += HandleDied;
    }

    public override void OnNetworkDespawn()
    {
        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged -= HandleCharacterChanged;
        if (playerHealth != null)
            playerHealth.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        StopOnDeathServer();
    }

    private void HandleCharacterChanged(CwslCharacterId characterId)
    {
        if (!IsServer || characterId == CwslCharacterId.MomentumRammer)
            return;

        ResetMomentumServer();
    }

    public void StopOnDeathServer()
    {
        if (!IsServer)
            return;

        ResetMomentumServer(enableAgent: false);
    }

    private void ResetMomentumServer(bool enableAgent = true)
    {
        syncedSpeed.Value = 0f;
        momentumActive = false;
        hasDestination = false;
        playerStun?.ClearStunServer();

        if (enableAgent)
            EnableNavMeshAgent();
        else
            DisableNavMeshAgent();
    }

    private void DisableNavMeshAgent()
    {
        if (agent == null)
            return;

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        agent.enabled = false;
    }

    public override bool CanCastServer(ulong senderClientId)
    {
        return IsServer &&
               playerCharacter != null &&
               playerCharacter.CharacterId == CwslCharacterId.MomentumRammer &&
               (playerHealth == null || playerHealth.IsAlive) &&
               Time.time >= nextBrakeTime;
    }

    public override void OnSkillPressedServer(ulong senderClientId)
    {
        if (!IsServer || playerCharacter == null || playerCharacter.CharacterId != CwslCharacterId.MomentumRammer)
            return;

        if (Time.time < nextBrakeTime)
            return;

        nextBrakeTime = Time.time + CwslGameConstants.RammerBrakeCooldown;
        brakeTurnBoostUntil = Time.time + CwslGameConstants.RammerBrakeTurnBoostDuration;
        playerStun?.ClearStunServer();
        ResetMomentumServer();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        PlayBrakeClientRpc();
    }

    public void SetDestinationServer(Vector3 worldPoint)
    {
        if (!IsServer ||
            playerCharacter == null ||
            playerCharacter.CharacterId != CwslCharacterId.MomentumRammer ||
            (playerHealth != null && !playerHealth.IsAlive) ||
            IsStunned)
            return;

        destination = worldPoint;
        destination.y = transform.position.y;
        hasDestination = true;

        if (!momentumActive)
            BeginMomentumMode();
    }

    public void StopMomentumServer()
    {
        if (!IsServer)
            return;

        hasDestination = false;
    }

    public void TickMovementServer()
    {
        if (!IsServer ||
            playerCharacter == null ||
            playerCharacter.CharacterId != CwslCharacterId.MomentumRammer)
            return;

        if (playerHealth != null && !playerHealth.IsAlive)
        {
            StopOnDeathServer();
            return;
        }

        if (IsStunned)
        {
            syncedSpeed.Value = 0f;
            return;
        }

        if (!momentumActive)
        {
            syncedSpeed.Value = agent != null && agent.enabled ? agent.velocity.magnitude : 0f;
            return;
        }

        TickMomentumServer();
        CheckCollisionsServer();
    }

    private void BeginMomentumMode()
    {
        momentumActive = true;
        if (agent != null)
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            moveDirection = transform.forward;
            moveDirection.y = 0f;
            moveDirection.Normalize();
        }

        if (syncedSpeed.Value < CwslGameConstants.BaseMoveSpeed * 0.5f)
            syncedSpeed.Value = CwslGameConstants.BaseMoveSpeed * 0.5f;
    }

    private void TickMomentumServer()
    {
        var speed = syncedSpeed.Value;
        var flatPos = transform.position;
        flatPos.y = 0f;

        if (hasDestination)
        {
            var toDestination = destination - transform.position;
            toDestination.y = 0f;
            if (toDestination.sqrMagnitude <= 0.3f * 0.3f)
            {
                hasDestination = false;
            }
            else
            {
                var desiredDirection = toDestination.normalized;
                var speedRatio = Mathf.Clamp01(speed / CwslGameConstants.RammerMaxSpeed);
                var turnRate = Mathf.Lerp(420f, 52f, speedRatio * speedRatio);
                if (Time.time < brakeTurnBoostUntil)
                    turnRate *= 2.4f;

                moveDirection = RotateFlat(moveDirection, desiredDirection, turnRate * Time.deltaTime);
                speed = Mathf.Min(
                    CwslGameConstants.RammerMaxSpeed,
                    speed + CwslGameConstants.RammerAccelPerSecond * Time.deltaTime);
            }
        }
        else
        {
            speed = Mathf.Max(0f, speed - CwslGameConstants.RammerDecelPerSecond * Time.deltaTime);
        }

        if (speed <= CwslGameConstants.RammerStopSpeed && !hasDestination)
        {
            syncedSpeed.Value = 0f;
            momentumActive = false;
            EnableNavMeshAgent();
            return;
        }

        syncedSpeed.Value = speed;
        if (speed <= 0.01f)
            return;

        var delta = moveDirection * (speed * Time.deltaTime);
        var beforePosition = transform.position;
        var nextPosition = beforePosition + delta;
        nextPosition.y = beforePosition.y;

        if (TryDetectWallBlock(beforePosition, nextPosition, speed))
            return;

        if (NavMesh.SamplePosition(nextPosition, out var hit, 1.5f, NavMesh.AllAreas))
            nextPosition = new Vector3(nextPosition.x, hit.position.y, nextPosition.z);

        transform.position = nextPosition;

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            var lookRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, turnRateForSpeed(speed) * Time.deltaTime);
        }
    }

    private bool TryDetectWallBlock(Vector3 from, Vector3 to, float speed)
    {
        if (speed < CwslGameConstants.RammerWallStunMinSpeed)
            return false;

        var extent = CwslGameConstants.ArenaHalfExtent - 0.55f;
        if (Mathf.Abs(to.x) > extent || Mathf.Abs(to.z) > extent)
        {
            TriggerWallStunServer(speed);
            return true;
        }

        return false;
    }

    private void TriggerWallStunServer(float impactSpeed)
    {
        if (IsStunned || impactSpeed < CwslGameConstants.RammerWallStunMinSpeed)
            return;

        StopMomentumForStunServer();
        playerStun?.ApplyStunServer(CwslGameConstants.RammerWallStunDuration, transform.position);
    }

    public void StopMomentumForStunServer()
    {
        if (!IsServer)
            return;

        syncedSpeed.Value = 0f;
        hasDestination = false;
        momentumActive = false;
        EnableNavMeshAgent();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private float turnRateForSpeed(float speed)
    {
        var speedRatio = Mathf.Clamp01(speed / CwslGameConstants.RammerMaxSpeed);
        var turnRate = Mathf.Lerp(720f, 120f, speedRatio * speedRatio);
        if (Time.time < brakeTurnBoostUntil)
            turnRate *= 2.2f;
        return turnRate;
    }

    private static Vector3 RotateFlat(Vector3 current, Vector3 target, float maxDegrees)
    {
        current.y = 0f;
        target.y = 0f;
        if (current.sqrMagnitude < 0.0001f)
            return target.sqrMagnitude > 0.0001f ? target.normalized : Vector3.forward;
        if (target.sqrMagnitude < 0.0001f)
            return current.normalized;

        current.Normalize();
        target.Normalize();
        return Vector3.RotateTowards(current, target, maxDegrees * Mathf.Deg2Rad, 0f).normalized;
    }

    private void CheckCollisionsServer()
    {
        if (!momentumActive)
            return;

        var speed = syncedSpeed.Value;
        var selfCenter = transform.position;
        var selfRadius = ResolveCollisionRadius();

        if (speed >= CwslGameConstants.RammerDamageSpeedThreshold)
            CheckMonsterCollisionsServer(selfCenter, selfRadius);

        if (speed >= CwslGameConstants.RammerWallStunMinSpeed)
            CheckAllyCollisionsServer(speed, selfCenter, selfRadius);
    }

    private void CheckMonsterCollisionsServer(Vector3 selfCenter, float selfRadius)
    {
        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (!IsFlatBodyHit(selfCenter, selfRadius, monster.transform.position, GetMonsterFlatRadius(monster)))
                continue;

            TryDamageTargetServer(monster.NetworkObjectId, () =>
                monster.DamageFromPlayer(OwnerClientId, CwslGameConstants.RammerCollisionDamage));
        }
    }

    private void CheckAllyCollisionsServer(float speed, Vector3 selfCenter, float selfRadius)
    {
        if (NetworkManager.Singleton == null)
            return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null || playerObject.NetworkObjectId == NetworkObjectId)
                continue;

            var allyHealth = playerObject.GetComponent<CwslPlayerHealth>();
            if (allyHealth == null || !allyHealth.IsAlive)
                continue;

            var allyBody = playerObject.GetComponent<CwslPlayerBodyCollider>();
            var allyRadius = allyBody != null
                ? allyBody.Radius
                : CwslGameConstants.PlayerBodyColliderRadiusDefault;

            if (!IsFlatBodyHit(selfCenter, selfRadius, playerObject.transform.position, allyRadius))
                continue;

            TryAllyCollisionStunServer(allyHealth, speed);
        }
    }

    private static bool IsFlatBodyHit(Vector3 selfCenter, float selfRadius, Vector3 targetCenter, float targetRadius)
    {
        var flat = targetCenter - selfCenter;
        flat.y = 0f;
        var hitDistance = selfRadius + targetRadius + CwslGameConstants.PlayerBodyHitSlop;
        return flat.sqrMagnitude <= hitDistance * hitDistance;
    }

    private static float GetMonsterFlatRadius(CwslMonsterHealth monster)
    {
        var capsule = monster.GetComponent<CapsuleCollider>();
        return capsule != null
            ? capsule.radius
            : CwslGameConstants.MonsterHitMinRadius;
    }

    private void TryAllyCollisionStunServer(CwslPlayerHealth allyHealth, float speed)
    {
        if (speed < CwslGameConstants.RammerWallStunMinSpeed || allyHealth == null)
            return;

        var allyId = allyHealth.NetworkObjectId;
        if (nextAllyStunTimeByTarget.TryGetValue(allyId, out var nextTime) && Time.time < nextTime)
            return;

        var impactPosition = Vector3.Lerp(transform.position, allyHealth.transform.position, 0.5f) + Vector3.up * 0.4f;

        StopMomentumForStunServer();
        playerStun?.ApplyStunServer(CwslGameConstants.RammerWallStunDuration, impactPosition);

        var allyStun = allyHealth.GetComponent<CwslPlayerStun>();
        allyStun?.ApplyStunServer(CwslGameConstants.RammerWallStunDuration, impactPosition);

        nextAllyStunTimeByTarget[allyId] = Time.time + CwslGameConstants.RammerAllyStunCooldown;
    }

    private float ResolveCollisionRadius()
    {
        var radius = bodyCollider != null
            ? bodyCollider.Radius
            : CwslPlayerBodyCollider.ResolveDefaultRadius(CwslCharacterId.MomentumRammer);
        return radius + 0.12f;
    }

    private void TryDamageTargetServer(ulong targetId, System.Action applyDamage)
    {
        if (!nextHitTimeByTarget.TryGetValue(targetId, out var nextTime) || Time.time >= nextTime)
        {
            applyDamage();
            nextHitTimeByTarget[targetId] = Time.time + CwslGameConstants.RammerCollisionCooldown;
        }
    }

    private void EnableNavMeshAgent()
    {
        if (agent == null)
            return;

        agent.enabled = true;
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.Warp(transform.position);
        }
    }

    [ClientRpc]
    private void PlayBrakeClientRpc()
    {
        var visual = transform.Find("Visual");
        visual?.GetComponent<CwslPlayerRammerBrakeVisual>()?.PlayBrake();
    }
}
