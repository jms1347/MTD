using UnityEngine;

public static class CwslNexusVisualBuilder
{
    public const float AimPointHeight = 6.55f;
    public const float HitCenterY = 3.8f;
    public const float HitHeight = 7.6f;
    public const float HitRadius = 3.05f;

    public static void Build(Transform root)
    {
        if (root == null)
            return;

        ClearLegacyVisuals(root);

        var visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(root, false);

        var stone = new Color(0.42f, 0.44f, 0.5f);
        var trim = new Color(0.26f, 0.28f, 0.34f);
        var mortar = new Color(0.34f, 0.36f, 0.4f);
        var gold = new Color(1f, 0.84f, 0.22f);

        // 넓은 성채 기단
        var foundation = Prim(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 0.12f, 0f),
            new Vector3(6.4f, 0.14f, 6.4f), stone);
        var courtyard = Prim(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 0.22f, 0f),
            new Vector3(5.2f, 0.08f, 5.2f), mortar);

        // 외벽 4면
        var wallN = Prim(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 1.05f, 2.15f),
            new Vector3(4.6f, 1.5f, 0.55f), stone);
        var wallS = Prim(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 1.05f, -2.15f),
            new Vector3(4.6f, 1.5f, 0.55f), stone);
        var wallE = Prim(PrimitiveType.Cube, visualRoot.transform, new Vector3(2.15f, 1.05f, 0f),
            new Vector3(0.55f, 1.5f, 4.6f), stone);
        var wallW = Prim(PrimitiveType.Cube, visualRoot.transform, new Vector3(-2.15f, 1.05f, 0f),
            new Vector3(0.55f, 1.5f, 4.6f), stone);

        // 모서리 망대 4개
        BuildCornerTower(visualRoot.transform, new Vector3(2.05f, 0f, 2.05f), stone, trim, gold);
        BuildCornerTower(visualRoot.transform, new Vector3(-2.05f, 0f, 2.05f), stone, trim, gold);
        BuildCornerTower(visualRoot.transform, new Vector3(2.05f, 0f, -2.05f), stone, trim, gold);
        BuildCornerTower(visualRoot.transform, new Vector3(-2.05f, 0f, -2.05f), stone, trim, gold);

        // 중앙 본탑 하부
        var keepBase = Prim(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 1.55f, 0f),
            new Vector3(2.8f, 0.55f, 2.8f), trim);
        var keepBody = Prim(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 3.35f, 0f),
            new Vector3(2.35f, 1.55f, 2.35f), stone);
        var keepBand = Prim(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 3.1f, 0f),
            new Vector3(2.55f, 0.12f, 2.55f), gold, goldTrim: true);

        // 본탑 상부 + 지붕
        var upperTower = Prim(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 5.35f, 0f),
            new Vector3(1.85f, 1.05f, 1.85f), mortar);
        var roof = Prim(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 6.35f, 0f),
            new Vector3(2.15f, 0.22f, 2.15f), trim);

        BuildBattlements(visualRoot.transform, new Vector3(0f, 6.55f, 0f), 2.05f, trim);

        // 정문 아치
        var gateFrame = Prim(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 0.72f, 2.18f),
            new Vector3(1.15f, 1.05f, 0.2f), trim);
        var gateGlow = Prim(PrimitiveType.Cube, visualRoot.transform, new Vector3(0f, 0.62f, 2.24f),
            new Vector3(0.72f, 0.82f, 0.12f), gold, emissive: true);

        // 바닥 황금 룬 링
        var runeRing = Prim(PrimitiveType.Cylinder, visualRoot.transform, new Vector3(0f, 0.05f, 0f),
            new Vector3(4.2f, 0.03f, 4.2f), gold, emissive: true);
        runeRing.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

        RemoveColliders(
            foundation, courtyard, wallN, wallS, wallE, wallW,
            keepBase, keepBody, keepBand, upperTower, roof,
            gateFrame, gateGlow, runeRing);
    }

    public static void ConfigureHitCollider(CapsuleCollider collider)
    {
        if (collider == null)
            return;

        collider.isTrigger = true;
        collider.direction = 1;
        collider.center = new Vector3(0f, HitCenterY, 0f);
        collider.height = HitHeight;
        collider.radius = HitRadius;
    }

    private static void BuildCornerTower(Transform parent, Vector3 localPosition, Color stone, Color trim, Color gold)
    {
        var baseBlock = Prim(PrimitiveType.Cube, parent, localPosition + new Vector3(0f, 0.55f, 0f),
            new Vector3(0.95f, 1.1f, 0.95f), stone);
        var shaft = Prim(PrimitiveType.Cylinder, parent, localPosition + new Vector3(0f, 1.75f, 0f),
            new Vector3(0.62f, 0.75f, 0.62f), trim);
        var cap = Prim(PrimitiveType.Cylinder, parent, localPosition + new Vector3(0f, 2.55f, 0f),
            new Vector3(0.78f, 0.14f, 0.78f), trim);
        var spire = Prim(PrimitiveType.Cylinder, parent, localPosition + new Vector3(0f, 2.95f, 0f),
            new Vector3(0.12f, 0.42f, 0.12f), gold, emissive: true);
        RemoveColliders(baseBlock, shaft, cap, spire);
    }

    private static void BuildBattlements(Transform parent, Vector3 center, float radius, Color color)
    {
        const int count = 10;
        for (var i = 0; i < count; i++)
        {
            if (i % 2 == 1)
                continue;

            var angle = i * Mathf.PI * 2f / count;
            var offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            var block = Prim(PrimitiveType.Cube, parent, center + offset, new Vector3(0.38f, 0.28f, 0.38f), color);
            RemoveCollider(block);
        }
    }

    private static void ClearLegacyVisuals(Transform root)
    {
        var legacyCore = root.Find("NexusCore");
        if (legacyCore != null)
        {
            if (Application.isPlaying)
                Object.Destroy(legacyCore.gameObject);
            else
                Object.DestroyImmediate(legacyCore.gameObject);
        }

        var legacyGlow = root.Find("Visual/NexusCoreGlow");
        if (legacyGlow == null)
            legacyGlow = root.Find("NexusCoreGlow");
        if (legacyGlow != null)
        {
            if (Application.isPlaying)
                Object.Destroy(legacyGlow.gameObject);
            else
                Object.DestroyImmediate(legacyGlow.gameObject);
        }

        var visual = root.Find("Visual");
        if (visual != null)
        {
            if (Application.isPlaying)
                Object.Destroy(visual.gameObject);
            else
                Object.DestroyImmediate(visual.gameObject);
        }
    }

    private static GameObject Prim(
        PrimitiveType type,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        bool emissive = false,
        bool goldTrim = false)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = emissive
                ? CwslNexusVisual.GetEmissiveMaterial(color)
                : goldTrim
                    ? CwslNexusVisual.GetGoldMaterial(color)
                    : CwslNexusVisual.GetStoneMaterial(color);
        }

        return go;
    }

    private static void RemoveCollider(GameObject go)
    {
        var collider = go.GetComponent<Collider>();
        if (collider == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(collider);
        else
            Object.DestroyImmediate(collider);
    }

    private static void RemoveColliders(params GameObject[] objects)
    {
        foreach (var go in objects)
            RemoveCollider(go);
    }
}
