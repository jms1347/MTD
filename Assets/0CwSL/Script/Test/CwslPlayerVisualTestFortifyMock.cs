using UnityEngine;

/// <summary>비주얼 테스트 씬 — Q 방패 강화 크기 모의.</summary>
public class CwslPlayerVisualTestFortifyMock : MonoBehaviour
{
    private Transform shieldRoot;
    private bool active;
    private float displayScale = 1f;
    private float displayVelocity;

    public bool IsActive => active;

    public void SetFortifyActive(bool value)
    {
        active = value;
    }

    private void Update()
    {
        shieldRoot ??= transform.Find("Visual/Shield");
        if (shieldRoot == null)
            return;

        var skillVisual = transform.Find("Visual")?.GetComponent<CwslTankShieldSkillVisual>();
        if (skillVisual != null && skillVisual.IsAnimating)
            return;

        var target = active ? CwslGameConstants.FortifyShieldScale : 1f;
        var smoothTime = active
            ? CwslGameConstants.FortifyShieldGrowSmoothTime
            : CwslGameConstants.FortifyShieldShrinkSmoothTime;
        displayScale = Mathf.SmoothDamp(
            displayScale,
            target,
            ref displayVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.deltaTime);
        shieldRoot.localScale = Vector3.one * displayScale;
    }
}
