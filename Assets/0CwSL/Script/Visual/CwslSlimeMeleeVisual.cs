using UnityEngine;

/// <summary>슬라임 근접 몬스터 — 걷기 Speed 연동, 루트 모션 없이 헤드박치 공격.</summary>
public class CwslSlimeMeleeVisual : MonoBehaviour
{
    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int AttackId = Animator.StringToHash("Attack");
    private static readonly int JumpId = Animator.StringToHash("Jump");

    private Animator animator;
    private Transform modelRoot;
    private CwslMeleeHeadbuttVisual headbutt;
    private CwslMonsterBase monster;

    private Vector3 modelBaseLocalPosition;
    private Vector3 modelBaseLocalScale;
    private float hopPhase;

    private void Awake()
    {
        EnsureAnimatorSetup();
    }

    public void Configure(Animator slimeAnimator, Transform model)
    {
        animator = slimeAnimator;
        modelRoot = model;
        EnsureAnimatorSetup();

        if (modelRoot != null)
        {
            modelBaseLocalPosition = modelRoot.localPosition;
            modelBaseLocalScale = modelRoot.localScale;
            headbutt = modelRoot.GetComponent<CwslMeleeHeadbuttVisual>();
            if (headbutt == null)
                headbutt = modelRoot.gameObject.AddComponent<CwslMeleeHeadbuttVisual>();
        }
    }

    private void EnsureAnimatorSetup()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            return;

        animator.applyRootMotion = false;
        if (animator.GetComponent<CwslSlimeAnimationEventReceiver>() == null)
            animator.gameObject.AddComponent<CwslSlimeAnimationEventReceiver>();
    }

    private void LateUpdate()
    {
        if (animator == null)
            return;

        if (monster == null)
            monster = GetComponentInParent<CwslMonsterBase>();

        if (monster == null)
        {
            animator.SetFloat(SpeedId, 0f);
            animator.SetBool(JumpId, false);
            ResetModelPose();
            return;
        }

        var isMoving = monster.LastWalkSpeed > 0.04f;
        animator.SetFloat(SpeedId, 0f);
        animator.SetBool(JumpId, false);

        if (!isMoving || modelRoot == null)
        {
            ResetModelPose();
            return;
        }

        var speedNorm = Mathf.Clamp01(monster.LastWalkSpeed / 5.5f);
        hopPhase += Time.deltaTime * monster.LastWalkSpeed * 1.15f;
        var hop = Mathf.Sin(hopPhase) * Mathf.Lerp(0.04f, 0.11f, speedNorm);
        if (hop < 0f)
            hop *= 0.35f;

        modelRoot.localPosition = modelBaseLocalPosition + Vector3.up * hop;
        var squash = 1f - Mathf.Abs(hop) * 0.9f;
        modelRoot.localScale = new Vector3(
            modelBaseLocalScale.x * (1f + Mathf.Abs(hop) * 0.25f),
            modelBaseLocalScale.y * squash,
            modelBaseLocalScale.z * (1f + Mathf.Abs(hop) * 0.25f));
    }

    private void ResetModelPose()
    {
        if (modelRoot == null)
            return;

        modelRoot.localPosition = modelBaseLocalPosition;
        modelRoot.localScale = modelBaseLocalScale;
        hopPhase = 0f;
    }

    public void PlayWindup()
    {
        headbutt?.PlayWindup();
    }

    public void PlayHit()
    {
        headbutt?.PlayHit();
        animator?.SetTrigger(AttackId);
    }
}
