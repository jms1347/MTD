using UnityEngine;
using UnityEngine.Rendering;

public static class CwslGroundRingVisual
{
    private static Material sharedTransparentMaterial;
    private static MaterialPropertyBlock propertyBlock;

    public static GameObject Create(Vector3 worldPosition, float diameter, Color color, float height = 0.04f)
    {
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "GroundRing";
        Object.Destroy(ring.GetComponent<Collider>());
        ring.transform.position = worldPosition + Vector3.up * height;
        ring.transform.localScale = new Vector3(diameter, 0.02f, diameter);
        ApplyTransparent(ring.GetComponent<Renderer>(), color);
        return ring;
    }

    public static void ApplyTransparent(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;

        renderer.sharedMaterial = GetSharedTransparentMaterial();
        propertyBlock ??= new MaterialPropertyBlock();
        propertyBlock.Clear();
        propertyBlock.SetColor("_Color", color);
        propertyBlock.SetColor("_BaseColor", color);
        renderer.SetPropertyBlock(propertyBlock);
    }

    public static GameObject CreateEdgeRing(
        Vector3 worldPosition,
        float diameter,
        Color color,
        float lineWidth = 0.22f,
        int segments = 72,
        float height = 0.04f)
    {
        var root = new GameObject("GroundEdgeRing");
        root.transform.position = worldPosition;

        var line = root.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.numCornerVertices = 2;
        line.numCapVertices = 2;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.sharedMaterial = CwslMaterialUtil.CreateMatteColored(color);
        line.positionCount = segments;

        var radius = diameter * 0.5f;
        for (var i = 0; i < segments; i++)
        {
            var angle = i / (float)segments * Mathf.PI * 2f;
            line.SetPosition(
                i,
                new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius));
        }

        return root;
    }

    private static Material GetSharedTransparentMaterial()
    {
        if (sharedTransparentMaterial != null)
            return sharedTransparentMaterial;

        sharedTransparentMaterial = CwslMaterialUtil.CreateMatteColored(Color.white);
        sharedTransparentMaterial.SetFloat("_Mode", 3f);
        sharedTransparentMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        sharedTransparentMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        sharedTransparentMaterial.SetInt("_ZWrite", 0);
        sharedTransparentMaterial.DisableKeyword("_ALPHATEST_ON");
        sharedTransparentMaterial.EnableKeyword("_ALPHABLEND_ON");
        sharedTransparentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        sharedTransparentMaterial.renderQueue = 3000;
        return sharedTransparentMaterial;
    }
}
