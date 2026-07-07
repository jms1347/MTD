using UnityEngine;

/// <summary>몬스터 이동 시 다리 스윙 + 몸통 바운스 (방패 캐릭터와 동일 원리).</summary>
public class CwslMonsterLegWalkVisual : MonoBehaviour
{
    private const float StopSpeedThreshold = 0.035f;
    private const float MaxLegSwing = 46f;
    private const float WalkPhaseSpeed = 1.85f;
    private const float BodyBobAmount = 0.035f;

    private Transform legL;
    private Transform legR;
    private Transform legBL;
    private Transform legBR;
    private bool quadruped;
    private CwslMonsterBase monster;
    private Vector3 lastTrackPosition;
    private Vector3 baseLocalPosition;
    private float walkPhase;
    private float smoothedSpeed;

    private void Awake()
    {
        CacheRefs();
    }

    private void OnEnable()
    {
        CacheRefs();
        baseLocalPosition = transform.localPosition;
        lastTrackPosition = GetTrackPosition();
        smoothedSpeed = 0f;
        walkPhase = 0f;
        ResetPose();
    }

    private void LateUpdate()
    {
        if (legL == null || legR == null)
        {
            CacheRefs();
            if (legL == null || legR == null)
                return;
        }

        var speed = ResolveSpeed();
        if (speed < StopSpeedThreshold)
        {
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, 0f, Time.deltaTime * 14f);
            if (smoothedSpeed < StopSpeedThreshold)
            {
                ResetPose();
                return;
            }
        }

        walkPhase += Time.deltaTime * smoothedSpeed * WalkPhaseSpeed;
        var stride = Mathf.Clamp01(smoothedSpeed / 5.5f);
        var swing = Mathf.Sin(walkPhase) * stride * MaxLegSwing;
        legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
        legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);

        if (quadruped)
        {
            var backSwing = Mathf.Sin(walkPhase + Mathf.PI) * stride * MaxLegSwing;
            legBL.localRotation = Quaternion.Euler(backSwing, 0f, 0f);
            legBR.localRotation = Quaternion.Euler(-backSwing, 0f, 0f);
        }

        var bob = Mathf.Abs(Mathf.Sin(walkPhase)) * stride * BodyBobAmount;
        transform.localPosition = baseLocalPosition + Vector3.up * bob;
    }

    private void CacheRefs()
    {
        legL = transform.Find("LegL");
        legR = transform.Find("LegR");
        legBL = transform.Find("LegBL");
        legBR = transform.Find("LegBR");
        quadruped = legBL != null && legBR != null;
        if (monster == null)
            monster = GetComponentInParent<CwslMonsterBase>();
    }

    private Vector3 GetTrackPosition()
    {
        return monster != null ? monster.transform.position : transform.root.position;
    }

    private float ResolveSpeed()
    {
        var trackPosition = GetTrackPosition();
        var flatDelta = trackPosition - lastTrackPosition;
        flatDelta.y = 0f;
        lastTrackPosition = trackPosition;

        var instant = Time.deltaTime > 0.0001f ? flatDelta.magnitude / Time.deltaTime : 0f;
        if (monster != null && monster.LastWalkSpeed > instant)
            instant = monster.LastWalkSpeed;

        smoothedSpeed = Mathf.Lerp(smoothedSpeed, instant, Time.deltaTime * 16f);
        return smoothedSpeed;
    }

    private void ResetPose()
    {
        if (legL != null)
            legL.localRotation = Quaternion.identity;
        if (legR != null)
            legR.localRotation = Quaternion.identity;
        if (legBL != null)
            legBL.localRotation = Quaternion.identity;
        if (legBR != null)
            legBR.localRotation = Quaternion.identity;
        transform.localPosition = baseLocalPosition;
    }
}
