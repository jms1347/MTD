using UnityEngine;

/// <summary>
/// 시야 거리에 따라 렌더러/라이트를 부드럽게 페이드 (칼질 on/off 아님).
/// </summary>
public class CwslVisionOccludee : MonoBehaviour
{
    private Renderer[] renderers;
    private Canvas[] canvases;
    private Light[] lights;
    private Color[] originalColors;
    private Color[] originalBaseColors;
    private float[] originalLightIntensities;
    private bool cached;
    private float currentVisibility = 1f;

    public float Visibility => currentVisibility;

    public void SetVisibility(float visibility)
    {
        if (!cached)
            CacheVisuals();

        visibility = Mathf.Clamp01(visibility);
        if (Mathf.Abs(currentVisibility - visibility) < 0.02f)
            return;

        currentVisibility = visibility;
        Apply();
    }

    public void SetVisible(bool visible)
    {
        SetVisibility(visible ? 1f : 0f);
    }

    private void CacheVisuals()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        canvases = GetComponentsInChildren<Canvas>(true);
        lights = GetComponentsInChildren<Light>(true);

        originalColors = new Color[renderers.Length];
        originalBaseColors = new Color[renderers.Length];
        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || renderer.sharedMaterial == null)
            {
                originalColors[i] = Color.white;
                originalBaseColors[i] = Color.white;
                continue;
            }

            var mat = renderer.material;
            originalColors[i] = mat.HasProperty("_Color") ? mat.color : Color.white;
            originalBaseColors[i] = mat.HasProperty("_BaseColor")
                ? mat.GetColor("_BaseColor")
                : originalColors[i];
        }

        originalLightIntensities = new float[lights.Length];
        for (var i = 0; i < lights.Length; i++)
            originalLightIntensities[i] = lights[i] != null ? lights[i].intensity : 0f;

        cached = true;
    }

    private void Apply()
    {
        var fullyHidden = currentVisibility <= 0.03f;
        // 실루엣은 어둡게, 가까울수록 본색
        var colorBlend = Mathf.SmoothStep(0f, 1f, currentVisibility);

        if (renderers != null)
        {
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;

                renderer.enabled = !fullyHidden;
                if (fullyHidden || renderer.sharedMaterial == null)
                    continue;

                var mat = renderer.material;
                var faded = Color.Lerp(Color.black, originalColors[i], colorBlend);
                faded.a = originalColors[i].a;
                if (mat.HasProperty("_Color"))
                    mat.color = faded;
                if (mat.HasProperty("_BaseColor"))
                {
                    var baseFaded = Color.Lerp(Color.black, originalBaseColors[i], colorBlend);
                    baseFaded.a = originalBaseColors[i].a;
                    mat.SetColor("_BaseColor", baseFaded);
                }
            }
        }

        if (canvases != null)
        {
            for (var i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null)
                    canvases[i].enabled = currentVisibility > 0.55f;
            }
        }

        if (lights != null)
        {
            for (var i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                if (light == null)
                    continue;

                // 위협 불빛도 시야 밖에서는 거의 안 보이게
                var lightVisibility = Mathf.Pow(currentVisibility, 1.6f);
                light.enabled = lightVisibility > 0.08f;
                light.intensity = originalLightIntensities[i] * lightVisibility;
            }
        }
    }

    private void OnDisable()
    {
        if (currentVisibility < 0.99f)
        {
            currentVisibility = 1f;
            if (cached)
                Apply();
        }
    }
}
