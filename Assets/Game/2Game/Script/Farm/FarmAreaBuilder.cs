using UnityEngine;

/// <summary>
/// 넥서스 옆에 벽으로 둘러싼 농장 구역을 생성합니다.
/// </summary>
public static class FarmAreaBuilder
{
    private static readonly Color SoilColor = new(0.45f, 0.3f, 0.16f);
    private static readonly Color SoilDarkColor = new(0.36f, 0.24f, 0.12f);
    private static readonly Color PlatformColor = new(0.34f, 0.48f, 0.28f);
    private static readonly Color WallColor = new(0.5f, 0.41f, 0.3f);
    private static readonly Color FencePostColor = new(0.38f, 0.3f, 0.22f);
    private static readonly Color GateColor = new(0.55f, 0.38f, 0.2f);
    private static readonly Color CropGreen = new(0.28f, 0.62f, 0.22f);

    private const int Columns = 4;
    private const int Rows = 3;
    private const float TileSize = 1f;
    private const float TileGap = 0.04f;
    private const float InnerPadding = 0.55f;
    private const float WallThickness = 0.24f;
    private const float WallHeight = 0.82f;
    private const float GateWidth = 1.35f;

    public static Transform Build(Vector3 nexusCenter)
    {
        var existing = GameObject.Find("DefenseFarm");
        if (existing != null)
            Object.Destroy(existing);

        var farmRoot = new GameObject("DefenseFarm");
        Vector3 farmOrigin = nexusCenter + new Vector3(-0.5f, 0f, -9f);
        farmRoot.transform.position = farmOrigin;

        float gridWidth = Columns * TileSize + (Columns - 1) * TileGap;
        float gridDepth = Rows * TileSize + (Rows - 1) * TileGap;
        float outerWidth = gridWidth + InnerPadding * 2f;
        float outerDepth = gridDepth + InnerPadding * 2f;

        CreatePlatform(farmRoot.transform, outerWidth, outerDepth);

        var soilRoot = new GameObject("SoilTiles").transform;
        soilRoot.SetParent(farmRoot.transform, false);

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                float x = InnerPadding + col * (TileSize + TileGap) + TileSize * 0.5f;
                float z = InnerPadding + row * (TileSize + TileGap) + TileSize * 0.5f;
                var localPos = new Vector3(x, 0.02f, z);
                CreateSoilTile(soilRoot, localPos, TileSize, (row + col) % 2 == 0);

                if ((row + col) % 3 == 0)
                    CreateCropDecoration(soilRoot, localPos + new Vector3(0f, 0.08f, 0f));
            }
        }

        var gate = BuildWallsWithGate(farmRoot.transform, outerWidth, outerDepth);

        var zone = farmRoot.AddComponent<FarmZone>();
        zone.Configure(
            new Vector3(InnerPadding, 0f, InnerPadding),
            new Vector3(outerWidth - InnerPadding, 0f, outerDepth - InnerPadding));

        var gateController = farmRoot.AddComponent<FarmGateController>();
        gateController.Initialize(gate.collider, gate.visual);

        CreateSign(farmRoot.transform, new Vector3(outerWidth * 0.5f, 0f, outerDepth + 0.45f));
        return farmRoot.transform;
    }

    private static void CreatePlatform(Transform parent, float width, float depth)
    {
        var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "FarmPlatform";
        platform.transform.SetParent(parent, false);
        platform.transform.localPosition = new Vector3(width * 0.5f, 0.02f, depth * 0.5f);
        platform.transform.localScale = new Vector3(width + 0.2f, 0.04f, depth + 0.2f);

        var collider = platform.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = true;

        ApplyColor(platform, PlatformColor);
    }

    private static void CreateSoilTile(Transform parent, Vector3 localPosition, float size, bool light)
    {
        var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = "FarmSoil";
        tile.tag = "FarmSoil";
        tile.transform.SetParent(parent, false);
        tile.transform.localPosition = new Vector3(localPosition.x, 0.02f, localPosition.z);
        tile.transform.localScale = new Vector3(size * 0.98f, 0.04f, size * 0.98f);

        var renderer = tile.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = light ? SoilColor : SoilDarkColor;
            renderer.material = material;
        }

        tile.AddComponent<FarmDrillTile>();
    }

    private static void CreateCropDecoration(Transform parent, Vector3 localPosition)
    {
        var crop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        crop.name = "Crop";
        crop.transform.SetParent(parent, false);
        crop.transform.localPosition = localPosition;
        crop.transform.localScale = new Vector3(0.16f, 0.2f, 0.16f);

        var collider = crop.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        var renderer = crop.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = CropGreen;
            renderer.material = material;
        }
    }

    private static (Collider collider, GameObject visual) BuildWallsWithGate(Transform farmRoot, float outerWidth, float outerDepth)
    {
        var wallRoot = new GameObject("FarmWalls").transform;
        wallRoot.SetParent(farmRoot, false);

        float centerY = WallHeight * 0.5f;
        float gateCenterZ = outerDepth * 0.42f;
        float gateHalf = GateWidth * 0.5f;

        CreateObstacleWall(wallRoot, "Wall_North",
            new Vector3(outerWidth * 0.5f, centerY, outerDepth + WallThickness * 0.5f),
            new Vector3(outerWidth + WallThickness * 2f, WallHeight, WallThickness));

        CreateObstacleWall(wallRoot, "Wall_South",
            new Vector3(outerWidth * 0.5f, centerY, -WallThickness * 0.5f),
            new Vector3(outerWidth + WallThickness * 2f, WallHeight, WallThickness));

        CreateObstacleWall(wallRoot, "Wall_East",
            new Vector3(outerWidth + WallThickness * 0.5f, centerY, outerDepth * 0.5f),
            new Vector3(WallThickness, WallHeight, outerDepth + WallThickness * 2f));

        float westSegmentDepth = Mathf.Max(0.2f, gateCenterZ - gateHalf);
        float westTopStart = gateCenterZ + gateHalf;
        float westTopDepth = Mathf.Max(0.2f, outerDepth - westTopStart);

        CreateObstacleWall(wallRoot, "Wall_West_Lower",
            new Vector3(-WallThickness * 0.5f, centerY, westSegmentDepth * 0.5f),
            new Vector3(WallThickness, WallHeight, westSegmentDepth));

        CreateObstacleWall(wallRoot, "Wall_West_Upper",
            new Vector3(-WallThickness * 0.5f, centerY, westTopStart + westTopDepth * 0.5f),
            new Vector3(WallThickness, WallHeight, westTopDepth));

        for (int i = 0; i <= 4; i++)
        {
            float t = i / 4f;
            CreatePost(wallRoot, $"Post_N_{i}", new Vector3(t * outerWidth, centerY, outerDepth));
            CreatePost(wallRoot, $"Post_S_{i}", new Vector3(t * outerWidth, centerY, 0f));
        }

        return CreateGate(wallRoot, new Vector3(-WallThickness * 0.5f, centerY, gateCenterZ));
    }

    private static (Collider collider, GameObject visual) CreateGate(Transform parent, Vector3 localPos)
    {
        var gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = "FarmGate";
        gate.tag = "Obstacle";
        gate.transform.SetParent(parent, false);
        gate.transform.localPosition = localPos;
        gate.transform.localScale = new Vector3(WallThickness, WallHeight, GateWidth);
        ApplyColor(gate, GateColor);

        var collider = gate.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = false;

        gate.SetActive(false);
        return (collider, gate);
    }

    private static void CreateObstacleWall(Transform parent, string name, Vector3 localPos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.tag = "Obstacle";
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = localPos;
        wall.transform.localScale = scale;

        var collider = wall.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = false;

        ApplyColor(wall, WallColor);
    }

    private static void CreatePost(Transform parent, string name, Vector3 localPos)
    {
        var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = name;
        post.tag = "Obstacle";
        post.transform.SetParent(parent, false);
        post.transform.localPosition = localPos + new Vector3(0f, 0f, 0f);
        post.transform.localScale = new Vector3(0.14f, WallHeight + 0.08f, 0.14f);

        var collider = post.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = false;

        ApplyColor(post, FencePostColor);
    }

    private static void CreateSign(Transform parent, Vector3 localPos)
    {
        var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.name = "FarmSign";
        sign.transform.SetParent(parent, false);
        sign.transform.localPosition = localPos + new Vector3(0f, 0.45f, 0f);
        sign.transform.localScale = new Vector3(0.9f, 0.9f, 0.12f);

        var collider = sign.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = true;

        ApplyColor(sign, new Color(0.62f, 0.42f, 0.18f));
    }

    private static void ApplyColor(GameObject target, Color color)
    {
        var renderer = target.GetComponent<Renderer>();
        if (renderer == null)
            return;

        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        renderer.material = material;
    }
}
