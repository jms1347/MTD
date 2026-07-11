using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 관성 주행: heading/velocity 분리 + 벡터 블렌딩 드리프트.
/// Owner는 로컬 즉시 시뮬레이션, 서버는 권위 시뮬레이션.
/// </summary>
[DefaultExecutionOrder(10)]
public class StllHorseMotor : NetworkBehaviour
{
    private readonly NetworkVariable<float> syncedSpeed = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Vector3 headingDirection = Vector3.forward;
    private Vector3 velocityDirection = Vector3.forward;
    private Vector3 desiredInputDirection = Vector3.forward;
    private bool hasSteerInput;
    private bool isPivotTurning;
    private bool momentumActive;
    private bool isCharging;
    private float chargeTimer;
    private StllMountedCharge mountedCharge;

    public float CurrentSpeed => IsOwner || IsServer ? LocalSpeed : syncedSpeed.Value;
    public float LocalSpeed { get; private set; }
    public Vector3 MoveDirection => velocityDirection;
    public Vector3 HeadingDirection => headingDirection;
    public bool IsCharging => isCharging;
    public bool IsMomentumActive => momentumActive;

    private void Awake()
    {
        mountedCharge = GetComponent<StllMountedCharge>();
        headingDirection = FlatForward();
        velocityDirection = headingDirection;
    }

    public override void OnNetworkSpawn()
    {
        LocalSpeed = syncedSpeed.Value;
    }

