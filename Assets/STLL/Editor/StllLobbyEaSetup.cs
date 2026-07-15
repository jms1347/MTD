#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StllLobbyEaSetup
{
    [MenuItem("Tools/STLL/Apply Lobby → STLL EA Scene", false, 21)]
    public static void ApplyLobbyBootstrap()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Game/0Scene/LobbyScene.unity");
        var bootstraps = Object.FindObjectsByType<LobbySceneBootstrap>(FindObjectsSortMode.None);
        if (bootstraps.Length == 0)
        {
            Debug.LogWarning("[STLL] LobbySceneBootstrap을 찾지 못했습니다.");
            return;
        }

        foreach (var bootstrap in bootstraps)
        {
            var serialized = new SerializedObject(bootstrap);
            var property = serialized.FindProperty("useStllEaScene");
            if (property != null)
            {
                property.boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(bootstrap);
            }
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[STLL] 로비가 StllGameScene(EA)으로 연결되었습니다.");
    }
}
#endif
