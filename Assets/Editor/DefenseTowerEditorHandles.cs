#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 씬 뷰 타워 배치 핸들 (클릭 선택, XZ 이동, 그리드 스냅).
/// </summary>
public static class DefenseTowerEditorHandles
{
    public enum Tool
    {
        Move,
        Rotate
    }

    public struct Context
    {
        public DefenseTowerLayout layout;
        public DefenseMapLayout mapLayout;
        public int selectedIndex;
        public Tool activeTool;
        public bool snapToGrid;
    }

    public struct Result
    {
        public int selectedIndex;
        public bool changed;
        public bool toolChanged;
        public Tool activeTool;
    }

    public static Result DrawSceneHandles(Context ctx)
    {
        var result = new Result
        {
            selectedIndex = ctx.selectedIndex,
            activeTool = ctx.activeTool
        };

        if (ctx.layout == null || ctx.layout.towers == null)
            return result;

        HandleToolShortcuts(ref result);

        Vector3 origin = ctx.layout.TowerOrigin;
        float groundY = origin.y;

        DrawNexusAndFarmGuides(ctx);

        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            int picked = PickTower(ctx, groundY, e.mousePosition);
            if (picked >= 0)
            {
                result.selectedIndex = picked;
                e.Use();
                GUIUtility.hotControl = 0;
            }
        }

        for (int i = 0; i < ctx.layout.towers.Count; i++)
        {
            var tower = ctx.layout.towers[i];
            Vector3 world = GetTowerWorld(ctx.layout, i, groundY);
            float size = HandleUtility.GetHandleSize(world) * (i == result.selectedIndex ? 0.32f : 0.24f);
            Handles.color = i == result.selectedIndex ? Color.white : tower.color;
            Handles.CubeHandleCap(
                0,
                world,
                Quaternion.Euler(0f, tower.rotationY, 0f),
                size,
                EventType.Repaint);

            if (i != result.selectedIndex)
                Handles.Label(world + Vector3.up * 0.7f, tower.towerName, EditorStyles.miniLabel);
        }

        if (result.selectedIndex < 0 || result.selectedIndex >= ctx.layout.towers.Count)
            return result;

        var selected = ctx.layout.towers[result.selectedIndex];
        Vector3 selectedWorld = GetTowerWorld(ctx.layout, result.selectedIndex, groundY);

        if (ctx.snapToGrid && ctx.mapLayout != null)
            DrawSnapCellHighlight(ctx.mapLayout, selectedWorld);

        Handles.Label(selectedWorld + Vector3.up * 1.15f, selected.towerName, EditorStyles.whiteBoldLabel);

        EditorGUI.BeginChangeCheck();

