using UnityEngine;

public static class CwslSimpleVfx
{
    public static void SpawnBurst(Vector3 position, Color color, float size = 0.6f, float lifetime = 0.35f)
    {
        var burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        burst.transform.position = position + Vector3.up * 0.2f;
        burst.transform.localScale = Vector3.one * size;
        Object.Destroy(burst.GetComponent<Collider>());
        CwslMaterialUtil.ApplyColor(burst.GetComponent<Renderer>(), color);
        Object.Destroy(burst, lifetime);
    }
}
