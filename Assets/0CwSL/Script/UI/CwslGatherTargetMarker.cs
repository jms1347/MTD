using UnityEngine;

public static class CwslGatherTargetMarker
{
    private static GameObject marker;
    private static Transform ring;
    private static Renderer ringRenderer;

    public static void Show(Vector3 worldPoint, float radius, bool atMax)
    {
        Ensure();
        marker.SetActive(true);
        marker.transform.position = worldPoint + Vector3.up * 0.04f;
        if (ring != null)
            ring.localScale = new Vector3(radius * 2f, 0.04f, radius * 2f);

        if (ringRenderer != null)
        {
            var color = atMax
                ? new Color(1f, 0.92f, 0.2f, 0.48f)
                : new Color(0.55f, 0.35f, 0.95f, 0.34f);
            ringRenderer.sharedMaterial.color = color;
            if (ringRenderer.sharedMaterial.HasProperty("_BaseColor"))
                ringRenderer.sharedMaterial.SetColor("_BaseColor", color);
        }
    }

    public static void Hide()
    {
        if (marker != null)
            marker.SetActive(false);
    }

    private static void Ensure()
    {
        if (marker != null)
            return;

        marker = new GameObject("CwslGatherTargetMarker");
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Ring";
        disc.transform.SetParent(marker.transform, false);
        Object.Destroy(disc.GetComponent<Collider>());

        ringRenderer = disc.GetComponent<Renderer>();
        var color = new Color(0.55f, 0.35f, 0.95f, 0.34f);
        var material = CwslMaterialUtil.CreateColored(color);
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = 3000;
        ringRenderer.sharedMaterial = material;
        ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring = disc.transform;
        marker.SetActive(false);
    }
}
