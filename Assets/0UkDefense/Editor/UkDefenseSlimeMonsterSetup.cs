#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class UkDefenseSlimeMonsterSetup
{
    private const string SlimeControllerPath = "Assets/Kawaii Slimes/Animator/Slime.controller";
    private const string SlimeAvatarPath = "Assets/Kawaii Slimes/Animation/Slime_Anim.fbx";
    private const string FaceAssetPath = "Assets/Kawaii Slimes/Scripts/AI/DataFace.asset";

    public const string MonsterTsvPath = "Assets/0UkDefense/1Data/SheetExport/Monster.tsv";

    [MenuItem(UkDefenseSetupMenu.DataRoot + "Import Monster TSV", false, 5)]
    public static void ImportMonsterTsvFromSheetExport()
    {
        if (!File.Exists(MonsterTsvPath))
        {
            Debug.LogError($"[UkDefense] Monster.tsv 없음: {MonsterTsvPath}");
            return;
        }

        var asset = AssetDatabase.LoadAssetAtPath<MonsterDataSo>(GoogleSheetDefinitions.MonsterDataAssetPath);
        if (asset == null)
        {
            Debug.LogError("[UkDefense] MonsterDataSo 없음");
            return;
        }

        asset.SetData(GoogleSheetManager.ParseMonsterData(File.ReadAllText(MonsterTsvPath)));
        asset.RebuildLookup();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        UkDefenseBossDataSetup.ImportAllBossTsvsFromSheetExport();
        DefenseCombatResourceSync.SyncMonsterModelAddressables();
        Debug.Log($"[UkDefense] Monster.tsv → MonsterDataSo 적용 ({asset.list.Count} monsters)");
    }

    [MenuItem(UkDefenseSetupMenu.Root + "Monsters/Rebuild Slime Monster Prefabs", false, 40)]
    public static void RebuildSlimeMonsterPrefabsFromMenu()
    {
        UkDefenseBossDataSetup.EnsureBossDataAssets();
        UkDefenseBossDataSetup.ImportAllBossTsvsFromSheetExport();
        int built = RebuildGroundMonsterPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Slime Monsters",
            $"Kawaii Slimes 모델로 지상 몬스터 {built}개 프리팹을 재생성했습니다.\n" +
            "플레이 모드를 다시 시작해 주세요.",
            "확인");
    }

    public static int RebuildGroundMonsterPrefabs()
    {
        var monsterSo = AssetDatabase.LoadAssetAtPath<MonsterDataSo>(GoogleSheetDefinitions.MonsterDataAssetPath);
        if (monsterSo == null)
        {
            Debug.LogError("[UkDefenseSlimeMonsterSetup] MonsterDataSo가 없습니다.");
            return 0;
        }

        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SlimeControllerPath);
        var faceAsset = AssetDatabase.LoadAssetAtPath<Face>(FaceAssetPath);
        var avatar = LoadSlimeAvatar();
        if (controller == null || avatar == null)
        {
            Debug.LogError("[UkDefenseSlimeMonsterSetup] Slime Animator/Avatar 로드 실패");
            return 0;
        }

        int built = 0;

        for (int i = 0; i < monsterSo.list.Count; i++)
        {
            var monsterData = monsterSo.list[i];
            if (monsterData == null || string.IsNullOrWhiteSpace(monsterData.code))
                continue;

            if (monsterData.code.StartsWith("MS"))
                continue;

            var prefab = BuildMonsterPrefab(monsterData, controller, avatar, faceAsset);
            if (prefab == null)
                continue;

            built++;
        }

        UkDefenseMonsterPrefabValidator.FixAllMonsterPrefabs();
        DefenseCombatResourceSync.SyncMonsterModelAddressables();
        return built;
    }

    private static GameObject BuildMonsterPrefab(
        MonsterData monsterData,
        RuntimeAnimatorController controller,
        Avatar avatar,
        Face faceAsset)
    {
        var monsterCode = monsterData.code;
        bool isSuicide = monsterData.IsSuicideAttacker;
        float colliderRadius = MonsterVisualUtility.GetColliderRadius(monsterData);

        var root = new GameObject(monsterCode);
        root.tag = "Enemy";
        root.transform.localScale = Vector3.one * 0.9f;

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

        if (isSuicide)
            root.AddComponent<SuicideMonster>();
        else
            root.AddComponent<MeleeMonster>();

        var healthBar = root.AddComponent<HealthBarUI>();
        healthBar.ConfigureAsEnemy();

        var combatVfx = root.AddComponent<UnitCombatVFX>();
        var catalog = DefenseCombatCatalogFactory.EnsureCatalogAsset();
        combatVfx.ConfigureDeathEffect(catalog.defaultDeathEffectPrefab);

        var hitFlash = root.AddComponent<CombatHitFlash>();
        root.AddComponent<PooledEnemy>();
        root.AddComponent<HealthDamagePopupBridge>();

        if (!MonsterEditorPrefabLoader.TryLoadModelPrefab(monsterData.prefabKey, out var slimeSource))
        {
            Debug.LogWarning($"[UkDefenseSlimeMonsterSetup] 모델 프리팹 없음: {monsterData.prefabKey} ({monsterCode})");
            Object.DestroyImmediate(root);
            return null;
        }

        var visual = (GameObject)PrefabUtility.InstantiatePrefab(slimeSource, root.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        var animator = visual.GetComponent<Animator>();
        if (animator == null)
            animator = visual.AddComponent<Animator>();

        animator.runtimeAnimatorController = controller;
        animator.avatar = avatar;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

        if (visual.GetComponent<MonsterSlimeAnimationRelay>() == null)
            visual.AddComponent<MonsterSlimeAnimationRelay>();

        var slimeVisual = root.AddComponent<MonsterSlimeVisual>();
        slimeVisual.BindRuntimeVisual(visual.transform, animator, faceAsset);
        hitFlash.BindVisualRoot(visual.transform);
        MonsterGroundPlacement.AlignVisualFeetToLocalGround(visual.transform);

        if (monsterData.IsBoss)
            root.AddComponent<BossCombatProfile>();

        var path = $"{UkDefenseSetupMenu.MonsterPrefabDir}/{monsterCode}.prefab";
        EnsureFolder(UkDefenseSetupMenu.MonsterPrefabDir);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static Avatar LoadSlimeAvatar()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(SlimeAvatarPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Avatar avatar)
                return avatar;
        }

        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parts = path.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
