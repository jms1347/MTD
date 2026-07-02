using UnityEngine;

/// <summary>
/// Monster 테이블 + Addressables 모델(prefabKey)로 전투 몬스터 풀 템플릿을 런타임 조립합니다.
/// </summary>
public static class MonsterRuntimeFactory
{
    public static bool TryCreatePoolTemplate(
        MonsterData monsterData,
        GameObject modelPrefab,
        Transform poolRoot,
        RuntimeAnimatorController animatorController,
        Face faceAsset,
        Avatar avatar,
        out GameObject template)
    {
        template = null;
        if (monsterData == null || modelPrefab == null || poolRoot == null)
            return false;

        var root = new GameObject($"{monsterData.code}_Template");
        root.transform.SetParent(poolRoot, false);
        root.tag = "Enemy";

        float colliderRadius = MonsterVisualUtility.GetColliderRadius(monsterData);
        var capsule = root.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, colliderRadius, 0f);
        capsule.radius = colliderRadius;
        capsule.height = colliderRadius * 2f;
        capsule.direction = 1;

        var rigidbody = root.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;

        root.AddComponent<Health>();
        root.AddComponent<Monster>();
        root.AddComponent<MonsterStatusController>();
        root.AddComponent<MonsterStatusOverlayUI>();
        root.AddComponent<GroundMonster>();
        root.AddComponent<UnitGridNavigator>();

        if (monsterData.IsSuicideAttacker)
            root.AddComponent<SuicideMonster>();
        else
            root.AddComponent<MeleeMonster>();

        var healthBar = root.AddComponent<HealthBarUI>();
        healthBar.ConfigureAsEnemy();

        root.AddComponent<UnitCombatVFX>();
        var hitFlash = root.AddComponent<CombatHitFlash>();
        root.AddComponent<PooledEnemy>();
        root.AddComponent<HealthDamagePopupBridge>();

        if (monsterData.IsBoss)
            root.AddComponent<BossCombatProfile>();

        var visual = Object.Instantiate(modelPrefab, root.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        var animator = visual.GetComponent<Animator>();
        if (animator == null)
            animator = visual.AddComponent<Animator>();

        if (animatorController != null)
            animator.runtimeAnimatorController = animatorController;

        if (avatar != null)
            animator.avatar = avatar;

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

        if (visual.GetComponent<MonsterSlimeAnimationRelay>() == null)
            visual.AddComponent<MonsterSlimeAnimationRelay>();

        var slimeVisual = root.AddComponent<MonsterSlimeVisual>();
        slimeVisual.BindRuntimeVisual(visual.transform, animator, faceAsset);
        hitFlash.BindVisualRoot(visual.transform);
        MonsterGroundPlacement.AlignVisualFeetToLocalGround(visual.transform);

        template = root;
        template.SetActive(false);
        return true;
    }
}
