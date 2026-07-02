using UnityEngine;

/// <summary>
/// 건설 미리보기·스캐폴드용 반투명 고스트 모형.
/// </summary>
public static class DefenseBuildGhostVisual
{
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private const float ValidAlpha = 0.38f;
    private const float InvalidAlpha = 0.42f;

    public static void BuildWall(Transform root, float cellSize)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "WallGhost";
        body.transform.SetParent(root, false);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(cellSize * 0.98f, 0.84f, cellSize * 0.98f);
        Object.Destroy(body.GetComponent<Collider>());

        var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cap.name = "WallCap";
        cap.transform.SetParent(root, false);
        cap.transform.localPosition = new Vector3(0f, 0.38f, 0f);
        cap.transform.localScale = new Vector3(cellSize * 0.72f, 0.12f, cellSize * 0.72f);
        Object.Destroy(cap.GetComponent<Collider>());
    }

    public static void BuildTower(Transform root, int towerSheetId)
    {
        if (towerSheetId <= 0)
            return;

        root.localScale = Vector3.one * DefenseTowerVisualResolver.DefaultVisualScale;

        var data = DefenseTowerBuildTable.CreateSpawnData(towerSheetId, 0);
        DefenseTowerVisualResolver.TryInstantiateVisual(root, data, towerSheetId, data.kind);
    }

    public static void ApplyGhostTint(GameObject root, bool isValid)
    {
        if (root == null)
            return;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            ApplyGhostMaterial(renderer, isValid);
    }

    public static void ApplyWallGhostTint(GameObject root, Color wallTint, bool isValid)
    {
        if (root == null)
            return;

        Color tint = isValid
            ? new Color(wallTint.r, wallTint.g, wallTint.b, ValidAlpha)
            : new Color(0.95f, 0.22f, 0.18f, InvalidAlpha);

        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            ApplyFlatGhostMaterial(renderer, tint, isValid);
    }

    private static void ApplyGhostMaterial(Renderer renderer, bool isValid)
    {
        if (renderer == null)
            return;

        var source = renderer.sharedMaterial;
        if (source == null)
        {
            ApplyFlatGhostMaterial(
                renderer,
                isValid
                    ? new Color(0.75f, 0.78f, 0.82f, ValidAlpha)
                    : new Color(0.95f, 0.28f, 0.22f, InvalidAlpha),
                isValid);
            return;
        }

        var ghost = new Material(source);
        ConfigureTransparent(ghost);

        if (ghost.HasProperty(ColorId))
        {
            Color baseColor = source.HasProperty(ColorId) ? source.GetColor(ColorId) : Color.white;
            ghost.SetColor(
                ColorId,
                isValid
                    ? new Color(baseColor.r, baseColor.g, baseColor.b, ValidAlpha)
                    : new Color(0.95f, 0.28f, 0.22f, InvalidAlpha));
        }

        if (source.IsKeywordEnabled("_EMISSION") && ghost.HasProperty(EmissionColorId))
        {
            Color emission = source.GetColor(EmissionColorId);
            ghost.SetColor(EmissionColorId, emission * (isValid ? 0.35f : 0.12f));
        }

        renderer.sharedMaterial = ghost;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void ApplyFlatGhostMaterial(Renderer renderer, Color tint, bool isValid)
    {
        if (renderer == null)
            return;

        var shader = Shader.Find("Standard");
        if (shader == null)
            shader = renderer.sharedMaterial != null ? renderer.sharedMaterial.shader : null;
        if (shader == null)
            return;

        var material = new Material(shader);
        ConfigureTransparent(material);
        material.SetColor(ColorId, tint);

        if (isValid)
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.SetColor(EmissionColorId, new Color(tint.r, tint.g, tint.b, 1f) * 0.2f);
        }

        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void ConfigureTransparent(Material material)
    {
        if (material == null)
            return;

        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }
}