    public void SetSteerInput(Vector3 direction, bool hold)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            ReleaseSteerInput();
            return;
        }

        desiredInputDirection = direction.normalized;
        hasSteerInput = true;

        if (!momentumActive)
            BeginMomentumMode();
    }

    public void ReleaseSteerInput()
    {
        hasSteerInput = false;
        isPivotTurning = false;
    }

    public void SetSteerDirectionServer(Vector3 direction, bool hold)
    {
        if (!IsServer || isCharging)
            return;

        SetSteerInput(direction, hold);
    }

    public void ReleaseSteerServer()
    {
        if (!IsServer)
            return;

        ReleaseSteerInput();
    }

    [ServerRpc]
    public void SubmitSteerDirectionServerRpc(Vector3 direction, bool hold)
    {
        SetSteerDirectionServer(direction, hold);
    }

    [ServerRpc]
    public void SubmitReleaseSteerServerRpc()
    {
        ReleaseSteerServer();
    }

    public void BeginChargeServer()
    {
        if (!IsServer || isCharging)
            return;

        isCharging = true;
        chargeTimer = StllGlaiveConstants.ChargeDuration;
        if (velocityDirection.sqrMagnitude < 0.001f)
            velocityDirection = FlatForward();
        headingDirection = velocityDirection;
        mountedCharge?.OnChargeStartedServer();
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        if (isCharging)
        {
            if (IsServer || IsOwner)
                TickCharge();
            return;
        }

        if (IsServer || IsOwner)
            TickMomentum();
    }

    private void TickCharge()
    {
        chargeTimer -= Time.deltaTime;
        LocalSpeed = StllGlaiveConstants.ChargeSpeed;

        if (IsServer)
        {
            syncedSpeed.Value = LocalSpeed;
            mountedCharge?.TickChargeKnockbackServer(velocityDirection, LocalSpeed);
        }

        ApplyMovement(velocityDirection * (LocalSpeed * Time.deltaTime));

        if (chargeTimer <= 0f)
        {
            isCharging = false;
            LocalSpeed = CwslGameConstants.RammerMaxSpeed * 0.65f;
            if (IsServer)
                syncedSpeed.Value = LocalSpeed;
        }
    }

    private void TickMomentum()
    {
        var speed = LocalSpeed;

        if (hasSteerInput)
        {
            var inputDir = desiredInputDirection;
            var velocityAlignment = Vector3.Dot(velocityDirection, inputDir);
            var isOpposing = velocityAlignment < StllGlaiveConstants.HorseOpposingDotThreshold;

            if (isOpposing && speed > StllGlaiveConstants.HorseBrakeMinSpeed)
            {
                isPivotTurning = false;
                speed = Mathf.Max(
                    0f,
                    speed - StllGlaiveConstants.HorseBrakeDecelPerSecond * Time.deltaTime);
            }
            else if (isOpposing)
            {
                isPivotTurning = true;
                var pivotRate = StllGlaiveConstants.HorsePivotTurnRate * Time.deltaTime;
                headingDirection = RotateFlat(headingDirection, inputDir, pivotRate);

                var pivotTarget = headingDirection * CwslGameConstants.RammerMaxSpeed;
                var pivotStrength = StllGlaiveConstants.HorsePivotVelocityBlend;
                BlendVelocityToward(pivotTarget, pivotStrength, ref velocityDirection, ref speed);

                if (Vector3.Dot(headingDirection, inputDir) >= StllGlaiveConstants.HorsePivotAlignDot)
                {
                    isPivotTurning = false;
                    speed = Mathf.Min(
                        CwslGameConstants.RammerMaxSpeed,
                        speed + CwslGameConstants.RammerAccelPerSecond * Time.deltaTime);
                }
            }
            else
            {
                isPivotTurning = false;
                var turnRate = TurnRateForSpeed(speed) * StllGlaiveConstants.HorseSteerTurnRateMultiplier;
                headingDirection = RotateFlat(headingDirection, inputDir, turnRate * Time.deltaTime);

                var steerAlignment = Vector3.Dot(velocityDirection, inputDir);
                if (steerAlignment >= StllGlaiveConstants.HorseSteerAccelDotThreshold)
                {
                    speed = Mathf.Min(
                        CwslGameConstants.RammerMaxSpeed,
                        speed + CwslGameConstants.RammerAccelPerSecond * Time.deltaTime);
                }
                else if (speed > 0f)
                {
                    speed = Mathf.Max(
                        0f,
                        speed - CwslGameConstants.RammerDecelPerSecond
                            * StllGlaiveConstants.HorseTurnDecelFactor
                            * Time.deltaTime);
                }

                var targetSpeed = speed;
                var targetVelocity = headingDirection * targetSpeed;
                var blendStrength = VelocityBlendStrength(speed);
                BlendVelocityToward(targetVelocity, blendStrength, ref velocityDirection, ref speed);
            }
        }
        else
        {
            isPivotTurning = false;
            speed = Mathf.Max(0f, speed - CwslGameConstants.RammerDecelPerSecond * Time.deltaTime);

            if (speed > 0.01f)
            {
                var coastTarget = headingDirection * speed;
                BlendVelocityToward(coastTarget, StllGlaiveConstants.HorseVelocityBlendHigh * 0.45f, ref velocityDirection, ref speed);
            }
        }

        if (speed <= CwslGameConstants.RammerStopSpeed && !hasSteerInput)
        {
            LocalSpeed = 0f;
            if (IsServer)
                syncedSpeed.Value = 0f;
            momentumActive = false;
            return;
        }

        LocalSpeed = speed;
        if (IsServer)
            syncedSpeed.Value = speed;

        if (velocityDirection.sqrMagnitude < 0.0001f)
            velocityDirection = FlatForward();
        if (headingDirection.sqrMagnitude < 0.0001f)
            headingDirection = velocityDirection;

        if (speed > 0.01f)
            ApplyMovement(velocityDirection * (speed * Time.deltaTime));

        ApplyBodyRotation(speed);
    }

    private void ApplyBodyRotation(float speed)
    {
        if (headingDirection.sqrMagnitude < 0.0001f)
            return;

        var turnRate = isPivotTurning
            ? StllGlaiveConstants.HorsePivotTurnRate
            : TurnRateForSpeed(Mathf.Max(speed, 1f)) * StllGlaiveConstants.HorseBodyTurnRateMultiplier;

        var lookRotation = Quaternion.LookRotation(headingDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            lookRotation,
            turnRate * Time.deltaTime);
    }

    private static void BlendVelocityToward(
        Vector3 targetVelocity,
        float blendStrength,
        ref Vector3 direction,
        ref float speed)
    {
        var currentVelocity = direction * speed;
        var t = 1f - Mathf.Exp(-blendStrength * Time.deltaTime);
        var blended = Vector3.Lerp(currentVelocity, targetVelocity, t);

        speed = blended.magnitude;
        if (speed > 0.01f)
            direction = blended / speed;
        else if (targetVelocity.sqrMagnitude > 0.0001f)
            direction = targetVelocity.normalized;
    }

    private static float VelocityBlendStrength(float speed)
    {
        var speedRatio = Mathf.Clamp01(speed / CwslGameConstants.RammerMaxSpeed);
        var drift = Mathf.Lerp(
            StllGlaiveConstants.HorseDriftAtLowSpeed,
            StllGlaiveConstants.HorseDriftAtHighSpeed,
            speedRatio);
        return Mathf.Lerp(
            StllGlaiveConstants.HorseVelocityBlendHigh,
            StllGlaiveConstants.HorseVelocityBlendLow,
            drift);
    }

    private void BeginMomentumMode()
    {
        momentumActive = true;
        if (velocityDirection.sqrMagnitude < 0.0001f)
        {
            velocityDirection = FlatForward();
            headingDirection = velocityDirection;
        }
    }

    private void ApplyMovement(Vector3 delta)
    {
        var next = transform.position + delta;
        var extent = StllGameConstants.ArenaHalfExtent - 0.5f;
        next.x = Mathf.Clamp(next.x, -extent, extent);
        next.z = Mathf.Clamp(next.z, -extent, extent);
        transform.position = next;
    }

    private Vector3 FlatForward()
    {
        var forward = transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
    }

    private static float TurnRateForSpeed(float speed)
    {
        var ratio = Mathf.Clamp01(speed / CwslGameConstants.RammerMaxSpeed);
        return Mathf.Lerp(
            CwslGameConstants.RammerSteerTurnRateHigh,
            CwslGameConstants.RammerSteerTurnRateLow,
            Mathf.Pow(ratio, CwslGameConstants.RammerSteerTurnSpeedExponent));
    }

    private static Vector3 RotateFlat(Vector3 current, Vector3 target, float maxDegrees)
    {
        if (current.sqrMagnitude < 0.0001f)
            current = Vector3.forward;
        if (target.sqrMagnitude < 0.0001f)
            return current.normalized;

        current.y = 0f;
        target.y = 0f;
        var rotated = Vector3.RotateTowards(current.normalized, target.normalized, maxDegrees * Mathf.Deg2Rad, 0f);
        return rotated.sqrMagnitude > 0.0001f ? rotated.normalized : current.normalized;
    }
}
