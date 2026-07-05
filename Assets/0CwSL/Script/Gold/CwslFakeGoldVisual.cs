using UnityEngine;

/// <summary>가짜 골드 — 더 밝고 크게 반짝여 유인.</summary>
public class CwslFakeGoldVisual : MonoBehaviour
{
    private static readonly Color FakeGoldColor = new(1f, 0.95f, 0.35f);
    private static readonly Color NormalGoldColor = new(1f, 0.84f, 0.12f);

    private CwslGoldPickup pickup;
    private Renderer coinRenderer;
    private Light glowLight;
    private Vector3 baseScale;
    private bool wasFake;

    private void Awake()
    {
        pickup = GetComponentInParent<CwslGoldPickup>();
        coinRenderer = GetComponent<Renderer>();
        baseScale = transform.localScale;
        EnsureGlowLight();
    }

    private void Update()
    {
        if (pickup == null || coinRenderer == null)
            return;

        var isFake = pickup.IsFake;
        if (isFake != wasFake)
        {
            wasFake = isFake;
            ApplyFakeLook(isFake);
        }

        if (!isFake)
            return;

        var pulse = 1f + Mathf.Sin(Time.time * 7.5f) * 0.14f;
        transform.localScale = baseScale * pulse;
        if (glowLight != null)
            glowLight.intensity = 2.4f + Mathf.Sin(Time.time * 9f) * 0.8f;
    }

    private void ApplyFakeLook(bool isFake)
    {
        CwslMaterialUtil.ApplyColor(coinRenderer, isFake ? FakeGoldColor : NormalGoldColor);
        transform.localScale = isFake ? baseScale * 1.22f : baseScale;
        if (glowLight != null)
            glowLight.enabled = isFake;
    }

    private void EnsureGlowLight()
    {
        if (glowLight != null)
            return;

        var lightObject = new GameObject("FakeGoldGlow");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = Vector3.up * 0.15f;
        glowLight = lightObject.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = new Color(1f, 0.92f, 0.45f);
        glowLight.range = 4.2f;
        glowLight.intensity = 0f;
        glowLight.shadows = LightShadows.None;
        glowLight.enabled = false;
    }
}
