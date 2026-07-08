#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PanicPrototypeSceneSetup
{
    private const string ScenePath = "Assets/MosquitoGame/Panic/Scenes/MosquitoPanicPrototype.unity";
    private const string PrefabFolder = "Assets/MosquitoGame/Panic/Prefabs";
    private const string HumanPrefabPath = PrefabFolder + "/PanicHuman.prefab";
    private const string MosquitoPrefabPath = PrefabFolder + "/PanicMosquito.prefab";

    [MenuItem("Tools/MosquitoGame/Setup Panic Prototype Scene", false, 11)]
    public static void SetupFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        BuildAndSave();
        if (EditorUtility.DisplayDialog(
                "Mosquito Panic 3D",
                "프로토타입 씬이 생성되었습니다.\n\n" +
                "• Play → Host 자동 시작\n" +
                "• 인간: WASD, 마우스, E 함정, F 미션 홀드, 탭/클릭 사격\n" +
                "• 모기: Shift 대시, 우클릭/탭 흡혈\n" +
                "• 함정 선택: 1 모기향 / 2 끈끈이 / 3 미끼",
                "씬 열기",
                "닫기"))
        {
            EditorSceneManager.OpenScene(ScenePath);
        }
    }

    public static void BuildSceneBatch()
    {
        BuildAndSave();
    }

    private static void BuildAndSave()
    {
        EnsureFolders();
        MosquitoGameSceneSetup.EnsureAwlokImportedPublic();
        var humanPrefab = EnsureHumanPrefab();
        var mosquitoPrefab = EnsureMosquitoPrefab();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MosquitoGameSceneSetup.CreateBaseLighting();

        var systems = CreateSystemsRoot();
        var apartment = MosquitoGameSceneSetup.BuildApartmentContent();
        CreateMissions(apartment.transform);
        CreateFan(apartment.transform);
        var spawns = CreateSpawns(apartment.transform);
        CreateUi();
        WireBootstrap(systems, humanPrefab, mosquitoPrefab, spawns);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AppendBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("[MosquitoPanic] Scene saved: " + ScenePath);
    }

    private static GameObject CreateSystemsRoot()
    {
        var root = new GameObject("PanicSystems");
        var network = root.AddComponent<NetworkManager>();
        root.AddComponent<UnityTransport>();
        root.AddComponent<PanicGameManager>();
        root.AddComponent<ScoreManager>();
        root.AddComponent<TrapManager>();
        return root;
    }

    private static void CreateMissions(Transform parent)
    {
        var missions = new GameObject("Missions");
        missions.transform.SetParent(parent, false);

        SpawnMission(PanicMissionType.LaptopHomework, "노트북 과제", new Vector3(4.3f, 0.8f, 2.4f), new Vector3(0.8f, 0.12f, 0.55f), new Color(0.25f, 0.35f, 0.85f));
        SpawnMission(PanicMissionType.TurnOffTabs, "멀티탭 끄기", new Vector3(9.5f, 0.55f, 0.8f), new Vector3(0.7f, 0.1f, 0.5f), new Color(0.85f, 0.35f, 0.25f));
        SpawnMission(PanicMissionType.TurnOffFan, "선풍기 끄기", new Vector3(0.8f, 0.7f, 5.2f), new Vector3(0.55f, 0.55f, 0.55f), new Color(0.35f, 0.75f, 0.85f));
    }

    private static void SpawnMission(PanicMissionType type, string label, Vector3 pos, Vector3 scale, Color color)
    {
        var mission = MissionObject.Create(type, label, pos, scale, color);
        mission.transform.SetParent(GameObject.Find("Missions").transform, true);
    }

    private static void CreateFan(Transform parent)
    {
        var fan = FanGimmick.Create(new Vector3(0.8f, 0.08f, 5.2f));
        fan.transform.SetParent(parent, true);
    }

    private static (Transform human, Transform[] mosquitos) CreateSpawns(Transform parent)
    {
        var root = new GameObject("SpawnPoints");
        root.transform.SetParent(parent, false);

        var human = CreateSpawn(root.transform, "HumanSpawn", new Vector3(3f, 0f, 3f));
        var mosquitoA = CreateSpawn(root.transform, "MosquitoSpawnA", new Vector3(1f, 1.2f, 4.5f));
        var mosquitoB = CreateSpawn(root.transform, "MosquitoSpawnB", new Vector3(10f, 1.2f, 2.5f));
        return (human, new[] { mosquitoA, mosquitoB });
    }

    private static Transform CreateSpawn(Transform parent, string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        return go.transform;
    }

    private static void CreateUi()
    {
        var hud = new GameObject("PanicHud");
        hud.AddComponent<PanicHudController>();
    }

    private static void WireBootstrap(
        GameObject systems,
        GameObject humanPrefab,
        GameObject mosquitoPrefab,
        (Transform human, Transform[] mosquitos) spawns)
    {
        var bootstrap = systems.AddComponent<PanicOfflineBootstrap>();
        var serialized = new SerializedObject(bootstrap);
        serialized.FindProperty("autoStartHost").boolValue = true;
        serialized.FindProperty("humanPrefab").objectReferenceValue = humanPrefab;
        serialized.FindProperty("mosquitoPrefab").objectReferenceValue = mosquitoPrefab;
        serialized.FindProperty("humanSpawn").objectReferenceValue = spawns.human;
        serialized.FindProperty("mosquitoSpawns").arraySize = spawns.mosquitos.Length;
        for (var i = 0; i < spawns.mosquitos.Length; i++)
            serialized.FindProperty("mosquitoSpawns").GetArrayElementAtIndex(i).objectReferenceValue = spawns.mosquitos[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();

        RegisterNetworkPrefabs(systems.GetComponent<NetworkManager>(), humanPrefab, mosquitoPrefab);
    }

    private static void RegisterNetworkPrefabs(NetworkManager networkManager, GameObject humanPrefab, GameObject mosquitoPrefab)
    {
        if (networkManager == null)
            return;

        var listPath = PrefabFolder + "/PanicNetworkPrefabs.asset";
        var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(listPath);
        if (list == null)
        {
            list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            AssetDatabase.CreateAsset(list, listPath);
        }

        AddPrefab(list, humanPrefab);
        AddPrefab(list, mosquitoPrefab);
        EditorUtility.SetDirty(list);

        if (networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists == null)
            networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists = new List<NetworkPrefabsList>();
        networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
        networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(list);
    }

    private static void AddPrefab(NetworkPrefabsList list, GameObject prefab)
    {
        if (prefab == null)
            return;

        if (list.Contains(prefab))
            return;

        list.Add(new NetworkPrefab { Prefab = prefab });
    }

    private static GameObject EnsureHumanPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath);
        if (existing != null)
            return existing;

        var root = new GameObject("PanicHuman");
        root.AddComponent<NetworkObject>();
        root.AddComponent<HumanController>();
        return SavePrefab(root, HumanPrefabPath);
    }

    private static GameObject EnsureMosquitoPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(MosquitoPrefabPath);
        if (existing != null)
            return existing;

        var root = new GameObject("PanicMosquito");
        root.AddComponent<NetworkObject>();
        root.AddComponent<Rigidbody>();
        root.AddComponent<MosquitoController>();
        return SavePrefab(root, MosquitoPrefabPath);
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        EnsureFolders();
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/MosquitoGame/Panic"))
            AssetDatabase.CreateFolder("Assets/MosquitoGame", "Panic");
        if (!AssetDatabase.IsValidFolder("Assets/MosquitoGame/Panic/Scenes"))
            AssetDatabase.CreateFolder("Assets/MosquitoGame/Panic", "Scenes");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/MosquitoGame/Panic", "Prefabs");
    }

    private static void AppendBuildSettings()
    {
        var merged = new List<EditorBuildSettingsScene>();
        var seen = new HashSet<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.path == null || !seen.Add(scene.path))
                continue;
            merged.Add(scene);
        }

        if (seen.Add(ScenePath))
            merged.Add(new EditorBuildSettingsScene(ScenePath, true));

        EditorBuildSettings.scenes = merged.ToArray();
    }
}
#endif
