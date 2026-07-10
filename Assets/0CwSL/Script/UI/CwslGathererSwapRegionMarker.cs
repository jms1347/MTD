using UnityEngine;

/// <summary>링거 E — 교환 대상·플레이어 주변 영역 동시 표시.</summary>
public static class CwslGathererSwapRegionMarker
{
    private static GameObject markerRoot;
    private static GameObject targetRing;
    private static GameObject playerRing;

    public static void Show(Vector3 targetCenter, Vector3 playerCenter, float radius)
    {
        Ensure();
        markerRoot.SetActive(true);

        var diameter = Mathf.Max(0.8f, radius * 2f);
        PositionRing(targetRing, targetCenter, diameter);
        PositionRing(playerRing, playerCenter, diameter);
    }

    public static void Hide()
    {
        if (markerRoot != null)
            markerRoot.SetActive(false);
    }

    private static void Ensure()
    {
        if (markerRoot != null)
            return;

        markerRoot = new GameObject("CwslGathererSwapRegionMarker");
        Object.DontDestroyOnLoad(markerRoot);

        targetRing = CwslGroundRingVisual.Create(
            Vector3.zero,
            1f,
            new Color(0.45f, 0.55f, 1f, 0.42f),
            0.12f);
        targetRing.name = "TargetRegion";
        targetRing.transform.SetParent(markerRoot.transform, false);

        playerRing = CwslGroundRingVisual.Create(
            Vector3.zero,
            1f,
            new Color(0.35f, 0.95f, 0.65f, 0.42f),
            0.12f);
        playerRing.name = "PlayerRegion";
        playerRing.transform.SetParent(markerRoot.transform, false);

        markerRoot.SetActive(false);
    }

    private static void PositionRing(GameObject ring, Vector3 center, float diameter)
    {
        if (ring == null)
            return;

        ring.transform.position = center + Vector3.up * 0.12f;
        ring.transform.localScale = new Vector3(diameter, 0.02f, diameter);
    }
}
