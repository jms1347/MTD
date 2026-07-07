using System.Collections;
using UnityEngine;

/// <summary>탱커 E/R 스킬 방패 연출 — 지진 강타·회전.</summary>
public class CwslTankShieldSkillVisual : MonoBehaviour
{
    private const float VisualGroundY = CwslTankShieldVfxUtil.VisualGroundY;
    private const float ShieldBottomLocalY = CwslTankShieldVfxUtil.ShieldBottomLocalY;

    private Transform shield;
    private Transform visualRoot;
    private Vector3 shieldBaseLocalPosition;
    private Quaternion shieldBaseLocalRotation;
    private Vector3 shieldBaseLocalScale;
    private Vector3 visualBaseLocalPosition;
    private Coroutine routine;
    private GameObject whirlwindInstance;

    public bool IsAnimating { get; private set; }

    private void Awake()
    {
        CacheParts();
    }

    public void PlaySlam(bool empowered)
    {
        CacheParts();
        if (shield == null)
            return;

        RefreshShieldBase();

        if (routine != null)
            StopCoroutine(routine);

        ResetShieldPoseForSkill();
        routine = StartCoroutine(SlamRoutine(empowered));
    }

    public void PlayWhirlwind(float duration, bool empowered)
    {
        CacheParts();
        if (shield == null)
            return;

        RefreshShieldBase();

        if (routine != null)
            StopCoroutine(routine);

        ResetShieldPoseForSkill();
        routine = StartCoroutine(WhirlwindRoutine(duration, empowered));
    }

    private void CacheParts()
    {
        if (shield != null)
            return;

        visualRoot = transform;
        shield = transform.Find("Shield");
        if (shield == null)
            return;

        shieldBaseLocalPosition = shield.localPosition;
        shieldBaseLocalRotation = shield.localRotation;
        shieldBaseLocalScale = shield.localScale;
        visualBaseLocalPosition = visualRoot.localPosition;
    }

    private void RefreshShieldBase()
    {
        if (shield == null)
            return;

        var scale = shield.localScale;
        if (IsFinite(scale))
            shieldBaseLocalScale = scale;
    }

    private void ResetShieldPoseForSkill()
    {
        if (shield == null)
            return;

        shield.localPosition = shieldBaseLocalPosition;
        shield.localRotation = shieldBaseLocalRotation;
        shield.localScale = shieldBaseLocalScale;

        if (visualRoot != null)
            visualRoot.localPosition = visualBaseLocalPosition;

        ClearWhirlwindFx();
        IsAnimating = false;
    }

    private IEnumerator SlamRoutine(bool empowered)
    {
        IsAnimating = true;

        var startPos = Sanitize(shield.localPosition, shieldBaseLocalPosition);
        var startBodyPos = visualRoot.localPosition;
        var slamRot = shieldBaseLocalRotation;

        var scaleY = SafeScaleY(shield.localScale);
        var raiseLift = empowered
            ? CwslGameConstants.TankShieldSlamRaiseHeightEmpowered
            : CwslGameConstants.TankShieldSlamRaiseHeight;
        var raisedPos = shieldBaseLocalPosition + new Vector3(0f, raiseLift * scaleY, empowered ? 0.02f : -0.08f);
        raisedPos = Sanitize(raisedPos, shieldBaseLocalPosition + Vector3.up * 0.9f);

        ComputeSlamDownPosition(shieldBaseLocalPosition, slamRot, scaleY, empowered, out var slamPos);
        slamPos = Sanitize(slamPos, raisedPos + Vector3.down * 1f);

        var shieldRaiseDelta = Mathf.Max(0.01f, raisedPos.y - startPos.y);
        var bodyJumpLift = Mathf.Max(shieldRaiseDelta, raisedPos.y - startBodyPos.y);
        var peakBodyPos = startBodyPos + Vector3.up * bodyJumpLift;
        var bodySlamDip = empowered
            ? CwslGameConstants.TankShieldSlamBodySlamDipEmpowered
            : CwslGameConstants.TankShieldSlamBodySlamDip;
        var slamBodyPos = startBodyPos + new Vector3(
            0f,
            bodySlamDip,
            CwslGameConstants.TankShieldSlamBodySlamForward);

        var windup = CwslGameConstants.TankShieldSlamWindup;
        var raiseTime = Mathf.Max(0.01f, windup * 0.58f);
        var holdTime = Mathf.Max(0.01f, windup - raiseTime);
        var slamDown = CwslGameConstants.TankShieldSlamSlamDownTime;

        var timer = 0f;
        while (timer < raiseTime)
        {
            timer += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, timer / raiseTime);
            shield.localPosition = Vector3.Lerp(startPos, raisedPos, t);
            shield.localRotation = slamRot;
            visualRoot.localPosition = Vector3.Lerp(startBodyPos, peakBodyPos, t);
            yield return null;
        }

