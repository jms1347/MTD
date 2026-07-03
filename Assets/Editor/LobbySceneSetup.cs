#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// LobbyScene 생성 및 빌드 설정.
/// 메뉴: Tools → Multiplayer → Setup LobbyScene
/// </summary>
public static class LobbySceneSetup
{
    private const string ScenePath = "Assets/Game/0Scene/LobbyScene.unity";

    [MenuItem("Tools/Multiplayer/Setup LobbyScene", false, 0)]
    public static void SetupLobbyScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        EnsureMainCamera();
        EnsureEventSystem();
        EnsureLobbyBootstrap();
        EnsureBuildSettings();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log("[Multiplayer] LobbyScene 생성 완료.");
    }

    [MenuItem("Tools/Multiplayer/Setup LobbyScene", true)]
    private static bool ValidateSetupLobbyScene()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static void EnsureMainCamera()
    {
        var camera = Object.FindFirstObjectByType<Camera>();
        if (camera == null)
            return;

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.1f, 0.16f, 1f);
        camera.tag = "MainCamera";
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static void EnsureLobbyBootstrap()
    {
        if (Object.FindFirstObjectByType<LobbySceneBootstrap>() != null)
            return;

        var bootstrapObject = new GameObject("LobbySceneBootstrap");
        bootstrapObject.AddComponent<LobbySceneBootstrap>();
    }

    private static void EnsureBuildSettings()
    {
        var scenes = new[]
        {
            ScenePath,
            "Assets/0CwSL/Scenes/CwslGameScene.unity",
            "Assets/Game/0Scene/SplashScene.unity",
            "Assets/Game/0Scene/LoadingScene.unity",
            "Assets/Game/0Scene/TestScene.unity"
        };

        var buildScenes = new EditorBuildSettingsScene[scenes.Length];
        for (var i = 0; i < scenes.Length; i++)
            buildScenes[i] = new EditorBuildSettingsScene(scenes[i], i <= 1);

        EditorBuildSettings.scenes = buildScenes;
    }
}
#endif
