using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 질주자 말 몸통 날(blade) 회전 — Q 날개 펼치기 시 확대·고속 회전.
/// </summary>
public class CwslPlayerRammerTopSpinVisual : MonoBehaviour
{
    private Transform spinCubePivot;
    private CwslMomentumRammerSkill rammerSkill;
    private CwslPlayerMovement movement;
    private CwslPlayerHealth playerHealth;
    private NavMeshAgent agent;
    private Vector3 lastRootPosition;
    private float spinAngle;

    private void Awake()
    {
        spinCubePivot = transform.Find("HorseRoot/HorseBodyBladeSpin");
        rammerSkill = GetComponentInParent<CwslMomentumRammerSkill>();
        movement = GetComponentInParent<CwslPlayerMovement>();
        playerHealth = GetComponentInParent<CwslPlayerHealth>();
        agent = GetComponentInParent<NavMeshAgent>();
        lastRootPosition = transform.root.position;
    }

    private void Start()
    {
        if (spinCubePivot == null)
            spinCubePivot = transform.Find("HorseRoot/HorseBodyBladeSpin");
    }

    private void Update()
    {
        if (spinCubePivot == null || rammerSkill == null)
            return;

        if (playerHealth != null && !playerHealth.IsAlive)
        {
            ResetBlade();
            return;
        }

        if (rammerSkill.IsStunned)
        {
            ResetBlade();
            return;
        }

        var wingActive = rammerSkill.IsWingSpreadActive;
        var speed = ReadSpeed();
        var targetScale = wingActive ? Vector3.one * rammerSkill.BladeScale : Vector3.one;
        spinCubePivot.localScale = Vector3.Lerp(spinCubePivot.localScale, targetScale, Time.deltaTime * 10f);

        if (!wingActive && speed < CwslGameConstants.RammerDamageSpeedThreshold)
        {
            spinCubePivot.localRotation = Quaternion.Lerp(
                spinCubePivot.localRotation,
                Quaternion.identity,
                Time.deltaTime * 10f);
            return;
        }

        var spinSpeed = wingActive
            ? ResolveWingSpinSpeed(speed, rammerSkill.BladeScale)
            : ResolveSpinSpeed(speed);
        spinAngle += spinSpeed * Time.deltaTime;
        spinCubePivot.localRotation = Quaternion.Euler(0f, spinAngle, 0f);
    }

    private void ResetBlade()
    {
        spinCubePivot.localRotation = Quaternion.identity;
        spinCubePivot.localScale = Vector3.one;
        spinAngle = 0f;
    }

    private float ReadSpeed()
    {
        var root = transform.root;
        var flatDelta = root.position - lastRootPosition;
        flatDelta.y = 0f;
        lastRootPosition = root.position;
        var estimatedSpeed = Time.deltaTime > 0.0001f ? flatDelta.magnitude / Time.deltaTime : 0f;

        if (rammerSkill != null && rammerSkill.CurrentSpeed > estimatedSpeed)
            return rammerSkill.CurrentSpeed;
        if (movement != null && movement.CurrentMoveSpeed > estimatedSpeed)
            return movement.CurrentMoveSpeed;
        if (agent != null && agent.enabled && agent.velocity.magnitude > estimatedSpeed)
            return agent.velocity.magnitude;
        return estimatedSpeed;
    }

    private static float ResolveSpinSpeed(float speed)
    {
        var speedRatio = Mathf.InverseLerp(
            CwslGameConstants.RammerDamageSpeedThreshold,
            CwslGameConstants.RammerMaxSpeed,
            speed);
        speedRatio = Mathf.Clamp01(speedRatio);
        return Mathf.Lerp(220f, 1180f, speedRatio * speedRatio);
    }

    private static float ResolveWingSpinSpeed(float speed, float bladeScale)
    {
        var speedRatio = Mathf.Clamp01(speed / CwslGameConstants.RammerMaxSpeed);
        var scaleRatio = Mathf.InverseLerp(1f, CwslGameConstants.RammerWingSpreadMaxScale, bladeScale);
        return Mathf.Lerp(520f, 1680f, scaleRatio) + speedRatio * 320f;
    }
}
