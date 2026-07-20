#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SamgukMarble;

/// <summary>
/// Tools/SamgukMarble/Setup MainGame3DScene — 삼국마블 3D 씬 생성.
/// </summary>
public static class SamgukMarbleSceneSetup
{
    const string Root = "Assets/SamgukMarble";
    const string ScenePath = Root + "/Scenes/MainGame3DScene.unity";

    [MenuItem("Tools/SamgukMarble/Setup MainGame3DScene", false, 10)]
    public static void SetupScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolders();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 바닥 조명
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.9f);
        light.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.45f, 0.62f, 0.75f);
        cam.transform.position = new Vector3(0f, 28f, -18f);
        cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        camGo.AddComponent<AudioListener>();

        var root = new GameObject("SamgukMarbleGame");
        root.AddComponent<BoardBuilder3D>();
        root.AddComponent<BuildingManager>();
        root.AddComponent<TreasuryManager>();
        root.AddComponent<CardUI>();
        var gm = root.AddComponent<GameManager3D>();
        gm.Board = root.GetComponent<BoardBuilder3D>();
        gm.Buildings = root.GetComponent<BuildingManager>();
        gm.Treasury = root.GetComponent<TreasuryManager>();
        gm.Cards = root.GetComponent<CardUI>();
        gm.PlayerCount = 2;

        var diceGo = new GameObject("Dice3D");
        diceGo.transform.SetParent(root.transform, false);
        gm.Dice = diceGo.AddComponent<Dice3D>();

        // 에디터에서도 보드 미리보기 생성
        var board = root.GetComponent<BoardBuilder3D>();
        board.Build();

        Directory.CreateDirectory(Root + "/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureBuildSettings();

        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("[삼국마블] MainGame3DScene 셋업 완료. Play로 테스트하세요. Space=주사위");
    }

    [MenuItem("Tools/SamgukMarble/Rebuild Board In Open Scene", false, 11)]
    public static void RebuildBoard()
    {
        var board = Object.FindFirstObjectByType<BoardBuilder3D>();
        if (board == null)
        {
            Debug.LogWarning("BoardBuilder3D가 씬에 없습니다. Setup MainGame3DScene을 먼저 실행하세요.");
            return;
        }
        board.Build();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[삼국마블] 보드 100칸 재생성 완료");
    }

    static void EnsureFolders()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Root + "/Scripts");
        Directory.CreateDirectory(Root + "/Scenes");
        Directory.CreateDirectory(Root + "/Editor");
    }

    static void EnsureBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        bool exists = scenes.Any(s => s.path == ScenePath);
        if (!exists)
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
