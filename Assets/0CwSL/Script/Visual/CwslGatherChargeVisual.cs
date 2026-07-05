using UnityEngine;

public static class CwslGatherChargeVisual
{
    private static GameObject chargeRoot;
    private static GameObject circleInstance;
    private static float lastRadius;
    private static bool lastAtMax;

    public static void Sync(Vector3 center, float radius, bool atMax)
    {
        EnsureRoot(center);

        if (circleInstance == null)
        {
            circleInstance = CwslVfxSpawner.AttachGatherChargeCircle(chargeRoot.transform);
            if (circleInstance == null)
                return;
        }

        var diameterScale = radius * 2f / 4f;
        circleInstance.transform.localScale = Vector3.one * Mathf.Max(0.35f, diameterScale);
        lastRadius = radius;
        lastAtMax = atMax;
    }

    public static void Hide()
    {
        if (chargeRoot != null)
            Object.Destroy(chargeRoot);
        chargeRoot = null;
        circleInstance = null;
    }

    private static void EnsureRoot(Vector3 center)
    {
        if (chargeRoot == null)
        {
            chargeRoot = new GameObject("CwslGatherChargeVisual");
            chargeRoot.transform.position = center + Vector3.up * 0.05f;
            return;
        }

        chargeRoot.transform.position = center + Vector3.up * 0.05f;
    }
}