        if (ctx.activeTool == Tool.Rotate)
        {
            Quaternion rot = Quaternion.Euler(0f, selected.rotationY, 0f);
            rot = Handles.RotationHandle(rot, selectedWorld);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(ctx.layout, "Rotate Tower");
                selected.rotationY = rot.eulerAngles.y;
                result.changed = true;
            }
        }
        else
        {
            Vector3 newWorld = Handles.Slider2D(
                selectedWorld,
                Vector3.up,
                Vector3.right,
                Vector3.forward,
                HandleUtility.GetHandleSize(selectedWorld) * 0.85f,
                Handles.CircleHandleCap,
                0f);

            newWorld.y = groundY;
            if (ctx.snapToGrid && ctx.mapLayout != null)
                newWorld = DefenseMapGrid.SnapWorldToCellCenter(ctx.mapLayout, newWorld);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(ctx.layout, "Move Tower");
                selected.positionOffset = newWorld - origin;
                result.changed = true;
            }
        }

        HandleKeyboardNudge(ctx, result.selectedIndex, groundY, ref result);

        return result;
    }

    public static void DrawToolbar(Rect sceneViewRect, ref Tool tool, ref bool snapToGrid)
    {
        Handles.BeginGUI();
        var area = new Rect(10f, sceneViewRect.height - 54f, 400f, 40f);
        GUILayout.BeginArea(area, EditorStyles.helpBox);
        GUILayout.BeginHorizontal();
        tool = (Tool)GUILayout.Toolbar((int)tool, new[] { "이동 (W)", "회전 (E)" }, GUILayout.Height(24f));
        snapToGrid = GUILayout.Toggle(snapToGrid, "그리드 스냅", GUILayout.Height(24f), GUILayout.Width(90f));
        GUILayout.Label("클릭 선택 · 화살표 이동", EditorStyles.miniLabel);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private static void HandleToolShortcuts(ref Result result)
    {
        var e = Event.current;
        if (e.type != EventType.KeyDown || e.control || e.command || e.alt)
            return;

        switch (e.keyCode)
        {
            case KeyCode.W:
                result.activeTool = Tool.Move;
                result.toolChanged = true;
                e.Use();
                break;
            case KeyCode.E:
                result.activeTool = Tool.Rotate;
                result.toolChanged = true;
                e.Use();
                break;
        }
    }

    private static void DrawNexusAndFarmGuides(Context ctx)
    {
        Vector3 nexus = ctx.mapLayout != null
            ? ctx.mapLayout.GetNexusWorld()
            : ctx.layout.arenaCenter;

        Handles.color = Color.yellow;
        Handles.DrawWireDisc(nexus, Vector3.up, 0.55f);
        Handles.Label(nexus + Vector3.up * 0.6f, "Nexus");

        Vector3 farmCenter = ctx.mapLayout != null
            ? DefenseMapGrid.CellToWorld(ctx.mapLayout, ctx.mapLayout.playerSpawnCell)
            : DefenseTowerLayoutDefaults.FarmAreaCenter + ctx.layout.arenaCenter;

        Handles.color = new Color(0.35f, 0.8f, 1f, 0.5f);
        Handles.DrawWireDisc(farmCenter, Vector3.up, 2f);
        Handles.Label(farmCenter + Vector3.up, "Farm");
    }

    private static void DrawSnapCellHighlight(DefenseMapLayout map, Vector3 world)
    {
        Vector3 cellCenter = DefenseMapGrid.SnapWorldToCellCenter(map, world) + Vector3.up * 0.04f;
        float half = map.cellSize * 0.48f;
        Handles.color = new Color(1f, 0.92f, 0.2f, 0.35f);
        var corners = new Vector3[]
        {
            cellCenter + new Vector3(-half, 0f, -half),
            cellCenter + new Vector3(-half, 0f, half),
            cellCenter + new Vector3(half, 0f, half),
            cellCenter + new Vector3(half, 0f, -half)
        };
        Handles.DrawSolidRectangleWithOutline(corners, Handles.color, new Color(1f, 0.85f, 0.1f, 0.9f));
    }

    private static bool HandleKeyboardNudge(Context ctx, int selectedIndex, float groundY, ref Result result)
    {
        var e = Event.current;
        if (e.type != EventType.KeyDown)
            return false;

        Vector2Int delta = e.keyCode switch
        {
            KeyCode.UpArrow => new Vector2Int(0, 1),
            KeyCode.DownArrow => new Vector2Int(0, -1),
            KeyCode.LeftArrow => new Vector2Int(-1, 0),
            KeyCode.RightArrow => new Vector2Int(1, 0),
            _ => new Vector2Int(int.MinValue, int.MinValue)
        };

        if (delta.x == int.MinValue)
            return false;

        var tower = ctx.layout.towers[selectedIndex];
        Vector3 world = GetTowerWorld(ctx.layout, selectedIndex, groundY);

        if (ctx.snapToGrid && ctx.mapLayout != null)
        {
            Vector2Int cell = DefenseMapGrid.WorldToCell(ctx.mapLayout, world) + delta;
            if (!ctx.mapLayout.IsInside(cell))
                return false;

            world = DefenseMapGrid.CellToWorld(ctx.mapLayout, cell);
        }
        else
        {
            float step = e.shift ? 0.25f : 1f;
            world += new Vector3(delta.x * step, 0f, delta.y * step);
        }

        world.y = groundY;
        Undo.RecordObject(ctx.layout, "Nudge Tower");
        tower.positionOffset = world - ctx.layout.TowerOrigin;
        result.changed = true;
        e.Use();
        return true;
    }

    private static int PickTower(Context ctx, float groundY, Vector2 mousePosition)
    {
        if (!TryRaycastGround(ctx, mousePosition, out Vector3 hit))
            return -1;

        int best = -1;
        float bestDist = float.MaxValue;
        float pickRadius = ctx.mapLayout != null ? ctx.mapLayout.cellSize * 0.6f : 1.2f;

        for (int i = 0; i < ctx.layout.towers.Count; i++)
        {
            Vector3 world = GetTowerWorld(ctx.layout, i, groundY);
            Vector3 flat = hit - world;
            flat.y = 0f;
            float dist = flat.magnitude;
            if (dist <= pickRadius && dist < bestDist)
            {
                bestDist = dist;
                best = i;
            }
        }

        return best;
    }

    private static bool TryRaycastGround(Context ctx, Vector2 mousePosition, out Vector3 hit)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        float planeY = ctx.mapLayout != null ? ctx.mapLayout.origin.y : ctx.layout.arenaCenter.y;
        var plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
        if (!plane.Raycast(ray, out float dist))
        {
            hit = default;
            return false;
        }

        hit = ray.GetPoint(dist);
        return true;
    }

    private static Vector3 GetTowerWorld(DefenseTowerLayout layout, int index, float groundY)
    {
        Vector3 world = layout.TowerOrigin + layout.towers[index].positionOffset;
        world.y = groundY;
        return world;
    }
}
#endif
