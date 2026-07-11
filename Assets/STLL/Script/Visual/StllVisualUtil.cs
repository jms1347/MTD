using UnityEngine;

public static class StllVisualUtil
{
    public static GameObject CreatePrimitive(
        PrimitiveType type,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            material.color = color;
            renderer.sharedMaterial = material;
        }

        RemoveCollider(go);
        return go;
    }

    public static void RemoveCollider(GameObject go)
    {
        var collider = go.GetComponent<Collider>();
        if (collider == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(collider);
        else
            Object.DestroyImmediate(collider);
    }
}
