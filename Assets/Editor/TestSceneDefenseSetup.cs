using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// TestScene 디펜스 오브젝트 자동 배치.
/// 메뉴: Tools → UkDefense
/// </summary>
public static class TestSceneDefenseSetup
{
    private const string ScenePath = "Assets/Game/0Scene/TestScene.unity";

    private const string BlueMissilePath = "Assets/Epic Toon FX/Demo/Missile Prefabs/Rocket/RocketBlueOBJ.prefab";
    private const string GreenMissilePath = "Assets/Epic Toon FX/Demo/Missile Prefabs/Rocket/RocketGreenOBJ.prefab";
    private const string PinkMissilePath = "Assets/Epic Toon FX/Demo/Missile Prefabs/Rocket/RocketPinkOBJ.prefab";
    private const string FireMissilePath = "Assets/Epic Toon FX/Demo/Missile Prefabs/Rocket/RocketFireOBJ.prefab";
    private const string MeteorMissilePath = "Assets/Epic Toon FX/Prefabs/Combat/Missiles/Nuke/NukeMissileFire.prefab";
    private const string MeteorExplosionPath = "Assets/Epic Toon FX/Prefabs/Combat/Explosions/NukeExplosion/NukeExplosionFire.prefab";
    private const string ChainBoltPath = "Assets/Epic Toon FX/Prefabs/Environment/Lightning/Sharp/LightningStrikeSharpBlue.prefab";
    private const string ChainHitExplosionPath = "Assets/Epic Toon FX/Prefabs/Combat/Explosions/LightningExplosion/LightningExplosionBlue.prefab";
    private const string StunHeadEffectPath = "Assets/Epic Toon FX/Prefabs/Combat/Brawling/Stun/StunnedCirclingStarsSimple.prefab";
    private const string StunBodyEffectPath = "Assets/Epic Toon FX/Prefabs/Environment/Lightning/Soft/LightningOrbBlue.prefab";
    private const string FarmGoldBurstPath = "Assets/Epic Toon FX/Prefabs/Interactive/Money/Coins/GoldCoinBlast.prefab";
    private const string FarmDrillDebrisPath = "Assets/Epic Toon FX/Prefabs/Environment/Dust/DustDirtyPoof.prefab";
    private const string FarmDrillSoundPath = "Assets/Game/0Sound/drillsound.mp3";
    private const string FarmBuildHammerSoundPath = "Assets/Game/0Sound/hammerplay.mp3";
    private const string FarmGoldCoinSoundPath = "Assets/Game/0Sound/coindrop.mp3";
    private const string SingletonLoaderPath = "Assets/Game/0Splash/Prefab/SingletonLoader.prefab";
    private const string GameManagerPath = DefensePrefabFactory.GameManagerPath;

