using UnityEngine;

public static class CoopEnemyVisualFactory
{
    public static GameObject Create(Transform parent, string visualType, bool isBoss)
    {
        var visual = GameObject.CreatePrimitive(isBoss ? PrimitiveType.Capsule : PrimitiveType.Sphere);
        visual.name = isBoss ? $"Boss_{visualType}" : $"Enemy_{visualType}";
        visual.transform.SetParent(parent, false);
        UnityEngine.Object.Destroy(visual.GetComponent<Collider>());

        var renderer = visual.GetComponent<Renderer>();
        if (renderer == null)
            return visual;

        renderer.material.color = ResolveColor(visualType, isBoss);
        visual.transform.localScale = isBoss
            ? Vector3.one * 2.2f
            : ResolveScale(visualType);

        return visual;
    }

    private static Color ResolveColor(string visualType, bool isBoss)
    {
        if (isBoss)
            return new Color(0.75f, 0.1f, 0.85f);

        return visualType switch
        {
            "runner" => new Color(0.95f, 0.55f, 0.2f),
            "brute" => new Color(0.55f, 0.28f, 0.22f),
            "stalker" => new Color(0.35f, 0.75f, 0.4f),
            _ => new Color(0.95f, 0.25f, 0.2f)
        };
    }

    private static Vector3 ResolveScale(string visualType)
    {
        return visualType switch
        {
            "runner" => Vector3.one * 0.85f,
            "brute" => Vector3.one * 1.2f,
            "stalker" => Vector3.one * 0.95f,
            _ => Vector3.one
        };
    }
}
