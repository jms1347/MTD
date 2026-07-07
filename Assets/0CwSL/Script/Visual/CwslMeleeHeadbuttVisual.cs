using System.Collections;
using UnityEngine;

/// <summary>제자리 머리 박치기 — 몸통 루트는 이동하지 않고 모델만 전후 이동.</summary>
public class CwslMeleeHeadbuttVisual : MonoBehaviour
{
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Coroutine routine;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalScale = transform.localScale;
    }

    public void PlayWindup()
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(WindupRoutine());
    }

    public void PlayHit()
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(HitRoutine());
    }

    private IEnumerator WindupRoutine()
    {
        var timer = 0f;
        const float duration = 0.12f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            var t = timer / duration;
            var eased = t * t;
            transform.localPosition = baseLocalPosition - Vector3.forward * Mathf.Lerp(0f, 0.22f, eased);
            transform.localScale = Vector3.Lerp(baseLocalScale, baseLocalScale * 0.96f, eased);
            yield return null;
        }

        routine = null;
    }

    private IEnumerator HitRoutine()
    {
        var timer = 0f;
        const float duration = 0.22f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            var t = timer / duration;
            float forward;
            if (t < 0.35f)
            {
                var rush = t / 0.35f;
                forward = Mathf.Lerp(0f, 0.72f, rush * rush);
            }
            else
            {
                var recover = (t - 0.35f) / 0.65f;
                forward = Mathf.Lerp(0.72f, 0f, recover * recover);
            }

            transform.localPosition = baseLocalPosition + Vector3.forward * forward;
            var squash = Mathf.Sin(t * Mathf.PI);
            transform.localScale = baseLocalScale * Mathf.Lerp(1f, 1.12f, squash);
            yield return null;
        }

        transform.localPosition = baseLocalPosition;
        transform.localScale = baseLocalScale;
        routine = null;
    }
}
