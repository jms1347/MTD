#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 씬 뷰에서 타워 위치를 드래그하며 DefenseTowerLayout 에셋을 편집합니다.
/// </summary>
public class DefenseTowerPlacementEditor : EditorWindow
{
    private const string DefaultAssetPath = "Assets/Game/2Game/Data/SO/DefenseTowerLayout_Default.asset";

    private DefenseTowerLayout layout;
    private DefenseSceneSetup sceneSetup;
    private int selectedIndex = -1;
    private Vector2 scroll;

    [MenuItem("Tools/UkDefense/4. Tower Layout Editor", false, 3)]
    public static void OpenWindow()
    {
        DefenseMapEditorWindow.OpenWindow(DefenseMapEditorWindow.EditMode.Towers);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        TryAutoFindReferences();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("타워 배치 에디터", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "씬 뷰에서 타워 핸들을 드래그해 위치를 조정합니다.\n" +
            "위치는 넥서스(arenaCenter) + towerHeight 기준 오프셋입니다.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        layout = (DefenseTowerLayout)EditorGUILayout.ObjectField("Layout Asset", layout, typeof(DefenseTowerLayout), false);
        sceneSetup = (DefenseSceneSetup)EditorGUILayout.ObjectField("Scene Setup", sceneSetup, typeof(DefenseSceneSetup), true);
        if (EditorGUI.EndChangeCheck())
            Repaint();

        EditorGUILayout.Space(6f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("기본 에셋 생성/열기"))
                layout = EnsureDefaultAsset();

            if (GUILayout.Button("씬에서 찾기"))
                TryAutoFindReferences();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("농장 근처 프리셋"))
                ApplyNearFarmPreset();

            if (GUILayout.Button("프리팹 연결"))
                ApplyPrefabsFromSceneSetup();
        }

        if (layout == null)
        {
            EditorGUILayout.HelpBox("DefenseTowerLayout 에셋을 지정하거나 '기본 에셋 생성'을 눌러 주세요.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(4f);
        layout.arenaCenter = EditorGUILayout.Vector3Field("Arena Center", layout.arenaCenter);
        layout.towerHeight = EditorGUILayout.FloatField("Tower Height", layout.towerHeight);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField($"타워 ({layout.towers.Count})", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < layout.towers.Count; i++)
        {
            DrawTowerRow(i);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("타워 추가"))
                AddTower();

            GUI.enabled = selectedIndex >= 0 && selectedIndex < layout.towers.Count;
            if (GUILayout.Button("선택 삭제"))
                RemoveSelectedTower();
            GUI.enabled = true;
        }

        if (sceneSetup != null && GUILayout.Button("DefenseSceneSetup에 Layout 연결", GUILayout.Height(26f)))
        {
            Undo.RecordObject(sceneSetup, "Assign Tower Layout");
            var so = new SerializedObject(sceneSetup);
            so.FindProperty("towerLayout").objectReferenceValue = layout;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(sceneSetup);
        }
    }

    private void DrawTowerRow(int index)
    {
        var tower = layout.towers[index];
        bool selected = selectedIndex == index;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(selected, tower.towerName, "Button", GUILayout.Height(22f)))
                    selectedIndex = index;
                else if (selected)
                    selectedIndex = -1;

