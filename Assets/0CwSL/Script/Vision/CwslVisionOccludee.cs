using UnityEngine;

/// <summary>
/// 시야 밖 오브젝트는 렌더러를 끄는 단순 컬링 (MPB 색 페이드 없음 — 배칭 유지).
/// </summary>
public class CwslVisionOccludee : MonoBehaviour
{
    private Renderer[] renderers;
    private Canvas[] canvases;
    private Light[] lights;
    private bool cached;
    private bool currentVisible = true;

    public float Visibility => currentVisible ? 1f : 0f;

    public void SetVisibility(float visibility)
    {
        if (!cached)
            CacheVisuals();

        var visible = visibility > CwslGameConstants.SimpleVisionShowThreshold;
        if (currentVisible == visible)
            return;

        currentVisible = visible;
        Apply();
    }

    public void SetVisible(bool visible)
    {
        SetVisibility(visible ? 1f : 0f);
    }

    private void CacheVisuals()
    {
        var allRenderers = GetComponentsInChildren<Renderer>(true);
        var filtered = new System.Collections.Generic.List<Renderer>(allRenderers.Length);
        for (var i = 0; i < allRenderers.Length; i++)
        {
            var renderer = allRenderers[i];
            if (renderer == null || ShouldSkipRenderer(renderer))
                continue;

            filtered.Add(renderer);
        }

        renderers = filtered.ToArray();
        canvases = GetComponentsInChildren<Canvas>(true);
        lights = GetComponentsInChildren<Light>(true);
        cached = true;
    }

    private static bool ShouldSkipRenderer(Renderer renderer)
    {
        return renderer is ParticleSystemRenderer or TrailRenderer or LineRenderer;
    }

    private void Apply()
    {
        if (renderers != null)
        {
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;

                renderer.enabled = currentVisible;
                renderer.SetPropertyBlock(null);
            }
        }

        if (canvases != null)
        {
            for (var i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null)
                    canvases[i].enabled = currentVisible;
            }
        }

        if (lights != null)
        {
            for (var i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                    lights[i].enabled = currentVisible;
            }
        }
    }

    private void OnDisable()
    {
        if (!cached)
            return;

        currentVisible = true;
        Apply();
    }
}
