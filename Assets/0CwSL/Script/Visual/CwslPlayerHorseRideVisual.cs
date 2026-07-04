using UnityEngine;
using UnityEngine.AI;

public class CwslPlayerHorseRideVisual : MonoBehaviour
{
    private Transform horseLegFl;
    private Transform horseLegFr;
    private Transform horseLegBl;
    private Transform horseLegBr;
    private Transform riderPivot;
    private Vector3 riderBaseLocalPosition;
    private CwslPlayerMovement movement;
    private CwslMomentumRammerSkill rammerSkill;
    private CwslPlayerHealth playerHealth;
    private NavMeshAgent agent;
    private Vector3 lastRootPosition;
    private float trotPhase;

    private void Awake()
    {
        horseLegFl = transform.Find("HorseRoot/HorseLegFL");
        horseLegFr = transform.Find("HorseRoot/HorseLegFR");
        horseLegBl = transform.Find("HorseRoot/HorseLegBL");
        horseLegBr = transform.Find("HorseRoot/HorseLegBR");
        riderPivot = transform.Find("HorseRoot/RiderPivot");
        if (riderPivot != null)
            riderBaseLocalPosition = riderPivot.localPosition;

        movement = GetComponentInParent<CwslPlayerMovement>();
        rammerSkill = GetComponentInParent<CwslMomentumRammerSkill>();
        playerHealth = GetComponentInParent<CwslPlayerHealth>();
        agent = GetComponentInParent<NavMeshAgent>();
        lastRootPosition = transform.root.position;
    }

    private void Update()
    {
        if (horseLegFl == null || horseLegFr == null || horseLegBl == null || horseLegBr == null)
            return;

        if (playerHealth != null && !playerHealth.IsAlive)
        {
            ResetPose();
            return;
        }

        var speed = ReadSpeed();
        if (speed < 0.12f)
        {
            ResetPose();
            return;
        }

        var speedRatio = Mathf.Clamp01(speed / CwslGameConstants.RammerMaxSpeed);
        trotPhase += Time.deltaTime * Mathf.Lerp(4.5f, 11f, speedRatio);
        var hoofSwing = Mathf.Sin(trotPhase) * Mathf.Lerp(22f, 42f, speedRatio);

        horseLegFl.localRotation = Quaternion.Euler(hoofSwing, 0f, 0f);
        horseLegBr.localRotation = Quaternion.Euler(hoofSwing, 0f, 0f);
        horseLegFr.localRotation = Quaternion.Euler(-hoofSwing, 0f, 0f);
        horseLegBl.localRotation = Quaternion.Euler(-hoofSwing, 0f, 0f);

        if (riderPivot != null)
        {
            var bounce = Mathf.Abs(Mathf.Sin(trotPhase * 2f)) * Mathf.Lerp(0.03f, 0.16f, speedRatio);
            riderPivot.localPosition = riderBaseLocalPosition + Vector3.up * bounce;
            riderPivot.localRotation = Quaternion.Euler(-8f * speedRatio, 0f, 0f);
        }
    }

    private void ResetPose()
    {
        horseLegFl.localRotation = Quaternion.identity;
        horseLegFr.localRotation = Quaternion.identity;
        horseLegBl.localRotation = Quaternion.identity;
        horseLegBr.localRotation = Quaternion.identity;
        if (riderPivot != null)
        {
            riderPivot.localPosition = riderBaseLocalPosition;
            riderPivot.localRotation = Quaternion.identity;
        }
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
}
