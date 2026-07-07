using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 질주자: 전령식 우클릭 홀드 조향·관성 이동·충돌, Q 홀드 날개 펼치기(날 확대·골드 소모·광역 피해, 아군 포함).
/// </summary>
public class CwslMomentumRammerSkill : CwslPlayerSkillBase
{
    private readonly NetworkVariable<float> syncedSpeed = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> isWingSpreadActive = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> syncedBladeScale = new(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly Dictionary<ulong, float> nextHitTimeByTarget = new();
    private readonly Dictionary<ulong, float> nextAllyStunTimeByTarget = new();
    private readonly Dictionary<ulong, float> nextWingHitTimeByTarget = new();

    private NavMeshAgent agent;
    private CwslPlayerCharacter playerCharacter;
    private CwslPlayerHealth playerHealth;
    private CwslPlayerStun playerStun;
    private CwslPlayerGold playerGold;
    private CwslPlayerPillBuff pillBuff;
    private CwslPlayerBodyCollider bodyCollider;

    private Vector3 moveDirection = Vector3.forward;
    private Vector3 destination;
    private bool hasDestination;
    private bool steerHeld;
    private bool momentumActive;
    private float wingSpreadStartTime;
    private float nextGoldSpendTime;
    private CwslPlayerSkillCooldowns skillCooldowns;

    public float CurrentSpeed => syncedSpeed.Value;
    public bool IsMomentumActive => momentumActive;
    public bool IsStunned => playerStun != null && playerStun.IsStunned;
    public bool IsWingSpreadActive => isWingSpreadActive.Value;
    public float BladeScale => syncedBladeScale.Value;

    public override CwslSkillActivationType ActivationType => CwslSkillActivationType.Charged;

    public override bool IsActiveForCharacter(CwslCharacterId characterId) =>
        characterId == CwslCharacterId.MomentumRammer;

    public override void OnNetworkSpawn()
    {
        agent = GetComponent<NavMeshAgent>();
        playerCharacter = GetComponent<CwslPlayerCharacter>();
        playerHealth = GetComponent<CwslPlayerHealth>();
        playerStun = GetComponent<CwslPlayerStun>();
        playerGold = GetComponent<CwslPlayerGold>();
        pillBuff = GetComponent<CwslPlayerPillBuff>();
        bodyCollider = GetComponent<CwslPlayerBodyCollider>();
        skillCooldowns = GetComponent<CwslPlayerSkillCooldowns>();
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

        StopWingSpreadServer();
        ResetMomentumServer();
    }

    public void StopOnDeathServer()
    {
        if (!IsServer)
            return;

        StopWingSpreadServer();
        ResetMomentumServer(enableAgent: false);
    }

    private void ResetMomentumServer(bool enableAgent = true)
    {
        syncedSpeed.Value = 0f;
        momentumActive = false;
        hasDestination = false;
        steerHeld = false;

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
               !IsStunned &&
               !isWingSpreadActive.Value &&
               (skillCooldowns == null || skillCooldowns.IsReady(0)) &&
               CanAffordSkillGold(CwslGameConstants.RammerWingSpreadStartGoldCost);
    }

    public override void OnSkillPressedServer(ulong senderClientId)
    {
        if (!IsServer ||
            playerCharacter == null ||
            playerCharacter.CharacterId != CwslCharacterId.MomentumRammer ||
            IsStunned ||
            isWingSpreadActive.Value)
            return;

        if (skillCooldowns != null && !skillCooldowns.IsReady(0))
            return;

        if (!TrySpendSkillGold(CwslGameConstants.RammerWingSpreadStartGoldCost, playSpendEffect: false))
        {
            GetComponent<CwslPlayerSkills>()?.NotifyGoldInsufficientServer();
            return;
        }

        isWingSpreadActive.Value = true;
        wingSpreadStartTime = Time.time;
        nextGoldSpendTime = Time.time + CwslGameConstants.RammerWingSpreadGoldIntervalSeconds;
        syncedBladeScale.Value = 1f;
        PlayWingGoldSpendClientRpc(transform.position, CwslGameConstants.RammerWingSpreadStartGoldCost);
    }

    public override void OnSkillReleasedServer(ulong senderClientId)
    {
        if (!IsServer)
            return;

        StopWingSpreadServer();
    }

    public override void TickChargedServer()
    {
        if (!IsServer || !isWingSpreadActive.Value)
            return;

        if (playerHealth != null && !playerHealth.IsAlive)
        {
            StopWingSpreadServer();
            return;
        }

        if (IsStunned)
        {
            StopWingSpreadServer();
            return;
        }

        var elapsed = Time.time - wingSpreadStartTime;
        syncedBladeScale.Value = Mathf.Lerp(
            1f,
            CwslGameConstants.RammerWingSpreadMaxScale,
            Mathf.Clamp01(elapsed / CwslGameConstants.RammerWingSpreadGrowSeconds));

        if (Time.time >= nextGoldSpendTime)
        {
            nextGoldSpendTime = Time.time + CwslGameConstants.RammerWingSpreadGoldIntervalSeconds;
            if (!TrySpendSkillGold(CwslGameConstants.RammerWingSpreadTickGoldCost, playSpendEffect: false))
            {
                StopWingSpreadServer();
                return;
            }

            PlayWingGoldSpendClientRpc(transform.position, CwslGameConstants.RammerWingSpreadTickGoldCost);
        }

        if (syncedBladeScale.Value >= CwslGameConstants.RammerWingSpreadMinScaleForDamage)
            TickWingSpreadDamageServer();
    }

    private void StopWingSpreadServer()
    {
        if (!IsServer || !isWingSpreadActive.Value)
        {
            syncedBladeScale.Value = 1f;
            return;
        }

        isWingSpreadActive.Value = false;
        syncedBladeScale.Value = 1f;
        skillCooldowns?.BeginCooldown(0);
    }

    public void SetDestinationServer(Vector3 worldPoint)
    {
        if (!CanAcceptSteerServer())
            return;

        steerHeld = false;
        ApplyDestinationServer(worldPoint, snapDirection: true);
    }

    public void SetSteerDestinationServer(Vector3 worldPoint)
    {
        if (!CanAcceptSteerServer())
            return;

        steerHeld = true;
        ApplyDestinationServer(worldPoint, snapDirection: false);
    }

    public void ReleaseSteerServer()
    {
        if (!IsServer)
            return;

        steerHeld = false;
    }

    public void StopMomentumServer()
    {
        if (!IsServer)
            return;

        hasDestination = false;
        steerHeld = false;
    }

    private bool CanAcceptSteerServer()
    {
        return IsServer &&
               playerCharacter != null &&
               playerCharacter.CharacterId == CwslCharacterId.MomentumRammer &&
               (playerHealth == null || playerHealth.IsAlive) &&
               !IsStunned;
    }

    private void ApplyDestinationServer(Vector3 worldPoint, bool snapDirection)
    {
        destination = worldPoint;
        destination.y = transform.position.y;
        hasDestination = true;

        if (snapDirection)
            SnapMoveDirectionTowardDestination();

        if (!momentumActive)
            BeginMomentumMode();
    }

    private void SnapMoveDirectionTowardDestination()
    {
        var toDestination = destination - transform.position;
        toDestination.y = 0f;
        if (toDestination.sqrMagnitude < 0.12f)
            return;

        var desiredDirection = toDestination.normalized;
        moveDirection = Vector3.Slerp(
            moveDirection,
            desiredDirection,
            CwslGameConstants.RammerSteerDirectionSnap).normalized;
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
            StopWingSpreadServer();
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

    }

    private void TickMomentumServer()
    {
        var speed = syncedSpeed.Value;

        if (hasDestination)
        {
            var toDestination = destination - transform.position;
            toDestination.y = 0f;
            var arrivalDistance = CwslGameConstants.RammerArrivalDistance;
            if (!steerHeld && toDestination.sqrMagnitude <= arrivalDistance * arrivalDistance)
            {
                hasDestination = false;
            }
            else if (toDestination.sqrMagnitude > 0.04f)
            {
                var desiredDirection = toDestination.normalized;
                var speedRatio = Mathf.Clamp01(speed / CwslGameConstants.RammerMaxSpeed);
                var turnBlend = Mathf.Pow(
                    speedRatio,
                    CwslGameConstants.RammerSteerTurnSpeedExponent);
                var turnRate = Mathf.Lerp(
                    CwslGameConstants.RammerSteerTurnRateHigh,
                    CwslGameConstants.RammerSteerTurnRateLow,
                    turnBlend);
                if (isWingSpreadActive.Value)
                    turnRate *= 0.58f;

                moveDirection = RotateFlat(moveDirection, desiredDirection, turnRate * Time.deltaTime);
                var accel = CwslGameConstants.RammerAccelPerSecond;
                if (isWingSpreadActive.Value)
                    accel *= 0.72f;
                speed = Mathf.Min(CwslGameConstants.RammerMaxSpeed, speed + accel * Time.deltaTime);
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

        var grassSlow = GetComponent<CwslSlowModifier>()?.SpeedMultiplier ?? 1f;
        var delta = moveDirection * (speed * grassSlow * Time.deltaTime);
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
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                lookRotation,
                turnRateForSpeed(speed) * Time.deltaTime);
        }
    }

    private bool TryDetectWallBlock(Vector3 from, Vector3 to, float speed)
    {
        var bodyRadius = ResolveCollisionRadius();

        if (CwslDefensePrepUtility.IsPrepBoundaryActive())
        {
            var maxRadius = CwslDefensePrepUtility.GetPrepInnerRadius(bodyRadius);
            var fromRadius = new Vector2(from.x, from.z).magnitude;
            var toRadius = new Vector2(to.x, to.z).magnitude;
            if (fromRadius <= maxRadius && toRadius > maxRadius)
            {
                var flat = new Vector2(to.x, to.z).normalized * maxRadius;
                transform.position = new Vector3(flat.x, from.y, flat.y);
                syncedSpeed.Value = 0f;
                return true;
            }
        }

        if (speed < CwslGameConstants.RammerWallStunMinSpeed)
            return false;

        var extent = CwslGameConstants.ArenaMapHalfExtent - bodyRadius;

        var fromInside = Mathf.Abs(from.x) <= extent && Mathf.Abs(from.z) <= extent;
        var toOutside = Mathf.Abs(to.x) > extent || Mathf.Abs(to.z) > extent;
        if (!fromInside || !toOutside)
            return false;

        var clamped = to;
        clamped.x = Mathf.Clamp(clamped.x, -extent, extent);
        clamped.z = Mathf.Clamp(clamped.z, -extent, extent);
        clamped.y = from.y;
        transform.position = clamped;

        TriggerWallStunServer(speed);
        return true;
    }

    private void TriggerWallStunServer(float impactSpeed)
    {
        if (IsStunned || impactSpeed < CwslGameConstants.RammerWallStunMinSpeed)
            return;

        StopWingSpreadServer();
        StopMomentumForStunServer();
        playerStun?.ApplyStunServer(CwslGameConstants.RammerWallStunDuration, transform.position);
    }

    public void StopMomentumForStunServer()
    {
        if (!IsServer)
            return;

        StopWingSpreadServer();
        syncedSpeed.Value = 0f;
        hasDestination = false;
        steerHeld = false;
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
        var turnBlend = Mathf.Pow(speedRatio, CwslGameConstants.RammerSteerTurnSpeedExponent);
        return Mathf.Lerp(720f, 165f, turnBlend);
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

    private void TickWingSpreadDamageServer()
    {
        var center = transform.position;
        var radius = ResolveWingSpreadRadius();
        var radiusSqr = radius * radius;

        var monsters = FindObjectsByType<CwslMonsterHealth>(FindObjectsSortMode.None);
        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (!IsInsideFlatRadius(center, monster.transform.position, radiusSqr))
                continue;

            TryWingDamageTargetServer(monster.NetworkObjectId, () =>
                monster.DamageFromPlayer(OwnerClientId, CwslGameConstants.RammerWingSpreadDamage));
        }

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

            if (!IsInsideFlatRadius(center, playerObject.transform.position, radiusSqr))
                continue;

            TryWingDamageTargetServer(playerObject.NetworkObjectId, () =>
                allyHealth.TryReceiveMeleeHitServer(
                    CwslGameConstants.RammerWingSpreadDamage,
                    playerObject.transform.position + Vector3.up * 0.5f));
        }
    }

