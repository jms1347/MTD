using UnityEngine;

public static class CwslGroundTargetMarker
{
    private static GameObject marker;
    private static Transform ring;

    public static void Show(Vector3 worldPoint, float radius = 4.8f)
    {
        Ensure();
        marker.SetActive(true);
        marker.transform.position = worldPoint + Vector3.up * 0.05f;
        if (ring != null)
            ring.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
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

        marker = new GameObject("CwslGroundTargetMarker");
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Ring";
        disc.transform.SetParent(marker.transform, false);
        disc.transform.localPosition = Vector3.zero;
        Object.Destroy(disc.GetComponent<Collider>());

        var renderer = disc.GetComponent<Renderer>();
        var color = new Color(1f, 0.2f, 0.08f, 0.35f);
        var material = CwslMaterialUtil.CreateColored(color);
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
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        ring = disc.transform;
        marker.SetActive(false);
    }
}
