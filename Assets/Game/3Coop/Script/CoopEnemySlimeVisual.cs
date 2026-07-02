using System.Collections;
using UnityEngine;

/// <summary>
/// 협동 적 슬라임 애니메이션 — MonsterSlimeVisual과 동일한 Kawaii Slimes 연출.
/// </summary>
public class CoopEnemySlimeVisual : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int DamageHash = Animator.StringToHash("Damage");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DamageTypeHash = Animator.StringToHash("DamageType");

    private Animator animator;
    private Face faceAsset;
    private Health health;
    private CoopEnemyActor enemyActor;
    private Material faceMaterial;
    private Vector3 lastPosition;
    private bool isPlayingHit;
    private bool isPlayingAttack;
    private bool isDeadVisual;
    private Coroutine hitResetRoutine;
    private Coroutine attackResetRoutine;

    public void Initialize(Animator boundAnimator, Face faces, Health boundHealth, CoopEnemyActor actor)
    {
        animator = boundAnimator;
        faceAsset = faces;
        health = boundHealth;
        enemyActor = actor;
        lastPosition = transform.position;
        CacheFaceMaterial();
        SetFaceTexture(faceAsset != null ? faceAsset.Idleface : null);

        if (health != null)
        {
            health.OnDamaged -= OnDamaged;
            health.OnDamaged += OnDamaged;
            health.OnDeath -= OnDeathVisual;
            health.OnDeath += OnDeathVisual;
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.SetFloat(SpeedHash, 0f);
        }
    }

    private void OnDisable()
    {
        if (health == null)
            return;

        health.OnDamaged -= OnDamaged;
        health.OnDeath -= OnDeathVisual;
    }

    private void Update()
    {
        if (animator == null || enemyActor == null || isDeadVisual)
            return;

        if (health != null && !health.IsAlive)
            return;

        if (isPlayingHit || isPlayingAttack)
            return;

        var delta = (transform.position - lastPosition).magnitude;
        var speed = Time.deltaTime > 0.0001f ? delta / Time.deltaTime : 0f;
        var moveSpeed = enemyActor.MoveSpeed;
        var normalized = moveSpeed > 0.01f ? Mathf.Clamp01(speed / moveSpeed) : 0f;
        animator.SetFloat(SpeedHash, normalized > 0.12f ? Mathf.Lerp(0.35f, 1f, normalized) : 0f);
        lastPosition = transform.position;

        if (normalized > 0.12f)
            SetFaceTexture(faceAsset != null ? faceAsset.WalkFace : null);
        else
            SetFaceTexture(faceAsset != null ? faceAsset.Idleface : null);
    }

    public void PlayAttack()
    {
        if (animator == null || isDeadVisual)
            return;

        isPlayingAttack = true;
        SetFaceTexture(faceAsset != null ? faceAsset.attackFace : null);
        animator.SetTrigger(AttackHash);

        if (attackResetRoutine != null)
            StopCoroutine(attackResetRoutine);
        attackResetRoutine = StartCoroutine(ResetAttackAfter(0.55f));
    }

    public void OnAnimationEvent(string message)
    {
        if (message == "AnimationDamageEnded")
            isPlayingHit = false;
        if (message == "AnimationAttackEnded")
            isPlayingAttack = false;
    }

    private void OnDamaged(float amount)
    {
        if (amount <= 0f || isDeadVisual)
            return;

        PlayHit();
    }

    private void OnDeathVisual()
    {
        isDeadVisual = true;
        if (animator != null)
        {
            animator.SetFloat(SpeedHash, 0f);
            if (!isPlayingHit)
                PlayHit();
        }
    }

    private void PlayHit()
    {
        if (animator == null || isPlayingHit)
            return;

        isPlayingHit = true;
        SetFaceTexture(faceAsset != null ? faceAsset.damageFace : null);
        animator.SetInteger(DamageTypeHash, Random.Range(0, 3));
        animator.SetTrigger(DamageHash);

        if (hitResetRoutine != null)
            StopCoroutine(hitResetRoutine);
        hitResetRoutine = StartCoroutine(ResetHitAfter(isDeadVisual ? 0.85f : 0.5f));
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
        var smileBody = transform.Find("Visual/Smile_body");
        if (smileBody == null)
            smileBody = FindChildRecursive(transform, "Smile_body");

        if (smileBody == null)
            return;

        var renderer = smileBody.GetComponent<Renderer>();
        if (renderer != null && renderer.materials.Length > 1)
            faceMaterial = renderer.materials[1];
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        for (var i = 0; i < parent.childCount; i++)
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

    private void SetFaceTexture(Texture texture)
    {
        if (faceMaterial == null || texture == null)
            return;

        faceMaterial.SetTexture("_MainTex", texture);
    }
}
