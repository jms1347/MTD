using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 질주자 말 몸통 중간 날(blade) 팽이 회전 — 속도 단계별 Y축 회전.
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
        if (spinCubePivot != null)
            return;

        spinCubePivot = transform.Find("HorseRoot/HorseBodyBladeSpin");
    }

    private void Update()
    {
        if (spinCubePivot == null || rammerSkill == null)
            return;

        if (playerHealth != null && !playerHealth.IsAlive)
        {
            spinCubePivot.localRotation = Quaternion.identity;
            spinAngle = 0f;
            return;
        }

        if (rammerSkill.IsStunned)
        {
            spinCubePivot.localRotation = Quaternion.identity;
            spinAngle = 0f;
            return;
        }

        var speed = ReadSpeed();
        if (speed < CwslGameConstants.RammerDamageSpeedThreshold)
        {
            spinCubePivot.localRotation = Quaternion.Lerp(
                spinCubePivot.localRotation,
                Quaternion.identity,
                Time.deltaTime * 10f);
            return;
        }

        spinAngle += ResolveSpinSpeed(speed) * Time.deltaTime;
        spinCubePivot.localRotation = Quaternion.Euler(0f, spinAngle, 0f);
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
}
