using System.Collections;
using UnityEngine;

public class CwslPlayerRammerBrakeVisual : MonoBehaviour
{
    private Vector3 baseScale;
    private Coroutine routine;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    public void PlayBrake()
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(BrakeRoutine());
    }

    private IEnumerator BrakeRoutine()
    {
        const float duration = 0.18f;
        var timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            var t = timer / duration;
            var squash = 1f + Mathf.Sin(t * Mathf.PI) * 0.08f;
            transform.localScale = new Vector3(baseScale.x * (2f - squash), baseScale.y * squash, baseScale.z * (2f - squash));
            yield return null;
        }

        transform.localScale = baseScale;
        routine = null;
    }
}