    private float ResolveWingSpreadRadius()
    {
        return CwslGameConstants.RammerWingSpreadBaseRadius * syncedBladeScale.Value;
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

    private static bool IsInsideFlatRadius(Vector3 center, Vector3 target, float radiusSqr)
    {
        var flat = target - center;
        flat.y = 0f;
        return flat.sqrMagnitude <= radiusSqr;
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

    private void TryWingDamageTargetServer(ulong targetId, System.Action applyDamage)
    {
        if (!nextWingHitTimeByTarget.TryGetValue(targetId, out var nextTime) || Time.time >= nextTime)
        {
            applyDamage();
            nextWingHitTimeByTarget[targetId] = Time.time + CwslGameConstants.RammerWingSpreadHitCooldown;
        }
    }

    [ClientRpc]
    private void PlayWingGoldSpendClientRpc(Vector3 position, int amount)
    {
        CwslGoldFeedback.PlaySpend(position + Vector3.up * 0.9f, amount);
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

    private bool CanAffordSkillGold(int amount)
    {
        if (!CwslGameConstants.SkillsConsumeGold)
            return true;

        if (pillBuff != null && pillBuff.CanAffordSkillGold(playerGold, amount))
            return true;

        return playerGold != null && playerGold.Gold >= amount;
    }

    private bool TrySpendSkillGold(int amount, bool playSpendEffect = true)
    {
        if (!CwslGameConstants.SkillsConsumeGold)
            return true;

        if (pillBuff != null && pillBuff.TrySpendSkillGold(playerGold, amount, playSpendEffect))
            return true;

        return playerGold != null && playerGold.TrySpendGoldServer(amount, playSpendEffect);
    }
}
