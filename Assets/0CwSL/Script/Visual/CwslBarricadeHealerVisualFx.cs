using System.Collections;
using UnityEngine;

/// <summary>바리케이드 E 수리 모션.</summary>
public class CwslBarricadeRepairVisual : MonoBehaviour
{
    private Coroutine routine;
    private Vector3 baseLocalPos;

    private void Awake()
    {
        baseLocalPos = transform.localPosition;
    }

    public void Play()
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(RepairRoutine());
    }

    private IEnumerator RepairRoutine()
    {
        var duration = CwslGameConstants.BarricadeRepairDuration;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var bob = Mathf.Sin(elapsed * 14f) * 0.08f;
            transform.localPosition = baseLocalPos + Vector3.up * bob;
            yield return null;
        }

        transform.localPosition = baseLocalPos;
        routine = null;
    }
}

/// <summary>힐러 요정 — 살짝 floating.</summary>
public class CwslHealerFloatVisual : MonoBehaviour
{
    private Vector3 baseLocalPos;

    private void Awake()
    {
        baseLocalPos = transform.localPosition;
    }

    private void Update()
    {
        var bob = Mathf.Sin(Time.time * 2.4f) * 0.12f;
        transform.localPosition = baseLocalPos + Vector3.up * (0.35f + bob);
    }
}
