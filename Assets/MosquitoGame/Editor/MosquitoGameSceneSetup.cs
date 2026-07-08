#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MosquitoGameSceneSetup
{
    private const string RootFolder = "Assets/MosquitoGame";
    private const string ScenePath = RootFolder + "/Scenes/MosquitoVsHumanScene.unity";
    private const string MaterialPath = RootFolder + "/Materials/MosquitoInterior.mat";
    private const string KenneyFolder = RootFolder + "/kenney_furniture-kit/Models/FBX format";
    private const string CubedBearFolder = RootFolder + "/Free_V1.1/Free_V1.1/FBX";
    private const string AwlokFolder = RootFolder + "/LowPolyBundle_Modern";
    private const string UnityPackagePath = RootFolder + "/LowPolyBundle_Modern.unitypackage";

    private static readonly Color FloorColor = new(0.78f, 0.74f, 0.68f);
    private static readonly Color WallColor = new(0.93f, 0.9f, 0.84f);
    private static readonly Color WoodColor = new(0.62f, 0.45f, 0.3f);
    private static readonly Color FabricColor = new(0.45f, 0.52f, 0.58f);

    [MenuItem("Tools/MosquitoGame/Setup Scene", false, 10)]
    public static void SetupFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        BuildAndSaveScene();

        if (EditorUtility.DisplayDialog(
                "Mosquito vs Human",
                "씬이 생성되었습니다.\n\n" +
                "• Tab: 인간 / 모기 시점 전환\n" +
                "• 모기: Space 상승, Ctrl 하강\n" +
                "• 인간: WASD + 마우스",
                "씬 열기",
                "닫기"))
        {
            EditorSceneManager.OpenScene(ScenePath);
        }
    }

    public static void BuildSceneBatch()
    {
        BuildAndSaveScene();
    }

    private static void BuildAndSaveScene()
    {
        EnsureFolders();
        EnsureAwlokImported();
        var scene = BuildScene();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AppendBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("[MosquitoGame] Scene saved: " + ScenePath);
    }

    public static GameObject BuildApartmentContent()
    {
        var apartment = new GameObject("Apartment");
        BuildApartmentShell(apartment.transform);
        BuildLivingFurniture(apartment.transform);
        BuildBedroomFurniture(apartment.transform);
        TryPlaceAwlokDecor(apartment.transform);
        BakeNavMesh(apartment);
        return apartment;
    }

    public static void CreateBaseLighting()
    {
        CreateLighting();
        CreateCameraFallback();
    }

    private static Scene BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateBaseLighting();

        var apartment = BuildApartmentContent();
        CreateHideZones(apartment.transform);
        CreateGameplay(apartment.transform);

        return scene;
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/MosquitoGame");
        EnsureFolder(RootFolder + "/Editor");
        EnsureFolder(RootFolder + "/Scripts");
        EnsureFolder(RootFolder + "/Scenes");
        EnsureFolder(RootFolder + "/Materials");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        var leaf = System.IO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
            return;

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }

    public static void EnsureAwlokImportedPublic() => EnsureAwlokImported();

    private static void EnsureAwlokImported()
    {
        if (AssetDatabase.IsValidFolder(AwlokFolder))
            return;

        if (!System.IO.File.Exists(UnityPackagePath))
        {
            Debug.LogWarning("[MosquitoGame] awlok unitypackage not found, skipping import.");
            return;
        }

        AssetDatabase.ImportPackage(UnityPackagePath, false);
        AssetDatabase.Refresh();
    }

    private static void CreateLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.44f, 0.48f);

        var sun = new GameObject("Directional Light");
        var light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.05f;
        light.color = new Color(1f, 0.97f, 0.9f);
        sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

        var ceiling = new GameObject("CeilingFillLight");
        var fill = ceiling.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.range = 18f;
        fill.intensity = 1.1f;
        fill.color = new Color(1f, 0.95f, 0.82f);
        ceiling.transform.position = new Vector3(5f, 2.35f, 3f);
    }

    private static void CreateCameraFallback()
    {
        var cameraGo = new GameObject("SceneCamera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
        cameraGo.transform.position = new Vector3(5f, 8f, -6f);
        cameraGo.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
    }

    private static void BuildApartmentShell(Transform parent)
    {
        var shell = new GameObject("Shell");
        shell.transform.SetParent(parent, false);

        // 거실 6x6m (CubedBear 2m 타일)
        for (var x = 0; x < 3; x++)
        {
            for (var z = 0; z < 3; z++)
                PlaceModel(CubedBearFolder + "/2m_Floor_white.fbx", new Vector3(x * 2f, 0f, z * 2f), Quaternion.identity, shell.transform, FloorColor);
        }

        // 침실 4x4m (x=8~12)
        for (var x = 0; x < 2; x++)
        {
            for (var z = 0; z < 2; z++)
                PlaceModel(CubedBearFolder + "/2m_Floor_white.fbx", new Vector3(8f + x * 2f, 0f, z * 2f), Quaternion.identity, shell.transform, FloorColor);
        }

        // 복도 바닥 2x2m
        PlaceModel(CubedBearFolder + "/2m_Floor_white.fbx", new Vector3(6f, 0f, 2f), Quaternion.identity, shell.transform, FloorColor);

        PlaceNorthSouthWalls(shell.transform, 0f, 6f, 0f, 6f, WallColor);
        PlaceNorthSouthWalls(shell.transform, 8f, 12f, 0f, 4f, WallColor);
        PlaceEastWestWalls(shell.transform, 0f, 6f, 0f, 6f, WallColor);
        PlaceEastWestWalls(shell.transform, 8f, 12f, 0f, 4f, WallColor);

        PlaceModel(CubedBearFolder + "/2m_Wall_Doorhole_cherry.fbx", new Vector3(6f, 0f, 2f), Quaternion.Euler(0f, 90f, 0f), shell.transform, WoodColor);
        PlaceModel(CubedBearFolder + "/2m_Wall_cherry.fbx", new Vector3(6f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), shell.transform, WallColor);
        PlaceModel(CubedBearFolder + "/2m_Wall_cherry.fbx", new Vector3(6f, 0f, 4f), Quaternion.Euler(0f, 90f, 0f), shell.transform, WallColor);
        PlaceModel(CubedBearFolder + "/2m_Bordered_Glass_cherry.fbx", new Vector3(3f, 0f, 6f), Quaternion.Euler(0f, 180f, 0f), shell.transform, WallColor);

        PlaceModel(KenneyFolder + "/wallWindow.fbx", new Vector3(2f, 0f, 0f), Quaternion.identity, shell.transform, WallColor);
        PlaceModel(KenneyFolder + "/wallWindow.fbx", new Vector3(10f, 0f, 0f), Quaternion.identity, shell.transform, WallColor);
    }

    private static void PlaceNorthSouthWalls(Transform parent, float minX, float maxX, float minZ, float maxZ, Color color)
    {
        for (var x = minX; x < maxX; x += 2f)
        {
            PlaceModel(CubedBearFolder + "/2m_Wall_cherry.fbx", new Vector3(x, 0f, minZ), Quaternion.identity, parent, color);
            PlaceModel(CubedBearFolder + "/2m_Wall_cherry.fbx", new Vector3(x, 0f, maxZ), Quaternion.Euler(0f, 180f, 0f), parent, color);
        }
    }

    private static void PlaceEastWestWalls(Transform parent, float minX, float maxX, float minZ, float maxZ, Color color)
    {
        for (var z = minZ; z < maxZ; z += 2f)
        {
            PlaceModel(CubedBearFolder + "/2m_Wall_cherry.fbx", new Vector3(minX, 0f, z), Quaternion.Euler(0f, 90f, 0f), parent, color);
            PlaceModel(CubedBearFolder + "/2m_Wall_cherry.fbx", new Vector3(maxX, 0f, z), Quaternion.Euler(0f, -90f, 0f), parent, color);
        }
    }

    private static void BuildLivingFurniture(Transform parent)
    {
        var living = new GameObject("LivingKitchen");
        living.transform.SetParent(parent, false);

        PlaceModel(CubedBearFolder + "/Couch_cherry.fbx", new Vector3(1.2f, 0f, 4.6f), Quaternion.Euler(0f, 180f, 0f), living.transform, FabricColor);
        PlaceModel(CubedBearFolder + "/Table_Long_spruce.fbx", new Vector3(3.2f, 0f, 3.1f), Quaternion.Euler(0f, 90f, 0f), living.transform, WoodColor);
        PlaceModel(CubedBearFolder + "/Chair_Brown_spruce.fbx", new Vector3(4.3f, 0f, 2.4f), Quaternion.Euler(0f, -35f, 0f), living.transform, WoodColor);
        PlaceModel(CubedBearFolder + "/StandingLamp_Lit_default.fbx", new Vector3(0.6f, 0f, 0.8f), Quaternion.identity, living.transform, WoodColor);
        PlaceModel(CubedBearFolder + "/Fridge_cherry.fbx", new Vector3(5.2f, 0f, 0.8f), Quaternion.Euler(0f, -90f, 0f), living.transform, WoodColor);
        PlaceModel(CubedBearFolder + "/CounterTop_Sink_0Layer_1Door_spruce.fbx", new Vector3(5.1f, 0f, 2.4f), Quaternion.Euler(0f, -90f, 0f), living.transform, WoodColor);
        PlaceModel(CubedBearFolder + "/CounterTop_2Layer_2_DrawerDoor_spruce.fbx", new Vector3(5.1f, 0f, 4.1f), Quaternion.Euler(0f, -90f, 0f), living.transform, WoodColor);
        PlaceModel(CubedBearFolder + "/Cactus2_wFlowerPot_brownpot.fbx", new Vector3(0.7f, 0f, 2.2f), Quaternion.identity, living.transform, null);
        PlaceModel(CubedBearFolder + "/Rug_Round_default.fbx", new Vector3(2.8f, 0.01f, 3.2f), Quaternion.identity, living.transform, new Color(0.55f, 0.58f, 0.62f));
        PlaceModel(KenneyFolder + "/lampRoundTable.fbx", new Vector3(3.1f, 0.72f, 3.1f), Quaternion.identity, living.transform, WoodColor);
        PlaceModel(KenneyFolder + "/pottedPlant.fbx", new Vector3(4.8f, 0f, 5.1f), Quaternion.identity, living.transform, null);
        PlaceModel(KenneyFolder + "/rugSquare.fbx", new Vector3(1.3f, 0.01f, 4.5f), Quaternion.identity, living.transform, FabricColor);
    }

    private static void BuildBedroomFurniture(Transform parent)
    {
        var bedroom = new GameObject("Bedroom");
        bedroom.transform.SetParent(parent, false);

        PlaceModel(CubedBearFolder + "/Bed_Twin_cherry.fbx", new Vector3(9.2f, 0f, 1.1f), Quaternion.Euler(0f, 90f, 0f), bedroom.transform, WoodColor);
        PlaceModel(CubedBearFolder + "/Shelf_Cube_3x3_white.fbx", new Vector3(11.1f, 0f, 3.1f), Quaternion.Euler(0f, 180f, 0f), bedroom.transform, WoodColor);
        PlaceModel(CubedBearFolder + "/CardboardBox_Square_default.fbx", new Vector3(8.5f, 0f, 3.2f), Quaternion.identity, bedroom.transform, new Color(0.72f, 0.55f, 0.35f));
        PlaceModel(CubedBearFolder + "/Clock_spruce.fbx", new Vector3(8.2f, 1.4f, 0.3f), Quaternion.identity, bedroom.transform, WoodColor);
        PlaceModel(CubedBearFolder + "/WallPhoto_Rectangle_spruce.fbx", new Vector3(11.3f, 1.3f, 0.25f), Quaternion.identity, bedroom.transform, WoodColor);
        PlaceModel(KenneyFolder + "/bookcaseClosed.fbx", new Vector3(11.2f, 0f, 0.8f), Quaternion.Euler(0f, 180f, 0f), bedroom.transform, WoodColor);
        PlaceModel(KenneyFolder + "/lampSquareTable.fbx", new Vector3(10.4f, 0.55f, 2.8f), Quaternion.identity, bedroom.transform, WoodColor);
    }

    private static void TryPlaceAwlokDecor(Transform parent)
    {
        if (!AssetDatabase.IsValidFolder(AwlokFolder))
            return;

        var decor = new GameObject("AwlokDecor");
        decor.transform.SetParent(parent, false);

        PlacePrefabIfExists(AwlokFolder + "/Prefabs/BuildingParts/Wall_04_Window.prefab", new Vector3(0f, 0f, 0f), Quaternion.identity, decor.transform);
        PlacePrefabIfExists(AwlokFolder + "/Prefabs/Furniture/Sofa/Sofa_01_03.prefab", new Vector3(2.2f, 0f, 5.2f), Quaternion.Euler(0f, 160f, 0f), decor.transform);

        var curtainGuids = AssetDatabase.FindAssets("Curtain t:Prefab", new[] { AwlokFolder });
        if (curtainGuids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(curtainGuids[0]);
            PlacePrefabIfExists(path, new Vector3(3f, 0f, 0.2f), Quaternion.identity, decor.transform);
        }
    }

    private static void CreateHideZones(Transform parent)
    {
        var zones = new GameObject("MosquitoHideZones");
        zones.transform.SetParent(parent, false);

        CreateHideZone(zones.transform, "UnderBed", new Vector3(9.2f, 0.18f, 1.1f), new Vector3(1.6f, 0.35f, 2.1f));
        CreateHideZone(zones.transform, "UnderTable", new Vector3(3.2f, 0.28f, 3.1f), new Vector3(1.4f, 0.5f, 0.9f));
        CreateHideZone(zones.transform, "BehindCouch", new Vector3(1.2f, 0.55f, 5.0f), new Vector3(1.8f, 1.0f, 0.45f));
        CreateHideZone(zones.transform, "CeilingCorner", new Vector3(0.8f, 2.2f, 0.8f), new Vector3(0.8f, 0.5f, 0.8f));
        CreateHideZone(zones.transform, "ShelfTop", new Vector3(11.1f, 1.7f, 3.1f), new Vector3(0.9f, 0.35f, 0.9f));
    }

    private static void CreateHideZone(Transform parent, string name, Vector3 center, Vector3 size)
    {
        var zone = new GameObject(name);
        zone.transform.SetParent(parent, false);
        zone.transform.position = center;
        zone.transform.localScale = size;
        zone.AddComponent<MosquitoGameHideZone>();

        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
    }

    private static void CreateGameplay(Transform parent)
    {
        var gameplay = new GameObject("Gameplay");
        gameplay.transform.SetParent(parent, false);

        var human = MosquitoGameHumanController.Create(gameplay.transform, new Vector3(3f, 0f, 3f), Quaternion.Euler(0f, 180f, 0f));
        var mosquito = MosquitoGameMosquitoController.Create(gameplay.transform, new Vector3(3.2f, 1.4f, 3.1f));

        var bootstrap = gameplay.AddComponent<MosquitoGamePlayBootstrap>();
        var serialized = new SerializedObject(bootstrap);
        serialized.FindProperty("human").objectReferenceValue = human;
        serialized.FindProperty("mosquito").objectReferenceValue = mosquito;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var hud = new GameObject("HUD");
        hud.transform.SetParent(gameplay.transform, false);
        var label = hud.AddComponent<MosquitoGameHudLabel>();
    }

    private static void BakeNavMesh(GameObject apartment)
    {
        var navRoot = new GameObject("Navigation");
        navRoot.transform.SetParent(apartment.transform, false);

        var surface = navRoot.AddComponent<NavMeshSurface>();
        surface.center = new Vector3(6f, 1f, 3f);
        surface.size = new Vector3(16f, 4f, 8f);
        surface.collectObjects = CollectObjects.Children;
        surface.BuildNavMesh();
    }

    private static GameObject PlaceModel(
        string assetPath,
        Vector3 position,
        Quaternion rotation,
        Transform parent,
        Color? tint)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (asset == null)
        {
            Debug.LogWarning("[MosquitoGame] Missing model: " + assetPath);
            return null;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        instance.transform.SetPositionAndRotation(position, rotation);
        ApplyMaterial(instance, tint);
        AddMeshColliders(instance);
        return instance;
    }

    private static GameObject PlacePrefabIfExists(string assetPath, Vector3 position, Quaternion rotation, Transform parent)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (asset == null)
            return null;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        instance.transform.SetPositionAndRotation(position, rotation);
        AddMeshColliders(instance);
        return instance;
    }

    private static void ApplyMaterial(GameObject root, Color? tint)
    {
        if (!tint.HasValue)
            return;

        var material = EnsureInteriorMaterial();
        var colorMaterial = new Material(material) { color = tint.Value };
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterial = colorMaterial;
    }

    private static Material EnsureInteriorMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null)
            return existing;

        var shader = Shader.Find("Standard");
        var material = new Material(shader)
        {
            color = FloorColor
        };
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    private static void AddMeshColliders(GameObject root)
    {
        foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (meshFilter.sharedMesh == null)
                continue;

            var collider = meshFilter.GetComponent<MeshCollider>();
            if (collider == null)
                collider = meshFilter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = meshFilter.sharedMesh;
            collider.convex = false;
        }
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
