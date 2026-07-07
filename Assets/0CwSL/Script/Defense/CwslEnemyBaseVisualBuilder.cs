using UnityEngine;

/// <summary>넥서스 성채 실루엣을 축소·적색 테마로 변형한 적 기지 비주얼.</summary>
public static class CwslEnemyBaseVisualBuilder
{
    private const float Scale = 0.42f;

    public static void Build(Transform root)
    {
        if (root == null)
            return;

        ClearVisuals(root);

        var visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(root, false);

        var stone = new Color(0.28f, 0.22f, 0.2f);
        var trim = new Color(0.16f, 0.12f, 0.1f);
        var mortar = new Color(0.22f, 0.17f, 0.15f);
        var ember = new Color(1f, 0.32f, 0.08f);
        var blood = new Color(0.72f, 0.08f, 0.05f);

        var foundation = Prim(PrimitiveType.Cylinder, visualRoot.transform, S(0f, 0.12f, 0f),
            S(6.4f, 0.14f, 6.4f), stone);
        var courtyard = Prim(PrimitiveType.Cylinder, visualRoot.transform, S(0f, 0.22f, 0f),
            S(5.2f, 0.08f, 5.2f), mortar);

        var wallN = Prim(PrimitiveType.Cube, visualRoot.transform, S(0f, 1.05f, 2.15f),
            S(4.6f, 1.5f, 0.55f), stone);
        var wallS = Prim(PrimitiveType.Cube, visualRoot.transform, S(0f, 1.05f, -2.15f),
            S(4.6f, 1.5f, 0.55f), stone);
        var wallE = Prim(PrimitiveType.Cube, visualRoot.transform, S(2.15f, 1.05f, 0f),
            S(0.55f, 1.5f, 4.6f), stone);
        var wallW = Prim(PrimitiveType.Cube, visualRoot.transform, S(-2.15f, 1.05f, 0f),
            S(0.55f, 1.5f, 4.6f), stone);

        BuildCornerTower(visualRoot.transform, S(2.05f, 0f, 2.05f), stone, trim, ember);
        BuildCornerTower(visualRoot.transform, S(-2.05f, 0f, 2.05f), stone, trim, ember);
        BuildCornerTower(visualRoot.transform, S(2.05f, 0f, -2.05f), stone, trim, ember);
        BuildCornerTower(visualRoot.transform, S(-2.05f, 0f, -2.05f), stone, trim, ember);

        var keepBase = Prim(PrimitiveType.Cylinder, visualRoot.transform, S(0f, 1.55f, 0f),
            S(2.8f, 0.55f, 2.8f), trim);
        var keepBody = Prim(PrimitiveType.Cylinder, visualRoot.transform, S(0f, 3.35f, 0f),
            S(2.35f, 1.55f, 2.35f), stone);
        var keepBand = Prim(PrimitiveType.Cylinder, visualRoot.transform, S(0f, 3.1f, 0f),
            S(2.55f, 0.12f, 2.55f), blood, emissive: true);

        var upperTower = Prim(PrimitiveType.Cylinder, visualRoot.transform, S(0f, 5.35f, 0f),
            S(1.85f, 1.05f, 1.85f), mortar);
        var roof = Prim(PrimitiveType.Cylinder, visualRoot.transform, S(0f, 6.35f, 0f),
            S(2.15f, 0.22f, 2.15f), trim);

        BuildBattlements(visualRoot.transform, S(0f, 6.55f, 0f), 2.05f, trim);

        var gateFrame = Prim(PrimitiveType.Cube, visualRoot.transform, S(0f, 0.72f, 2.18f),
            S(1.15f, 1.05f, 0.2f), trim);
        var gateGlow = Prim(PrimitiveType.Cube, visualRoot.transform, S(0f, 0.62f, 2.24f),
            S(0.72f, 0.82f, 0.12f), ember, emissive: true);

        var runeRing = Prim(PrimitiveType.Cylinder, visualRoot.transform, S(0f, 0.05f, 0f),
            S(4.2f, 0.03f, 4.2f), blood, emissive: true);

        var spawnCore = Prim(PrimitiveType.Sphere, visualRoot.transform, S(0f, 2.2f, 0f),
            S(0.55f, 0.55f, 0.55f), ember, emissive: true);

        RemoveColliders(
            foundation, courtyard, wallN, wallS, wallE, wallW,
            keepBase, keepBody, keepBand, upperTower, roof,
            gateFrame, gateGlow, runeRing, spawnCore);
    }

    private static Vector3 S(float x, float y, float z) => new Vector3(x, y, z) * Scale;
    private static Vector3 S(float uniform) => Vector3.one * uniform * Scale;

    private static void BuildCornerTower(Transform parent, Vector3 localPosition, Color stone, Color trim, Color ember)
    {
        var baseBlock = Prim(PrimitiveType.Cube, parent, localPosition + S(0f, 0.55f, 0f),
            S(0.95f, 1.1f, 0.95f), stone);
        var shaft = Prim(PrimitiveType.Cylinder, parent, localPosition + S(0f, 1.75f, 0f),
            S(0.62f, 0.75f, 0.62f), trim);
        var cap = Prim(PrimitiveType.Cylinder, parent, localPosition + S(0f, 2.55f, 0f),
            S(0.78f, 0.14f, 0.78f), trim);
        var spire = Prim(PrimitiveType.Cylinder, parent, localPosition + S(0f, 2.95f, 0f),
            S(0.12f, 0.42f, 0.12f), ember, emissive: true);
        RemoveColliders(baseBlock, shaft, cap, spire);
    }

    private static void BuildBattlements(Transform parent, Vector3 center, float radius, Color color)
    {
        const int count = 10;
        var scaledRadius = radius * Scale;
        for (var i = 0; i < count; i++)
        {
            if (i % 2 == 1)
                continue;

            var angle = i * Mathf.PI * 2f / count;
            var offset = new Vector3(Mathf.Cos(angle) * scaledRadius, 0f, Mathf.Sin(angle) * scaledRadius);
            var block = Prim(PrimitiveType.Cube, parent, center + offset, S(0.38f, 0.28f, 0.38f), color);
            RemoveCollider(block);
        }
    }

    private static void ClearVisuals(Transform root)
    {
        var visual = root.Find("Visual");
        if (visual == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(visual.gameObject);
        else
            Object.DestroyImmediate(visual.gameObject);
    }

    private static GameObject Prim(
        PrimitiveType type,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        bool emissive = false)
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
