using System.Collections;
using UnityEngine;

/// <summary>탱커 E/R 스킬 방패 연출 — 지진 강타·회전.</summary>
public class CwslTankShieldSkillVisual : MonoBehaviour
{
    private const float VisualGroundY = 0.05f;
    private const float ShieldBottomLocalY = -0.68f;
    private const float SlamPitchDegrees = 78f;

    private Transform shield;
    private Transform visualRoot;
    private Vector3 shieldBaseLocalPosition;
    private Quaternion shieldBaseLocalRotation;
    private Vector3 shieldBaseLocalScale;
    private Vector3 visualBaseLocalPosition;
    private Coroutine routine;

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

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(SlamRoutine(empowered));
    }

    public void PlayWhirlwind(float duration, bool empowered)
    {
        CacheParts();
        if (shield == null)
            return;

        if (routine != null)
            StopCoroutine(routine);

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

    private IEnumerator SlamRoutine(bool empowered)
    {
        IsAnimating = true;

        var startPos = shield.localPosition;
        var startRot = shield.localRotation;
        var startScale = shield.localScale;
        var startBodyPos = visualRoot.localPosition;

        var slamScale = empowered
            ? startScale
            : Vector3.Scale(shieldBaseLocalScale, new Vector3(1.12f, 1.12f, 1.12f));

        var windup = CwslGameConstants.TankShieldSlamWindup;
        var raiseLift = empowered ? 0.62f : 0.42f;
        var raisedPos = startPos + new Vector3(0f, raiseLift * Mathf.Max(1f, startScale.y), -0.2f);
        var raisedRot = startRot * Quaternion.Euler(-52f, 0f, 0f);
        var raisedScale = empowered
            ? startScale
            : Vector3.Lerp(startScale, slamScale, 0.85f);

        var timer = 0f;
        while (timer < windup)
        {
            timer += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, timer / windup);
            shield.localPosition = Vector3.Lerp(startPos, raisedPos, t);
            shield.localRotation = Quaternion.Slerp(startRot, raisedRot, t);
            shield.localScale = Vector3.Lerp(startScale, raisedScale, t);
            yield return null;
        }

        ComputeGroundSlamPose(startPos, startRot, raisedScale, out var slamPos, out var slamRot);
        var slamBodyPos = startBodyPos + new Vector3(0f, empowered ? -0.06f : -0.03f, 0.16f);

        timer = 0f;
        const float slamDown = 0.16f;
        while (timer < slamDown)
        {
            timer += Time.deltaTime;
            var t = 1f - Mathf.Pow(1f - timer / slamDown, 3f);
            shield.localPosition = Vector3.Lerp(raisedPos, slamPos, t);
            shield.localRotation = Quaternion.Slerp(raisedRot, slamRot, t);
            shield.localScale = raisedScale;
            visualRoot.localPosition = Vector3.Lerp(startBodyPos, slamBodyPos, t);
            yield return null;
        }

        var hitForward = 0.55f + Mathf.Max(0f, raisedScale.x - 1f) * 0.14f;
        var hitPoint = transform.root.position + transform.root.forward * hitForward + Vector3.up * 0.08f;
        CwslVfxSpawner.SpawnMeleeHit(hitPoint, transform.root.rotation);
        CwslVfxSpawner.SpawnFortifyBlock(hitPoint + Vector3.up * 0.12f);

        var recoverPos = empowered ? startPos : shieldBaseLocalPosition;
        var recoverRot = empowered ? startRot : shieldBaseLocalRotation;
        var recoverScale = empowered ? startScale : shieldBaseLocalScale;

        timer = 0f;
        const float recover = 0.3f;
        while (timer < recover)
        {
            timer += Time.deltaTime;
            var t = timer / recover;
            shield.localPosition = Vector3.Lerp(slamPos, recoverPos, t);
            shield.localRotation = Quaternion.Slerp(slamRot, recoverRot, t);
            shield.localScale = Vector3.Lerp(raisedScale, recoverScale, t);
            visualRoot.localPosition = Vector3.Lerp(slamBodyPos, startBodyPos, t);
            yield return null;
        }

        shield.localPosition = recoverPos;
        shield.localRotation = recoverRot;
        shield.localScale = recoverScale;
        visualRoot.localPosition = startBodyPos;

        IsAnimating = false;
        routine = null;
    }

    private void ComputeGroundSlamPose(
        Vector3 referencePos,
        Quaternion referenceRot,
        Vector3 scale,
        out Vector3 slamPos,
        out Quaternion slamRot)
    {
        slamRot = referenceRot * Quaternion.Euler(SlamPitchDegrees, 0f, 0f);

        var bottomLocal = new Vector3(0f, ShieldBottomLocalY * scale.y, 0f);
        var bottomOffset = slamRot * bottomLocal;
        var slamY = VisualGroundY - bottomOffset.y;
        var forwardBonus = 0.34f + Mathf.Max(0f, scale.x - 1f) * 0.18f;

        slamPos = new Vector3(referencePos.x, slamY, referencePos.z + forwardBonus);
    }

    private IEnumerator WhirlwindRoutine(float duration, bool empowered)
    {
        IsAnimating = true;

        var startPos = shield.localPosition;
        var startRot = shield.localRotation;
        var startScale = shield.localScale;
        var startBodyPos = visualRoot.localPosition;

        var whirlScale = empowered
            ? startScale
            : Vector3.Scale(shieldBaseLocalScale, new Vector3(1.26f, 1.26f, 1.26f));

        var reach = empowered ? 1.38f : 1f;
        var scaleReach = Mathf.Max(1f, whirlScale.x);

        // 1) 방패를 쫙 펼쳐 들어 올림
        var spreadPos = startPos + new Vector3(0f, 0.18f * scaleReach, 0.42f * reach);
        var spreadRot = startRot * Quaternion.Euler(-18f, 8f, -32f);
        var spreadScale = Vector3.Lerp(startScale, whirlScale, 0.75f);

        // 2) 끝을 잡고 가로로 내밀어 든 자세
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

        // 3) 가로로 든 방패를 수평 회전 (캐릭터 본체 회전과 합쳐져 휠윈드 느낌)
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

        var recoverPos = empowered ? startPos : shieldBaseLocalPosition;
        var recoverRot = empowered ? startRot : shieldBaseLocalRotation;
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
        visualRoot.localPosition = startBodyPos;

        IsAnimating = false;
        routine = null;
    }
}
