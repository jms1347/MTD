using System.Collections.Generic;

using UnityEngine;



/// <summary>기본 도형 맵 — 허브·사수관·호로관.</summary>

public static class StllPrimitiveMapBuilder

{

    public const int SpawnNorth = 0;

    public const int SpawnEast = 1;

    public const int SpawnWest = 2;



    private static readonly Dictionary<int, Vector3> SpawnPoints = new();



    public static Transform MapRoot { get; private set; }

    public static Transform HubRoot { get; private set; }

    public static Transform StageRoot { get; private set; }

    public static Transform HulaoRoot { get; private set; }



    public static void BuildAll()

    {

        Clear();



        MapRoot = new GameObject("StllEaMap").transform;

        HubRoot = new GameObject("HubArea").transform;

        HubRoot.SetParent(MapRoot, false);



        StageRoot = new GameObject("SashuguanArea").transform;

        StageRoot.SetParent(MapRoot, false);

        StageRoot.localPosition = new Vector3(80f, 0f, 0f);



        HulaoRoot = new GameObject("HulaoArea").transform;

        HulaoRoot.SetParent(MapRoot, false);

        HulaoRoot.localPosition = new Vector3(160f, 0f, 0f);



        BuildGround(HubRoot, Vector3.zero, new Color(0.32f, 0.4f, 0.28f), 12f);

        BuildHub(HubRoot);

        BuildGround(StageRoot, Vector3.zero, new Color(0.38f, 0.34f, 0.26f), 14f);

        BuildSashuguan(StageRoot);

        BuildGround(HulaoRoot, Vector3.zero, new Color(0.3f, 0.28f, 0.32f), 10f);

        BuildHulao(HulaoRoot);

    }



    public static void SetAreaActive(bool hubActive, bool sashuguanActive, bool hulaoActive = false)

    {

        if (HubRoot != null)

            HubRoot.gameObject.SetActive(hubActive);

        if (StageRoot != null)

            StageRoot.gameObject.SetActive(sashuguanActive);

        if (HulaoRoot != null)

            HulaoRoot.gameObject.SetActive(hulaoActive);

    }



    public static Vector3 GetSpawnPoint(int direction, bool stageArea = true)

    {

        var areaKey = stageArea ? 100 : 200;

        var key = areaKey + direction;

        if (SpawnPoints.TryGetValue(key, out var point))

            return point;



        if (!stageArea && HulaoRoot != null)

            return HulaoRoot.position + new Vector3(0f, 0f, 14f);



        return StageRoot != null ? StageRoot.position + new Vector3(0f, 0f, 18f) : Vector3.zero;

    }



    public static Vector3 GetDepotPosition(char label)

    {

        var origin = StageRoot != null ? StageRoot.position : new Vector3(80f, 0f, 0f);

        return label switch

        {

            'A' => origin + new Vector3(-8f, 0f, 10f),

            'B' => origin + new Vector3(0f, 0f, 4f),

            'C' => origin + new Vector3(8f, 0f, 10f),

            _ => origin

        };

    }



    public static Vector3 GetPlayerSpawnPoint(int index)

    {

        var hubCenter = HubRoot != null ? HubRoot.position : Vector3.zero;

        var angle = index * (360f / 3f) * Mathf.Deg2Rad;

        return hubCenter + new Vector3(Mathf.Cos(angle) * 4f, 0f, Mathf.Sin(angle) * 4f);

    }



    public static Vector3 GetStagePlayerSpawnPoint(int index)

    {

        var center = StageRoot != null ? StageRoot.position : new Vector3(80f, 0f, 0f);

        return center + new Vector3(-4f + index * 4f, 0f, -10f);

    }



    public static Vector3 GetHulaoPlayerSpawn(int index)

    {

        var center = HulaoRoot != null ? HulaoRoot.position : new Vector3(160f, 0f, 0f);

        return center + new Vector3(-4f + index * 4f, 0f, -8f);

    }



    public static Vector3 GetHulaoBossPosition()

    {

        return HulaoRoot != null ? HulaoRoot.position + new Vector3(0f, 0f, 6f) : new Vector3(160f, 0f, 6f);

    }



    private static void Clear()

    {

        SpawnPoints.Clear();

        if (MapRoot != null)

            Object.Destroy(MapRoot.gameObject);



        MapRoot = null;

        HubRoot = null;

        StageRoot = null;

        HulaoRoot = null;

    }



    private static void BuildGround(Transform parent, Vector3 localCenter, Color color, float halfSize)

    {

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);

        ground.name = "Ground";

        ground.transform.SetParent(parent, false);

        ground.transform.localPosition = localCenter;

        ground.transform.localScale = new Vector3(halfSize * 0.2f, 1f, halfSize * 0.2f);



        var renderer = ground.GetComponent<Renderer>();

        if (renderer != null)

        {

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            var material = new Material(shader);

            material.color = color;

            renderer.sharedMaterial = material;

        }

    }



    private static void BuildHub(Transform parent)

    {

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cylinder, parent, new Vector3(0f, 1.2f, 0f),

            new Vector3(4.5f, 0.05f, 4.5f), new Color(0.72f, 0.2f, 0.18f));

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, parent, new Vector3(0f, 2.4f, 0f),

            new Vector3(0.18f, 2.4f, 0.18f), new Color(0.42f, 0.3f, 0.18f));

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cylinder, parent, new Vector3(-5f, 0.2f, 2f),

            new Vector3(0.8f, 0.05f, 0.8f), new Color(0.9f, 0.45f, 0.12f));

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, parent, new Vector3(5f, 0.6f, -2f),

            new Vector3(2f, 0.8f, 1.2f), new Color(0.5f, 0.38f, 0.22f));

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, parent, new Vector3(-5f, 0.6f, -2f),

            new Vector3(2f, 0.8f, 1.2f), new Color(0.35f, 0.45f, 0.55f));

    }



    private static void BuildSashuguan(Transform parent)

    {

        var wallColor = new Color(0.45f, 0.4f, 0.36f);

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, parent, new Vector3(0f, 1.2f, 20f),

            new Vector3(28f, 2.4f, 1f), wallColor);

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, parent, new Vector3(-14f, 1.2f, 8f),

            new Vector3(1f, 2.4f, 16f), wallColor);

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, parent, new Vector3(14f, 1.2f, 8f),

            new Vector3(1f, 2.4f, 16f), wallColor);



        RegisterSpawns(parent.position, 100);

    }



    private static void BuildHulao(Transform parent)

    {

        var gateColor = new Color(0.5f, 0.35f, 0.3f);

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, parent, new Vector3(0f, 2f, 14f),

            new Vector3(12f, 4f, 2f), gateColor);

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, parent, new Vector3(-8f, 1.5f, 4f),

            new Vector3(2f, 3f, 12f), gateColor);

        StllVisualUtil.CreatePrimitive(PrimitiveType.Cube, parent, new Vector3(8f, 1.5f, 4f),

            new Vector3(2f, 3f, 12f), gateColor);

        RegisterSpawns(parent.position, 200);

    }



    private static void RegisterSpawns(Vector3 origin, int keyBase)

    {

        SpawnPoints[keyBase + SpawnNorth] = origin + new Vector3(0f, 0f, 22f);

        SpawnPoints[keyBase + SpawnEast] = origin + new Vector3(18f, 0f, 12f);

        SpawnPoints[keyBase + SpawnWest] = origin + new Vector3(-18f, 0f, 12f);

    }

}


