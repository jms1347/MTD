using System.Collections;
using Unity.Netcode;
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

        var bashVisual = GetComponent<CwslPlayerShieldBashVisual>();
        if (bashVisual != null && bashVisual.IsAnimating)
            return;

        var skillVisual = GetComponent<CwslTankShieldSkillVisual>();
        if (skillVisual != null && skillVisual.IsAnimating)
            return;

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

/// <summary>방패 탱커 평타 — 방패 들어 올렸다가 전방 강타 연출.</summary>
public class CwslPlayerShieldBashVisual : MonoBehaviour
{
    private const float WindupDuration = 0.32f;
    private const float ImpactDuration = 0.2f;
    private const float RecoverDuration = 0.24f;

    // BuildPlayer Shield z=0.7 + 강타 z오프셋(0.62+0.22) + 방패 면 z≈0.07
    private const float ShieldRestForward = 0.7f;
    private const float ImpactLungeForward = 0.84f;
    private const float ShieldFaceExtent = 0.07f;
    public const float StrikeReach = ShieldRestForward + ImpactLungeForward + ShieldFaceExtent;

    private Transform shield;
    private Transform visualRoot;
    private Vector3 shieldBaseLocalPosition;
    private Quaternion shieldBaseLocalRotation;
    private Vector3 visualBaseLocalPosition;
    private Coroutine routine;

    public bool IsAnimating { get; private set; }

    private void Awake()
    {
        CacheParts();
    }

    private void CacheParts()
    {
        visualRoot = transform;
        shield = transform.Find("Shield");
        if (shield == null)
            return;

        shieldBaseLocalPosition = shield.localPosition;
        shieldBaseLocalRotation = shield.localRotation;
        visualBaseLocalPosition = visualRoot.localPosition;
    }

    public void PlayWindup(Vector3 worldTarget)
    {
        if (shield == null)
            CacheParts();
        if (shield == null)
            return;

        if (routine != null)
            StopCoroutine(routine);

        FaceTarget(worldTarget);
        routine = StartCoroutine(WindupRoutine());
    }

    public void PlayImpact(Vector3 worldHitPoint, bool empowered = false)
    {
        if (shield == null)
            CacheParts();
        if (shield == null)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(ImpactRoutine(worldHitPoint, empowered));
    }

    private void FaceTarget(Vector3 worldTarget)
    {
        var root = transform.root;
        var flat = worldTarget - root.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
            return;

        root.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
    }

    private IEnumerator WindupRoutine()
    {
        IsAnimating = true;

        var windupShieldPos = shieldBaseLocalPosition + new Vector3(0f, 0.22f, -0.38f);
        var windupShieldRot = shieldBaseLocalRotation * Quaternion.Euler(-32f, 8f, -6f);
        var windupBodyPos = visualBaseLocalPosition + new Vector3(0f, 0f, -0.08f);

        var timer = 0f;
        while (timer < WindupDuration)
        {
            timer += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, timer / WindupDuration);
            shield.localPosition = Vector3.Lerp(shieldBaseLocalPosition, windupShieldPos, t);
            shield.localRotation = Quaternion.Slerp(shieldBaseLocalRotation, windupShieldRot, t);
            visualRoot.localPosition = Vector3.Lerp(visualBaseLocalPosition, windupBodyPos, t);
            yield return null;
        }

        routine = null;
    }

    private IEnumerator ImpactRoutine(Vector3 worldHitPoint, bool empowered)
    {
        IsAnimating = true;

        var impactShieldPos = shieldBaseLocalPosition + new Vector3(0f, -0.04f, 0.62f);
        var impactShieldRot = shieldBaseLocalRotation * Quaternion.Euler(18f, 0f, 0f);
        var impactBodyPos = visualBaseLocalPosition + new Vector3(0f, 0f, 0.22f);

        var timer = 0f;
        while (timer < ImpactDuration)
        {
            timer += Time.deltaTime;
            var t = timer / ImpactDuration;
            var slam = 1f - Mathf.Pow(1f - t, 3f);
            shield.localPosition = Vector3.Lerp(shield.localPosition, impactShieldPos, slam);
            shield.localRotation = Quaternion.Slerp(shield.localRotation, impactShieldRot, slam);
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, impactBodyPos, slam);
            yield return null;
        }

        var hitPoint = worldHitPoint + Vector3.up * 0.9f;
        CwslVfxSpawner.SpawnMeleeHit(hitPoint, transform.root.rotation);
        if (empowered)
        {
            var forward = transform.root.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            else
                forward.Normalize();

            var slamCenter = transform.root.position + forward * StrikeReach + Vector3.up * 0.9f;
            CwslVfxSpawner.SpawnMeleeHit(slamCenter, transform.root.rotation);
            CwslVfxSpawner.SpawnFortifyBlock(slamCenter + Vector3.up * 0.15f);
        }
        else
        {
            CwslVfxSpawner.SpawnFortifyBlock(hitPoint + Vector3.up * 0.15f);
        }

        timer = 0f;
        while (timer < RecoverDuration)
        {
            timer += Time.deltaTime;
            var t = timer / RecoverDuration;
            shield.localPosition = Vector3.Lerp(impactShieldPos, shieldBaseLocalPosition, t);
            shield.localRotation = Quaternion.Slerp(impactShieldRot, shieldBaseLocalRotation, t);
            visualRoot.localPosition = Vector3.Lerp(impactBodyPos, visualBaseLocalPosition, t);
            yield return null;
        }

        shield.localPosition = shieldBaseLocalPosition;
        shield.localRotation = shieldBaseLocalRotation;
        visualRoot.localPosition = visualBaseLocalPosition;
        IsAnimating = false;
        routine = null;
    }

    public static float GetAttackRange(NetworkObject target)
    {
        return StrikeReach + ResolveTargetHitRadius(target);
    }

    public static bool IsInStrikeRange(Transform attacker, NetworkObject target)
    {
        if (attacker == null || target == null)
            return false;

        var aimPoint = ResolveFlatAimPoint(target);
        var flat = aimPoint - attacker.position;
        flat.y = 0f;
        return flat.magnitude <= GetAttackRange(target);
    }

    private static Vector3 ResolveFlatAimPoint(NetworkObject target)
    {
        var monster = target.GetComponent<CwslMonsterHealth>();
        if (monster != null)
            return monster.GetFlatHitPoint();

        var enemyBase = target.GetComponent<CwslEnemyBase>();
        if (enemyBase != null)
        {
            var aim = enemyBase.GetAimPoint();
            aim.y = target.transform.position.y;
            return aim;
        }

        return target.transform.position;
    }

    private static float ResolveTargetHitRadius(NetworkObject target)
    {
        if (target == null)
            return 0f;

        var monster = target.GetComponent<CwslMonsterHealth>();
        if (monster != null)
            return monster.GetFlatHitRadius();

        return target.GetComponent<CwslEnemyBase>() != null ? 0.85f : 0.5f;
    }
}
