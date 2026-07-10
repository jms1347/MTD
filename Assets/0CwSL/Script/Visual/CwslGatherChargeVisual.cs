using UnityEngine;

public static class CwslGatherChargeVisual
{
    private const float PullEndScaleFactor = 0.15f;

    private static GameObject zoneRoot;
    private static GameObject vortexInstance;
    private static GameObject ringInstance;
    private static CwslGatherPullVisualRunner pullRunner;

    public static void BeginBlackHoleZone(Vector3 center, float radius)
    {
        Hide();
        zoneRoot = CwslVfxSpawner.SpawnGatherBlackHoleZone(center, radius, 0f);
    }

    public static void SyncBlackHoleZone(Vector3 center, float radius)
    {
        if (zoneRoot == null)
        {
            BeginBlackHoleZone(center, radius);
            return;
        }

        zoneRoot.transform.position = center + Vector3.up * 0.05f;
        var scale = Mathf.Max(0.8f, radius / 2.6f);
        if (vortexInstance != null)
            vortexInstance.transform.localScale = Vector3.one * scale;
        if (ringInstance != null)
            ringInstance.transform.localScale = Vector3.one * scale;
    }

    public static void PlayReleasePull(Vector3 center, float radius)
    {
        if (zoneRoot == null)
            BeginBlackHoleZone(center, radius);

        if (zoneRoot == null)
        {
            CwslSimpleVfx.SpawnBurst(center, new Color(0.35f, 0.55f, 0.95f), radius * 0.35f, 0.4f);
            return;
        }

        zoneRoot.transform.position = center + Vector3.up * 0.05f;
        var startScale = Mathf.Max(0.8f, radius / 2.6f);
        var shrinkTransform = vortexInstance != null ? vortexInstance.transform : zoneRoot.transform;
        EnsurePullRunner();
        pullRunner.BeginPull(
            shrinkTransform,
            zoneRoot.transform,
            startScale,
            startScale * PullEndScaleFactor,
            CwslGameConstants.GatherPullSeconds,
            Hide);

        if (ringInstance != null)
            ringInstance.transform.localScale = Vector3.one * (startScale * PullEndScaleFactor);
    }

    public static void Hide()
    {
        if (pullRunner != null)
            pullRunner.StopPull();

        if (zoneRoot != null)
            Object.Destroy(zoneRoot);

        zoneRoot = null;
        vortexInstance = null;
        ringInstance = null;
        pullRunner = null;
    }

    public static void PlayPull(Vector3 center, float radius)
    {
        PlayReleasePull(center, radius);
    }

    internal static void BindInstances(GameObject root, GameObject vortex, GameObject ring)
    {
        zoneRoot = root;
        vortexInstance = vortex;
        ringInstance = ring;
        pullRunner = root != null ? root.GetComponent<CwslGatherPullVisualRunner>() : null;
    }

    private static void EnsurePullRunner()
    {
        if (zoneRoot == null)
            return;

        if (pullRunner == null)
            pullRunner = zoneRoot.GetComponent<CwslGatherPullVisualRunner>();
        if (pullRunner == null)
            pullRunner = zoneRoot.AddComponent<CwslGatherPullVisualRunner>();
    }
}
