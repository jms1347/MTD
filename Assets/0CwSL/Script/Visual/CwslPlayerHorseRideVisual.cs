using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 질주자 말+기수 프로시저럴 연출 — 다그닥 보행, 말 몸 전후 흔들림, 기수 탁탁 튕김.
/// </summary>
public class CwslPlayerHorseRideVisual : MonoBehaviour
{
    private Transform horseRoot;
    private Transform horseLegFl;
    private Transform horseLegFr;
    private Transform horseLegBl;
    private Transform horseLegBr;
    private Transform riderPivot;
    private Transform headPivot;

    private Vector3 legFlBaseLocalPosition;
    private Vector3 legFrBaseLocalPosition;
    private Vector3 legBlBaseLocalPosition;
    private Vector3 legBrBaseLocalPosition;

    private Vector3 horseRootBaseLocalPosition;
    private Quaternion horseRootBaseLocalRotation;
    private Vector3 riderBaseLocalPosition;
    private Quaternion riderBaseLocalRotation;
    private Vector3 headBaseLocalPosition;
    private Quaternion headBaseLocalRotation;

    private CwslPlayerMovement movement;
    private CwslMomentumRammerSkill rammerSkill;
    private CwslPlayerHealth playerHealth;
    private NavMeshAgent agent;
    private Vector3 lastRootPosition;
    private float trotPhase;

    private void Awake()
    {
        horseRoot = transform.Find("HorseRoot");
        horseLegFl = transform.Find("HorseRoot/HorseLegFL");
        horseLegFr = transform.Find("HorseRoot/HorseLegFR");
        horseLegBl = transform.Find("HorseRoot/HorseLegBL");
        horseLegBr = transform.Find("HorseRoot/HorseLegBR");
        riderPivot = transform.Find("HorseRoot/RiderPivot");
        headPivot = transform.Find("HorseRoot/RiderPivot/HeadPivot");

        CacheLegBase(horseLegFl, ref legFlBaseLocalPosition);
        CacheLegBase(horseLegFr, ref legFrBaseLocalPosition);
        CacheLegBase(horseLegBl, ref legBlBaseLocalPosition);
        CacheLegBase(horseLegBr, ref legBrBaseLocalPosition);

        if (horseRoot != null)
        {
            horseRootBaseLocalPosition = horseRoot.localPosition;
            horseRootBaseLocalRotation = horseRoot.localRotation;
        }

        if (riderPivot != null)
        {
            riderBaseLocalPosition = riderPivot.localPosition;
            riderBaseLocalRotation = riderPivot.localRotation;
        }

        if (headPivot != null)
        {
            headBaseLocalPosition = headPivot.localPosition;
            headBaseLocalRotation = headPivot.localRotation;
        }

        movement = GetComponentInParent<CwslPlayerMovement>();
        rammerSkill = GetComponentInParent<CwslMomentumRammerSkill>();
        playerHealth = GetComponentInParent<CwslPlayerHealth>();
        agent = GetComponentInParent<NavMeshAgent>();
        lastRootPosition = transform.root.position;
    }

    private void Start()
    {
        if (horseLegFl != null)
            return;

        horseRoot = transform.Find("HorseRoot");
        horseLegFl = transform.Find("HorseRoot/HorseLegFL");
        horseLegFr = transform.Find("HorseRoot/HorseLegFR");
        horseLegBl = transform.Find("HorseRoot/HorseLegBL");
        horseLegBr = transform.Find("HorseRoot/HorseLegBR");
        riderPivot = transform.Find("HorseRoot/RiderPivot");
        headPivot = transform.Find("HorseRoot/RiderPivot/HeadPivot");

        CacheLegBase(horseLegFl, ref legFlBaseLocalPosition);
        CacheLegBase(horseLegFr, ref legFrBaseLocalPosition);
        CacheLegBase(horseLegBl, ref legBlBaseLocalPosition);
        CacheLegBase(horseLegBr, ref legBrBaseLocalPosition);

        if (horseRoot != null)
        {
            horseRootBaseLocalPosition = horseRoot.localPosition;
            horseRootBaseLocalRotation = horseRoot.localRotation;
        }

        if (riderPivot != null)
        {
            riderBaseLocalPosition = riderPivot.localPosition;
            riderBaseLocalRotation = riderPivot.localRotation;
        }

        if (headPivot != null)
        {
            headBaseLocalPosition = headPivot.localPosition;
            headBaseLocalRotation = headPivot.localRotation;
        }
    }

