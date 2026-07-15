#if UNITY_EDITOR
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StllGameSceneSetup
{
    private const string NetworkPrefabsPath = StllGameConstants.RootFolder + "/Data/StllNetworkPrefabs.asset";

    [MenuItem("Tools/STLL/Setup Game Scene", false, 10)]
    public static void SetupGameScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolders();
        var networkPrefabs = EnsureNetworkPrefabsList();
        var assets = EnsureGameAssets();
        var gallopClip = AssetDatabase.LoadAssetAtPath<AudioClip>(StllGameConstants.HorseGallopClipPath);
        if (gallopClip != null)
            assets.horseGallopSound = gallopClip;

        var minionPrefab = BuildMinionPrefab();
        var enemyPrefab = BuildEnemyGruntPrefab();
        var playerPrefab = BuildPlayerPrefab(assets, minionPrefab, gallopClip);

        assets.playerPrefab = playerPrefab;
        assets.minionPrefab = minionPrefab;
        assets.enemyGruntPrefab = enemyPrefab;

        RegisterNetworkPrefab(networkPrefabs, playerPrefab);
        RegisterNetworkPrefab(networkPrefabs, minionPrefab);
        RegisterNetworkPrefab(networkPrefabs, enemyPrefab);

        EditorUtility.SetDirty(assets);
        EditorUtility.SetDirty(networkPrefabs);
        AssetDatabase.SaveAssets();

        BuildScene(assets, networkPrefabs);
        EnsureBuildSettingsEntry();

        AssetDatabase.Refresh();
        Debug.Log("[STLL] 언월도 마상 씬 셋업 완료. StllGameScene에서 Play로 테스트하세요.");
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(StllGameConstants.RootFolder + "/Data");
        Directory.CreateDirectory(StllGameConstants.RootFolder + "/Prefabs/Characters");
        Directory.CreateDirectory(StllGameConstants.RootFolder + "/Prefabs/Units");
        Directory.CreateDirectory(StllGameConstants.RootFolder + "/Prefabs/Structures");
        Directory.CreateDirectory(StllGameConstants.RootFolder + "/Scenes");
    }

    public static void EnsureFoldersPublic() => EnsureFolders();

    private static StllGameAssets EnsureGameAssets()
    {
        var assets = AssetDatabase.LoadAssetAtPath<StllGameAssets>(StllGameConstants.GameAssetsPath);
        if (assets != null)
            return assets;

        assets = ScriptableObject.CreateInstance<StllGameAssets>();
        AssetDatabase.CreateAsset(assets, StllGameConstants.GameAssetsPath);
        return assets;
    }

    private static NetworkPrefabsList EnsureNetworkPrefabsList()
    {
        var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NetworkPrefabsPath);
        if (list != null)
            return list;

        list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
        AssetDatabase.CreateAsset(list, NetworkPrefabsPath);
        return list;
    }

    private static void RegisterNetworkPrefab(NetworkPrefabsList list, GameObject prefab)
    {
        if (list == null || prefab == null)
            return;

        var netObj = prefab.GetComponent<NetworkObject>();
        if (netObj == null)
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

        var gallopObject = new GameObject("GallopAudio");
        gallopObject.transform.SetParent(root.transform, false);
        gallopObject.transform.localPosition = new Vector3(0f, 0.15f, 0f);
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

    private static GameObject BuildEnemyGruntPrefab()
    {
        var root = new GameObject("StllEnemyGrunt");

        StllVisualUtil.CreatePrimitive(PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.55f, 0f),
            new Vector3(0.7f, 0.7f, 0.7f), new Color(0.12f, 0.12f, 0.14f));
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, root.transform, new Vector3(-0.12f, 0.62f, 0.22f),
            new Vector3(0.08f, 0.05f, 0.04f), Color.red);
        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, root.transform, new Vector3(0.12f, 0.62f, 0.22f),
            new Vector3(0.08f, 0.05f, 0.04f), Color.red);

        var collider = root.AddComponent<CapsuleCollider>();
        collider.height = 1.2f;
        collider.radius = 0.38f;
        collider.center = new Vector3(0f, 0.6f, 0f);

        root.AddComponent<NetworkObject>();
        root.AddComponent<NetworkTransform>();
        var health = root.AddComponent<StllEnemyHealth>();

        return SavePrefab(root, StllPrefabPaths.EnemyGrunt);
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void BuildScene(StllGameAssets assets, NetworkPrefabsList networkPrefabs)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "StllArenaPlane";
        plane.transform.localScale = new Vector3(8f, 1f, 8f);
        var renderer = plane.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = new Color(0.35f, 0.42f, 0.32f);
            renderer.sharedMaterial = material;
        }

        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.9f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.35f, 0.38f, 0.42f);
        RenderSettings.fog = false;

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

        var enemySpawner = systems.AddComponent<StllTestEnemySpawner>();
        var spawnerSerialized = new SerializedObject(enemySpawner);
        spawnerSerialized.FindProperty("enemyPrefab").objectReferenceValue = assets.enemyGruntPrefab;
        spawnerSerialized.ApplyModifiedPropertiesWithoutUndo();

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
