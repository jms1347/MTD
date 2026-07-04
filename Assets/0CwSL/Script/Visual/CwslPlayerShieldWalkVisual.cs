using UnityEngine;
using UnityEngine.AI;

public class CwslPlayerShieldWalkVisual : MonoBehaviour
{
    private static readonly Vector3 RaisedLocalPosition = new(0f, 1.72f, 0.08f);
    private static readonly Vector3 RaisedLocalEuler = new(-78f, 0f, 0f);

    private Transform shield;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private CwslPlayerMovement movement;
    private CwslTankFortifySkill fortifySkill;
    private NavMeshAgent agent;
    private Vector3 lastRootPosition;
    private float raiseBlend;

    private void Awake()
    {
        shield = transform.Find("Shield");
        if (shield != null)
        {
            baseLocalPosition = shield.localPosition;
            baseLocalRotation = shield.localRotation;
        }

        movement = GetComponentInParent<CwslPlayerMovement>();
        fortifySkill = GetComponentInParent<CwslTankFortifySkill>();
        agent = GetComponentInParent<NavMeshAgent>();
        lastRootPosition = transform.root.position;
    }

    private void Update()
    {
        if (shield == null)
            return;

        if (fortifySkill != null && fortifySkill.IsShieldActive)
        {
            raiseBlend = Mathf.MoveTowards(raiseBlend, 0f, Time.deltaTime * 8f);
            ApplyRaise(raiseBlend);
            return;
        }

        var speed = ReadSpeed();
        var targetRaise = speed > 0.15f ? Mathf.Clamp01(speed / 4.5f) : 0f;
        raiseBlend = Mathf.MoveTowards(raiseBlend, targetRaise, Time.deltaTime * (targetRaise > raiseBlend ? 7f : 5f));
        ApplyRaise(raiseBlend);
    }

    private void ApplyRaise(float blend)
    {
        var raisedRotation = baseLocalRotation * Quaternion.Euler(RaisedLocalEuler);
        shield.localPosition = Vector3.Lerp(baseLocalPosition, RaisedLocalPosition, blend);
        shield.localRotation = Quaternion.Slerp(baseLocalRotation, raisedRotation, blend);
    }

    private float ReadSpeed()
    {
        var root = transform.root;
        var flatDelta = root.position - lastRootPosition;
        flatDelta.y = 0f;
        lastRootPosition = root.position;
        var estimatedSpeed = Time.deltaTime > 0.0001f ? flatDelta.magnitude / Time.deltaTime : 0f;
        if (movement != null && movement.CurrentMoveSpeed > estimatedSpeed)
            return movement.CurrentMoveSpeed;
        if (agent != null && agent.enabled && agent.velocity.magnitude > estimatedSpeed)
            return agent.velocity.magnitude;
        return estimatedSpeed;
    }
}
