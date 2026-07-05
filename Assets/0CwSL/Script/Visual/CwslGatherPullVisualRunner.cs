using System;
using System.Collections;
using UnityEngine;

/// <summary>끌모 SpinZone 수축 + 중심으로 모이는 연출.</summary>
public class CwslGatherPullVisualRunner : MonoBehaviour
{
    public bool IsPulling { get; private set; }

    private Coroutine pullRoutine;

    public void BeginPull(
        Transform spinZone,
        Transform zoneRoot,
        float startScale,
        float endScale,
        float duration,
        Action onComplete)
    {
        StopPull();
        if (spinZone == null)
        {
            onComplete?.Invoke();
            return;
        }

        pullRoutine = StartCoroutine(PullRoutine(spinZone, zoneRoot, startScale, endScale, duration, onComplete));
    }

    public void StopPull()
    {
        if (pullRoutine != null)
        {
            StopCoroutine(pullRoutine);
            pullRoutine = null;
        }

        IsPulling = false;
    }

    private IEnumerator PullRoutine(
        Transform spinZone,
        Transform zoneRoot,
        float startScale,
        float endScale,
        float duration,
        Action onComplete)
    {
        IsPulling = true;
        spinZone.localScale = Vector3.one * startScale;

        var startRootPos = zoneRoot != null ? zoneRoot.position : spinZone.position;
        var endRootPos = startRootPos + Vector3.up * 0.35f;
        var elapsed = 0f;

        while (elapsed < duration && spinZone != null)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / duration;
            var ease = t * t * t;
            spinZone.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, ease);

            if (zoneRoot != null)
                zoneRoot.position = Vector3.Lerp(startRootPos, endRootPos, ease);

            yield return null;
        }

        IsPulling = false;
        pullRoutine = null;
        onComplete?.Invoke();
    }
}