        timer = 0f;
        while (timer < holdTime)
        {
            timer += Time.deltaTime;
            shield.localPosition = raisedPos;
            shield.localRotation = slamRot;
            visualRoot.localPosition = peakBodyPos;
            yield return null;
        }

        timer = 0f;
        while (timer < slamDown)
        {
            timer += Time.deltaTime;
            var t = 1f - Mathf.Pow(Mathf.Clamp01(1f - timer / slamDown), 3.5f);
            shield.localPosition = Vector3.Lerp(raisedPos, slamPos, t);
            shield.localRotation = slamRot;
            visualRoot.localPosition = Vector3.Lerp(peakBodyPos, slamBodyPos, t);
            yield return null;
        }

        shield.localPosition = slamPos;
        shield.localRotation = slamRot;
        visualRoot.localPosition = slamBodyPos;

        var root = transform.root;
        var hitPoint = shield.position;
        hitPoint.y = VisualGroundY;
        var groundRotation = CwslTankShieldVfxUtil.GetSlamGroundRotation(root);
        var effectScale = CwslTankShieldVfxUtil.GetSlamGroundHitScale(root, empowered);
        CwslVfxSpawner.SpawnShieldSlamGroundHit(hitPoint, groundRotation, effectScale, empowered);

        timer = 0f;
        const float recover = 0.28f;
        while (timer < recover)
        {
            timer += Time.deltaTime;
            var t = timer / recover;
            shield.localPosition = Vector3.Lerp(slamPos, startPos, t);
            shield.localRotation = slamRot;
            visualRoot.localPosition = Vector3.Lerp(slamBodyPos, startBodyPos, t);
            yield return null;
        }

        shield.localPosition = startPos;
        shield.localRotation = slamRot;
        visualRoot.localPosition = startBodyPos;

