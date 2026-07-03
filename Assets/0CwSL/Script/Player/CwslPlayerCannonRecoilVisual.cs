using System.Collections;
using UnityEngine;

public class CwslPlayerCannonRecoilVisual : MonoBehaviour
{
    private Transform cannonPivot;
    private Vector3 baseLocalPosition;
    private Coroutine routine;

    private void Awake()
    {
        cannonPivot = transform.Find("CannonPivot");
        if (cannonPivot != null)
            baseLocalPosition = cannonPivot.localPosition;
    }

    public void PlayFire()
    {
        if (cannonPivot == null)
            return;

        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(RecoilRoutine());
    }

    private IEnumerator RecoilRoutine()
    {
        var timer = 0f;
        const float duration = 0.14f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            var t = timer / duration;
            var kick = Mathf.Sin(t * Mathf.PI);
            cannonPivot.localPosition = baseLocalPosition - Vector3.forward * (kick * 0.14f);
            yield return null;
        }

        cannonPivot.localPosition = baseLocalPosition;
        routine = null;
    }
}