                tower.kind = (TowerKind)EditorGUILayout.EnumPopup(tower.kind, GUILayout.Width(110f));
            }

            if (!selected)
                return;

            EditorGUI.BeginChangeCheck();
            tower.towerName = EditorGUILayout.TextField("Name", tower.towerName);
            tower.color = EditorGUILayout.ColorField("Color", tower.color);
            tower.positionOffset = EditorGUILayout.Vector3Field("Position Offset", tower.positionOffset);
            tower.rotationY = EditorGUILayout.FloatField("Rotation Y", tower.rotationY);
            tower.scaleMultiplier = EditorGUILayout.Vector3Field("Scale", tower.scaleMultiplier);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(layout);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("씬 뷰에서 포커스"))
            {
                Vector3 world = layout.TowerOrigin + tower.positionOffset;
                SceneView.lastActiveSceneView?.LookAt(world);
            }
        }
    }

    private void OnSceneGUI(SceneView view)
    {
        if (layout == null || layout.towers == null)
            return;

        Vector3 origin = layout.TowerOrigin;

        Handles.color = new Color(0.3f, 0.75f, 1f, 0.35f);
        Handles.DrawWireDisc(DefenseTowerLayoutDefaults.FarmAreaCenter + layout.arenaCenter, Vector3.up, 2.2f);
        Handles.Label(DefenseTowerLayoutDefaults.FarmAreaCenter + layout.arenaCenter, "Farm");

        Handles.color = Color.yellow;
        Handles.DrawWireDisc(layout.arenaCenter, Vector3.up, 0.6f);
        Handles.Label(layout.arenaCenter, "Nexus");

        for (int i = 0; i < layout.towers.Count; i++)
        {
            var tower = layout.towers[i];
            Vector3 world = origin + tower.positionOffset;
            float handleSize = HandleUtility.GetHandleSize(world) * 0.22f;

            Handles.color = tower.color;
            Handles.CubeHandleCap(0, world, Quaternion.Euler(0f, tower.rotationY, 0f), handleSize, EventType.Repaint);

            if (selectedIndex == i)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newWorld = Handles.PositionHandle(world, Quaternion.Euler(0f, tower.rotationY, 0f));
                float newRotation = Handles.RotationHandle(Quaternion.Euler(0f, tower.rotationY, 0f), world).eulerAngles.y;
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(layout, "Move Tower");
                    tower.positionOffset = newWorld - origin;
                    tower.rotationY = newRotation;
                    EditorUtility.SetDirty(layout);
                }
            }

            Handles.Label(world + Vector3.up * 0.8f, tower.towerName);
        }
    }

    private void TryAutoFindReferences()
    {
        if (sceneSetup == null)
            sceneSetup = FindFirstObjectByType<DefenseSceneSetup>();

        if (layout == null && sceneSetup != null)
        {
            var so = new SerializedObject(sceneSetup);
            layout = so.FindProperty("towerLayout").objectReferenceValue as DefenseTowerLayout;
        }

        if (layout == null)
            layout = AssetDatabase.LoadAssetAtPath<DefenseTowerLayout>(DefaultAssetPath);
    }

    public static DefenseTowerLayout GetOrCreateDefaultAsset()
    {
        return EnsureDefaultAsset();
    }

    private static DefenseTowerLayout EnsureDefaultAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<DefenseTowerLayout>(DefaultAssetPath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            return existing;
        }

        string dir = Path.GetDirectoryName(DefaultAssetPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var asset = ScriptableObject.CreateInstance<DefenseTowerLayout>();
        asset.SetDefaultNearFarmLayout();
        AssetDatabase.CreateAsset(asset, DefaultAssetPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = asset;
        Debug.Log($"[Tower Layout] 생성됨: {DefaultAssetPath}");
        return asset;
    }

    private void ApplyNearFarmPreset()
    {
        if (layout == null)
            layout = EnsureDefaultAsset();

        Undo.RecordObject(layout, "Near Farm Preset");
        layout.SetDefaultNearFarmLayout();
        EditorUtility.SetDirty(layout);
        SceneView.RepaintAll();
    }

    private void ApplyPrefabsFromSceneSetup()
    {
        if (layout == null || sceneSetup == null || sceneSetup.CombatCatalog == null)
            return;

        DefenseTowerLayoutApplier.ApplyCombatReferences(layout.ToSpawnArray(), sceneSetup.CombatCatalog);
        EditorUtility.SetDirty(layout);
        Debug.Log("[Tower Layout] DefenseSceneSetup 프리팹 참조를 적용했습니다.");
    }

    private void AddTower()
    {
        Undo.RecordObject(layout, "Add Tower");
        layout.towers.Add(new TowerSpawnData
        {
            towerName = $"Tower_{layout.towers.Count + 1:00}",
            positionOffset = DefenseTowerLayoutDefaults.FarmAreaCenter + new Vector3(2f, 0f, 0f),
            color = Color.white
        });
        selectedIndex = layout.towers.Count - 1;
        EditorUtility.SetDirty(layout);
    }

    private void RemoveSelectedTower()
    {
        Undo.RecordObject(layout, "Remove Tower");
        layout.towers.RemoveAt(selectedIndex);
        selectedIndex = Mathf.Clamp(selectedIndex - 1, -1, layout.towers.Count - 1);
        EditorUtility.SetDirty(layout);
    }
}
#endif
