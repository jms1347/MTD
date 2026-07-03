using UnityEngine;

/// <summary>
/// 어둠 속에서 위협을 알리는 작은 포인트 라이트 (미사일/자폭 등).
/// </summary>
public static class CwslThreatLight
{
    public static void Ensure(Transform parent, Color color, float range, float intensity, Vector3 localOffset)
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

        light.enabled = true;
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
    }
}
