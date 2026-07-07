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
                "몬스터 비주얼 테스트 씬",
                "씬이 생성되었습니다.\n\n" +
                "• 에디터: 타입 변경 후 Inspector에서 Rebuild Preview\n" +
                "• 플레이: 좌측 HUD로 타입·걷기·이펙트 테스트",
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
        cameraGo.transform.position = new Vector3(0f, 3.8f, -7.5f);
        cameraGo.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
        cameraGo.AddComponent<AudioListener>();

        var rig = new GameObject("MonsterVisualTestRig");
        rig.transform.position = Vector3.zero;
        var controller = rig.AddComponent<CwslMonsterVisualTestController>();

        var assets = AssetDatabase.LoadAssetAtPath<CwslGameAssets>(AssetsPath);
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("assets").objectReferenceValue = assets;
        serialized.FindProperty("previewType").enumValueIndex = (int)CwslMonsterType.Suicide;
        serialized.FindProperty("autoWalkInPlayMode").boolValue = true;
        serialized.FindProperty("showHudInPlayMode").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

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
