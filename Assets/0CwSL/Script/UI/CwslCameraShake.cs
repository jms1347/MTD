using System.Collections;
using UnityEngine;

/// <summary>로컬 카메라 쉐이크.</summary>
public class CwslCameraShake : MonoBehaviour
{
    private Coroutine routine;
    private Vector3 baseLocalPosition;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
    }

    public static void Play(float duration, float magnitude)
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        var shaker = camera.GetComponent<CwslCameraShake>();
        if (shaker == null)
            shaker = camera.gameObject.AddComponent<CwslCameraShake>();

        shaker.StartShake(duration, magnitude);
    }

    private void StartShake(float duration, float magnitude)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var falloff = 1f - Mathf.Clamp01(elapsed / duration);
            var offset = Random.insideUnitSphere * magnitude * falloff;
            offset.z = 0f;
            transform.localPosition = baseLocalPosition + offset;
            yield return null;
        }

        transform.localPosition = baseLocalPosition;
        routine = null;
    }
}
