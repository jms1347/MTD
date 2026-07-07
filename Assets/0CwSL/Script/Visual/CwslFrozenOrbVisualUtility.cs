using UnityEngine;

/// <summary>디아 오브 본체·파편 공통 얼음 구체 머티리얼 (UkDefense DefenseFrozenOrbVisualUtility).</summary>
public static class CwslFrozenOrbVisualUtility
{
    private static Material iceOrbMaterial;

    public static Material GetIceOrbMaterial()
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

    public static void ApplyIceOrbMaterial(Renderer renderer)
    {
        if (renderer == null)
            return;

        var material = GetIceOrbMaterial();
        if (material != null)
            renderer.sharedMaterial = material;
    }

    public static Transform BuildOrbVisual(Transform parent)
    {
        var orbVisual = new GameObject(CwslFrozenOrbEmitter.OrbVisualName).transform;
        orbVisual.SetParent(parent, false);
        orbVisual.localPosition = Vector3.zero;
        orbVisual.localRotation = Quaternion.identity;

        var orbCore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orbCore.name = "OrbCore";
        orbCore.transform.SetParent(orbVisual, false);
        orbCore.transform.localPosition = Vector3.zero;
        orbCore.transform.localScale = Vector3.one * 0.85f;
        Object.Destroy(orbCore.GetComponent<Collider>());
        ApplyIceOrbMaterial(orbCore.GetComponent<Renderer>());

        var orbAura = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orbAura.name = "OrbAura";
        orbAura.transform.SetParent(orbVisual, false);
        orbAura.transform.localPosition = Vector3.zero;
        orbAura.transform.localScale = Vector3.one * 0.42f;
        Object.Destroy(orbAura.GetComponent<Collider>());
        var auraRenderer = orbAura.GetComponent<Renderer>();
        if (auraRenderer != null)
        {
            var auraMat = GetIceOrbMaterial();
            if (auraMat != null)
            {
                auraMat = Object.Instantiate(auraMat);
                auraMat.color = new Color(0.55f, 0.88f, 1f, 0.35f);
                auraRenderer.sharedMaterial = auraMat;
            }
        }

        orbVisual.localScale = Vector3.one * 1.15f;
        return orbVisual;
    }

    public static Transform BuildShardVisual(Transform parent, float scale)
    {
        var shardRoot = new GameObject("ShardVisual").transform;
        shardRoot.SetParent(parent, false);
        shardRoot.localPosition = Vector3.zero;
        shardRoot.localRotation = Quaternion.identity;
        shardRoot.localScale = Vector3.one * Mathf.Max(0.15f, scale);

        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "ShardCore";
        core.transform.SetParent(shardRoot, false);
        core.transform.localScale = Vector3.one * 0.55f;
        Object.Destroy(core.GetComponent<Collider>());
        ApplyIceOrbMaterial(core.GetComponent<Renderer>());

        return shardRoot;
    }
}
