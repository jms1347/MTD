using UnityEngine;

public static class CwslGraveVisualBuilder
{
    public static void Build(Transform root)
    {
        var stoneColor = new Color(0.5f, 0.52f, 0.56f);
        var darkStone = new Color(0.34f, 0.36f, 0.4f);
        var mossColor = new Color(0.28f, 0.38f, 0.3f);

        var mound = CreatePart(PrimitiveType.Cube, root, new Vector3(0f, 0.1f, 0f),
            new Vector3(1.55f, 0.2f, 1.2f), mossColor);
        var baseStone = CreatePart(PrimitiveType.Cube, root, new Vector3(0f, 0.22f, 0f),
            new Vector3(1.25f, 0.16f, 0.95f), darkStone);
        var slab = CreatePart(PrimitiveType.Cube, root, new Vector3(0f, 0.72f, 0.06f),
            new Vector3(0.72f, 1.02f, 0.16f), stoneColor);
        slab.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
        var slabCap = CreatePart(PrimitiveType.Cube, root, new Vector3(0f, 1.28f, 0.12f),
            new Vector3(0.8f, 0.12f, 0.2f), darkStone);
        slabCap.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
        var crossV = CreatePart(PrimitiveType.Cube, root, new Vector3(0f, 1.52f, 0.18f),
            new Vector3(0.1f, 0.34f, 0.08f), darkStone);
        var crossH = CreatePart(PrimitiveType.Cube, root, new Vector3(0f, 1.58f, 0.18f),
            new Vector3(0.34f, 0.1f, 0.08f), darkStone);

        var anchor = new GameObject("LabelAnchor");
        anchor.transform.SetParent(root, false);
        anchor.transform.localPosition = new Vector3(0f, 2.05f, 0f);

        RemoveCollider(mound);
        RemoveCollider(baseStone);
        RemoveCollider(slab);
        RemoveCollider(slabCap);
        RemoveCollider(crossV);
        RemoveCollider(crossH);
    }

    private static GameObject CreatePart(PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        CwslMaterialUtil.ApplyColor(go.GetComponent<Renderer>(), color);
        return go;
    }

    private static void RemoveCollider(GameObject go)
    {
        var collider = go.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
    }
}