        IsAnimating = false;
        routine = null;
    }

    private static void ComputeSlamDownPosition(
        Vector3 referencePos,
        Quaternion poseRot,
        float scaleY,
        bool empowered,
        out Vector3 slamPos)
    {
        scaleY = Mathf.Max(0.01f, scaleY);
        if (!float.IsFinite(scaleY))
            scaleY = 1f;

        var bottomLocal = new Vector3(0f, ShieldBottomLocalY * scaleY, 0.06f);
        var bottomOffset = poseRot * bottomLocal;
        if (!IsFinite(bottomOffset))
        {
            slamPos = referencePos;
            slamPos.y = VisualGroundY + 0.35f * scaleY;
            slamPos.z += empowered ? 0.2f : 0.12f;
            return;
        }

        var slamY = VisualGroundY - bottomOffset.y;
        if (!float.IsFinite(slamY))
            slamY = VisualGroundY + 0.35f * scaleY;

        var forwardBonus = (empowered ? 0.2f : 0.12f) * scaleY;
        slamPos = new Vector3(referencePos.x, slamY, referencePos.z + forwardBonus);
    }

    private static float SafeScaleY(Vector3 scale)
    {
        var y = Mathf.Abs(scale.y);
        if (!float.IsFinite(y) || y < 0.01f)
            y = 1f;

        return y;
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

    private static Vector3 Sanitize(Vector3 value, Vector3 fallback) =>
        IsFinite(value) ? value : fallback;

    private static bool IsFinite(Quaternion value) =>
        float.IsFinite(value.x) && float.IsFinite(value.y)
        && float.IsFinite(value.z) && float.IsFinite(value.w);

    private IEnumerator WhirlwindRoutine(float duration, bool empowered)
    {
        IsAnimating = true;

        var startPos = shieldBaseLocalPosition;
        var startRot = shieldBaseLocalRotation;
        var startScale = shieldBaseLocalScale;
        var startBodyPos = visualBaseLocalPosition;

        var whirlScale = empowered
            ? startScale
            : Vector3.Scale(shieldBaseLocalScale, new Vector3(1.26f, 1.26f, 1.26f));

        var reach = empowered ? 1.38f : 1f;
        var scaleReach = Mathf.Max(1f, whirlScale.x);

        var spreadPos = startPos + new Vector3(0f, 0.18f * scaleReach, 0.42f * reach);
        var spreadRot = startRot * Quaternion.Euler(-18f, 8f, -32f);
        var spreadScale = Vector3.Lerp(startScale, whirlScale, 0.75f);

        var horizontalRot = startRot * Quaternion.Euler(94f, 6f, -90f);
        var horizontalPos = startPos + new Vector3(
            0.1f,
            -0.28f * scaleReach,
            0.62f * reach + (scaleReach - 1f) * 0.14f);
        var braceBodyPos = startBodyPos + new Vector3(0f, -0.05f, 0.1f);

        var timer = 0f;
        const float spreadTime = 0.18f;
        while (timer < spreadTime)
        {
            timer += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, timer / spreadTime);
            shield.localPosition = Vector3.Lerp(startPos, spreadPos, t);
            shield.localRotation = Quaternion.Slerp(startRot, spreadRot, t);
            shield.localScale = Vector3.Lerp(startScale, spreadScale, t);
            yield return null;
        }

        timer = 0f;
        const float armTime = 0.28f;
        while (timer < armTime)
        {
            timer += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, timer / armTime);
            shield.localPosition = Vector3.Lerp(spreadPos, horizontalPos, t);
            shield.localRotation = Quaternion.Slerp(spreadRot, horizontalRot, t);
            shield.localScale = Vector3.Lerp(spreadScale, whirlScale, t);
            visualRoot.localPosition = Vector3.Lerp(startBodyPos, braceBodyPos, t);
            yield return null;
        }

        var effectScale = CwslTankShieldVfxUtil.GetShieldEffectScale(transform.root, empowered);
        var vfxScale = effectScale * (empowered ? 0.58f : 0.42f);
        whirlwindInstance = CwslVfxSpawner.AttachShieldWhirlwind(shield, vfxScale, duration + 0.4f);

        var spinAccum = 0f;
        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            spinAccum += CwslGameConstants.TankShieldWhirlwindSpinSpeed * Time.deltaTime;
            shield.localRotation = Quaternion.Euler(0f, spinAccum, 0f) * horizontalRot;
            shield.localPosition = horizontalPos;
            shield.localScale = whirlScale;
            visualRoot.localPosition = braceBodyPos;
            yield return null;
        }

        ClearWhirlwindFx();

        var recoverPos = shieldBaseLocalPosition;
        var recoverRot = shieldBaseLocalRotation;
        var recoverScale = empowered ? startScale : shieldBaseLocalScale;

        timer = 0f;
        const float recover = 0.26f;
        while (timer < recover)
        {
            timer += Time.deltaTime;
            var t = timer / recover;
            shield.localPosition = Vector3.Lerp(horizontalPos, recoverPos, t);
            shield.localRotation = Quaternion.Slerp(shield.localRotation, recoverRot, t);
            shield.localScale = Vector3.Lerp(whirlScale, recoverScale, t);
            visualRoot.localPosition = Vector3.Lerp(braceBodyPos, startBodyPos, t);
            yield return null;
        }

        shield.localPosition = recoverPos;
        shield.localRotation = recoverRot;
        shield.localScale = recoverScale;
        visualRoot.localPosition = visualBaseLocalPosition;

        IsAnimating = false;
        routine = null;
    }

    private void ClearWhirlwindFx()
    {
        if (whirlwindInstance == null)
            return;

        Destroy(whirlwindInstance);
        whirlwindInstance = null;
    }

    private void OnDisable()
    {
        ClearWhirlwindFx();
    }
}
