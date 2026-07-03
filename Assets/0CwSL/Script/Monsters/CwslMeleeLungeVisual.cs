using System.Collections;
using UnityEngine;

public class CwslMeleeLungeVisual : MonoBehaviour
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
        const float duration = 0.13f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            var t = timer / duration;
            var eased = t * t;
            transform.localPosition = baseLocalPosition - Vector3.forward * Mathf.Lerp(0f, 0.28f, eased);
            transform.localScale = Vector3.Lerp(baseLocalScale, baseLocalScale * 0.94f, eased);
            yield return null;
        }

        routine = null;
    }

    private IEnumerator HitRoutine()
    {
        var timer = 0f;
        const float duration = 0.16f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            var t = timer / duration;
            var attackT = Mathf.Sin(t * Mathf.PI);
            transform.localPosition = baseLocalPosition + Vector3.forward * Mathf.Lerp(0.45f, 0f, t);
            transform.localScale = baseLocalScale * Mathf.Lerp(1f, 1.14f, attackT);
            yield return null;
        }

        transform.localPosition = baseLocalPosition;
        transform.localScale = baseLocalScale;
        routine = null;
    }
}
