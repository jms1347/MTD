using UnityEngine;

[DefaultExecutionOrder(-250)]
public class CoopMapBootstrap : MonoBehaviour
{
    public static CoopMapBootstrap Instance { get; private set; }

    [SerializeField] private float cameraOrthographicSize = 38f;

    public bool IsReady { get; private set; }
    public DefenseMapLayout MapLayout { get; private set; }
    public Vector3 MapCenter => MapLayout != null ? MapLayout.GetPlayerSpawnWorld() : Vector3.zero;
    public float MapHalfExtent => MapLayout != null ? MapLayout.MapHalfExtent : 40f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureMainCamera();
        EnsureSceneLighting();
        BuildWorld();
        IsReady = true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static Camera EnsureMainCamera()
    {
        var camera = Camera.main;
        if (camera != null)
        {
            camera.enabled = true;
            return camera;
        }

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        camera.enabled = true;
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.18f, 0.22f, 0.28f);
        return camera;
    }

    private void BuildWorld()
    {
        MapLayout = CoopMapLayoutBuilder.Build();
        DefenseMapBuilder.Build(MapLayout);
        DefenseMapPathfinder.Initialize(MapLayout);

        var center = MapLayout.GetPlayerSpawnWorld();
        CoopMapGimmicks.Build(MapLayout, null);
        CreateFarmPressurePlate(MapLayout, MapLayout.playerSpawnCell + new Vector2Int(0, -3));
        CreateFarmPressurePlate(MapLayout, MapLayout.playerSpawnCell + new Vector2Int(0, 7), "CoopFarmPressurePlate_North");
        SetupCamera(center);
        EnsureCameraFollow();
    }

    private void CreateFarmPressurePlate(DefenseMapLayout layout, Vector2Int cell, string objectName = "CoopFarmPressurePlate")
    {
        var world = DefenseMapGrid.CellToWorld(layout, cell);
        var plateObject = new GameObject(objectName);
        plateObject.transform.position = new Vector3(world.x, 0.06f, world.z);

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "PressurePlateVisual";
        visual.transform.SetParent(plateObject.transform, false);
        visual.transform.localScale = new Vector3(0.92f, 0.08f, 0.92f);
        Object.Destroy(visual.GetComponent<Collider>());

        var renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(0.2f, 0.85f, 0.45f, 0.85f);

        var trigger = plateObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(1.1f, 1.2f, 1.1f);
        trigger.center = new Vector3(0f, 0.5f, 0f);
        plateObject.AddComponent<CoopFarmGatePressurePlate>();
    }

    private void EnsureCameraFollow()
    {
        if (FindFirstObjectByType<CoopCameraFollow>() != null)
            return;

        var followObject = new GameObject("CoopCameraFollow");
        followObject.AddComponent<CoopCameraFollow>();
    }

    private void SetupCamera(Vector3 center)
    {
        var camera = EnsureMainCamera();
        camera.orthographic = true;
        camera.orthographicSize = cameraOrthographicSize;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.18f, 0.22f, 0.28f);

        var isoCamera = camera.GetComponent<DefenseIsometricCamera>();
        if (isoCamera == null)
            isoCamera = camera.gameObject.AddComponent<DefenseIsometricCamera>();

        isoCamera.SetFollowTarget(null, cameraOrthographicSize);
        camera.transform.position = center + new Vector3(0f, 24f, -12f);

        if (camera.GetComponent<DefenseCameraControlManager>() == null)
            camera.gameObject.AddComponent<DefenseCameraControlManager>();

        camera.enabled = true;
    }

    private static void EnsureSceneLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.58f, 0.6f, 0.65f);

        var light = FindDirectionalLight();
        if (light == null)
        {
            var lightObject = new GameObject("CoopDirectionalLight");
            light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
        }

        light.intensity = 1.5f;
        light.color = new Color(1f, 0.98f, 0.94f);
        light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
    }

    private static Light FindDirectionalLight()
    {
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        for (var i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null && lights[i].type == LightType.Directional)
                return lights[i];
        }

        return null;
    }
}
