using UnityEngine;

public static class CoopSlimeVisualFactory
{
    private const float DefaultColliderRadius = 0.55f;

    public static CoopEnemySlimeVisual Build(
        Transform root,
        string slimeKey,
        float moveSpeed,
        bool isBoss,
        out CapsuleCollider collider)
    {
        collider = EnsureCollider(root, isBoss);
        EnsureRigidbody(root);

        if (!CoopSlimeAssetCache.TryGetPrefab(slimeKey, out var prefab))
        {
            Debug.LogWarning($"[CoopSlimeVisualFactory] 슬라임 프리팹을 찾을 수 없습니다: {slimeKey}");
            return CreateFallbackVisual(root);
        }

        var visual = Object.Instantiate(prefab, root, false);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * ResolveScale(isBoss);

        var animator = visual.GetComponentInChildren<Animator>();
        if (animator == null)
            animator = visual.AddComponent<Animator>();

        if (CoopSlimeAssetCache.SlimeController != null)
            animator.runtimeAnimatorController = CoopSlimeAssetCache.SlimeController;

        if (CoopSlimeAssetCache.SlimeAvatar != null)
            animator.avatar = CoopSlimeAssetCache.SlimeAvatar;

        animator.applyRootMotion = false;

        var relay = visual.GetComponent<CoopSlimeAnimationRelay>();
        if (relay == null)
            relay = visual.AddComponent<CoopSlimeAnimationRelay>();

        var health = root.GetComponent<Health>();
        var actor = root.GetComponent<CoopEnemyActor>();
        var slimeVisual = root.GetComponent<CoopEnemySlimeVisual>();
        if (slimeVisual == null)
            slimeVisual = root.AddComponent<CoopEnemySlimeVisual>();

        slimeVisual.Initialize(animator, CoopSlimeAssetCache.FaceAsset, health, actor);
        relay.Bind(slimeVisual);
        MonsterGroundPlacement.AlignVisualFeetToLocalGround(visual.transform);

        DisableDemoComponents(visual);
        return slimeVisual;
    }

    private static CapsuleCollider EnsureCollider(Transform root, bool isBoss)
    {
        var collider = root.GetComponent<CapsuleCollider>();
        if (collider == null)
            collider = root.gameObject.AddComponent<CapsuleCollider>();

        var radius = DefaultColliderRadius * (isBoss ? 1.15f : 1f);
        collider.center = new Vector3(0f, radius, 0f);
        collider.radius = radius;
        collider.height = radius * 2f;
        collider.direction = 1;
        collider.isTrigger = false;
        return collider;
    }

    private static void EnsureRigidbody(Transform root)
    {
        var rigidbody = root.GetComponent<Rigidbody>();
        if (rigidbody == null)
            rigidbody = root.gameObject.AddComponent<Rigidbody>();

        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
    }

    private static float ResolveScale(bool isBoss) => isBoss ? 1.15f : 0.95f;

    private static void DisableDemoComponents(GameObject visual)
    {
        var enemyAi = visual.GetComponentInChildren<EnemyAi>();
        if (enemyAi != null)
            enemyAi.enabled = false;
    }

    public static void BuildMirrored(Transform root, string slimeKey, float moveSpeed, bool isBoss)
    {
        if (!CoopSlimeAssetCache.TryGetPrefab(slimeKey, out var prefab))
        {
            CreateFallbackVisual(root);
            return;
        }

        var visual = Object.Instantiate(prefab, root, false);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * ResolveScale(isBoss);

        var animator = visual.GetComponentInChildren<Animator>();
        if (animator == null)
            animator = visual.AddComponent<Animator>();

        if (CoopSlimeAssetCache.SlimeController != null)
            animator.runtimeAnimatorController = CoopSlimeAssetCache.SlimeController;

        if (CoopSlimeAssetCache.SlimeAvatar != null)
            animator.avatar = CoopSlimeAssetCache.SlimeAvatar;

        animator.applyRootMotion = false;

        var mirroredAnim = root.GetComponent<CoopMirroredSlimeAnim>();
        if (mirroredAnim == null)
            mirroredAnim = root.AddComponent<CoopMirroredSlimeAnim>();
        mirroredAnim.Initialize(animator, moveSpeed);

        MonsterGroundPlacement.AlignVisualFeetToLocalGround(visual.transform);
        DisableDemoComponents(visual);
    }

    private static CoopEnemySlimeVisual CreateFallbackVisual(Transform root)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(root, false);
        sphere.transform.localScale = Vector3.one * 0.9f;
        Object.Destroy(sphere.GetComponent<Collider>());

        var renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(0.4f, 0.85f, 0.45f);

        return root.GetComponent<CoopEnemySlimeVisual>();
    }
}

/// <summary>
/// Kawaii 슬라임 애니메이션 이벤트를 CoopEnemySlimeVisual로 전달합니다.
/// </summary>
public class CoopSlimeAnimationRelay : MonoBehaviour
{
    private CoopEnemySlimeVisual visual;

    public void Bind(CoopEnemySlimeVisual boundVisual) => visual = boundVisual;

    public void AlertObservers(string message) => visual?.OnAnimationEvent(message);
}

public class CoopMirroredSlimeAnim : MonoBehaviour
{
    private Animator animator;
    private float referenceSpeed;
    private Vector3 lastPosition;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    public void Initialize(Animator boundAnimator, float moveSpeed)
    {
        animator = boundAnimator;
        referenceSpeed = Mathf.Max(0.5f, moveSpeed);
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (animator == null)
            return;

        var delta = (transform.position - lastPosition).magnitude;
        var speed = Time.deltaTime > 0.0001f ? delta / Time.deltaTime : 0f;
        var normalized = referenceSpeed > 0.01f ? Mathf.Clamp01(speed / referenceSpeed) : 0f;
        animator.SetFloat(SpeedHash, normalized > 0.12f ? Mathf.Lerp(0.35f, 1f, normalized) : 0f);
        lastPosition = transform.position;
    }
}
