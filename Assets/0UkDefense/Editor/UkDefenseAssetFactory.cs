#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class UkDefenseAssetFactory
{
    private static readonly (string code, Color color)[] MonsterDefinitions =
    {
        ("MG-0001", new Color(0.12f, 0.18f, 0.58f)),
        ("MG-0002", new Color(0.18f, 0.55f, 0.28f)),
        ("MG-0003", new Color(0.92f, 0.15f, 0.12f)),
        ("MS-0001", new Color(0.55f, 0.35f, 0.82f)),
        ("MS-0002", new Color(0.45f, 0.62f, 0.95f)),
        ("MS-0003", new Color(0.78f, 0.22f, 0.55f)),
    };

    public static void EnsureDataScriptableObjects()
    {
        EnsureFolder("Assets/0UkDefense/1Data/SO");
        EnsureAsset<MonsterDataSo>(UkDefenseSetupMenu.SoDir + "/MonsterDataSo.asset");
        EnsureAsset<TowerDataSo>(UkDefenseSetupMenu.SoDir + "/TowerDataSo.asset");
        EnsureAsset<DefenseSkillDataSo>(UkDefenseSetupMenu.SoDir + "/DefenseSkillDataSo.asset");
        EnsureAsset<DefenseEffectDataSo>(UkDefenseSetupMenu.SoDir + "/DefenseEffectDataSo.asset");
        EnsureAsset<DefenseEffectGroupDataSo>(UkDefenseSetupMenu.SoDir + "/DefenseEffectGroupDataSo.asset");
        EnsureAsset<BossDataSo>(GoogleSheetDefinitions.BossDataAssetPath);
        EnsureAsset<BossElementGroupDataSo>(GoogleSheetDefinitions.BossElementGroupDataAssetPath);
        EnsureAsset<StageDataSo>(UkDefenseSetupMenu.SoDir + "/StageDataSo.asset");
        EnsureAsset<DefenseAddressableKeyDataSo>(GoogleSheetDefinitions.AddressableKeyDataAssetPath);
        EnsureAsset<RoguelikeCardDataSo>(GoogleSheetDefinitions.RoguelikeCardDataAssetPath);
    }

    public static GameObject EnsureDataManagerPrefab()
    {
        EnsureFolder("Assets/0UkDefense/1Data/Prefab");

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(UkDefenseSetupMenu.DataManagerPrefabPath);
        if (existing != null && existing.GetComponent<DataManager>() != null)
            return existing;

        if (existing != null)
            AssetDatabase.DeleteAsset(UkDefenseSetupMenu.DataManagerPrefabPath);

        var temp = new GameObject("DataManager");
        temp.AddComponent<DataManager>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, UkDefenseSetupMenu.DataManagerPrefabPath);
        Object.DestroyImmediate(temp);

        WireDataManagerPrefab();
        return prefab;
    }

    public static List<(string code, GameObject prefab)> CreateMonsterPrefabs(bool forceRebuild)
    {
        EnsureFolder(UkDefenseSetupMenu.MonsterPrefabDir);
        var created = new List<(string code, GameObject prefab)>();

        foreach (var def in MonsterDefinitions)
        {
            var path = $"{UkDefenseSetupMenu.MonsterPrefabDir}/{def.code}.prefab";
            if (!forceRebuild)
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null && IsLeanPrefab(existing, def.code))
                {
                    created.Add((def.code, existing));
                    continue;
                }
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var temp = BuildMonsterObject(def.code, def.color);
            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);

            if (prefab == null)
                throw new System.InvalidOperationException($"프리팹 저장 실패: {path}");

            created.Add((def.code, prefab));
        }

        return created;
    }

    public static void WireDataManagerPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UkDefenseSetupMenu.DataManagerPrefabPath);
        if (prefab == null)
            return;

        var dataManager = prefab.GetComponent<DataManager>();
        if (dataManager == null)
            return;

        var so = new SerializedObject(dataManager);
        so.FindProperty("monsterDataSo").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<MonsterDataSo>(UkDefenseSetupMenu.SoDir + "/MonsterDataSo.asset");
        so.FindProperty("towerDataSo").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<TowerDataSo>(UkDefenseSetupMenu.SoDir + "/TowerDataSo.asset");
        so.FindProperty("skillDataSo").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<DefenseSkillDataSo>(UkDefenseSetupMenu.SoDir + "/DefenseSkillDataSo.asset");
        so.FindProperty("effectDataSo").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<DefenseEffectDataSo>(UkDefenseSetupMenu.SoDir + "/DefenseEffectDataSo.asset");
        so.FindProperty("effectGroupDataSo").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<DefenseEffectGroupDataSo>(UkDefenseSetupMenu.SoDir + "/DefenseEffectGroupDataSo.asset");
        so.FindProperty("bossDataSo").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<BossDataSo>(GoogleSheetDefinitions.BossDataAssetPath);
        so.FindProperty("bossElementGroupDataSo").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<BossElementGroupDataSo>(GoogleSheetDefinitions.BossElementGroupDataAssetPath);
        so.FindProperty("stageDataSo").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<StageDataSo>(UkDefenseSetupMenu.SoDir + "/StageDataSo.asset");
        so.FindProperty("addressableKeyDataSo").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<DefenseAddressableKeyDataSo>(GoogleSheetDefinitions.AddressableKeyDataAssetPath);
        so.FindProperty("roguelikeCardDataSo").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<RoguelikeCardDataSo>(GoogleSheetDefinitions.RoguelikeCardDataAssetPath);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(prefab);
    }

    public static void WireSingletonLoaderPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UkDefenseSetupMenu.SingletonLoaderPrefabPath);
        if (prefab == null)
            return;

        var loader = prefab.GetComponent<SingletonLoader>();
        if (loader == null)
            return;

        var so = new SerializedObject(loader);
        AssignPrefab(so, "dataManagerPrefab", UkDefenseSetupMenu.DataManagerPrefabPath);
        AssignPrefab(so, "googlesheetManagerPrefab", UkDefenseSetupMenu.GoogleSheetManagerPrefabPath);
        AssignPrefab(so, "gameManagerPrefab", UkDefenseSetupMenu.GameManagerPrefabPath);
        AssignPrefab(so, "stageManagerPrefab", UkDefenseSetupMenu.StageManagerPrefabPath);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(prefab);
    }

    public static void WireStageManagerRuntimeAssets()
    {
        var stagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UkDefenseSetupMenu.StageManagerPrefabPath);
        if (stagePrefab == null)
            return;

        var stageManager = stagePrefab.GetComponent<StageManager>();
        if (stageManager == null)
            return;

        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Kawaii Slimes/Animator/Slime.controller");
        var faceAsset = AssetDatabase.LoadAssetAtPath<Face>(
            "Assets/Kawaii Slimes/Scripts/AI/DataFace.asset");
        var avatar = LoadSlimeAvatar();

        var so = new SerializedObject(stageManager);
        so.FindProperty("slimeAnimatorController").objectReferenceValue = controller;
        so.FindProperty("slimeFaceAsset").objectReferenceValue = faceAsset;
        so.FindProperty("slimeAvatar").objectReferenceValue = avatar;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(stagePrefab);
    }

    private static Avatar LoadSlimeAvatar()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Kawaii Slimes/Animation/Slime_Anim.fbx");
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Avatar avatar)
                return avatar;
        }

        return null;
    }

    private static bool IsLeanPrefab(GameObject prefab, string code)
    {
        if (prefab.GetComponent<Monster>() == null)
            return false;

        if (prefab.GetComponent<UnitCombatVFX>() == null)
            return false;

        bool isAir = code.StartsWith("MS");
        bool isSuicide = code.EndsWith("0003");

        bool hasGround = prefab.GetComponent<GroundMonster>() != null;
        bool hasAir = prefab.GetComponent<AirMonster>() != null;
        bool hasMelee = prefab.GetComponent<MeleeMonster>() != null;
        bool hasSuicide = prefab.GetComponent<SuicideMonster>() != null;
        bool hasNavigator = prefab.GetComponent<UnitGridNavigator>() != null;

        if (isAir)
            return hasAir && !hasGround && hasNavigator == false
                   && (isSuicide ? hasSuicide && !hasMelee : hasMelee && !hasSuicide);

        return hasGround && !hasAir && hasNavigator
               && (isSuicide ? hasSuicide && !hasMelee : hasMelee && !hasSuicide);
    }

    private static GameObject BuildMonsterObject(string code, Color color)
    {
        bool isAir = code.StartsWith("MS");
        bool isSuicide = code.EndsWith("0003");

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = code;
        go.tag = "Enemy";
        go.transform.localScale = Vector3.one * 0.9f;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            renderer.sharedMaterial = material;
        }

        var collider = go.GetComponent<SphereCollider>();
        if (collider != null)
            collider.isTrigger = false;

        var rigidbody = go.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;

        go.AddComponent<Health>();
        go.AddComponent<Monster>();
        go.AddComponent<MonsterStatusController>();
        go.AddComponent<MonsterStatusOverlayUI>();

        if (isAir)
            go.AddComponent<AirMonster>();
        else
        {
            go.AddComponent<GroundMonster>();
            go.AddComponent<UnitGridNavigator>();
        }

        if (isSuicide)
            go.AddComponent<SuicideMonster>();
        else
            go.AddComponent<MeleeMonster>();

        var healthBar = go.AddComponent<HealthBarUI>();
        healthBar.ConfigureAsEnemy();

        var combatVfx = go.AddComponent<UnitCombatVFX>();
        var catalog = DefenseCombatCatalogFactory.EnsureCatalogAsset();
        combatVfx.ConfigureDeathEffect(catalog.defaultDeathEffectPrefab);

        go.AddComponent<PooledEnemy>();
        go.AddComponent<HealthDamagePopupBridge>();
        go.AddComponent<CombatHitFlash>();

        return go;
    }

    private static void EnsureAsset<T>(string path) where T : ScriptableObject
    {
        if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            return;

        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
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

    private static void AssignPrefab(SerializedObject so, string propertyName, string assetPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[UkDefense] 프리팹 없음: {assetPath}");
            return;
        }

        so.FindProperty(propertyName).objectReferenceValue = prefab;
    }

    private const string GoldCoinSpritePath =
        "Assets/AssetKits/ParticleImage/Demo/Sprites/Coin.png";
    private const string GoldCoinOutputDir = "Assets/Game/2Game/Resources/DefenseGold";
    private const string GoldCoinFlyPrefabPath = GoldCoinOutputDir + "/CoinFlyParticle.prefab";
    private const string GoldCoinSpriteOutputPath = GoldCoinOutputDir + "/Coin.png";
    private const string GoldCoinAttractionPath =
        "Assets/AssetKits/ParticleImage/Demo/Prefabs/CoinAttraction.prefab";

    public static void CreateDefenseGoldCoinFlyPrefab()
    {
        EnsureFolder(GoldCoinOutputDir);
        EnsureGoldCoinSpriteInResources();

        var source = AssetDatabase.LoadAssetAtPath<GameObject>(GoldCoinAttractionPath);
        if (source == null)
        {
            Debug.LogError($"[UkDefense] CoinAttraction 프리팹을 찾을 수 없습니다: {GoldCoinAttractionPath}");
            return;
        }

        var particle = source.transform.Find("Particle Image");
        if (particle == null)
        {
            Debug.LogError("[UkDefense] Particle Image 자식을 찾을 수 없습니다.");
            return;
        }

        var temp = Object.Instantiate(particle.gameObject);
        temp.name = "CoinFlyParticle";

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(GoldCoinFlyPrefabPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(GoldCoinFlyPrefabPath);

        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, GoldCoinFlyPrefabPath);
        Object.DestroyImmediate(temp);

        if (prefab == null)
        {
            Debug.LogError("[UkDefense] CoinFlyParticle 저장 실패");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[UkDefense] CoinFlyParticle 생성 완료 → {GoldCoinFlyPrefabPath}");
    }

    public static void EnsureGoldCoinFlyPrefabIfMissing()
    {
        EnsureFolder(GoldCoinOutputDir);
        EnsureGoldCoinSpriteInResources();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(GoldCoinFlyPrefabPath) != null)
            return;

        CreateDefenseGoldCoinFlyPrefab();
    }

    private static void EnsureGoldCoinSpriteInResources()
    {
        if (AssetDatabase.LoadAssetAtPath<Sprite>(GoldCoinSpriteOutputPath) != null)
            return;

        if (!AssetDatabase.CopyAsset(GoldCoinSpritePath, GoldCoinSpriteOutputPath))
            Debug.LogWarning($"[UkDefense] Coin 스프라이트 복사 실패: {GoldCoinSpritePath}");
    }

    [InitializeOnLoadMethod]
    private static void AutoEnsureGoldCoinFlyPrefabOnEditorLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EnsureGoldCoinFlyPrefabIfMissing();
        };
    }
}
#endif
