using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kawaii Slimes 스킨드 메시 이동·피격·공격 애니메이션을 UkDefense 몬스터에 연결합니다.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public class MonsterSlimeVisual : MonoBehaviour
{
    private struct BonePose
    {
        public Transform transform;
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int DamageHash = Animator.StringToHash("Damage");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DamageTypeHash = Animator.StringToHash("DamageType");

    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Face faceAsset;
    [SerializeField] private float modelLocalScale = 1f;

    private Health health;
    private Monster monster;
    private Material faceMaterial;
    private Vector3 lastPosition;
    private readonly List<BonePose> suppressedBones = new();
    private Vector3 visualDefaultLocalPosition;
    private bool isPlayingHit;
    private bool isPlayingAttack;
    private bool isDeadVisual;
    private Vector3 deathWorldPosition;
    private Coroutine hitResetRoutine;
    private Coroutine attackResetRoutine;

    public Transform VisualRoot => visualRoot;
    public Animator Animator => animator;

    private void Awake()
    {
        monster = GetComponent<Monster>();
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDamaged -= OnDamaged;
            health.OnDamaged += OnDamaged;
            health.OnDeath -= OnDeathVisual;
            health.OnDeath += OnDeathVisual;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= OnDamaged;
            health.OnDeath -= OnDeathVisual;
        }
    }

    public void ApplyForSpawn(MonsterData monsterData)
    {
        if (visualRoot != null)
        {
            visualRoot.localScale = Vector3.one;
            visualRoot.localPosition = visualDefaultLocalPosition;
        }

        CacheAnimationRootDefaults();
        SuppressAnimatedRootMotion();

        CacheFaceMaterial();
        SetFaceTexture(faceAsset != null ? faceAsset.Idleface : null);
        lastPosition = transform.position;
        isPlayingHit = false;
        isPlayingAttack = false;
        isDeadVisual = false;
        GetComponent<CombatHitFlash>()?.ClearForSpawn();

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.Rebind();
            animator.Update(0f);
            SuppressAnimatedRootMotion();
            animator.SetFloat(SpeedHash, 0f);
        }
    }

    public void BindRuntimeVisual(Transform visual, Animator boundAnimator, Face faces)
    {
        visualRoot = visual;
        animator = boundAnimator;
        faceAsset = faces;
        visualDefaultLocalPosition = visual != null ? visual.localPosition : Vector3.zero;
        CacheFaceMaterial();
        CacheAnimationRootDefaults();
    }

    private void Update()
    {
        UpdateLocomotionAnimation();
    }

    private void LateUpdate()
    {
        SuppressAnimatedRootMotion();

        if (isDeadVisual)
            LockDeathPosition();
    }

    private void UpdateLocomotionAnimation()
    {
        if (animator == null || monster == null || !monster.IsLanded)
            return;

        if (health != null && !health.IsAlive)
            return;

        if (isDeadVisual)
            return;

        if (isPlayingHit || isPlayingAttack)
            return;

        float delta = (transform.position - lastPosition).magnitude;
        float speed = Time.deltaTime > 0.0001f ? delta / Time.deltaTime : 0f;
        float normalized = monster.MoveSpeed > 0.01f ? Mathf.Clamp01(speed / monster.MoveSpeed) : 0f;
        animator.SetFloat(SpeedHash, normalized > 0.12f ? Mathf.Lerp(0.35f, 1f, normalized) : 0f);
        lastPosition = transform.position;

        if (normalized > 0.12f)
            SetFaceTexture(faceAsset != null ? faceAsset.WalkFace : null);
        else
            SetFaceTexture(faceAsset != null ? faceAsset.Idleface : null);
    }

    public void PlayAttack()
    {
        if (animator == null)
            return;

        isPlayingAttack = true;
        SetFaceTexture(faceAsset != null ? faceAsset.attackFace : null);
        animator.SetTrigger(AttackHash);

        if (attackResetRoutine != null)
            StopCoroutine(attackResetRoutine);
        attackResetRoutine = StartCoroutine(ResetAttackAfter(0.55f));
    }

    private void OnDamaged(float amount)
    {
        if (amount <= 0f || (health != null && !health.IsAlive))
            return;

        PlayHit();
    }

    private void OnDeathVisual()
    {
        isDeadVisual = true;
        deathWorldPosition = transform.position;
        if (monster != null)
            deathWorldPosition.y = monster.GroundY;

        monster?.InterruptForStun();
        LockDeathPosition();
        SuppressAnimatedRootMotion();

        if (animator != null)
        {
            animator.SetFloat(SpeedHash, 0f);
            if (!isPlayingHit)
                PlayHit();
        }

        if (hitResetRoutine != null)
            StopCoroutine(hitResetRoutine);
        hitResetRoutine = StartCoroutine(ResetHitAfter(0.85f));
    }

    private void LockDeathPosition()
    {
        transform.position = deathWorldPosition;
    }

    /// <summary>
    /// Walk/Damage 클립에 baked된 휴머노이드 루트(Bone/Hips) 이동을 gameplay transform과 분리합니다.
    /// </summary>
    private void SuppressAnimatedRootMotion()
    {
        for (int i = 0; i < suppressedBones.Count; i++)
        {
            var pose = suppressedBones[i];
            if (pose.transform == null)
                continue;

            pose.transform.localPosition = pose.localPosition;
            pose.transform.localRotation = pose.localRotation;
        }

        if (visualRoot != null)
            visualRoot.localPosition = visualDefaultLocalPosition;
    }

    private void CacheAnimationRootDefaults()
    {
        suppressedBones.Clear();
        if (visualRoot == null)
            return;

        if (animator != null && animator.isHuman)
            TryAddSuppressedBone(animator.GetBoneTransform(HumanBodyBones.Hips));

        TryAddSuppressedBone(FindChildRecursive(visualRoot, "Bone"));
        TryAddSuppressedBone(FindChildRecursive(visualRoot, "Rig"));
    }

    private void TryAddSuppressedBone(Transform bone)
    {
        if (bone == null)
            return;

        for (int i = 0; i < suppressedBones.Count; i++)
        {
            if (suppressedBones[i].transform == bone)
                return;
        }

        suppressedBones.Add(new BonePose
        {
            transform = bone,
            localPosition = bone.localPosition,
            localRotation = bone.localRotation
        });
    }

    public void PlayHit()
    {
        if (animator == null || isPlayingHit)
            return;

        isPlayingHit = true;
        SetFaceTexture(faceAsset != null ? faceAsset.damageFace : null);
        animator.SetInteger(DamageTypeHash, Random.Range(0, 3));
        animator.SetTrigger(DamageHash);
        SuppressAnimatedRootMotion();

        if (hitResetRoutine != null)
            StopCoroutine(hitResetRoutine);
        hitResetRoutine = StartCoroutine(ResetHitAfter(isDeadVisual ? 0.85f : 0.5f));
    }

    public void OnAnimationEvent(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        if (message == "AnimationDamageEnded")
            isPlayingHit = false;

        if (message == "AnimationAttackEnded")
            isPlayingAttack = false;
    }

    private IEnumerator ResetHitAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        isPlayingHit = false;
        hitResetRoutine = null;
    }

    private IEnumerator ResetAttackAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        isPlayingAttack = false;
        attackResetRoutine = null;
    }

    private void CacheFaceMaterial()
    {
        faceMaterial = null;
        if (visualRoot == null)
            return;

        var smileBody = visualRoot.Find("Smile_body");
        if (smileBody == null)
            smileBody = FindChildRecursive(visualRoot, "Smile_body");

        if (smileBody == null)
            return;

        var renderer = smileBody.GetComponent<Renderer>();
        if (renderer != null && renderer.materials.Length > 1)
            faceMaterial = renderer.materials[1];
    }

    private void SetFaceTexture(Texture texture)
    {
        if (faceMaterial == null || texture == null)
            return;

        faceMaterial.SetTexture("_MainTex", texture);
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            var nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
