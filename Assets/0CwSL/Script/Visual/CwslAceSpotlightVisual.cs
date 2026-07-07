using System.Collections;
using UnityEngine;

/// <summary>수석 코치 — 골드 1위 플레이어 머리 위 빨간 스포트라이트.</summary>
public class CwslAceSpotlightVisual : MonoBehaviour
{
    private static readonly Color SpotlightColor = new(1f, 0.12f, 0.1f);

    private Light spotLight;
    private GameObject groundRing;
    private Coroutine routine;

    public static void Play(Transform playerRoot, float durationSeconds)
    {
        if (playerRoot == null)
            return;

        var visual = playerRoot.GetComponent<CwslAceSpotlightVisual>();
        if (visual == null)
            visual = playerRoot.gameObject.AddComponent<CwslAceSpotlightVisual>();

        visual.Begin(durationSeconds);
    }

    private void Begin(float durationSeconds)
    {
        if (routine != null)
            StopCoroutine(routine);

        EnsureSpotlight();
        EnsureGroundRing();
        routine = StartCoroutine(RunRoutine(durationSeconds));
    }

    private void EnsureSpotlight()
    {
        if (spotLight != null)
            return;

        var lightGo = new GameObject("AceSpotlight");
        lightGo.transform.SetParent(transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 4.8f, 0f);
        lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        spotLight = lightGo.AddComponent<Light>();
        spotLight.type = LightType.Spot;
        spotLight.color = SpotlightColor;
        spotLight.intensity = 3.6f;
        spotLight.range = 14f;
        spotLight.spotAngle = 28f;
        spotLight.shadows = LightShadows.None;
    }

    private void EnsureGroundRing()
    {
        if (groundRing != null)
            return;

        groundRing = CwslGroundRingVisual.Create(
            transform.position,
            2.4f,
            new Color(1f, 0.15f, 0.12f, 0.72f),
            0.06f);
        groundRing.transform.SetParent(transform, false);
        groundRing.transform.localPosition = new Vector3(0f, 0.05f, 0f);
    }

    private IEnumerator RunRoutine(float durationSeconds)
    {
        var timer = 0f;
        while (timer < durationSeconds)
        {
            timer += Time.deltaTime;
            if (groundRing != null)
            {
                var pulse = 1f + Mathf.Sin(timer * 8f) * 0.08f;
                groundRing.transform.localScale = Vector3.one * pulse;
            }

            yield return null;
        }

        Cleanup();
        routine = null;
    }

    private void Cleanup()
    {
        if (spotLight != null)
        {
            Destroy(spotLight.gameObject);
            spotLight = null;
        }

        if (groundRing != null)
        {
            Destroy(groundRing);
            groundRing = null;
        }
    }

    private void OnDisable()
    {
        Cleanup();
    }
}
