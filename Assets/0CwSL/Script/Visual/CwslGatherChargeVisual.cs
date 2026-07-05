using UnityEngine;

public static class CwslGatherChargeVisual
{
    private const float SpinZoneBaseDiameter = 6f;
    private const float MinChargeScale = 0.35f;
    private const float PullEndScale = 0.18f;

    private static GameObject chargeRoot;
    private static GameObject spinZoneInstance;
    private static CwslGatherPullVisualRunner pullRunner;
    private static Vector3 lockedCenter;

    public static void BeginLocalCharge(Vector3 center)
    {
        if (pullRunner != null && pullRunner.IsPulling)
            return;

        Hide();
        lockedCenter = center;
        EnsureRoot(center);
        EnsureSpinZone();
        spinZoneInstance.transform.localScale = Vector3.one * RadiusToScale(CwslGameConstants.GatherMinRadius);
    }

    public static void Sync(Vector3 center, float radius, bool atMax)
    {
        if (pullRunner != null && pullRunner.IsPulling)
            return;

        if (chargeRoot == null)
            lockedCenter = center;

        EnsureRoot(lockedCenter);
        EnsureSpinZone();
        spinZoneInstance.transform.localScale = Vector3.one * RadiusToScale(radius);
    }

    public static void PlayCenterSpend(Vector3 center)
    {
        CwslGoldFeedback.PlaySpend(center + Vector3.up * 0.15f, CwslGameConstants.GatherStartGoldCost);
    }

    public static void PlayPull(Vector3 center, float radius)
    {
        EnsureRoot(lockedCenter.sqrMagnitude > 0.01f ? lockedCenter : center);
        EnsureSpinZone();
        if (spinZoneInstance == null)
        {
            CwslSimpleVfx.SpawnBurst(center, new Color(0.35f, 0.55f, 0.95f), radius * 0.35f, 0.4f);
            return;
        }

        EnsurePullRunner();

        var startScale = spinZoneInstance.transform.localScale.x;
        pullRunner.BeginPull(
            spinZoneInstance.transform,
            chargeRoot.transform,
            startScale,
            PullEndScale,
            CwslGameConstants.GatherPullSeconds,
            Hide);
    }

    public static void Hide()
    {
        if (pullRunner != null)
            pullRunner.StopPull();

        if (chargeRoot != null)
            Object.Destroy(chargeRoot);

        chargeRoot = null;
        spinZoneInstance = null;
        pullRunner = null;
        lockedCenter = Vector3.zero;
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

    private static void EnsureSpinZone()
    {
        if (spinZoneInstance != null)
            return;

        spinZoneInstance = CwslVfxSpawner.AttachGatherChargeCircle(chargeRoot.transform);
    }

    private static void EnsurePullRunner()
    {
        if (pullRunner != null)
            return;

        pullRunner = chargeRoot.GetComponent<CwslGatherPullVisualRunner>();
        if (pullRunner == null)
            pullRunner = chargeRoot.AddComponent<CwslGatherPullVisualRunner>();
    }

    private static float RadiusToScale(float radius) =>
        Mathf.Max(MinChargeScale, (radius * 2f) / SpinZoneBaseDiameter);
}