    [MenuItem("Tools/UkDefense/3. Setup TestScene (All)", false, 2)]
    public static void SetupTestSceneAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var prefabs = DefensePrefabFactory.CreateAllPrefabs(false);
        DefenseCombatCatalogFactory.EnsureCatalogAsset();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureDirectionalLight();
        EnsureMainCamera();
        EnsureEventSystem();
        EnsureDefenseGame(prefabs);
        WireSingletonLoader(prefabs);
        ReapplyDefaultMapLayout();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[UkDefense] TestScene + SingletonLoader 배치 완료.");
    }

    [MenuItem("Tools/UkDefense/3. Setup TestScene (All)", true)]
    private static bool ValidateSetupTestScene()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    [MenuItem("Tools/UkDefense/6. Reapply Map Layout", false, 5)]
    public static void ReapplyFarmLayoutMenu()
    {
        ReapplyDefaultMapLayout();
        AssetDatabase.SaveAssets();
        Debug.Log("[UkDefense] 기본 맵 레이아웃(넥서스·레인·농장)을 다시 적용했습니다.");
    }

    [MenuItem("Tools/UkDefense/6. Reapply Map Layout", true)]
    private static bool ValidateReapplyFarmLayoutMenu()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static void ReapplyDefaultMapLayout()
    {
        var mapLayout = DefenseMapEditorWindow.GetOrCreateDefaultAsset();
        DefenseMapLayoutDefaults.ApplyDefaultLayout(mapLayout);
        EditorUtility.SetDirty(mapLayout);

        if (mapLayout.towerLayout != null)
            EditorUtility.SetDirty(mapLayout.towerLayout);
    }

    private static void EnsureDefenseGame(DefensePrefabFactory.DefensePrefabSet prefabs)
    {
        var setup = Object.FindFirstObjectByType<DefenseSceneSetup>();
        GameObject defenseGame;

        if (setup == null)
        {
            defenseGame = new GameObject("DefenseGame");
            setup = defenseGame.AddComponent<DefenseSceneSetup>();
        }
        else
        {
            defenseGame = setup.gameObject;
            defenseGame.name = "DefenseGame";
        }

        var so = new SerializedObject(setup);
        AssignPrefab(so, "dataManagerPrefab", UkDefenseSetupMenu.DataManagerPrefabPath);
        AssignPrefab(so, "gameManagerPrefab", GameManagerPath);
        so.FindProperty("missilePoolManagerPrefab").objectReferenceValue = prefabs.MissilePoolManager;
        so.FindProperty("nexusManagerPrefab").objectReferenceValue = prefabs.NexusManager;
        so.FindProperty("towerStatsManagerPrefab").objectReferenceValue = prefabs.TowerStatsManager;
        so.FindProperty("towerManagerPrefab").objectReferenceValue = prefabs.TowerManager;
        so.FindProperty("stageManagerPrefab").objectReferenceValue = prefabs.StageManager;
        so.FindProperty("defenseHudPrefab").objectReferenceValue = prefabs.DefenseHud;
        AssignScriptableObject(so, "combatCatalog", DefenseCombatCatalogFactory.CatalogPath);
        AssignPrefab(so, "farmGoldBurstPrefab", FarmGoldBurstPath);
        AssignPrefab(so, "farmDrillDebrisPrefab", FarmDrillDebrisPath);
        so.FindProperty("farmDrillSound").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>(FarmDrillSoundPath);
        so.FindProperty("farmBuildHammerSound").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>(FarmBuildHammerSoundPath);
        so.FindProperty("farmGoldCoinSound").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>(FarmGoldCoinSoundPath);
        so.FindProperty("arenaCenter").vector3Value = Vector3.zero;
        so.FindProperty("mapLayout").objectReferenceValue = DefenseMapEditorWindow.GetOrCreateDefaultAsset();
        so.FindProperty("towerLayout").objectReferenceValue = DefenseTowerPlacementEditor.GetOrCreateDefaultAsset();
        so.FindProperty("towerHeight").floatValue = 0.6f;
        so.FindProperty("nexusHealth").floatValue = 10000f;
        so.FindProperty("groundScale").floatValue = 8f;
        so.FindProperty("mapHalfExtent").floatValue = 40f;
        so.FindProperty("cameraOrthographicSize").floatValue = 26f;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(defenseGame);
    }

    private static void WireSingletonLoader(DefensePrefabFactory.DefensePrefabSet prefabs)
    {
        var loaderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SingletonLoaderPath);
        if (loaderPrefab == null)
            return;

        var loader = loaderPrefab.GetComponent<SingletonLoader>();
        if (loader == null)
            return;

        var so = new SerializedObject(loader);
        so.FindProperty("missilePoolManagerPrefab").objectReferenceValue = prefabs.MissilePoolManager;
        so.FindProperty("nexusManagerPrefab").objectReferenceValue = prefabs.NexusManager;
        so.FindProperty("towerStatsManagerPrefab").objectReferenceValue = prefabs.TowerStatsManager;
        so.FindProperty("towerManagerPrefab").objectReferenceValue = prefabs.TowerManager;
        so.FindProperty("stageManagerPrefab").objectReferenceValue = prefabs.StageManager;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(loaderPrefab);
    }

    private static void AssignScriptableObject(SerializedObject so, string propertyName, string assetPath)
    {
        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (asset == null)
            Debug.LogWarning($"[UkDefense] ScriptableObject 없음: {assetPath}");
        so.FindProperty(propertyName).objectReferenceValue = asset;
    }

    private static void AssignPrefab(SerializedObject so, string propertyName, string assetPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
            Debug.LogWarning($"[UkDefense] 프리팹 없음: {assetPath}");
        so.FindProperty(propertyName).objectReferenceValue = prefab;
    }

    private static void EnsureMainCamera()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            camera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        camera.orthographic = true;
        camera.orthographicSize = 18f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);

        if (camera.GetComponent<DefenseIsometricCamera>() == null)
            camera.gameObject.AddComponent<DefenseIsometricCamera>();

        if (camera.GetComponent<DefenseCameraControlManager>() == null)
            camera.gameObject.AddComponent<DefenseCameraControlManager>();

        EditorUtility.SetDirty(camera.gameObject);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        EditorUtility.SetDirty(go);
    }

    private static void EnsureDirectionalLight()
    {
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
                return;
        }

        var go = new GameObject("Directional Light");
        var lightComp = go.AddComponent<Light>();
        lightComp.type = LightType.Directional;
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        EditorUtility.SetDirty(go);
    }
}
