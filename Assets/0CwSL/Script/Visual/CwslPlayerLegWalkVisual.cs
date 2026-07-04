using UnityEngine;
using UnityEngine.AI;

public class CwslPlayerLegWalkVisual : MonoBehaviour
{
    private Transform legL;
    private Transform legR;
    private CwslPlayerMovement movement;
    private CwslMomentumRammerSkill rammerSkill;
    private NavMeshAgent agent;
    private Vector3 lastRootPosition;
    private float walkPhase;

    private void Awake()
    {
        legL = transform.Find("LegL");
        legR = transform.Find("LegR");
        movement = GetComponentInParent<CwslPlayerMovement>();
        rammerSkill = GetComponentInParent<CwslMomentumRammerSkill>();
        agent = GetComponentInParent<NavMeshAgent>();
        lastRootPosition = transform.root.position;
    }

    private void Update()
    {
        if (legL == null || legR == null)
            return;

        var root = transform.root;
        var flatDelta = root.position - lastRootPosition;
        flatDelta.y = 0f;
        lastRootPosition = root.position;
        var estimatedSpeed = Time.deltaTime > 0.0001f ? flatDelta.magnitude / Time.deltaTime : 0f;

        var speed = estimatedSpeed;
        if (rammerSkill != null && rammerSkill.CurrentSpeed > speed)
            speed = rammerSkill.CurrentSpeed;
        else if (movement != null && movement.CurrentMoveSpeed > speed)
            speed = movement.CurrentMoveSpeed;
        else if (agent != null && agent.enabled && agent.velocity.magnitude > speed)
            speed = agent.velocity.magnitude;

        if (speed < 0.12f)
        {
            legL.localRotation = Quaternion.identity;
            legR.localRotation = Quaternion.identity;
            return;
        }

        walkPhase += Time.deltaTime * speed * 1.65f;
        var swing = Mathf.Sin(walkPhase) * Mathf.Clamp01(speed / 7f) * 38f;
        legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
        legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
    }
}
