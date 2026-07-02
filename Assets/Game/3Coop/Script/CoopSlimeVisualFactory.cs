using UnityEngine;

public static class CoopSlimeVisualFactory
{
    private const float DefaultColliderRadius = 0.55f;
    private const float ModelScaleMultiplier = 10f / 3f;
    public static float MonsterScale => ModelScaleMultiplier;

    public static CoopEnemySlimeVisual Build(
        Transform root,
        string slimeKey,
        CoopEnemyArchetype archetype,
        float moveSpeed,
        bool isBoss,
        out CapsuleCollider collider)
    {
        collider = EnsureCollider(root, archetype, isBoss);
        EnsureRigidbody(root);

        if (!CoopSlimeAssetCache.TryGetPrefab(slimeKey, out var prefab))
        {
            Debug.LogWarning($"[CoopSlimeVisualFactory] 슬라임 프리팹을 찾을 수 없습니다: {slimeKey}");
            return CreateFallbackVisual(root, archetype);
        }

        var visual = Object.Instantiate(prefab, root, false);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * ResolveScale(archetype, isBoss);
        ApplyArchetypeTint(visual, archetype);

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
            slimeVisual = root.gameObject.AddComponent<CoopEnemySlimeVisual>();

        slimeVisual.Initialize(animator, CoopSlimeAssetCache.FaceAsset, health, actor);
        relay.Bind(slimeVisual);
        MonsterGroundPlacement.AlignVisualFeetToLocalGround(visual.transform);

        if (visual.GetComponent<CoopSlimeRootMotionSuppressor>() == null)
            visual.AddComponent<CoopSlimeRootMotionSuppressor>();

        DisableDemoComponents(visual);
        return slimeVisual;
    }

    public static void BuildMirrored(
        Transform root,
        string slimeKey,
        string archetypeId,
        float moveSpeed,
        bool isBoss)
    {
        if (!CoopEnemyArchetypeUtil.TryParse(archetypeId, out var archetype))
            archetype = CoopEnemyArchetype.Grunt;

        if (!CoopSlimeAssetCache.TryGetPrefab(slimeKey, out var prefab))
        {
            CreateFallbackVisual(root, archetype);
            return;
        }

        var visual = Object.Instantiate(prefab, root, false);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * ResolveScale(archetype, isBoss);
        ApplyArchetypeTint(visual, archetype);

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
            mirroredAnim = root.gameObject.AddComponent<CoopMirroredSlimeAnim>();
        mirroredAnim.Initialize(animator, moveSpeed);

        MonsterGroundPlacement.AlignVisualFeetToLocalGround(visual.transform);

        if (visual.GetComponent<CoopSlimeRootMotionSuppressor>() == null)
            visual.AddComponent<CoopSlimeRootMotionSuppressor>();

        DisableDemoComponents(visual);
    }

    private static CapsuleCollider EnsureCollider(Transform root, CoopEnemyArchetype archetype, bool isBoss)
    {
        var collider = root.GetComponent<CapsuleCollider>();
        if (collider == null)
            collider = root.gameObject.AddComponent<CapsuleCollider>();

        var radius = DefaultColliderRadius * ModelScaleMultiplier * ResolveColliderScale(archetype, isBoss);
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

    private static float ResolveScale(CoopEnemyArchetype archetype, bool isBoss)
    {
        var scale = archetype switch
        {
            CoopEnemyArchetype.Rusher => 0.8f,
            CoopEnemyArchetype.Tank => 1.28f,
            CoopEnemyArchetype.Bomber => 0.92f,
            CoopEnemyArchetype.Missile => 0.68f,
            CoopEnemyArchetype.HeavyBomber => 1.12f,
            _ => 0.95f
        };

        if (isBoss)
            scale *= 1.12f;

        return scale * ModelScaleMultiplier;
    }

    private static float ResolveColliderScale(CoopEnemyArchetype archetype, bool isBoss)
    {
        var scale = archetype switch
        {
            CoopEnemyArchetype.Rusher => 0.88f,
            CoopEnemyArchetype.Tank => 1.2f,
            CoopEnemyArchetype.Missile => 0.72f,
            CoopEnemyArchetype.HeavyBomber => 1.08f,
            _ => 1f
        };

        if (isBoss)
            scale *= 1.12f;

        return scale;
    }

    private static void ApplyArchetypeTint(GameObject visual, CoopEnemyArchetype archetype)
    {
        if (!archetype.IsSuicide())
            return;

        var tint = archetype switch
        {
            CoopEnemyArchetype.Missile => new Color(1f, 0.45f, 0.2f),
            CoopEnemyArchetype.HeavyBomber => new Color(0.85f, 0.2f, 0.15f),
            _ => new Color(1f, 0.7f, 0.25f)
        };

        var renderers = visual.GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || renderer.name.Contains("Smile"))
                continue;

            var material = renderer.material;
            material.color = Color.Lerp(material.color, tint, 0.35f);
        }
    }

    private static void DisableDemoComponents(GameObject visual)
    {
        var enemyAi = visual.GetComponentInChildren<EnemyAi>();
        if (enemyAi != null)
            enemyAi.enabled = false;
    }

    private static CoopEnemySlimeVisual CreateFallbackVisual(Transform root, CoopEnemyArchetype archetype)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(root, false);
        sphere.transform.localScale = Vector3.one * ResolveScale(archetype, false);
        Object.Destroy(sphere.GetComponent<Collider>());

        var renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = archetype switch
            {
                CoopEnemyArchetype.Rusher => new Color(0.95f, 0.45f, 0.2f),
                CoopEnemyArchetype.Tank => new Color(0.45f, 0.45f, 0.55f),
                CoopEnemyArchetype.Bomber => new Color(1f, 0.55f, 0.15f),
                CoopEnemyArchetype.Missile => new Color(1f, 0.3f, 0.15f),
                CoopEnemyArchetype.HeavyBomber => new Color(0.75f, 0.15f, 0.1f),
                _ => new Color(0.4f, 0.85f, 0.45f)
            };
        }

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
