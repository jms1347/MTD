#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 타일 맵 페인트 + 타워 배치 + 씬 연동 통합 에디터.
/// </summary>
public class DefenseMapEditorWindow : EditorWindow
{
    public enum EditMode
    {
        Paint,
        Markers,
        Towers
    }

    private const string DefaultMapPath = "Assets/Game/2Game/Data/SO/DefenseMapLayout_Default.asset";

    private DefenseMapLayout mapLayout;
    private DefenseSceneSetup sceneSetup;
    private EditMode editMode = EditMode.Paint;
    private DefenseMapTileType brushType = DefenseMapTileType.Path;
    private int selectedTowerIndex = -1;
    private bool showGrid = true;
    private bool showTileOverlay = true;
    private bool showHoverCell = true;
    private bool snapTowersToGrid = true;
    private bool paintLanesOnPreview;
    private Vector2Int? hoverCell;
    private Vector2Int? linePaintStart;
    private DefenseTowerEditorHandles.Tool towerTool = DefenseTowerEditorHandles.Tool.Move;
    private Vector2 scroll;

    [MenuItem("Tools/UkDefense/5. Map & Tower Editor", false, 4)]
    public static void OpenWindow()
    {
        OpenWindow(EditMode.Paint);
    }

    public static void OpenWindow(EditMode startMode)
    {
        var window = GetWindow<DefenseMapEditorWindow>("Map Editor");
        window.minSize = new Vector2(360f, 480f);
        window.editMode = startMode;
        window.Show();
    }

    public static DefenseMapLayout GetOrCreateDefaultAsset()
    {
        return EnsureDefaultMapAsset();
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
        EditorGUILayout.LabelField("맵 & 타워 에디터", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "타일: 좌클릭/드래그 = 칠하기 · 우클릭 = 잔디(지우기) · Shift+클릭 = 직선 · Alt+클릭 = 타일 샘플\n" +
            "농장 문(FarmGate)은 전투 시 닫히고 준비 시 열립니다. '농장만 배치'로 위치만 맞출 수 있습니다.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        mapLayout = (DefenseMapLayout)EditorGUILayout.ObjectField("Map Layout", mapLayout, typeof(DefenseMapLayout), false);
        sceneSetup = (DefenseSceneSetup)EditorGUILayout.ObjectField("Scene Setup", sceneSetup, typeof(DefenseSceneSetup), true);
        if (EditorGUI.EndChangeCheck())
            Repaint();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("기본 맵 생성/열기"))
                mapLayout = EnsureDefaultMapAsset();

            if (GUILayout.Button("씬에서 찾기"))
                TryAutoFindReferences();
        }

        if (mapLayout == null)
        {
            EditorGUILayout.HelpBox("DefenseMapLayout 에셋을 지정하거나 '기본 맵 생성'을 눌러 주세요.", MessageType.Warning);
            return;
        }

        mapLayout.EnsureTiles();

        EditorGUILayout.Space(6f);
        editMode = (EditMode)GUILayout.Toolbar((int)editMode, new[] { "타일", "마커", "타워" });

