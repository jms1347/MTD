using UnityEngine;

/// <summary>디아 오브 본체·파편 CFX3 얼음 구체 비주얼.</summary>
public static class CwslFrozenOrbVisualUtility
{
    private static Material iceOrbMaterial;

    public static Transform BuildOrbVisual(Transform parent)
    {
        var orbVisual = new GameObject(CwslFrozenOrbEmitter.OrbVisualName).transform;
        orbVisual.SetParent(parent, false);
        orbVisual.localPosition = Vector3.zero;
        orbVisual.localRotation = Quaternion.identity;
        orbVisual.localScale = Vector3.one * 1.35f;

        if (CwslVfxSpawner.AttachFrozenOrbIceBall(orbVisual, 1.35f) == null)
            BuildFallbackOrbCore(orbVisual, 0.85f);

        return orbVisual;
    }

    public static Transform BuildShardVisual(Transform parent, float scale)
    {
        var shardRoot = new GameObject("ShardVisual").transform;
        shardRoot.SetParent(parent, false);
        shardRoot.localPosition = Vector3.zero;
        shardRoot.localRotation = Quaternion.identity;
        shardRoot.localScale = Vector3.one * Mathf.Max(0.15f, scale);

        var shardScale = Mathf.Max(0.45f, scale * 0.95f);
        if (CwslVfxSpawner.AttachFrozenOrbIceBall(shardRoot, shardScale) == null)
            BuildFallbackOrbCore(shardRoot, 0.55f);

        return shardRoot;
    }

    private static void BuildFallbackOrbCore(Transform parent, float coreScale)
    {
        var material = GetIceOrbMaterial();
        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "OrbCore";
        core.transform.SetParent(parent, false);
        core.transform.localScale = Vector3.one * coreScale;
        Object.Destroy(core.GetComponent<Collider>());
        if (material != null)
            core.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Material GetIceOrbMaterial()
    {
        if (iceOrbMaterial != null)
            return iceOrbMaterial;

        var shader = Shader.Find("Standard");
        if (shader == null)
            return null;

        iceOrbMaterial = new Material(shader)
        {
            color = new Color(0.55f, 0.88f, 1f, 0.92f)
        };
        iceOrbMaterial.EnableKeyword("_EMISSION");
        iceOrbMaterial.SetColor("_EmissionColor", new Color(0.25f, 0.65f, 1f) * 1.35f);
        return iceOrbMaterial;
    }
}
