using UnityEngine;

public class CwslVisionOccludee : MonoBehaviour
{
    private Renderer[] renderers;
    private Canvas[] canvases;
    private bool isVisible = true;
    private bool cached;

    public bool IsVisible => isVisible;

    public void SetVisible(bool visible)
    {
        if (!cached)
            CacheVisuals();

        if (isVisible == visible)
            return;

        isVisible = visible;
        Apply();
    }

    private void CacheVisuals()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        canvases = GetComponentsInChildren<Canvas>(true);
        cached = true;
    }

    private void Apply()
    {
        if (renderers != null)
        {
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = isVisible;
            }
        }

        if (canvases != null)
        {
            for (var i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null)
                    canvases[i].enabled = isVisible;
            }
        }
    }

    private void OnDisable()
    {
        if (!isVisible)
        {
            isVisible = true;
            Apply();
        }
    }
}
