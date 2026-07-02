using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DefenseSceneSetup))]
public class DefenseSceneSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("에디터 도구", EditorStyles.boldLabel);

        if (GUILayout.Button("① Defense 프리팹 전부 생성", GUILayout.Height(28f)))
            DefensePrefabFactory.CreateAllPrefabs(false);

        if (GUILayout.Button("② 프리팹 강제 재생성 (GUID 오류 시)", GUILayout.Height(28f)))
            DefensePrefabFactory.CreateAllPrefabs(true);

        if (GUILayout.Button("③ TestScene 전체 배치", GUILayout.Height(32f)))
            TestSceneDefenseSetup.SetupTestSceneAll();

        if (GUILayout.Button("④ 타워 배치 에디터 열기", GUILayout.Height(28f)))
            DefenseTowerPlacementEditor.OpenWindow();
    }
}
