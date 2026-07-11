using System.Collections;
using UnityEngine;

/// <summary>언월도 베기·회전 연출 (5분 버티기 수준의 가시적 스윙).</summary>
public class StllGlaiveSwingVisual : MonoBehaviour
{
    private StllMountAssembly mountAssembly;
    private Coroutine activeRoutine;

    private void Awake()
    {
        mountAssembly = GetComponent<StllMountAssembly>();
    }

    public void PlayBasicSwing(Vector3 worldAimDirection)
    {
        if (mountAssembly == null || mountAssembly.BladePivot == null)
            return;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(BasicSwingRoutine(worldAimDirection));
    }

    public void PlayChargeSpin(float duration, int swings)
    {
        if (mountAssembly == null || mountAssembly.BladePivot == null)
            return;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(ChargeSpinRoutine(duration, swings));
    }

    private IEnumerator BasicSwingRoutine(Vector3 worldAimDirection)
    {
        var pivot = mountAssembly.BladePivot;
        var startLocal = pivot.localRotation;
        var duration = 0.26f;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / duration;
            var sweep = Mathf.Lerp(-62f, 62f, EaseOutQuad(t));
            var windUp = Mathf.Sin(t * Mathf.PI) * 22f;
            var thrust = Mathf.Sin(t * Mathf.PI) * 0.08f;
            pivot.localRotation = startLocal * Quaternion.Euler(windUp, sweep, -28f);
            pivot.localPosition = new Vector3(0f, 0f, 1.15f + thrust);
            yield return null;
        }

        pivot.localPosition = new Vector3(0f, 0f, 1.15f);
        pivot.localRotation = startLocal;
        activeRoutine = null;
    }

    private IEnumerator ChargeSpinRoutine(float duration, int swings)
    {
        var pivot = mountAssembly.BladePivot;
        var startLocal = pivot.localRotation;
        var elapsed = 0f;
        var totalDegrees = 360f * swings;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / duration;
            var angle = totalDegrees * t;
            var bob = Mathf.Sin(t * Mathf.PI * swings * 2f) * 16f;
            var extend = Mathf.Abs(Mathf.Sin(t * Mathf.PI * swings)) * 0.14f;
            pivot.localRotation = startLocal * Quaternion.Euler(bob, angle, -36f);
            pivot.localPosition = new Vector3(0f, 0f, 1.15f + extend);
            yield return null;
        }

        pivot.localPosition = new Vector3(0f, 0f, 1.15f);
        pivot.localRotation = startLocal;
        activeRoutine = null;
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
}
