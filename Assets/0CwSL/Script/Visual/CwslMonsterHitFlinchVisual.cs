using System.Collections;
using UnityEngine;

/// <summary>몬스터 피격 시 짧은 방향 흔들림 (스턴 없음, 클라이언트 전용).</summary>
public class CwslMonsterHitFlinchVisual : MonoBehaviour
{
    private const float FlinchOutSeconds = 0.07f;
    private const float FlinchBackSeconds = 0.14f;

    private Transform visualRoot;
    private Vector3 visualBaseLocalPosition;
    private Coroutine flinchRoutine;

    public static CwslMonsterHitFlinchVisual Ensure(GameObject root)
    {
        if (root == null)
            return null;

        var visual = root.GetComponent<CwslMonsterHitFlinchVisual>();
        if (visual == null)
            visual = root.AddComponent<CwslMonsterHitFlinchVisual>();

        return visual;
    }

    private void Awake()
    {
        CacheVisualRoot();
    }

    public void PlayFlinch(Vector3 worldDirection, float distance)
    {
        CacheVisualRoot();
        if (visualRoot == null)
            return;

        if (flinchRoutine != null)
            StopCoroutine(flinchRoutine);

        flinchRoutine = StartCoroutine(FlinchRoutine(worldDirection, distance));
    }

    private void CacheVisualRoot()
    {
        if (visualRoot != null)
            return;

        var slimeVisual = GetComponentInChildren<CwslSlimeMeleeVisual>();
        if (slimeVisual != null)
        {
            var model = transform.Find("Visual/Slime") ?? transform.Find("Visual/SlimeViking");
            if (model != null)
            {
                visualRoot = model;
                visualBaseLocalPosition = visualRoot.localPosition;
                return;
            }
        }

        visualRoot = transform.Find("Visual");
        if (visualRoot == null)
            visualRoot = transform;

        visualBaseLocalPosition = visualRoot.localPosition;
    }

    private IEnumerator FlinchRoutine(Vector3 worldDirection, float distance)
    {
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude < 0.0001f)
            worldDirection = transform.forward;

        worldDirection.Normalize();
        var safeDistance = Mathf.Clamp(distance, 0.08f, 1.4f);
        var localDirection = visualRoot.parent != null
            ? visualRoot.parent.InverseTransformDirection(-worldDirection)
            : -worldDirection;
        localDirection.y = 0f;
        if (localDirection.sqrMagnitude > 0.0001f)
            localDirection.Normalize();

        var peak = localDirection * (safeDistance * 0.32f);
        var timer = 0f;
        while (timer < FlinchOutSeconds)
        {
            timer += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, timer / FlinchOutSeconds);
            visualRoot.localPosition = visualBaseLocalPosition + peak * t;
            yield return null;
        }

        timer = 0f;
        while (timer < FlinchBackSeconds)
        {
            timer += Time.deltaTime;
            var t = 1f - Mathf.SmoothStep(0f, 1f, timer / FlinchBackSeconds);
            visualRoot.localPosition = visualBaseLocalPosition + peak * t;
            yield return null;
        }

        visualRoot.localPosition = visualBaseLocalPosition;
        flinchRoutine = null;
    }

    private void OnDisable()
    {
        if (visualRoot != null)
            visualRoot.localPosition = visualBaseLocalPosition;
    }
}
