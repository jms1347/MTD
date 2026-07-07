using System.Collections;
using UnityEngine;

/// <summary>마법사 W 라이트닝 구슬 — 전방 이동 + 지면 반경 루프 연출.</summary>
public class CwslRedMageLightningOrbTravelVisual : MonoBehaviour
{
    private const float GroundRingAlpha = 0.48f;
    private const float GroundRadiusRefreshInterval = 0.55f;

    private Transform radiusRoot;
    private GameObject orb;
    private GameObject groundRadius;
    private GameObject edgeRing;
    private float nextGroundRadiusRefresh;

    public void Play(Vector3 start, Vector3 direction, float duration, float scale, float strikeRadius)
    {
        StartCoroutine(Run(start, direction, duration, scale, strikeRadius));
    }

    private IEnumerator Run(
        Vector3 start,
        Vector3 direction,
        float duration,
        float scale,
        float strikeRadius)
    {
        var prefab = CwslGameSession.Instance?.Assets?.redMageLightningOrbVfx;
        if (prefab == null)
        {
            Destroy(gameObject);
            yield break;
        }

        var flatDir = direction;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.0001f)
            flatDir = Vector3.forward;
        else
            flatDir.Normalize();

        var travelDistance = CwslGameConstants.RedMageLightningOrbTravelDistance;
        var groundY = CwslTankShieldVfxUtil.VisualGroundY;

        radiusRoot = new GameObject("RedMageLightningOrbRadius").transform;
        radiusRoot.position = new Vector3(start.x, groundY, start.z);
        groundRadius = CwslVfxSpawner.AttachRedMageLightningOrbGroundRadius(radiusRoot, strikeRadius);
        edgeRing = CwslGroundRingVisual.CreateEdgeRing(
            radiusRoot.position,
            Mathf.Max(0.5f, strikeRadius * 2f),
            new Color(0.35f, 0.62f, 1f, GroundRingAlpha),
            0.14f);

        orb = CwslVfxSpawner.Spawn(
            prefab,
            start,
            Quaternion.LookRotation(flatDir, Vector3.up),
            duration + 0.35f,
            scale);
        if (orb == null)
        {
            Cleanup();
            Destroy(gameObject);
            yield break;
        }

        CwslVfxSpawner.PrepareReusedEffect(orb);
        RefreshGroundRadiusFx();
        nextGroundRadiusRefresh = Time.time + GroundRadiusRefreshInterval;

        var elapsed = 0f;
        while (elapsed < duration && orb != null)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var travelPoint = start + flatDir * (travelDistance * t);
            orb.transform.position = travelPoint;

            var groundPoint = new Vector3(travelPoint.x, groundY, travelPoint.z);
            if (radiusRoot != null)
                radiusRoot.position = groundPoint;
            if (edgeRing != null)
                edgeRing.transform.position = groundPoint;

            if (Time.time >= nextGroundRadiusRefresh)
            {
                RefreshGroundRadiusFx();
                nextGroundRadiusRefresh = Time.time + GroundRadiusRefreshInterval;
            }

            yield return null;
        }

        Cleanup();
        Destroy(gameObject);
    }

    private void RefreshGroundRadiusFx()
    {
        if (groundRadius != null)
            CwslVfxSpawner.PrepareLoopingGroundEffect(groundRadius);
    }

    private void Cleanup()
    {
        if (orb != null)
        {
            CwslVfxPool.Release(orb);
            orb = null;
        }

        if (groundRadius != null)
        {
            CwslVfxPool.Release(groundRadius);
            groundRadius = null;
        }

        if (edgeRing != null)
        {
            Destroy(edgeRing);
            edgeRing = null;
        }

        if (radiusRoot != null)
        {
            Destroy(radiusRoot.gameObject);
            radiusRoot = null;
        }
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
