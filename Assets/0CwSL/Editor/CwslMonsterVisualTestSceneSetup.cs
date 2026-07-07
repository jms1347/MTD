#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CwslMonsterVisualTestSceneSetup
{
    private const string ScenePath = "Assets/0CwSL/Scenes/CwslMonsterVisualTestScene.unity";
    private const string AssetsPath = "Assets/0CwSL/Data/CwslGameAssets.asset";

    [MenuItem("Tools/CwSL/Setup Monster Visual Test Scene", false, 20)]
    public static void SetupFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = BuildScene();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AppendBuildSettings();
        AssetDatabase.SaveAssets();

        if (EditorUtility.DisplayDialog(
                "전투 비주얼 테스트 씬",
                "씬이 생성되었습니다.\n\n" +
                "• 좌측 HUD: 몬스터 타입·걷기·이펙트\n" +
                "• 우측 HUD: 캐릭터·WASD·QWER 스킬·스턴\n" +
                "• 플레이 모드에서 테스트",
                "씬 열기",
                "닫기"))
        {
            EditorSceneManager.OpenScene(ScenePath);
        }
    }

    private static Scene BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "TestFloor";
        plane.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
        var floorRenderer = plane.GetComponent<Renderer>();
        if (floorRenderer != null)
            floorRenderer.sharedMaterial = CwslMaterialUtil.CreateMatteColored(new Color(0.22f, 0.24f, 0.28f));

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.05f;
        light.color = new Color(1f, 0.96f, 0.9f);
        lightGo.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.35f, 0.37f, 0.42f);

        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
        cameraGo.transform.position = new Vector3(0f, 4.2f, -9f);
        cameraGo.transform.rotation = Quaternion.Euler(16f, 0f, 0f);
        cameraGo.AddComponent<AudioListener>();

        var assets = AssetDatabase.LoadAssetAtPath<CwslGameAssets>(AssetsPath);

        var bootstrap = new GameObject("VisualTestBootstrap");
        var bootstrapComponent = bootstrap.AddComponent<CwslVisualTestAssetsBootstrap>();
        var bootstrapSerialized = new SerializedObject(bootstrapComponent);
        bootstrapSerialized.FindProperty("assets").objectReferenceValue = assets;
        bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

        var monsterRig = new GameObject("MonsterVisualTestRig");
        monsterRig.transform.position = Vector3.zero;
        var monsterController = monsterRig.AddComponent<CwslMonsterVisualTestController>();
        var monsterSerialized = new SerializedObject(monsterController);
        monsterSerialized.FindProperty("assets").objectReferenceValue = assets;
        monsterSerialized.FindProperty("previewType").enumValueIndex = (int)CwslMonsterType.Melee;
        monsterSerialized.FindProperty("autoWalkInPlayMode").boolValue = true;
        monsterSerialized.FindProperty("showHudInPlayMode").boolValue = true;
        monsterSerialized.FindProperty("spawnOffset").vector3Value = new Vector3(4f, 0f, 0f);
        monsterSerialized.ApplyModifiedPropertiesWithoutUndo();

        var playerRig = new GameObject("PlayerVisualTestRig");
        playerRig.transform.position = Vector3.zero;
        var playerController = playerRig.AddComponent<CwslPlayerVisualTestController>();
        var playerSerialized = new SerializedObject(playerController);
        playerSerialized.FindProperty("assets").objectReferenceValue = assets;
        playerSerialized.FindProperty("previewCharacter").enumValueIndex = (int)CwslCharacterId.Tank;
        playerSerialized.FindProperty("showHudInPlayMode").boolValue = true;
        playerSerialized.FindProperty("spawnOffset").vector3Value = new Vector3(-4f, 0f, 0f);
        playerSerialized.ApplyModifiedPropertiesWithoutUndo();

        return scene;
    }

    private static void AppendBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var entry in scenes)
        {
            if (entry.path == ScenePath)
                return;
        }

        var updated = new EditorBuildSettingsScene[scenes.Length + 1];
        for (var i = 0; i < scenes.Length; i++)
            updated[i] = scenes[i];

        updated[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = updated;
    }
}
#endif