        scroll = EditorGUILayout.BeginScrollView(scroll);
        switch (editMode)
        {
            case EditMode.Paint:
                DrawPaintPanel();
                break;
            case EditMode.Markers:
                DrawMarkersPanel();
                break;
            case EditMode.Towers:
                DrawTowersPanel();
                break;
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8f);
        DrawSceneActions();
    }

    private void DrawPaintPanel()
    {
        EditorGUILayout.LabelField("그리드", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        mapLayout.width = EditorGUILayout.IntField("Width", mapLayout.width);
        mapLayout.height = EditorGUILayout.IntField("Height", mapLayout.height);
        mapLayout.cellSize = EditorGUILayout.FloatField("Cell Size", mapLayout.cellSize);
        mapLayout.origin = EditorGUILayout.Vector3Field("Origin", mapLayout.origin);
        mapLayout.autoGenerateLanes = EditorGUILayout.Toggle("플레이 시 레인 자동 생성", mapLayout.autoGenerateLanes);
        if (EditorGUI.EndChangeCheck())
        {
            mapLayout.width = Mathf.Max(4, mapLayout.width);
            mapLayout.height = Mathf.Max(4, mapLayout.height);
            mapLayout.cellSize = Mathf.Max(0.25f, mapLayout.cellSize);
            mapLayout.EnsureTiles();
            EditorUtility.SetDirty(mapLayout);
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("브러시", EditorStyles.boldLabel);
        DrawBrushPalette();

        EditorGUILayout.Space(4f);
        showGrid = EditorGUILayout.Toggle("그리드 표시", showGrid);
        showTileOverlay = EditorGUILayout.Toggle("타일 오버레이", showTileOverlay);
        showHoverCell = EditorGUILayout.Toggle("호버 셀 표시", showHoverCell);
        paintLanesOnPreview = EditorGUILayout.Toggle("미리보기 시 레인 생성", paintLanesOnPreview);

        if (hoverCell.HasValue)
            EditorGUILayout.LabelField($"호버 셀: {hoverCell.Value}  ({mapLayout.GetTile(hoverCell.Value)})");

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("스케치 맵 생성 (48×48)"))
                ApplyFreshMapPreset();

            if (GUILayout.Button("농장만 배치"))
                ApplyFarmPlotsOnly();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("레인 자동 생성"))
                GenerateLanesOnMap();

            if (GUILayout.Button("전체 잔디"))
                FillMap(DefenseMapTileType.Grass);
        }
    }

    private void DrawBrushPalette()
    {
        var types = new[]
        {
            DefenseMapTileType.Grass,
            DefenseMapTileType.Path,
            DefenseMapTileType.FarmSoil,
            DefenseMapTileType.FarmGate,
            DefenseMapTileType.Obstacle
        };

        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (var type in types)
            {
                var color = GetTileColor(type);
                color.a = brushType == type ? 1f : 0.55f;
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = color;
                string label = type == DefenseMapTileType.Grass ? "잔디" : type.ToString();
                if (GUILayout.Toggle(brushType == type, label, "Button", GUILayout.Height(24f)))
                    brushType = type;
                GUI.backgroundColor = prev;
            }
        }
    }

    private void DrawMarkersPanel()
    {
        EditorGUILayout.LabelField("주요 위치 (셀 좌표)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        mapLayout.nexusCell = EditorGUILayout.Vector2IntField("넥서스", mapLayout.nexusCell);
        mapLayout.playerSpawnCell = EditorGUILayout.Vector2IntField("플레이어 스폰", mapLayout.playerSpawnCell);
        mapLayout.farmGateCell = EditorGUILayout.Vector2IntField("농장 문", mapLayout.farmGateCell);
        if (EditorGUI.EndChangeCheck())
        {
            ClampMarkerCells();
            EditorUtility.SetDirty(mapLayout);
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("월드 좌표 (미리보기)", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"넥서스: {mapLayout.GetNexusWorld()}");
        EditorGUILayout.LabelField($"스폰: {mapLayout.GetPlayerSpawnWorld()}");
        EditorGUILayout.LabelField($"문: {mapLayout.GetFarmGateWorld()}");
    }

    private void DrawTowersPanel()
    {
        var towerLayout = mapLayout.towerLayout;
        if (towerLayout == null)
        {
            EditorGUILayout.HelpBox("타워 레이아웃이 연결되지 않았습니다. 아래 버튼으로 생성하세요.", MessageType.Warning);
            if (GUILayout.Button("타워 레이아웃 생성/연결"))
                LinkTowerLayout();
            return;
        }

        snapTowersToGrid = EditorGUILayout.Toggle("그리드 스냅", snapTowersToGrid);
        towerTool = (DefenseTowerEditorHandles.Tool)GUILayout.Toolbar(
            (int)towerTool,
            new[] { "이동 (W)", "회전 (E)" });
        mapLayout.SyncTowerLayout();
        towerLayout.towerHeight = EditorGUILayout.FloatField("Tower Height", towerLayout.towerHeight);

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("농장 근처 프리셋"))
            {
                Undo.RecordObject(towerLayout, "Tower Preset");
                Undo.RecordObject(mapLayout, "Map Preset");
                DefenseMapLayoutDefaults.ApplyDefaultLayout(mapLayout);
                EditorUtility.SetDirty(mapLayout);
                EditorUtility.SetDirty(towerLayout);
            }

            if (GUILayout.Button("프리팹 연결") && sceneSetup != null && sceneSetup.CombatCatalog != null)
            {
                DefenseTowerLayoutApplier.ApplyCombatReferences(towerLayout.ToSpawnArray(), sceneSetup.CombatCatalog);
                EditorUtility.SetDirty(towerLayout);
            }
        }

        EditorGUILayout.LabelField($"타워 ({towerLayout.towers.Count})", EditorStyles.boldLabel);
        for (int i = 0; i < towerLayout.towers.Count; i++)
        {
            var tower = towerLayout.towers[i];
            bool selected = selectedTowerIndex == i;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(selected, tower.towerName, "Button"))
                    selectedTowerIndex = i;
                else if (selected)
                    selectedTowerIndex = -1;

                tower.kind = (TowerKind)EditorGUILayout.EnumPopup(tower.kind, GUILayout.Width(110f));
            }

            if (!selected)
                continue;

            EditorGUI.BeginChangeCheck();
            tower.towerName = EditorGUILayout.TextField("Name", tower.towerName);
            tower.color = EditorGUILayout.ColorField("Color", tower.color);
            tower.rotationY = EditorGUILayout.Slider("Rotation Y", tower.rotationY, 0f, 360f);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(towerLayout);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("씬 뷰 포커스"))
            {
                Vector3 world = towerLayout.TowerOrigin + tower.positionOffset;
                SceneView.lastActiveSceneView?.LookAt(world);
            }
        }

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("타워 추가"))
            {
                Undo.RecordObject(towerLayout, "Add Tower");
                Vector3 spawn = DefenseMapGrid.CellToWorld(mapLayout, mapLayout.playerSpawnCell);
                towerLayout.towers.Add(new TowerSpawnData
                {
                    towerName = $"Tower_{towerLayout.towers.Count + 1:00}",
                    positionOffset = spawn - towerLayout.TowerOrigin,
                    color = Color.white
                });
                selectedTowerIndex = towerLayout.towers.Count - 1;
                EditorUtility.SetDirty(towerLayout);
            }

            GUI.enabled = selectedTowerIndex >= 0 && selectedTowerIndex < towerLayout.towers.Count;
            if (GUILayout.Button("선택 삭제"))
            {
                Undo.RecordObject(towerLayout, "Remove Tower");
                towerLayout.towers.RemoveAt(selectedTowerIndex);
                selectedTowerIndex = Mathf.Clamp(selectedTowerIndex - 1, -1, towerLayout.towers.Count - 1);
                EditorUtility.SetDirty(towerLayout);
            }
            GUI.enabled = true;
        }
    }

    private void DrawSceneActions()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("씬 미리보기 빌드", GUILayout.Height(26f)))
                BuildPreviewInScene();

            if (sceneSetup != null && GUILayout.Button("SceneSetup 연결", GUILayout.Height(26f)))
                AssignToSceneSetup();
        }
    }

    private void OnSceneGUI(SceneView view)
    {
        if (mapLayout == null)
            return;

        mapLayout.EnsureTiles();

        if (editMode == EditMode.Paint)
        {
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlId);
            UpdateHoverCell(view);
        }

        if (showGrid)
            DrawGrid();

        if (showTileOverlay)
            DrawTileOverlay();

        if (showHoverCell && hoverCell.HasValue && mapLayout.IsInside(hoverCell.Value))
            DrawHoverCell(hoverCell.Value);

        if (editMode == EditMode.Towers)
        {
            DefenseTowerEditorHandles.DrawToolbar(view.position, ref towerTool, ref snapTowersToGrid);
            HandleTowerHandles(view);
            return;
        }

        DrawMarkersGizmo();

        if (editMode == EditMode.Markers)
            HandleMarkerHandles();
        else if (editMode == EditMode.Paint)
            HandlePaintInput();
    }

    private void HandlePaintInput()
    {
        var e = Event.current;

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            linePaintStart = null;
            e.Use();
            return;
        }

        if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            SceneView.RepaintAll();

        if (e.alt && e.type == EventType.MouseDown && e.button == 0)
        {
            if (TryRaycastMap(e.mousePosition, out _) && hoverCell.HasValue)
            {
                brushType = mapLayout.GetTile(hoverCell.Value);
                if (brushType == DefenseMapTileType.Grass && e.shift)
                    brushType = DefenseMapTileType.Path;
                Repaint();
                e.Use();
            }

            return;
        }

        if (e.type == EventType.MouseUp && e.button == 0)
            linePaintStart = null;

        bool erase = e.button == 1;
        if (e.button != 0 && !erase)
            return;

        if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag)
            return;

        if (!TryRaycastMap(e.mousePosition, out _))
            return;

        if (!hoverCell.HasValue || !mapLayout.IsInside(hoverCell.Value))
            return;

        var targetType = erase ? DefenseMapTileType.Grass : brushType;

        if (e.shift && e.type == EventType.MouseDown)
        {
            linePaintStart = hoverCell;
            PaintCell(hoverCell.Value, targetType);
            e.Use();
            return;
        }

        if (e.shift && linePaintStart.HasValue)
        {
            PaintLine(linePaintStart.Value, hoverCell.Value, targetType);
            e.Use();
            return;
        }

        PaintCell(hoverCell.Value, targetType);
        e.Use();
        SceneView.RepaintAll();
    }

    private void UpdateHoverCell(SceneView view)
    {
        if (!TryRaycastMap(Event.current.mousePosition, out Vector3 hit))
        {
            hoverCell = null;
            return;
        }

        hoverCell = DefenseMapGrid.WorldToCell(mapLayout, hit);
    }

    private void PaintCell(Vector2Int cell, DefenseMapTileType type)
    {
        if (!mapLayout.IsInside(cell))
            return;

        Undo.RecordObject(mapLayout, "Paint Tile");
        mapLayout.SetTile(cell, type);
        EditorUtility.SetDirty(mapLayout);
    }

    private void PaintLine(Vector2Int from, Vector2Int to, DefenseMapTileType type)
    {
        Undo.RecordObject(mapLayout, "Paint Line");
        Vector2Int current = from;
        for (int safety = 0; safety < 512; safety++)
        {
            if (!mapLayout.IsInside(current))
                break;

            mapLayout.SetTile(current, type);
            if (current == to)
                break;

            if (current.x != to.x)
                current.x += current.x < to.x ? 1 : -1;
            else if (current.y != to.y)
                current.y += current.y < to.y ? 1 : -1;
            else
                break;
        }

        EditorUtility.SetDirty(mapLayout);
    }

    private void DrawHoverCell(Vector2Int cell)
    {
        Vector3 center = DefenseMapGrid.CellToWorld(mapLayout, cell) + Vector3.up * 0.05f;
        Handles.color = new Color(1f, 1f, 0.2f, 0.85f);
        Handles.DrawWireCube(center, new Vector3(mapLayout.cellSize * 0.98f, 0.05f, mapLayout.cellSize * 0.98f));
        Handles.Label(center + Vector3.up * 0.35f, $"{cell.x},{cell.y}\n{mapLayout.GetTile(cell)}");
    }

    private void HandleMarkerHandles()
    {
        EditorGUI.BeginChangeCheck();
        Vector3 nexusWorld = DefenseMapGrid.CellToWorld(mapLayout, mapLayout.nexusCell);
        Vector3 spawnWorld = DefenseMapGrid.CellToWorld(mapLayout, mapLayout.playerSpawnCell);
        Vector3 gateWorld = DefenseMapGrid.CellToWorld(mapLayout, mapLayout.farmGateCell);

        nexusWorld = Handles.PositionHandle(nexusWorld, Quaternion.identity);
        spawnWorld = Handles.PositionHandle(spawnWorld + Vector3.up * 0.2f, Quaternion.identity) - Vector3.up * 0.2f;
        gateWorld = Handles.PositionHandle(gateWorld + Vector3.up * 0.4f, Quaternion.identity) - Vector3.up * 0.4f;

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(mapLayout, "Move Marker");
            mapLayout.nexusCell = DefenseMapGrid.WorldToCell(mapLayout, nexusWorld);
            mapLayout.playerSpawnCell = DefenseMapGrid.WorldToCell(mapLayout, spawnWorld);
            mapLayout.farmGateCell = DefenseMapGrid.WorldToCell(mapLayout, gateWorld);
            ClampMarkerCells();
            EditorUtility.SetDirty(mapLayout);
        }

        Handles.Label(nexusWorld + Vector3.up, "Nexus");
        Handles.Label(spawnWorld + Vector3.up, "Player");
        Handles.Label(gateWorld + Vector3.up * 1.2f, "Gate");
    }

    private void HandleTowerHandles(SceneView view)
    {
        var towerLayout = mapLayout.towerLayout;
        if (towerLayout == null)
            return;

        if (showGrid)
            DrawGrid();

        var ctx = new DefenseTowerEditorHandles.Context
        {
            layout = towerLayout,
            mapLayout = mapLayout,
            selectedIndex = selectedTowerIndex,
            activeTool = towerTool,
            snapToGrid = snapTowersToGrid
        };

        var handleResult = DefenseTowerEditorHandles.DrawSceneHandles(ctx);

        if (handleResult.toolChanged)
            towerTool = handleResult.activeTool;

        if (handleResult.selectedIndex != selectedTowerIndex)
        {
            selectedTowerIndex = handleResult.selectedIndex;
            Repaint();
        }

        if (handleResult.changed)
        {
            EditorUtility.SetDirty(towerLayout);
            view.Repaint();
        }
    }

    private void DrawGrid()
    {
        Handles.color = new Color(1f, 1f, 1f, 0.12f);
        float halfW = mapLayout.width * mapLayout.cellSize * 0.5f;
        float halfH = mapLayout.height * mapLayout.cellSize * 0.5f;
        Vector3 min = mapLayout.origin + new Vector3(-halfW, 0.02f, -halfH);
        Vector3 max = mapLayout.origin + new Vector3(halfW, 0.02f, halfH);

        for (int x = 0; x <= mapLayout.width; x++)
        {
            float t = x / (float)mapLayout.width;
            float wx = Mathf.Lerp(min.x, max.x, t);
            Handles.DrawLine(new Vector3(wx, min.y, min.z), new Vector3(wx, min.y, max.z));
        }

        for (int y = 0; y <= mapLayout.height; y++)
        {
            float t = y / (float)mapLayout.height;
            float wz = Mathf.Lerp(min.z, max.z, t);
            Handles.DrawLine(new Vector3(min.x, min.y, wz), new Vector3(max.x, min.y, wz));
        }
    }

    private void DrawTileOverlay()
    {
        for (int y = 0; y < mapLayout.height; y++)
        {
            for (int x = 0; x < mapLayout.width; x++)
            {
                var cell = new Vector2Int(x, y);
                var type = mapLayout.GetTile(cell);
                if (type == DefenseMapTileType.Grass)
                    continue;

                Vector3 center = DefenseMapGrid.CellToWorld(mapLayout, cell) + Vector3.up * 0.03f;
                Color tileColor = GetTileColor(type);
                tileColor.a = 0.45f;
                Handles.color = tileColor;
                Handles.DrawSolidRectangleWithOutline(
                    GetCellCorners(center, mapLayout.cellSize * 0.96f),
                    Handles.color,
                    Color.clear);
            }
        }
    }

    private void DrawMarkersGizmo()
    {
        Handles.color = Color.cyan;
        Handles.DrawWireDisc(mapLayout.GetNexusWorld(), Vector3.up, mapLayout.cellSize * 0.35f);
    }

    private static Vector3[] GetCellCorners(Vector3 center, float size)
    {
        float half = size * 0.5f;
        return new[]
        {
            center + new Vector3(-half, 0f, -half),
            center + new Vector3(-half, 0f, half),
            center + new Vector3(half, 0f, half),
            center + new Vector3(half, 0f, -half)
        };
    }

    private static Color GetTileColor(DefenseMapTileType type)
    {
        return type switch
        {
            DefenseMapTileType.FarmSoil => new Color(0.7f, 0.45f, 0.2f),
            DefenseMapTileType.Path => new Color(0.55f, 0.5f, 0.4f),
            DefenseMapTileType.Obstacle => new Color(0.45f, 0.35f, 0.25f),
            DefenseMapTileType.FarmGate => new Color(0.2f, 0.85f, 0.35f),
            _ => new Color(0.35f, 0.5f, 0.3f)
        };
    }

    private bool TryRaycastMap(Vector2 guiPosition, out Vector3 hit)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);
        var plane = new Plane(Vector3.up, mapLayout.origin);
        if (!plane.Raycast(ray, out float dist))
        {
            hit = default;
            return false;
        }

        hit = ray.GetPoint(dist);
        return true;
    }

    private void FillMap(DefenseMapTileType type)
    {
        Undo.RecordObject(mapLayout, "Fill Map");
        mapLayout.Fill(type);
        EditorUtility.SetDirty(mapLayout);
        SceneView.RepaintAll();
    }

    private void ApplyFreshMapPreset()
    {
        Undo.RecordObject(mapLayout, "Fresh Map Preset");
        DefenseMapLayoutDefaults.ApplyDefaultLayout(mapLayout);
        if (mapLayout.towerLayout != null)
            EditorUtility.SetDirty(mapLayout.towerLayout);
        EditorUtility.SetDirty(mapLayout);
        SceneView.RepaintAll();
    }

    private void ApplyFarmPlotsOnly()
    {
        Undo.RecordObject(mapLayout, "Farm Plots Only");
        DefenseMapLayoutDefaults.PaintReferenceFarms(mapLayout);
        EditorUtility.SetDirty(mapLayout);
        SceneView.RepaintAll();
    }

    private void GenerateLanesOnMap()
    {
        Undo.RecordObject(mapLayout, "Generate Lanes");
        DefenseMapLayoutDefaults.GenerateLanes(mapLayout, paintTiles: true);
        EditorUtility.SetDirty(mapLayout);
        SceneView.RepaintAll();
    }

    private void ClampMarkerCells()
    {
        mapLayout.nexusCell = ClampCell(mapLayout.nexusCell);
        mapLayout.playerSpawnCell = ClampCell(mapLayout.playerSpawnCell);
        mapLayout.farmGateCell = ClampCell(mapLayout.farmGateCell);
    }

    private Vector2Int ClampCell(Vector2Int cell)
    {
        return new Vector2Int(
            Mathf.Clamp(cell.x, 0, mapLayout.width - 1),
            Mathf.Clamp(cell.y, 0, mapLayout.height - 1));
    }

    private void BuildPreviewInScene()
    {
        if (mapLayout == null)
            return;

        Undo.RegisterFullObjectHierarchyUndo(FindMapRootOrCreate(), "Build Map Preview");

        if (mapLayout.autoGenerateLanes || paintLanesOnPreview)
            DefenseMapLayoutDefaults.GenerateLanes(mapLayout, paintTiles: true);
        else
            DefenseMonsterLaneRegistry.Clear();

        DefenseMapBuilder.Build(mapLayout);
        SceneView.RepaintAll();
    }

    private static GameObject FindMapRootOrCreate()
    {
        var existing = GameObject.Find("DefenseMap");
        return existing != null ? existing : new GameObject("DefenseMap");
    }

    private void AssignToSceneSetup()
    {
        if (sceneSetup == null || mapLayout == null)
            return;

        Undo.RecordObject(sceneSetup, "Assign Map Layout");
        var so = new SerializedObject(sceneSetup);
        so.FindProperty("mapLayout").objectReferenceValue = mapLayout;
        so.FindProperty("towerLayout").objectReferenceValue = mapLayout.towerLayout;
        so.FindProperty("arenaCenter").vector3Value = mapLayout.GetNexusWorld();
        so.FindProperty("mapHalfExtent").floatValue = mapLayout.MapHalfExtent;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(sceneSetup);
        Debug.Log("[Map Editor] DefenseSceneSetup에 맵을 연결했습니다.");
    }

    private void LinkTowerLayout()
    {
        var towerLayout = DefenseTowerPlacementEditor.GetOrCreateDefaultAsset();
        Undo.RecordObject(mapLayout, "Link Tower Layout");
        mapLayout.towerLayout = towerLayout;
        mapLayout.SyncTowerLayout();
        EditorUtility.SetDirty(mapLayout);
    }

    private void TryAutoFindReferences()
    {
        if (sceneSetup == null)
            sceneSetup = FindFirstObjectByType<DefenseSceneSetup>();

        if (mapLayout == null && sceneSetup != null)
        {
            var so = new SerializedObject(sceneSetup);
            mapLayout = so.FindProperty("mapLayout").objectReferenceValue as DefenseMapLayout;
        }

        if (mapLayout == null)
            mapLayout = AssetDatabase.LoadAssetAtPath<DefenseMapLayout>(DefaultMapPath);

        if (mapLayout != null && mapLayout.towerLayout == null)
            mapLayout.towerLayout = AssetDatabase.LoadAssetAtPath<DefenseTowerLayout>(
                "Assets/Game/2Game/Data/SO/DefenseTowerLayout_Default.asset");
    }

    private static DefenseMapLayout EnsureDefaultMapAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<DefenseMapLayout>(DefaultMapPath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            return existing;
        }

        string dir = Path.GetDirectoryName(DefaultMapPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var map = ScriptableObject.CreateInstance<DefenseMapLayout>();
        var towerLayout = DefenseTowerPlacementEditor.GetOrCreateDefaultAsset();
        map.towerLayout = towerLayout;
        DefenseMapLayoutDefaults.ApplyDefaultLayout(map);

        AssetDatabase.CreateAsset(map, DefaultMapPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = map;
        Debug.Log($"[Map Editor] 생성됨: {DefaultMapPath}");
        return map;
    }
}
#endif
