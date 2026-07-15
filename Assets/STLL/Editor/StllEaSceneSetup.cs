#if UNITY_EDITOR
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>EA 프로토타입 — 기본 도형 맵·3역할·사수관·카드.</summary>
public static class StllEaSceneSetup
{
    private const string NetworkPrefabsPath = StllGameConstants.RootFolder + "/Data/StllNetworkPrefabs.asset";

    [MenuItem("Tools/STLL/Setup EA Prototype (Primitives)", false, 11)]
    public static void SetupEaPrototype()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        StllGameSceneSetup.EnsureFoldersPublic();
        Directory.CreateDirectory(StllGameConstants.RootFolder + "/Prefabs/Structures");

        var networkPrefabs = LoadOrCreateNetworkPrefabs();
        var assets = LoadOrCreateAssets();
        var gallopClip = AssetDatabase.LoadAssetAtPath<AudioClip>(StllGameConstants.HorseGallopClipPath);
        if (gallopClip != null)
            assets.horseGallopSound = gallopClip;

        var minionPrefab = BuildMinionPrefab();
        var enemyPrefab = BuildEnemyPrefab();
        var depotPrefab = BuildDepotPrefab();
        var miniBossPrefab = BuildMiniBossPrefab();
        var bossPrefab = BuildBossPrefab();
        var playerPrefab = BuildPlayerPrefab(assets, minionPrefab, gallopClip);

        assets.playerPrefab = playerPrefab;
        assets.minionPrefab = minionPrefab;
        assets.enemyGruntPrefab = enemyPrefab;
        assets.supplyDepotPrefab = depotPrefab;
        assets.miniBossPrefab = miniBossPrefab;
        assets.bossLuBuPrefab = bossPrefab;

        RegisterNetworkPrefab(networkPrefabs, playerPrefab);
        RegisterNetworkPrefab(networkPrefabs, minionPrefab);
        RegisterNetworkPrefab(networkPrefabs, enemyPrefab);
        RegisterNetworkPrefab(networkPrefabs, depotPrefab);
        RegisterNetworkPrefab(networkPrefabs, miniBossPrefab);
        RegisterNetworkPrefab(networkPrefabs, bossPrefab);

        EditorUtility.SetDirty(assets);
        EditorUtility.SetDirty(networkPrefabs);
        AssetDatabase.SaveAssets();

        BuildScene(assets, networkPrefabs, enemyPrefab, depotPrefab, miniBossPrefab, bossPrefab);
        EnsureBuildSettingsEntry();

