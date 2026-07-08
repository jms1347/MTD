using UnityEngine;

/// <summary>
/// 어둠 속 위협 표시용 포인트 라이트 (투사체·이펙트 전용 — 몬스터에는 사용하지 않음).
/// </summary>
public static class CwslThreatLight
{
    public static void Ensure(Transform parent, Color color, float range, float intensity, Vector3 localOffset, bool startEnabled = true)
    {
        if (parent == null)
            return;

        var existing = parent.Find("ThreatLight");
        Light light;
        if (existing != null)
        {
            light = existing.GetComponent<Light>();
            if (light == null)
                light = existing.gameObject.AddComponent<Light>();
        }
        else
        {
            var go = new GameObject("ThreatLight");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localOffset;
            light = go.AddComponent<Light>();
        }

        light.enabled = startEnabled;
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
    }

    public static void RemoveFromHierarchy(Transform root)
    {
        if (root == null)
            return;

        var nodes = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            if (node == null || node.name != "ThreatLight")
                continue;

            if (Application.isPlaying)
                Object.Destroy(node.gameObject);
            else
                Object.DestroyImmediate(node.gameObject);
        }
    }
}
