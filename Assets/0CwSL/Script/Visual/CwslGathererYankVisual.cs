using System.Collections;
using UnityEngine;

/// <summary>링거 W — 블랙홀 장판·슬로우 VFX (클라이언트).</summary>
public static class CwslGathererYankVisual
{
    private static GameObject zoneRoot;

    public static void Begin(Vector3 center, float radius, float pullDuration, float convergeSeconds)
    {
        Hide();
        zoneRoot = CwslVfxSpawner.SpawnGathererYankZone(center, radius, 0f);
        if (zoneRoot == null)
            return;

        var host = zoneRoot.GetComponent<CwslGathererYankZoneRunner>();
        if (host == null)
            host = zoneRoot.AddComponent<CwslGathererYankZoneRunner>();

        host.Begin(center, radius, pullDuration, convergeSeconds, Hide);
    }

    public static void PlayExplosion(Vector3 center, float radius)
    {
        CwslVfxSpawner.SpawnGathererRopeConvergeBurst(center);
        CwslSimpleVfx.SpawnBurst(center, new Color(1f, 0.55f, 0.25f), radius * 0.45f, 0.55f);
    }

    public static void Hide()
    {
        if (zoneRoot != null)
        {
            var host = zoneRoot.GetComponent<CwslGathererYankZoneRunner>();
            if (host != null)
                host.StopRunner();

            Object.Destroy(zoneRoot);
        }

        zoneRoot = null;
        CwslGatherSlowVisual.Clear();
    }
}

public class CwslGathererYankZoneRunner : MonoBehaviour
{
    private Vector3 center;
    private float radius;
    private float pullDuration;
    private float convergeSeconds;
    private System.Action onComplete;
    private Coroutine routine;
    private CwslGatherPullVisualRunner pullRunner;

    public void Begin(
        Vector3 worldCenter,
        float worldRadius,
        float pullSeconds,
        float converge,
        System.Action complete)
    {
        StopRunner();
        center = worldCenter;
        radius = worldRadius;
        pullDuration = pullSeconds;
        convergeSeconds = converge;
        onComplete = complete;
        routine = StartCoroutine(RunRoutine());
    }

    public void StopRunner()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (pullRunner != null)
            pullRunner.StopPull();
    }

    private IEnumerator RunRoutine()
    {
        CwslGatherAudioFeedback.StartChargeLoop(center);
        var elapsed = 0f;
        while (elapsed < pullDuration)
        {
            elapsed += Time.deltaTime;
            CwslGatherSlowVisual.Sync(center, radius);
            CwslGatherAudioFeedback.UpdateChargeLoopPosition(center);
            yield return null;
        }

        CwslGatherAudioFeedback.PlayChargeEnd(center);
        CwslGatherSlowVisual.Clear();

        var scale = Mathf.Max(0.8f, radius / 2.6f);
        Transform shrinkTarget = null;
        if (transform.childCount > 0)
            shrinkTarget = transform.GetChild(0);

        if (shrinkTarget != null)
        {
            if (pullRunner == null)
                pullRunner = gameObject.AddComponent<CwslGatherPullVisualRunner>();

            var done = false;
            pullRunner.BeginPull(
                shrinkTarget,
                transform,
                scale,
                scale * 0.12f,
                convergeSeconds,
                () => done = true);

            while (!done)
                yield return null;
        }
        else
        {
            yield return new WaitForSeconds(convergeSeconds);
        }

        onComplete?.Invoke();
        routine = null;
    }
}