        AssetDatabase.Refresh();
        Debug.Log("[STLL] EA 프로토타입 셋업 완료. StllGameScene에서 Play → 도원결의 → 허브 → 사수관");
    }

    [MenuItem("Tools/STLL/Set Lobby To STLL EA Scene", false, 20)]
    public static void SetLobbyToStll()
    {
        Debug.Log($"[STLL] 로비 게임 씬을 {StllGameConstants.GameSceneName}으로 설정하려면 " +
                  "LobbyScene의 LobbySceneBootstrap GameSceneName을 StllGameScene으로 바꾸거나 " +
                  "Play 전 Tools 메뉴 안내를 따르세요.");
    }

    private static NetworkPrefabsList LoadOrCreateNetworkPrefabs()
    {
        var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NetworkPrefabsPath);
        if (list != null)
            return list;

        list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
        AssetDatabase.CreateAsset(list, NetworkPrefabsPath);
        return list;
    }

    private static StllGameAssets LoadOrCreateAssets()
    {
        var assets = AssetDatabase.LoadAssetAtPath<StllGameAssets>(StllGameConstants.GameAssetsPath);
        if (assets != null)
            return assets;

        assets = ScriptableObject.CreateInstance<StllGameAssets>();
        AssetDatabase.CreateAsset(assets, StllGameConstants.GameAssetsPath);
        return assets;
    }

    private static void RegisterNetworkPrefab(NetworkPrefabsList list, GameObject prefab)
    {
        if (list == null || prefab == null)
            return;

        for (var i = 0; i < list.PrefabList.Count; i++)
        {
            if (list.PrefabList[i].Prefab == prefab)
                return;
        }

        list.Add(new NetworkPrefab { Prefab = prefab });
    }

    private static GameObject BuildPlayerPrefab(StllGameAssets assets, GameObject minionPrefab, AudioClip gallopClip)
    {
        var root = new GameObject("StllPlayer");

        var capsule = root.AddComponent<CapsuleCollider>();
        capsule.height = 2.4f;
        capsule.radius = 0.55f;
        capsule.center = new Vector3(0f, 1.2f, 0f);

        root.AddComponent<NetworkObject>();
        var networkTransform = root.AddComponent<NetworkTransform>();
        var networkTransformSerialized = new SerializedObject(networkTransform);
        networkTransformSerialized.FindProperty("AuthorityMode").enumValueIndex = 1;
        networkTransformSerialized.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<StllMountAssembly>();
        root.AddComponent<StllGlaiveSwingVisual>();
        root.AddComponent<StllHorseMotor>();
        root.AddComponent<StllMountedCharge>();
        root.AddComponent<StllPlayerStamina>();
        root.AddComponent<StllGlaiveAim>();
        root.AddComponent<StllGlaiveCombat>();
        root.AddComponent<StllMinionCommander>();
        root.AddComponent<StllCommanderAura>();
        root.AddComponent<StllMountedInput>();
        root.AddComponent<StllPlayerController>();
        root.AddComponent<StllBrotherhoodRoleState>();
        root.AddComponent<StllPlayerCardInventory>();
        root.AddComponent<StllPlayerHealth>();
        root.AddComponent<StllPlayerGold>();
        root.AddComponent<StllPlayerLoadout>();
        root.AddComponent<StllPlayerMoveModifiers>();
        root.AddComponent<StllRoleSkills>();
        root.AddComponent<StllActiveCardCaster>();

        var gallopObject = new GameObject("GallopAudio");
        gallopObject.transform.SetParent(root.transform, false);
        var gallopAudio = gallopObject.AddComponent<StllHorseGallopAudio>();
        if (gallopClip != null)
            gallopAudio.AssignClip(gallopClip);

        var spawner = root.AddComponent<StllMinionSpawner>();
        var spawnerSerialized = new SerializedObject(spawner);
        spawnerSerialized.FindProperty("minionPrefab").objectReferenceValue = minionPrefab;
        spawnerSerialized.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(root, StllPrefabPaths.Player);
    }

    private static GameObject BuildMinionPrefab()
    {
        var root = new GameObject("StllMinion");
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.55f, 0f),
            new Vector3(0.42f, 0.55f, 0.32f), new Color(0.25f, 0.35f, 0.65f));
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, root.transform, new Vector3(0f, 1.05f, 0f),
            new Vector3(0.32f, 0.32f, 0.28f), new Color(0.88f, 0.72f, 0.58f));

        var collider = root.AddComponent<CapsuleCollider>();
        collider.height = 1.6f;
        collider.radius = 0.28f;
        collider.center = new Vector3(0f, 0.8f, 0f);

        root.AddComponent<NetworkObject>();
        root.AddComponent<NetworkTransform>();
        root.AddComponent<StllMinionAI>();
        return SavePrefab(root, StllPrefabPaths.Minion);
    }

    private static GameObject BuildEnemyPrefab()
    {
        var root = new GameObject("StllEnemyGrunt");
        StllVisualUtil.CreatePrimitive(PrimitiveType.Capsule, root.transform, new Vector3(0f, 0.9f, 0f),
            new Vector3(0.5f, 0.9f, 0.5f), new Color(0.35f, 0.3f, 0.22f));

        var collider = root.AddComponent<CapsuleCollider>();
        collider.height = 1.6f;
        collider.radius = 0.32f;
        collider.center = new Vector3(0f, 0.8f, 0f);

        root.AddComponent<NetworkObject>();
        root.AddComponent<NetworkTransform>();
        root.AddComponent<StllEnemyHealth>();
        root.AddComponent<StllEnemyGruntAI>();
        return SavePrefab(root, StllPrefabPaths.EnemyGrunt);
    }

    private static GameObject BuildMiniBossPrefab()
    {
        var root = new GameObject("StllMiniBossHuangYing");
        root.AddComponent<NetworkObject>();
        root.AddComponent<NetworkTransform>();
        root.AddComponent<StllMiniBossHuangYing>();
        return SavePrefab(root, StllPrefabPaths.MiniBoss);
    }

    private static GameObject BuildBossPrefab()
    {
        var root = new GameObject("StllBossLuBu");
        root.AddComponent<NetworkObject>();
        root.AddComponent<NetworkTransform>();
        root.AddComponent<StllBossLuBu>();
        return SavePrefab(root, StllPrefabPaths.BossLuBu);
    }

    private static GameObject BuildDepotPrefab()
    {
        var root = new GameObject("StllSupplyDepot");
        root.AddComponent<NetworkObject>();
        root.AddComponent<StllSupplyDepot>();
        return SavePrefab(root, StllPrefabPaths.SupplyDepot);
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void BuildScene(StllGameAssets assets, NetworkPrefabsList networkPrefabs, GameObject enemyPrefab, GameObject depotPrefab, GameObject miniBossPrefab, GameObject bossPrefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.95f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.38f, 0.4f, 0.44f);

        var networkRoot = new GameObject("NetworkManager");
        var networkManager = networkRoot.AddComponent<NetworkManager>();
        var transport = networkRoot.AddComponent<UnityTransport>();
        transport.ConnectionData.Port = StllGameConstants.GameNetcodePort;
        networkManager.NetworkConfig.PlayerPrefab = assets.playerPrefab;
        networkManager.NetworkConfig.NetworkTransport = transport;
        networkManager.NetworkConfig.ConnectionApproval = true;
        networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists ??= new System.Collections.Generic.List<NetworkPrefabsList>();
        networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
        networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(networkPrefabs);

        var bootstrap = networkRoot.AddComponent<StllNetworkBootstrap>();
        var bootstrapSerialized = new SerializedObject(bootstrap);
        bootstrapSerialized.FindProperty("gameAssets").objectReferenceValue = assets;
        bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

        var systems = new GameObject("StllGameSystems");
        systems.AddComponent<NetworkObject>();
        var session = systems.AddComponent<StllGameSession>();
        var sessionSerialized = new SerializedObject(session);
        sessionSerialized.FindProperty("assets").objectReferenceValue = assets;
        sessionSerialized.ApplyModifiedPropertiesWithoutUndo();

        systems.AddComponent<StllRoleAssigner>();
        systems.AddComponent<StllCardPickerController>();
        systems.AddComponent<StllTeamGold>();
        systems.AddComponent<StllHubShopController>();

        var run = systems.AddComponent<StllRunController>();
        var runSerialized = new SerializedObject(run);
        runSerialized.FindProperty("depotPrefab").objectReferenceValue = depotPrefab;
        runSerialized.FindProperty("bossPrefab").objectReferenceValue = bossPrefab;
        runSerialized.ApplyModifiedPropertiesWithoutUndo();

        var waveSpawner = systems.AddComponent<StllStageWaveSpawner>();
        var waveSerialized = new SerializedObject(waveSpawner);
        waveSerialized.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
        waveSerialized.FindProperty("miniBossPrefab").objectReferenceValue = miniBossPrefab;
        waveSerialized.ApplyModifiedPropertiesWithoutUndo();

        runSerialized.FindProperty("waveSpawner").objectReferenceValue = waveSpawner;
        runSerialized.ApplyModifiedPropertiesWithoutUndo();

        var hudObject = new GameObject("StllEaHud");
        hudObject.AddComponent<StllEaHud>();
        hudObject.AddComponent<StllEaDebugInput>();
        hudObject.AddComponent<StllFogVisionController>();

        EditorSceneManager.SaveScene(scene, StllGameConstants.GameScenePath);
    }

    private static void EnsureBuildSettingsEntry()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (var i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == StllGameConstants.GameScenePath)
                return;
        }

        scenes.Add(new EditorBuildSettingsScene(StllGameConstants.GameScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
