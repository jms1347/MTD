using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class CwslSlimeMeleeVisualBuilder
{
    private const float MeleeModelScale = 1.85f;
    private const float NexusMeleeModelScale = 3f;

    public static void Build(Transform root, CwslMonsterType type)
    {
        var isViking = type == CwslMonsterType.NexusMelee;
        var modelPrefab = ResolveModelPrefab(isViking);
        if (modelPrefab == null)
        {
            CwslMonsterVisualBuilder.BuildJugglerFallback(root, CwslMonsterVisualPalette.GetPalette(type));
            return;
        }

        var model = Object.Instantiate(modelPrefab, root, false);
        model.name = isViking ? "SlimeViking" : "Slime";
        var scale = isViking ? NexusMeleeModelScale : MeleeModelScale;
        model.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one * scale;

        StripForeignComponents(model);
        DisableModelColliders(model);

        var animator = model.GetComponent<Animator>();
        if (animator == null)
            animator = model.AddComponent<Animator>();

        var controller = ResolveAnimatorController();
        if (controller != null)
            animator.runtimeAnimatorController = controller;

        animator.applyRootMotion = false;

        if (model.GetComponent<CwslSlimeAnimationEventReceiver>() == null)
            model.AddComponent<CwslSlimeAnimationEventReceiver>();

        var driver = root.gameObject.AddComponent<CwslSlimeMeleeVisual>();
        driver.Configure(animator, model.transform);

        var legacyLunge = root.GetComponent<CwslMeleeLungeVisual>();
        if (legacyLunge != null)
        {
            if (Application.isPlaying)
                Object.Destroy(legacyLunge);
            else
                Object.DestroyImmediate(legacyLunge);
        }
    }

    private static GameObject ResolveModelPrefab(bool viking)
    {
        var assets = CwslGameSession.Instance?.Assets;
        if (assets != null)
        {
            if (viking && assets.slimeNexusMeleeModelPrefab != null)
                return assets.slimeNexusMeleeModelPrefab;
            if (!viking && assets.slimeMeleeModelPrefab != null)
                return assets.slimeMeleeModelPrefab;
        }

#if UNITY_EDITOR
        var path = viking ? CwslSlimeAssetPaths.SlimeViking : CwslSlimeAssetPaths.Slime01;
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
        return null;
#endif
    }

    private static RuntimeAnimatorController ResolveAnimatorController()
    {
        var assets = CwslGameSession.Instance?.Assets;
        if (assets != null && assets.slimeAnimatorController != null)
            return assets.slimeAnimatorController;

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CwslSlimeAssetPaths.AnimatorController);
#else
        return null;
#endif
    }

    private static void StripForeignComponents(GameObject model)
    {
        foreach (var ai in model.GetComponentsInChildren<EnemyAi>(true))
        {
            if (Application.isPlaying)
                Object.Destroy(ai);
            else
                Object.DestroyImmediate(ai);
        }

        foreach (var agent in model.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))
        {
            if (Application.isPlaying)
                Object.Destroy(agent);
            else
                Object.DestroyImmediate(agent);
        }
    }

    private static void DisableModelColliders(GameObject model)
    {
        foreach (var collider in model.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
    }
}
