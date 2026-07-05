using UnityEngine;

public static class CwslGroundRingVisual
{
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

        var material = CwslMaterialUtil.CreateMatteColored(color);
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        renderer.material = material;
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
        line.material = CwslMaterialUtil.CreateMatteColored(color);
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
}
