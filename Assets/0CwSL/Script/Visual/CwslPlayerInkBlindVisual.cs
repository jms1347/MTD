using UnityEngine;

/// <summary>먹물 블라인드 — 머리 주변 잉크 안개 + 스플래시.</summary>
public class CwslPlayerInkBlindVisual : MonoBehaviour
{
    private Transform auraAnchor;
    private GameObject auraInstance;
    private float endTime;

    public void Play(Vector3 impactPosition, float durationSeconds)
    {
        if (durationSeconds <= 0f)
            return;

        endTime = Time.time + durationSeconds;
        CwslVfxSpawner.SpawnShadowProjectileHit(impactPosition + Vector3.up * 1.1f, Vector3.up);
        EnsureAuraAnchor();
        EnsureAura();
    }

    private void Update()
    {
        if (auraInstance == null || Time.time < endTime)
            return;

        ClearAura();
    }

    private void EnsureAuraAnchor()
    {
        if (auraAnchor != null)
            return;

        var visual = transform.Find("Visual");
        var head = visual != null ? visual.Find("Helm") : null;
        if (head == null && visual != null)
            head = visual;

        auraAnchor = new GameObject("InkBlindAuraAnchor").transform;
        auraAnchor.SetParent(head != null ? head : transform, false);
        auraAnchor.localPosition = new Vector3(0f, 0.35f, 0f);
    }

    private void EnsureAura()
    {
        if (auraInstance != null)
            return;

        EnsureAuraAnchor();
        if (auraAnchor == null)
            return;

        auraInstance = CwslVfxSpawner.AttachInkBlindAura(auraAnchor);
    }

    private void ClearAura()
    {
        if (auraInstance == null)
            return;

        Destroy(auraInstance);
        auraInstance = null;
    }

    private void OnDisable()
    {
        ClearAura();
    }
}