    private static void CacheLegBase(Transform leg, ref Vector3 baseLocalPosition)
    {
        if (leg != null)
            baseLocalPosition = leg.localPosition;
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

        if (rammerSkill != null && rammerSkill.IsStunned)
        {
            ResetPose();
            return;
        }

        var speed = ReadSpeed();
        if (speed < 0.05f)
        {
            ResetPose();
            return;
        }

        // 일반 이동(저속)에서도 다리는 확실히 들썩이도록 별도 강도 사용
        var legMotionRatio = Mathf.Clamp01(speed / CwslGameConstants.BaseMoveSpeed);
        legMotionRatio = Mathf.Max(legMotionRatio, 0.68f);
        var bodyMotionRatio = Mathf.Clamp01(speed / CwslGameConstants.RammerMaxSpeed);

        trotPhase += Time.deltaTime * Mathf.Lerp(9f, 24f, legMotionRatio);

        var gallopSin = Mathf.Sin(trotPhase);
        var gallopSharp = SharpenWave(gallopSin);
        var hoofHit = SharpenWave(Mathf.Sin(trotPhase * 2f));
        var hoofSwing = gallopSharp * Mathf.Lerp(42f, 92f, legMotionRatio);
        var legLift = hoofHit * Mathf.Lerp(0.09f, 0.22f, legMotionRatio);
        var legKick = gallopSharp * Mathf.Lerp(0.05f, 0.14f, legMotionRatio);

        ApplyLegPose(horseLegFl, legFlBaseLocalPosition, hoofSwing, legLift, legKick);
        ApplyLegPose(horseLegBr, legBrBaseLocalPosition, hoofSwing, legLift, legKick);
        ApplyLegPose(horseLegFr, legFrBaseLocalPosition, -hoofSwing, legLift, -legKick);
        ApplyLegPose(horseLegBl, legBlBaseLocalPosition, -hoofSwing, legLift, -legKick);

        if (speed >= 0.75f)
        {
            ApplyHorseBodyGallop(bodyMotionRatio, gallopSin, gallopSharp, hoofHit);
            ApplyRiderBounce(bodyMotionRatio);
        }
        else
        {
            ResetHorseBodyAndRider();
        }
    }

    private static void ApplyLegPose(
        Transform leg,
        Vector3 baseLocalPosition,
        float swing,
        float lift,
        float kick)
    {
        if (leg == null)
            return;

        leg.localRotation = Quaternion.Euler(swing, 0f, swing * 0.22f);
        leg.localPosition = baseLocalPosition + new Vector3(0f, lift, kick);
    }

    private void ApplyHorseBodyGallop(float speedRatio, float gallopSin, float gallopSharp, float hoofHit)
    {
        if (horseRoot == null)
            return;

        var pitch = gallopSin * Mathf.Lerp(10f, 22f, speedRatio);
        var lift = hoofHit * Mathf.Lerp(0.035f, 0.1f, speedRatio);
        var surge = gallopSharp * Mathf.Lerp(0.04f, 0.14f, speedRatio);

        horseRoot.localRotation = horseRootBaseLocalRotation * Quaternion.Euler(pitch, 0f, 0f);
        horseRoot.localPosition = horseRootBaseLocalPosition + new Vector3(0f, lift, surge);
    }

    private void ApplyRiderBounce(float speedRatio)
    {
        if (riderPivot == null)
            return;

        var hoofHit = SharpenWave(Mathf.Sin(trotPhase * 2f));
        var vertical = Mathf.Abs(hoofHit) * Mathf.Lerp(0.12f, 0.34f, speedRatio);

        var sidePhase = trotPhase * 1.45f + 0.65f;
        var sideX = Mathf.Sin(sidePhase) * Mathf.Lerp(0.07f, 0.22f, speedRatio);
        var sideZ = SharpenWave(Mathf.Sin(trotPhase * 1.15f + 1.1f)) * Mathf.Lerp(0.045f, 0.14f, speedRatio);

        var roll = Mathf.Sin(sidePhase * 1.55f) * Mathf.Lerp(14f, 32f, speedRatio);
        var pitch = Mathf.Sin(trotPhase * 2.35f + 0.9f) * Mathf.Lerp(12f, 28f, speedRatio);
        var yaw = Mathf.Sin(trotPhase * 0.85f + 0.4f) * Mathf.Lerp(8f, 18f, speedRatio);

        riderPivot.localPosition = riderBaseLocalPosition + new Vector3(sideX, vertical, sideZ);
        riderPivot.localRotation = riderBaseLocalRotation * Quaternion.Euler(-pitch * 0.45f, yaw * 0.35f, -roll * 0.35f);

        if (headPivot != null)
        {
            // 머리는 인간처럼 안정 — 몸통 흔들림만 살짝 따라감
            headPivot.localPosition = headBaseLocalPosition + new Vector3(sideX * 0.25f, vertical * 0.35f, sideZ * 0.2f);
            headPivot.localRotation = headBaseLocalRotation * Quaternion.Euler(-pitch * 0.12f, yaw * 0.08f, -roll * 0.1f);
        }
    }

    private void ResetHorseBodyAndRider()
    {
        if (horseRoot != null)
        {
            horseRoot.localPosition = horseRootBaseLocalPosition;
            horseRoot.localRotation = horseRootBaseLocalRotation;
        }

        if (riderPivot != null)
        {
            riderPivot.localPosition = riderBaseLocalPosition;
            riderPivot.localRotation = riderBaseLocalRotation;
        }

        if (headPivot != null)
        {
            headPivot.localPosition = headBaseLocalPosition;
            headPivot.localRotation = headBaseLocalRotation;
        }
    }

    private static float SharpenWave(float value)
    {
        return Mathf.Sign(value) * Mathf.Pow(Mathf.Abs(value), 0.45f);
    }

    private void ResetPose()
    {
        ApplyLegPose(horseLegFl, legFlBaseLocalPosition, 0f, 0f, 0f);
        ApplyLegPose(horseLegFr, legFrBaseLocalPosition, 0f, 0f, 0f);
        ApplyLegPose(horseLegBl, legBlBaseLocalPosition, 0f, 0f, 0f);
        ApplyLegPose(horseLegBr, legBrBaseLocalPosition, 0f, 0f, 0f);
        ResetHorseBodyAndRider();
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
