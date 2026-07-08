using UnityEngine;

public static class CwslGroundTargetMarker
{
    private static GameObject marker;
    private static Transform ring;
    private static Transform line;
    private static Material markerMaterial;

    public static void Show(Vector3 worldPoint, float radius = 4.8f)
    {
        Show(worldPoint, radius, new Color(1f, 0.2f, 0.08f, 0.35f));
    }

    public static void Show(Vector3 worldPoint, float radius, Color color)
    {
        Ensure();
        SetColor(color);
        marker.SetActive(true);
        marker.transform.position = worldPoint + Vector3.up * 0.05f;
        if (ring != null)
            ring.gameObject.SetActive(true);
        if (line != null)
            line.gameObject.SetActive(false);
        if (ring != null)
            ring.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
    }

    public static void ShowLine(Vector3 start, Vector3 end, float thickness = 0.5f)
    {
        Ensure();
        marker.SetActive(true);
        if (ring != null)
            ring.gameObject.SetActive(false);
        if (line == null)
            return;

        line.gameObject.SetActive(true);
        start.y = 0.05f;
        end.y = 0.05f;
        var delta = end - start;
        delta.y = 0f;
        var length = delta.magnitude;
        if (length < 0.001f)
            return;

        var center = (start + end) * 0.5f;
        marker.transform.position = center;
        marker.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        line.localPosition = Vector3.zero;
        line.localRotation = Quaternion.identity;
        line.localScale = new Vector3(thickness, 0.05f, length);
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
        markerMaterial = material;

        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = "Line";
        bar.transform.SetParent(marker.transform, false);
        bar.transform.localPosition = Vector3.zero;
        Object.Destroy(bar.GetComponent<Collider>());
        var barRenderer = bar.GetComponent<Renderer>();
        if (barRenderer != null)
        {
            barRenderer.sharedMaterial = material;
            barRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        ring = disc.transform;
        line = bar.transform;
        line.gameObject.SetActive(false);
        marker.SetActive(false);
    }

    private static void SetColor(Color color)
    {
        if (markerMaterial == null)
            return;

        markerMaterial.color = color;
        if (markerMaterial.HasProperty("_BaseColor"))
            markerMaterial.SetColor("_BaseColor", color);
    }
}
