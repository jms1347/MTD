#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

public static class CoopDefenseSceneSetup
{
    private const string ScenePath = "Assets/Game/0Scene/CoopDefenseScene.unity";

    [MenuItem("Tools/Multiplayer/Setup CoopDefenseScene", false, 1)]
    public static void SetupScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        if (Object.FindFirstObjectByType<CoopSceneBootstrap>() == null)
        {
            var bootstrapObject = new GameObject("CoopSceneBootstrap");
            bootstrapObject.AddComponent<CoopSceneBootstrap>();
        }

        if (Object.FindFirstObjectByType<CoopMapBootstrap>() == null)
        {
            var mapObject = new GameObject("CoopMapBootstrap");
            mapObject.AddComponent<CoopMapBootstrap>();
        }

        EnsureBuildSettings();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[Multiplayer] CoopDefenseScene 생성 완료.");
    }

    private static void EnsureBuildSettings()
    {
        var scenes = new[]
        {
            "Assets/Game/0Scene/LobbyScene.unity",
            ScenePath,
            "Assets/Game/0Scene/SplashScene.unity",
            "Assets/Game/0Scene/TestScene.unity"
        };

        var buildScenes = new EditorBuildSettingsScene[scenes.Length];
        for (var i = 0; i < scenes.Length; i++)
            buildScenes[i] = new EditorBuildSettingsScene(scenes[i], true);

        EditorBuildSettings.scenes = buildScenes;
    }
}
#endif
